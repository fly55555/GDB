using GDB.Core.Disassembly;
using GDB.Core.Protocol;
using GDB.Core.Register;
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
        }

        public void SetActiveDebugger(DebugMachineType type)
        {
            if (_debugControlInstances.TryGetValue(type, out var debugger))
            {
                _activeDebugger = debugger;
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
            SetState(DebuggerState.Busy);
            return await _activeDebugger.Step();
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
            SetState(DebuggerState.Busy);
            return await _activeDebugger.Continue();
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
        /// 直接对提供的字节数据进行反汇编
        /// </summary>
        /// <param name="bytes">要反汇编的字节数据</param>
        /// <param name="startAddress">字节数据对应的起始地址</param>
        /// <returns>反汇编后的指令列表</returns>
        public List<CommonInstruction> DisassemblyBytes(byte[] bytes, ulong startAddress)
        {
            if (bytes == null || bytes.Length == 0) return new List<CommonInstruction>();
            if (_activeDebugger == null) return new List<CommonInstruction>();

            // 使用当前活动的调试器进行反汇编
            return _activeDebugger.DisassemblyBytes(bytes, startAddress);
        }
    }
}
