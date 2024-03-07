using GDB.Core.Disassembly;
using GDB.Core.Register;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core
{
    /// <summary>
    /// QEMU调试控制器
    /// </summary>
    public class DebugControlQemu : IDebugControl
    {
        public bool IsHalt { get; set; }

        public event EventHandler OnHaltHandler;

        public bool LinkStart(string connectionstring)
        {
            return true;
        }

        public string Execute(string command, bool monitor = false)
        {
            return "";
        }

        public bool Break()
        {
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
            return true;
        }

        public bool GetContext(out CommonRegister_x64 context)
        {
            throw new NotImplementedException();
        }

        public byte[] ReadVirtual(long addr, int size)
        {
            return new byte[0];
        }

        public bool Step()
        {
            return true;
        }

        public bool StepOver()
        {
            return true;
        }

        public List<CommonInstruction> Disassembly(long addr, int size)
        {
            throw new NotImplementedException();
        }
    }
}
