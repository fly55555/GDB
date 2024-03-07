using GDB.Core.Disassembly;
using GDB.Core.Register;
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
    public class DebugControlVmware : CommandInstance, IDebugControl
    {
        public bool IsHalt { get; set; }

        public ulong KernBase { get; set; }


        public event EventHandler OnHaltHandler;

        public DebugControlVmware()
        {
            OnHalt += (object sender, EventArgs e) =>
            {
                IsHalt = true;

                if (KernBase == 0)
                {
                    KernBase = 1;
                    if (GetContext(out CommonRegister_x64 context))
                    {
                        if (context.RIP < 0xFFFF800000000000)
                        {
                            KernBase = 0;
                            Continue();
                            Break();
                        }
                        else
                        {
                            var data = ReadVirtual((long)context.IDT, 16);
                            ulong kiDivide = 0;
                            kiDivide += ((ulong)BitConverter.ToUInt16(data, 10) << 48);
                            kiDivide += ((ulong)BitConverter.ToUInt16(data, 8) << 32);
                            kiDivide += ((ulong)BitConverter.ToUInt16(data, 6) << 16);
                            kiDivide += ((ulong)BitConverter.ToUInt16(data, 0) << 0);
                            ulong searchBased = kiDivide & 0xFFFFFFFFFFFF0000;
                            for (ulong i = searchBased; i > 0xFFFFF80000000000; i -= 0x1000)
                            {
                                using (var stream = new Symbole.RemoteStream(this, (long)i))
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
            ExecuteCommand("?", true);
            return true;
        }

        public string Execute(string command, bool monitor = false)
        {
            if (IsHalt == false)
                return "Machine is running. \r\n Can not execute string command.";

            var result = string.Empty;
            if (monitor)
            {
                result = MonitorCommand(command).Replace("\n", "\r\n");
            }
            else
            {
                if (command == "c")
                    IsHalt = false;

                if (command == "?" || command == "\x03" || command.StartsWith("s:"))
                    result = ExecuteCommand(command, true).Original;
                else
                    result = ExecuteCommand(command).Original;
            }
            return result;
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

                context.CR0 = GetSpecialRegister(0).Item1;
                context.CR2 = GetSpecialRegister(1).Item1;
                context.CR3 = GetSpecialRegister(2).Item1;
                context.CR4 = GetSpecialRegister(3).Item1;
                context.CR8 = GetSpecialRegister(4).Item1;

                var idtr = GetSpecialRegister(5);
                var gdtr = GetSpecialRegister(6);
                context.IDT = idtr.Item1;
                context.IDTL = idtr.Item2;
                context.GDT = gdtr.Item1;
                context.GDTL = gdtr.Item2;
                return true;
            }
        }

        public (ulong, ushort) GetSpecialRegister(int index)
        {
            var result1 = default(ulong);
            var result2 = default(ushort);
            var response = string.Empty;
            if (index == 0)
                response = MonitorCommand("r cr0");
            if (index == 1)
                response = MonitorCommand("r cr2");
            if (index == 2)
                response = MonitorCommand("r cr3");
            if (index == 3)
                response = MonitorCommand("r cr4");
            if (index == 4)
                response = MonitorCommand("r cr8");
            if (index == 5)
                response = MonitorCommand("r idtr");
            if (index == 6)
                response = MonitorCommand("r gdtr");
            response = response.TrimEnd('\n');

            if (!string.IsNullOrEmpty(response) && response.Length > 4)
            {
                var spiltstr = response.Split('=');
                if (spiltstr.Length == 2)
                {
                    foreach (var item in spiltstr[1].ToBin())
                    {
                        result1 = result1 << 8;
                        result1 += item;
                    }
                }
                if (spiltstr.Length == 3)
                {
                    var spiltstr2 = spiltstr[1].Split(' ');
                    foreach (var item in spiltstr2[0].ToBin())
                    {
                        result1 = result1 << 8;
                        result1 += item;
                    }
                    foreach (var item in spiltstr[2].ToBin())
                    {
                        result2 = (ushort)(result2 << 8);
                        result2 += item;
                    }
                }
            }
            return (result1, result2);
        }

        public bool Step()
        {
            IsHalt = false;
            ExecuteCommand("s:1", true);
            return true;
        }

        public bool Break()
        {
            ExecuteCommand("\x03", true);
            return true;
        }

        public bool Continue()
        {
            IsHalt = false;
            ExecuteCommand("c");
            return true;
        }

        public bool StepOver()
        {
            return true;
        }

        public bool BreakPointAdd(int type, long addr, int size)
        {
            if (IsHalt == false)
                return false;

            //Execute
            if (type == 0)
            {
                var result = ExecuteCommand(string.Format("Z0,{0:X16},{1:X1}", addr, size));
                if (result.Naked.StartsWith("OK"))
                    return true;
            }

            //Write
            if (type == 1)
            {
                var result = ExecuteCommand(string.Format("Z2,{0:X16},{1:X1}", addr, size));
                if (result.Naked.StartsWith("OK"))
                    return true;
            }

            //Access
            if (type == 2)
            {
                var result = ExecuteCommand(string.Format("Z4,{0:X16},{1:X1}", addr, size));
                if (result.Naked.StartsWith("OK"))
                    return true;
            }

            return false;
        }

        public bool BreakPointDel(int type, long addr, int size)
        {
            if (IsHalt == false)
                return false;

            //Execute
            if (type == 0)
            {
                var result = ExecuteCommand(string.Format("z0,{0:X16},{1:X1}", addr, size));
                if (result.Naked.StartsWith("OK"))
                    return true;
            }

            //Write
            if (type == 1)
            {
                var result = ExecuteCommand(string.Format("z2,{0:X16},{1:X1}", addr, size));
                if (result.Naked.StartsWith("OK"))
                    return true;
            }

            //Access
            if (type == 2)
            {
                var result = ExecuteCommand(string.Format("z4,{0:X16},{1:X1}", addr, size));
                if (result.Naked.StartsWith("OK"))
                    return true;
            }

            return false;
        }

        public byte[] ReadVirtual(long ptr, int size)
        {
            if (size <= 0)
                throw new NotImplementedException();

            if (size <= 500)
            {
                var response = ExecuteCommand(string.Format("m{0:X16},{1:X3}", ptr, size));
                return response.Naked.ToBin();
            }
            else
            {
                var current = ptr;
                var residue = size;
                var result = new byte[size];
                do
                {
                    if (residue > 500)
                    {
                        var response = ExecuteCommand(string.Format("m{0:X16},{1:X3}", current, 500));
                        var data = response.Naked.ToBin();
                        Array.Copy(data, 0, result, size - residue, 500);
                    }
                    else
                    {
                        var response = ExecuteCommand(string.Format("m{0:X16},{1:X3}", current, residue));
                        var data = response.Naked.ToBin();
                        Array.Copy(data, 0, result, size - residue, residue);
                        break;
                    }
                    residue -= 500;
                } while (true);
                return result;
            }
        }

        public List<CommonInstruction> Disassembly(long addr, int size)
        {
            var binarycode = ControlCenter.Instance.ReadVirtual(addr, size);
            return CommonDisassembly_x64.GetResult(binarycode, addr);
        }
    }
}
