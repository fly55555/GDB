using GDB.Core.Disassembly;
using GDB.Core.Protocol;
using GDB.Core.Register;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GDB.Core
{
    /// <summary>
    /// QEMU调试控制器
    /// </summary>
    public class DebugControlQemu : IDebugControl
    {
        public bool IsHalt { get; set; }
        
        public ulong KernBase { get; set; }

        public event EventHandler OnHaltHandler;
        public GdbClient Client { get; }

        public DebugControlQemu(GdbClient client)
        {
            Client = client;
        }

        public void ProcessStopReply(GdbPacket packet)
        {
            IsHalt = true;
            OnHaltHandler?.Invoke(this, EventArgs.Empty);
        }

        public async Task<string> Execute(string command)
        {
            if (IsHalt == false)
                return "Machine is running. \r\n Can not execute string command.";

            var response = await Client.SendCommandAndReceiveResponseAsync(command);
            return response.Data;
        }

        public async Task<bool> Break()
        {
            await Client.SendPacketAsync(new GdbPacket("\x03"));
            return true;
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

        public async Task<bool> Continue()
        {
            IsHalt = false;
            await Client.SendCommandAndReceiveResponseAsync("vCont;c");
            return true;
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
                return context;
            }
        }

        public async Task<byte[]> ReadVirtual(long addr, int size)
        {
            var command = $"m{addr:x},{size:x}";
            var response = await Client.SendCommandAndReceiveResponseAsync(command);
            if (response.Data.StartsWith("E"))
            {
                return new byte[0];
            }
            return response.Data.ToBin();
        }

        public async Task<bool> Step()
        {
            IsHalt = false;
            await Client.SendCommandAndReceiveResponseAsync("vCont;s");
            return true;
        }

        public async Task<bool> StepOver()
        {
            IsHalt = false;
            await Client.SendCommandAndReceiveResponseAsync("vCont;s");
            return true;
        }

        public async Task<List<CommonInstruction>> Disassembly(long addr, int size)
        {
            var code = await ReadVirtual(addr, size);
            if (code == null || code.Length == 0)
            {
                return new List<CommonInstruction>();
            }
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
