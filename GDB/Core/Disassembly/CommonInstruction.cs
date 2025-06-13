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
        
        /// <summary>
        /// 符号名称（如果有）
        /// </summary>
        public string SymbolName { get; set; }
        
        /// <summary>
        /// 偏移量（相对于符号开始）
        /// </summary>
        public int SymbolOffset { get; set; }
        
        /// <summary>
        /// 获取带有符号信息的描述
        /// </summary>
        public string DescribeWithSymbol 
        { 
            get 
            {
                if (string.IsNullOrEmpty(SymbolName))
                    return Describe;
                    
                if (SymbolOffset == 0)
                    return $"{Describe} ; {SymbolName}";
                else
                    return $"{Describe} ; {SymbolName}+0x{SymbolOffset:X}";
            } 
        }
        
        /// <summary>
        /// 获取符号形式的地址显示
        /// </summary>
        public string SymbolicAddress
        {
            get
            {
                if (string.IsNullOrEmpty(SymbolName))
                    return $"0x{IP:X}";
                    
                if (SymbolOffset == 0)
                    return SymbolName;
                else
                    return $"{SymbolName}+0x{SymbolOffset:X}";
            }
        }
    }
}
