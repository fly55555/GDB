using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.Register
{
    public class CommonRegister_x64
    {
        public ulong RAX { get; set; }
        public ulong RBX { get; set; }
        public ulong RCX { get; set; }
        public ulong RDX { get; set; }
        public ulong RSI { get; set; }
        public ulong RDI { get; set; }
        public ulong RBP { get; set; }
        public ulong RSP { get; set; }
        public ulong R8 { get; set; }
        public ulong R9 { get; set; }
        public ulong R10 { get; set; }
        public ulong R11 { get; set; }
        public ulong R12 { get; set; }
        public ulong R13 { get; set; }
        public ulong R14 { get; set; }
        public ulong R15 { get; set; }
        public ulong RIP { get; set; }

        /// <summary>
        /// Rflags
        /// </summary>
        public ulong RFL { get; set; }

        public ushort CS { get; set; }
        public ushort SS { get; set; }
        public ushort DS { get; set; }
        public ushort ES { get; set; }
        public ushort FS { get; set; }
        public ushort GS { get; set; }

        public uint DR0 { get; set; }
        public uint DR1 { get; set; }
        public uint DR2 { get; set; }
        public uint DR3 { get; set; }
        public uint DR6 { get; set; }
        public uint DR7 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ulong CR0 { get; set; }
        /// <summary>
        /// PFL
        /// </summary>
        public ulong CR2 { get; set; }
        /// <summary>
        /// DTB
        /// </summary>
        public ulong CR3 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ulong CR4 { get; set; }
        /// <summary>
        /// IRQL
        /// </summary>
        public ulong CR8 { get; set; }

        /// <summary>
        /// IDT Base
        /// </summary>
        public ulong IDT { get; set; }
        /// <summary>
        /// IDT Limit
        /// </summary>
        public ushort IDTL { get; set; }
        /// <summary>
        /// GDT Base
        /// </summary>
        public ulong GDT { get; set; }
        /// <summary>
        /// GDT Limit
        /// </summary>
        public ushort GDTL { get; set; }
    }
}
