using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.Disassembly
{
    public enum DisassemblyType
    {
        /// <summary>
        /// 
        /// </summary>
        Iced,
        /// <summary>
        /// 
        /// </summary>
        Capstone
    }


    public class CommonDisassembly_x64
    {
        public static List<CommonInstruction> GetResult(byte[] binaryCode, long address, DisassemblyType type = DisassemblyType.Iced)
        {
            var instructions = new List<CommonInstruction>();
            if (type == DisassemblyType.Iced)
            {
                var codereader = new Iced.Intel.ByteArrayCodeReader(binaryCode);
                var decoder = Iced.Intel.Decoder.Create(64, codereader, (ulong)address);
                ulong endip = decoder.IP + (uint)binaryCode.Length;
                while (decoder.IP < endip)
                {
                    var item = decoder.Decode();
                    var instruction = new CommonInstruction();
                    instruction.IP = item.IP;
                    instruction.Length = item.Length;
                    instruction.Describe = item.ToString();
                    instruction.Code = binaryCode.SkipTake((int)(item.IP - (ulong)address), item.Length);
                    instruction.CodeStr = string.Format("{0}", BitConverter.ToString(instruction.Code).Replace("-", " "));
                    instructions.Add(instruction);
                }

            }
            return instructions;
        }
    }
}
