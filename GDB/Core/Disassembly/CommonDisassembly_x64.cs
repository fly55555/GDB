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
            if (binaryCode == null || binaryCode.Length == 0)
            {
                return instructions;
            }
            
            if (type == DisassemblyType.Iced)
            {
                try
                {
                    var codereader = new Iced.Intel.ByteArrayCodeReader(binaryCode);
                    var decoder = Iced.Intel.Decoder.Create(64, codereader, (ulong)address);
                    ulong endip = decoder.IP + (uint)binaryCode.Length;
                    
                    while (decoder.IP < endip)
                    {
                        try
                        {
                            var item = decoder.Decode();
                            
                            // 检查指令是否有效且在内存范围内
                            if (item.Length > 0 && (item.IP - (ulong)address) + (ulong)item.Length <= (ulong)binaryCode.Length)
                            {
                                var instruction = new CommonInstruction();
                                instruction.IP = item.IP;
                                instruction.Length = item.Length;
                                instruction.Describe = item.ToString();
                                
                                // 安全提取指令字节
                                int offset = (int)(item.IP - (ulong)address);
                                if (offset >= 0 && offset + item.Length <= binaryCode.Length)
                                {
                                    instruction.Code = binaryCode.SkipTake(offset, item.Length);
                                    instruction.CodeStr = string.Format("{0}", BitConverter.ToString(instruction.Code).Replace("-", " "));
                                }
                                else
                                {
                                    // 如果超出边界，使用空字节数组
                                    instruction.Code = new byte[item.Length];
                                    instruction.CodeStr = "<out of bounds>";
                                    instruction.Describe = "(bad - memory boundary)";
                                }
                                
                                instructions.Add(instruction);
                            }
                            else
                            {
                                // 无效指令，添加占位符
                                var invalidInstruction = new CommonInstruction
                                {
                                    IP = decoder.IP,
                                    Length = 1,
                                    Describe = "(bad - invalid instruction)",
                                    Code = new byte[1],
                                    CodeStr = "<invalid>"
                                };
                                instructions.Add(invalidInstruction);
                                
                                // 移动到下一个字节位置
                                decoder.IP++;
                            }
                        }
                        catch (Exception ex)
                        {
                            // 解码单个指令出错，记录错误并继续
                            System.Diagnostics.Debug.WriteLine($"Error decoding instruction at 0x{decoder.IP:X}: {ex.Message}");
                            
                            // 添加错误指令占位符
                            var errorInstruction = new CommonInstruction
                            {
                                IP = decoder.IP,
                                Length = 1,
                                Describe = "(bad - decode error)",
                                Code = new byte[1],
                                CodeStr = "<error>"
                            };
                            instructions.Add(errorInstruction);
                            
                            // 移动到下一个字节位置
                            decoder.IP++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 整体解码过程出错
                    System.Diagnostics.Debug.WriteLine($"Error in disassembly process: {ex.Message}");
                    
                    // 添加一个错误指示指令
                    instructions.Add(new CommonInstruction
                    {
                        IP = (ulong)address,
                        Length = 1,
                        Describe = "(disassembly process error)",
                        Code = binaryCode.Length > 0 ? new byte[] { binaryCode[0] } : new byte[1],
                        CodeStr = "<error>"
                    });
                }
            }
            
            return instructions;
        }
    }
}
