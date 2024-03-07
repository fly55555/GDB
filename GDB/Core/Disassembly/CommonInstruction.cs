using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.Disassembly
{
    public class CommonInstruction
    {
        /// <summary>
        /// rip eip
        /// </summary>
        public ulong IP { get; set; }

        /// <summary>
        /// Instruction length
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Disassembly
        /// </summary>
        public string Describe { get; set; }

        /// <summary>
        /// Original code
        /// </summary>
        public byte[] Code { get; set; }

        /// <summary>
        /// Original code string
        /// </summary>
        public string CodeStr { get; set; }
}
}
