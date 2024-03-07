using GDB.Core.Disassembly;
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
    public class DebugControlQemu : CommandInstance, IDebugControl
    {
        public bool IsHalt { get; set; }

        public event EventHandler OnHaltHandler;

        public DebugControlQemu()
        {
            OnHalt += (object sender, EventArgs e) =>
            {
                IsHalt = true;

                if (OnHaltHandler != null)
                    OnHaltHandler.Invoke(sender, e);
            };
        }

        public bool LinkStart(string connectionstring)
        {
            if (string.IsNullOrEmpty(connectionstring))
                return false;

            var spiltstr = connectionstring.Split(':');
            if (spiltstr.Length != 2)
                return false;

            Connect(spiltstr[0], int.Parse(spiltstr[1]));
            CommandInit();
            ExecuteCommand("?", true);
            return true;
        }

        public string Execute(string command, bool monitor = false)
        {
            return "";
        }

        public bool Break()
        {
            ExecuteCommand("\x03");
            Thread.Sleep(100);
            ExecuteCommand("?", true);
            return true;
        }

        public bool BreakPointAdd(int type, long addr, int size)
        {
            return true;
        }

        public bool BreakPointDel(int type, long addr, int size)
        {
            return true;
        }

        public bool Continue()
        {
            IsHalt = false;
            ExecuteCommand2("vCont;c");
            return true;
        }

        public bool GetContext(out CommonRegister_x64 context)
        {
            context = new CommonRegister_x64();
            var msg = ExecuteCommand("g");
            var data = msg.Naked.ToBin();
            if (data.Length == 0)
            {
                return false;
            }
            else
            {
                //var infos = MonitorCommand("info registers");

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
            }
            return true;
        }

        public byte[] ReadVirtual(long addr, int size)
        {
            if (size <= 0)
                throw new NotImplementedException();

            if (size <= PacketSize)
            {
                var response = ExecuteCommand(string.Format("m{0:X16},{1:X3}", addr, size));
                return response.Naked.ToBin();
            }
            else
            {
                var current = addr;
                var residue = size;
                var result = new byte[size];
                do
                {
                    if (residue > PacketSize)
                    {
                        var response = ExecuteCommand(string.Format("m{0:X16},{1:X3}", current, PacketSize));
                        var data = response.Naked.ToBin();
                        Array.Copy(data, 0, result, size - residue, PacketSize);
                    }
                    else
                    {
                        var response = ExecuteCommand(string.Format("m{0:X16},{1:X3}", current, residue));
                        var data = response.Naked.ToBin();
                        Array.Copy(data, 0, result, size - residue, residue);
                        break;
                    }
                    residue -= PacketSize;
                } while (true);
                return result;
            }
        }

        public bool Step()
        {
            IsHalt = false;
            ExecuteCommand("vCont;s", true);
            return true;
        }

        public bool StepOver()
        {
            IsHalt = false;
            ExecuteCommand("vCont;s", true);
            return true;
        }

        public List<CommonInstruction> Disassembly(long addr, int size)
        {
            throw new NotImplementedException();
        }
    }
}
