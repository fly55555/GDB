using GDB.Core.Disassembly;
using GDB.Core.Protocol;
using GDB.Core.Register;
using GDB.Core.Symbol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GDB.Core
{
    public enum DebuggerState
    {
        Disconnected,
        Running,
        Halted,
        Busy
    }

    /// <summary>
    /// 控制中心
    /// </summary>
    public class ControlCenter
    {
        public GdbClient Client { get; }
        private readonly Dictionary<DebugMachineType, IDebugControl> _debugControlInstances;
        private IDebugControl _activeDebugger;
        private SymbolManager _symbolManager;
        
        // 符号加载状态事件
        public event Action<string> OnSymbolLoadStatusChanged;
        
        public DebuggerState State { get; private set; } = DebuggerState.Disconnected;
        public event Action<DebuggerState> OnStateChanged;

        private void SetState(DebuggerState newState)
        {
            if (State == newState) return;
            State = newState;
            System.Diagnostics.Debug.WriteLine($"[State Change] => {newState}");
            OnStateChanged?.Invoke(State);
        }

        public ControlCenter()
        {
            Client = new GdbClient();
            Client.OnStopReceived += Client_OnStopReceived;
            Client.OnResumed += Client_OnResumed;
            _debugControlInstances = new Dictionary<DebugMachineType, IDebugControl>
            {
                { DebugMachineType.Qemu, new DebugControlQemu(Client) },
                { DebugMachineType.Vmware, new DebugControlVmware(Client) }
            };
        }

        private void Client_OnResumed()
        {
            SetState(DebuggerState.Running);
        }

        private void Client_OnStopReceived(GdbPacket packet)
        {
            SetState(DebuggerState.Halted);
            _activeDebugger?.ProcessStopReply(packet);
            
            // 如果内核基址已找到且符号管理器还未初始化，尝试加载符号
            if (_activeDebugger?.KernBase > 0 && _symbolManager == null)
            {
                // 异步加载符号，不阻塞UI
                Task.Run(async () =>
                {
                    _symbolManager = new SymbolManager(_activeDebugger);
                    await _symbolManager.LoadKernelSymbols();
                });
            }
        }

        public void SetActiveDebugger(DebugMachineType type)
        {
            if (_debugControlInstances.TryGetValue(type, out var debugger))
            {
                _activeDebugger = debugger;
                _symbolManager = null; // 重置符号管理器
            }
            else
            {
                throw new ArgumentException("Unsupported debugger type", nameof(type));
            }
        }

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            if (State != DebuggerState.Disconnected) return;
            SetState(DebuggerState.Busy);
            await Client.ConnectAsync(host, port, cancellationToken);
        }

        public void Disconnect()
        {
            Client.Dispose();
            _symbolManager = null;
            SetState(DebuggerState.Disconnected);
        }

        public async Task<string> Execute(string command)
        {
            if (State != DebuggerState.Halted) return "Debugger is not halted.";
            return await _activeDebugger.Execute(command);
        }

        public async Task<bool> Step()
        {
            if (State != DebuggerState.Halted) return false;
            
            // 设置状态为Busy
            SetState(DebuggerState.Busy);
            
            try
            {
                // 执行单步命令
                return await _activeDebugger.Step();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Step failed: {ex.Message}");
                // 如果发生异常，恢复状态为Halted
                SetState(DebuggerState.Halted);
                return false;
            }
        }

        public async Task<bool> Break()
        {
            if (State == DebuggerState.Disconnected || State == DebuggerState.Halted || State == DebuggerState.Busy) return false;
            SetState(DebuggerState.Busy);
            return await _activeDebugger.Break();
        }

        public async Task<bool> Continue()
        {
            if (State != DebuggerState.Halted) return false;
            
            // 设置状态为Busy
            SetState(DebuggerState.Busy);
            
            try
            {
                // 执行继续命令
                return await _activeDebugger.Continue();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Continue failed: {ex.Message}");
                // 如果发生异常，恢复状态为Halted
                SetState(DebuggerState.Halted);
                return false;
            }
        }

        public Task<bool> StepOver() => Step();

        public async Task<CommonRegister_x64> GetContext()
        {
            if (State != DebuggerState.Halted) return null;
            return await _activeDebugger.GetContext();
        }

        public async Task<bool> BreakPointAdd(int type, long addr, int size)
        {
            if (State != DebuggerState.Halted) return false;
            return await _activeDebugger.BreakPointAdd(type, addr, size);
        }

        public async Task<bool> BreakPointDel(int type, long addr, int size)
        {
            if (State != DebuggerState.Halted) return false;
            return await _activeDebugger.BreakPointDel(type, addr, size);
        }

        public async Task<byte[]> ReadVirtual(long addr, int size)
        {
            if (State != DebuggerState.Halted) return null;
            return await _activeDebugger.ReadVirtual(addr, size);
        }

        public async Task<List<CommonInstruction>> Disassembly(long addr, int size)
        {
            if (State != DebuggerState.Halted) return null;
            return await _activeDebugger.Disassembly(addr, size);
        }

        /// <summary>
        /// 将字节数据反汇编为指令列表
        /// </summary>
        /// <param name="bytes">要反汇编的字节数据</param>
        /// <param name="startAddress">字节数据对应的起始地址</param>
        /// <returns>反汇编后的指令列表</returns>
        public List<CommonInstruction> DisassemblyBytes(byte[] bytes, ulong startAddress)
        {
            if (bytes == null || bytes.Length == 0) return new List<CommonInstruction>();
            if (_activeDebugger == null) return new List<CommonInstruction>();

            // 使用当前活动的调试器进行反汇编
            var instructions = _activeDebugger.DisassemblyBytes(bytes, startAddress);
            
            // 如果符号管理器已初始化，添加符号信息
            if (_symbolManager != null)
            {
                foreach (var instruction in instructions)
                {
                    // 使用改进的符号查找策略
                    // 1. 首先尝试使用精确匹配（函数范围内）
                    if (_symbolManager.TryGetSymbolByAddress(instruction.IP, out string symbolName, out int offset, SymbolLookupStrategy.ExactMatch))
                    {
                        instruction.SymbolName = symbolName;
                        instruction.SymbolOffset = offset;
                    }
                    // 2. 如果精确匹配失败，尝试使用宽松匹配（相邻符号之间）
                    else if (_symbolManager.TryGetSymbolByAddress(instruction.IP, out symbolName, out offset, SymbolLookupStrategy.Relaxed))
                    {
                        // 对于宽松匹配，如果偏移量过大（超过4KB），可能不是同一个函数，添加警告标记
                        if (offset > 4096)
                        {
                            instruction.SymbolName = symbolName + "?";  // 添加问号表示不确定
                        }
                        else
                        {
                            instruction.SymbolName = symbolName;
                        }
                        instruction.SymbolOffset = offset;
                    }
                }
            }
            
            return instructions;
        }
        
        /// <summary>
        /// 加载符号
        /// </summary>
        public async Task<bool> LoadSymbols()
        {
            if (State != DebuggerState.Halted) return false;
            if (_activeDebugger?.KernBase == 0) return false;
            
            try
            {
                RaiseSymbolLoadStatus("正在初始化符号管理器...");
                
                // 初始化符号管理器并加载符号
                if (_symbolManager == null)
                {
                    _symbolManager = new SymbolManager(_activeDebugger);
                }
                
                RaiseSymbolLoadStatus("正在加载内核符号...");
                var result = await _symbolManager.LoadKernelSymbols();
                
                if (result)
                {
                    RaiseSymbolLoadStatus("符号加载成功！");
                }
                else
                {
                    RaiseSymbolLoadStatus("符号加载失败，将使用硬编码的符号信息。");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                RaiseSymbolLoadStatus($"符号加载出错：{ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 引发符号加载状态事件
        /// </summary>
        /// <param name="status">状态信息</param>
        private void RaiseSymbolLoadStatus(string status)
        {
            System.Diagnostics.Debug.WriteLine($"[符号] {status}");
            OnSymbolLoadStatusChanged?.Invoke(status);
        }
        
        /// <summary>
        /// 获取符号管理器
        /// </summary>
        public SymbolManager GetSymbolManager()
        {
            return _symbolManager;
        }
    }
}
