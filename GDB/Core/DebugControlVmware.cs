using GDB.Core.Disassembly;
using GDB.Core.Protocol;
using GDB.Core.Register;
using GDB.Core.Symbol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace GDB.Core
{
    public class DebugControlVmware : IDebugControl
    {
        public bool IsHalt { get; set; }

        public ulong KernBase { get; set; }

        public GdbClient Client { get; }

        public event EventHandler OnHaltHandler;

        public DebugControlVmware(GdbClient client)
        {
            Client = client;
        }

        public void ProcessStopReply(GdbPacket packet)
        {
            IsHalt = true;

            // Offload the kernel base discovery to a background thread
            // to avoid deadlocking the GDB receive loop.
            if (KernBase == 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        KernBase = 1; // Mark as "in progress"
                        var context = await GetContext();
                        if (context != null)
                        {
                            if (context.RIP < 0xFFFF800000000000)
                            {
                                KernBase = 0; // Reset for next attempt
                                await Continue();
                                await Break();
                            }
                            else
                            {
                                var data = await ReadVirtual((long)context.IDT, 16);
                                if (data != null && data.Length >= 16)
                                {
                                    ulong kiDivide = 0;
                                    kiDivide += ((ulong)BitConverter.ToUInt16(data, 10) << 48);
                                    kiDivide += ((ulong)BitConverter.ToUInt16(data, 8) << 32);
                                    kiDivide += ((ulong)BitConverter.ToUInt16(data, 6) << 16);
                                    kiDivide += ((ulong)BitConverter.ToUInt16(data, 0) << 0);
                                    ulong searchBased = kiDivide & 0xFFFFFFFFFFFF0000;
                                    for (ulong i = searchBased; i > 0xFFFFF80000000000; i -= 0x1000)
                                    {
                                        using (var stream = new RemoteStream(this, (long)i))
                                        {
                                            var error = PE.PeHeader.TryReadFrom(stream, out PE.PeHeader header);
                                            if (error != PE.ReaderError.NoError)
                                                continue;

                                            var rsdsi = header.GetRSDSI(stream);
                                            if (rsdsi == null)
                                                continue;

                                            if (rsdsi.PDB == "ntoskrnl.pdb" || rsdsi.PDB == "ntkrnlpa.pdb" || rsdsi.PDB == "ntkrnlmp.pdb" || rsdsi.PDB == "ntkrpamp.pdb")
                                            {
                                                KernBase = i;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // If anything goes wrong, reset the discovery process.
                        KernBase = 0;
                    }
                });
            }

            // Notify the UI immediately that a halt occurred.
            OnHaltHandler?.Invoke(this, EventArgs.Empty);
        }

        public async Task<string> Execute(string command)
        {
            if (IsHalt == false)
                return "Machine is running. \r\n Can not execute string command.";

            var response = await Client.SendCommandAndReceiveResponseAsync(command);
            return response.Data;
        }

        public async Task<CommonRegister_x64> GetContext()
        {
            var msg = await Client.SendCommandAndReceiveResponseAsync("g");
            var data = msg.Data.ToBin();
            if (data.Length == 0)
            {
                return null;
            }
            else
            {
                var context = new CommonRegister_x64();
                var core_register = data.ToStruct<Register_Vmware_x64.Context>();
                context.RAX = core_register.rax;
                context.RBX = core_register.rbx;
                context.RCX = core_register.rcx;
                context.RDX = core_register.rdx;
                context.RSI = core_register.rsi;
                context.RDI = core_register.rdi;
                context.RBP = core_register.rbp;
                context.RSP = core_register.rsp;
                context.R8 = core_register.r8;
                context.R9 = core_register.r9;
                context.R10 = core_register.r10;
                context.R11 = core_register.r11;
                context.R12 = core_register.r12;
                context.R13 = core_register.r13;
                context.R14 = core_register.r14;
                context.R15 = core_register.r15;
                context.RIP = core_register.rip;

                context.RFL = core_register.rflags;

                context.CS = (ushort)core_register.cs;
                context.SS = (ushort)core_register.ss;
                context.DS = (ushort)core_register.ds;
                context.ES = (ushort)core_register.es;
                context.FS = (ushort)core_register.fs;
                context.GS = (ushort)core_register.gs;

                //DR0-DR7 Not Support!

                context.CR0 = (await GetSpecialRegister(0)).Item1;
                context.CR2 = (await GetSpecialRegister(1)).Item1;
                context.CR3 = (await GetSpecialRegister(2)).Item1;
                context.CR4 = (await GetSpecialRegister(3)).Item1;
                context.CR8 = (await GetSpecialRegister(4)).Item1;

                var idtr = await GetSpecialRegister(5);
                var gdtr = await GetSpecialRegister(6);
                context.IDT = idtr.Item1;
                context.IDTL = idtr.Item2;
                context.GDT = gdtr.Item1;
                context.GDTL = gdtr.Item2;
                return context;
            }
        }

        public async Task<(ulong, ushort)> GetSpecialRegister(int index)
        {
            var result1 = default(ulong);
            var result2 = default(ushort);

            // 准备命令、期望的响应前缀，并区分寄存器类型
            string command;
            string expectedPrefix;
            bool isMultiValue;

            switch (index)
            {
                case 0: command = "r cr0"; expectedPrefix = "cr0"; isMultiValue = false; break;
                case 1: command = "r cr2"; expectedPrefix = "cr2"; isMultiValue = false; break;
                case 2: command = "r cr3"; expectedPrefix = "cr3"; isMultiValue = false; break;
                case 3: command = "r cr4"; expectedPrefix = "cr4"; isMultiValue = false; break;
                case 4: command = "r cr8"; expectedPrefix = "cr8"; isMultiValue = false; break;
                case 5: command = "r idtr"; expectedPrefix = "idtr"; isMultiValue = true; break;
                case 6: command = "r gdtr"; expectedPrefix = "gdtr"; isMultiValue = true; break;
                default:
                    System.Diagnostics.Debug.WriteLine($"[ERROR] GetSpecialRegister called with invalid index: {index}");
                    return (0, 0);
            }

            try
            {
                // 使用项目中现有的扩展方法将命令转换为十六进制格式
                string hexCommand = command.ToBytes().ToHex();
                
                // 发送命令并获取响应
                var responsePacket = await Client.SendCommandAndReceiveResponseAsync($"qRcmd,{hexCommand}");
                if (responsePacket == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] GetSpecialRegister for '{command}' received null response.");
                    return (result1, result2);
                }
                var response = responsePacket.Data;
                
                // 处理GDB协议中"O"开头的响应（十六进制编码的ASCII文本）
                string decodedResponse;
                if (response.StartsWith("O"))
                {
                    var decodedText = new StringBuilder();
                    int currentIndex = 0;
                    while (currentIndex < response.Length)
                    {
                        if (response[currentIndex] != 'O') { currentIndex++; continue; }
                        int nextO = response.IndexOf('O', currentIndex + 1);
                        if (nextO == -1) nextO = response.Length;
                        string hexPart = response.Substring(currentIndex + 1, nextO - currentIndex - 1);
                        if (!string.IsNullOrEmpty(hexPart))
                        {
                            decodedText.Append(hexPart.ToBin().ToTextA());
                        }
                        currentIndex = nextO;
                    }
                    decodedResponse = decodedText.ToString();
                }
                else
                {
                    decodedResponse = response;
                }
                
                // --- 核心修复：验证并进行健壮性解析 ---
                string trimmedResponse = decodedResponse.Trim();
                
                // 1. 验证响应是否与请求匹配
                if (!trimmedResponse.StartsWith(expectedPrefix))
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] GetSpecialRegister mismatch for command '{command}'. Expected prefix '{expectedPrefix}' but got response: '{trimmedResponse}'");
                    return (result1, result2); // 关键：不匹配则直接返回，不再错误解析
                }

                // 2. 根据寄存器类型使用不同的健壮解析逻辑
                if (isMultiValue)
                {
                    // 解析 "idtr base=0x... limit=0x..." 格式
                    var parts = trimmedResponse.Split(new[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
                    // 期望格式: [ "idtr", "base", "0x...", "limit", "0x..." ]
                    if (parts.Length >= 5 && parts[1] == "base" && parts[3] == "limit")
                    {
                        foreach (var item in parts[2].ToBin()) { result1 = (result1 << 8) + item; }
                        foreach (var item in parts[4].ToBin()) { result2 = (ushort)((result2 << 8) + item); }
                    }
                }
                else
                {
                    // 解析 "cr0=0x..." 格式
                    var parts = trimmedResponse.Split('=');
                    if (parts.Length >= 2)
                    {
                        foreach (var item in parts[1].ToBin()) { result1 = (result1 << 8) + item; }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetSpecialRegister for '{command}' threw an exception: {ex.Message}");
            }
            
            return (result1, result2);
        }

        public async Task<bool> Break()
        {
            await Client.SendPacketAsync(new GdbPacket("\x03"));
            return true;
        }

        public async Task<bool> Continue()
        {
            IsHalt = false;
            await Client.SendCommandAndReceiveResponseAsync("c");
            return true;
        }

        public async Task<bool> Step()
        {
            IsHalt = false;
            await Client.SendCommandAndReceiveResponseAsync("s");
            return true;
        }

        public Task<bool> StepOver()
        {
            return Task.FromResult(true);
        }

        public async Task<bool> BreakPointAdd(int type, long addr, int size)
        {
            if (IsHalt == false)
                return false;

            var command = $"Z{type},{addr:x},{size:x}";
            var response = await Client.SendCommandAndReceiveResponseAsync(command);
            return response.Data == "OK";
        }

        public async Task<bool> BreakPointDel(int type, long addr, int size)
        {
            if (IsHalt == false)
                return false;

            var command = $"z{type},{addr:x},{size:x}";
            var response = await Client.SendCommandAndReceiveResponseAsync(command);
            return response.Data == "OK";
        }

        public async Task<byte[]> ReadVirtual(long ptr, int size)
        {
            var command = $"m{ptr:x},{size:x}";
            var response = await Client.SendCommandAndReceiveResponseAsync(command);
            return response.Data.ToBin();
        }

        public async Task<List<CommonInstruction>> Disassembly(long addr, int size)
        {
            var code = await ReadVirtual(addr, size);
            return CommonDisassembly_x64.GetResult(code, addr);
        }

        public List<CommonInstruction> DisassemblyBytes(byte[] bytes, ulong startAddress)
        {
            if (bytes == null || bytes.Length == 0)
                return new List<CommonInstruction>();
                
            return CommonDisassembly_x64.GetResult(bytes, (long)startAddress);
        }
    }
}
