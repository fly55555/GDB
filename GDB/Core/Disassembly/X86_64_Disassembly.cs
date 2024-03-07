using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.Disassembly
{
    public class X86_64_Disassembly
    {
        //using Gee.External.Capstone;
        //using Gee.External.Capstone.X86;

        //public static CapstoneX86Disassembler disassembler { get; set; }

        //public static X86Instruction[] DisassemblyCodeCapstone(byte[] binaryCode, long address)
        //{
        //    if (disassembler == null)
        //    {
        //        var disassemblermode = X86DisassembleMode.Bit64;
        //        disassembler = CapstoneDisassembler.CreateX86Disassembler(disassemblermode);
        //        disassembler.EnableInstructionDetails = true;
        //        disassembler.DisassembleSyntax = DisassembleSyntax.Intel;
        //    }
        //    return disassembler.Disassemble(binaryCode, address);
        //}


        //public static bool NextInstructionIsCall(byte[] binaryCode, long address, out long nextAddress)
        //{
        //    var instructions = disassembler.Disassemble(binaryCode, address);
        //    if (instructions[0].Id == X86InstructionId.X86_INS_CALL)
        //    {
        //        nextAddress = instructions[1].Address;
        //        return true;
        //    }
        //    nextAddress = 0;
        //    return false;
        //}

        public static List<Instruction> DisassemblyCodeIced64(byte[] binaryCode, long address)
        {
            var codeReader = new ByteArrayCodeReader(binaryCode);
            var decoder = Iced.Intel.Decoder.Create(64, codeReader, (ulong)address);
            ulong endRip = decoder.IP + (uint)binaryCode.Length;
            var instructions = new List<Instruction>();
            while (decoder.IP < endRip)
                instructions.Add(decoder.Decode());

            return instructions;
        }


    }
}
