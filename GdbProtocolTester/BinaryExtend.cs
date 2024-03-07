using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GdbProtocolTester
{
    public static class BinaryExtend
    {

        private static class Cache<Itype>
        {
            public static Func<byte[], int, Itype> ConvertToObject;

            public static Func<Itype, bool, byte[]> ConvertToBinary;

        }

        static BinaryExtend()
        {
            MakeConvertToObject();
            MakeConvertToBinary();
        }

        private static void MakeConvertToObject()
        {
            Cache<float>.ConvertToObject = (x, i) =>
            BitConverter.ToSingle(x, i);
            //BitConverter.ToSingle(new byte[] { x[3], x[2], x[1], x[0] }, i);

            Cache<double>.ConvertToObject = (x, i) =>
            BitConverter.ToDouble(x, i);
            //BitConverter.ToDouble(new byte[] { x[7], x[6], x[5], x[4], x[3], x[2], x[1], x[0] }, i);



            Cache<short>.ConvertToObject = (x, i) =>
            (short)((x[0 + i] << 8) + x[1 + i]);

            Cache<int>.ConvertToObject = (x, i) =>
            (x[0 + i] << 24) + (x[1 + i] << 16) + (x[2 + i] << 8) + x[3 + i];

            Cache<long>.ConvertToObject = (x, i) =>
            (x[0 + i] << 56) + (x[1 + i] << 48) + (x[2 + i] << 40) + (x[3 + i] << 32) + (x[4 + i] << 24) + (x[5 + i] << 16) + (x[6 + i] << 8) + x[7 + i];



            Cache<ushort>.ConvertToObject = (x, i) =>
            (ushort)((x[0 + i] << 8) + x[1 + i]);

            Cache<uint>.ConvertToObject = (x, i) =>
            (uint)((x[0 + i] << 24) + (x[1 + i] << 16) + (x[2 + i] << 8) + x[3 + i]);

            Cache<ulong>.ConvertToObject = (x, i) =>
            (ulong)((x[0 + i] << 56) + (x[1 + i] << 48) + (x[2 + i] << 40) + (x[3 + i] << 32) + (x[4 + i] << 24) + (x[5 + i] << 16) + (x[6 + i] << 8) + x[7 + i]);
        }


        private static void MakeConvertToBinary()
        {
            Cache<bool>.ConvertToBinary = (x, i) => i ? BitConverter.GetBytes(x).ToArray() : BitConverter.GetBytes(x).Reverse().ToArray();
            Cache<char>.ConvertToBinary = (x, i) => i ? BitConverter.GetBytes(x).ToArray() : BitConverter.GetBytes(x).Reverse().ToArray();
            Cache<short>.ConvertToBinary = (x, i) => i ? BitConverter.GetBytes(x).ToArray() : BitConverter.GetBytes(x).Reverse().ToArray();
            Cache<int>.ConvertToBinary = (x, i) => i ? BitConverter.GetBytes(x).ToArray() : BitConverter.GetBytes(x).Reverse().ToArray();
            Cache<long>.ConvertToBinary = (x, i) => i ? BitConverter.GetBytes(x).ToArray() : BitConverter.GetBytes(x).Reverse().ToArray();
            Cache<ushort>.ConvertToBinary = (x, i) => i ? BitConverter.GetBytes(x).ToArray() : BitConverter.GetBytes(x).Reverse().ToArray();
            Cache<uint>.ConvertToBinary = (x, i) => i ? BitConverter.GetBytes(x).ToArray() : BitConverter.GetBytes(x).Reverse().ToArray();
            Cache<ulong>.ConvertToBinary = (x, i) => i ? BitConverter.GetBytes(x).ToArray() : BitConverter.GetBytes(x).Reverse().ToArray();
            Cache<float>.ConvertToBinary = (x, i) => i ? BitConverter.GetBytes(x).ToArray() : BitConverter.GetBytes(x).Reverse().ToArray();
            Cache<double>.ConvertToBinary = (x, i) => i ? BitConverter.GetBytes(x).ToArray() : BitConverter.GetBytes(x).Reverse().ToArray();
        }


        public static Itype To<Itype>(this byte[] value, int skip = 0)
        {
            return Cache<Itype>.ConvertToObject(value, skip);
        }

        public static byte[] ToBinary<Itype>(this Itype value, bool rev = false)
        {
            return Cache<Itype>.ConvertToBinary(value, rev);
        }

        /// <summary>
        /// 字符串转byte[]
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        /// <summary>
        /// 加速 .skip().take()
        /// </summary>
        /// <param name="input"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public static byte[] SkipTake(this byte[] input, int skip, int take)
        {
            var result = new byte[take];
            if (take <= 0) return result;
            Array.Copy(input, skip, result, 0, take);
            return result;
        }

        /// <summary>
        /// 分割
        /// </summary>
        /// <param name="data"></param>
        /// <param name="spilt"></param>
        /// <returns></returns>
        public static List<byte[]> Spilt(this byte[] data, byte[] spilt)
        {
            var poss = new List<int>();
            var result = new List<byte[]>();

            for (int i = 0; i < data.Length; i++)
            {
                if (data.Cmp(i, spilt)) poss.Add(i);
            }

            for (int i = 0; i < poss.Count - 1; i++)
            {
                result.Add(data.SkipTake(poss[i], poss[i + 1] - poss[i]));
            }

            result.Add(data.SkipTake(poss.Last(), data.Length - poss.Last()));

            return result;
        }

        /// <summary>
        /// 分割(等分)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="wide"></param>
        /// <returns></returns>
        public static List<byte[]> Spilt(this byte[] data, int wide)
        {
            var result = new List<byte[]>();
            for (int i = 0; i < data.Length / wide; i++)
            {
                result.Add(data.SkipTake(wide * i, wide));
            }
            return result;
        }

        /// <summary>
        /// 对比
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="spilt"></param>
        /// <param name="inx"></param>
        /// <returns></returns>
        public static bool Cmp(this byte[] data, int index, byte[] spilt, int inx = 0)
        {
            for (int i = 0; i < spilt.Length; i++)
            {
                if (spilt[i] != data[index + i]) return false;
            }
            return true;
        }

        /// <summary>
        /// 反转类型 Unicode转换
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ToUnicode(this byte[] data)
        {
            var list = new List<byte>();
            for (int i = 0; i < data.Length; i += 2)
            {
                list.Add(data[i + 1]);
                list.Add(data[i]);
            }
            return Encoding.Unicode.GetString(list.ToArray());
        }

        /// <summary>
        /// byte[]转ASCII
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static string ToTextA(this byte[] data, int index = 0, int count = 0)
        {
            if (count == 0) count = data.Length - index;
            return Encoding.ASCII.GetString(data, index, count);
        }

        /// <summary>
        /// Hex字符串转 byte[]
        /// </summary>
        /// <param name="hexString"></param>
        /// <param name="stat"></param>
        /// <returns></returns>
        public static byte[] ToBin(this string hexString, bool stat = true)
        {
            if (stat) hexString = hexString.Replace("0x", "").Replace(" ", "").Replace("\r\n", "");
            if (hexString.Length % 2 != 0)
                hexString = "0" + hexString;
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
            {
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return returnBytes;
        }

        /// <summary>
        /// 二进制转十六进制 (BCD码)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="beg">是否保留头部半字节</param>
        /// <param name="end">是否保留尾部半字节</param>
        /// <returns></returns>
        public static string ToHex(this byte[] data, int skip = 0, int take = 0, int beg = 1, int end = 1)
        {
            var msg = new StringBuilder();
            if (skip == 0 && take == 0)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    msg.Append(data[i].ToString("X2"));
                }
            }
            else
            {
                if (skip > data.Length) return string.Empty;
                if (skip + take > data.Length || take == 0) take = data.Length - skip;

                for (int i = 0; i < take; i++)
                {
                    msg.Append(data[i + skip].ToString("X2"));
                }
            }

            var str = msg.ToString();
            if (beg == 0) str = str.Remove(0, 1);
            if (end == 0) str = str.Remove(str.Length - 1, 1);
            return str;
        }

        /// <summary>
        /// byte转十六进制字符串
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Hex(this byte data)
        {
            return data.ToString("X2");
        }

        /// <summary>
        /// byte转bit[]
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Bit(this byte data)
        {
            var row = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                row[i] = (byte)((data & 1 << i) >> i);
            }
            return row;
        }

        /// <summary>
        /// byte[]转DateTme
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="dec"></param>
        /// <returns></returns>
        public static DateTime ToTime(this byte[] data, int index = 0, bool dec = false)
        {
            var text =
                dec ?
                $"{data[index + 0]}{data[index + 1]}{data[index + 2]}{data[index + 3]}{data[index + 4]}{data[index + 5]}" :
                data.ToHex(index, 6);

            return DateTime.ParseExact(text, "yyMMddHHmmss", null);
        }


        /// <summary>
        /// 将byte[]转换为结构体类型
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Itype ToStruct<Itype>(this byte[] source)
        {
            int size = Marshal.SizeOf(typeof(Itype));
            IntPtr buffer = Marshal.AllocHGlobal(size);          //分配结构体内存空间
            Marshal.Copy(source, 0, buffer, size);                //将byte数组拷贝到分配好的内存空间
            var obj = Marshal.PtrToStructure(buffer, typeof(Itype));   //将内存空间转换为目标结构体
            Marshal.FreeHGlobal(buffer);                         //释放内存空间
            return (Itype)obj;
        }

        /// <summary>
        /// 将结构体转换为byte[]类型
        /// </summary>
        /// <param name="structObj"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this object structObj)
        {
            int size = Marshal.SizeOf(structObj);
            IntPtr buffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structObj, buffer, false);
            byte[] bytes = new byte[size];
            Marshal.Copy(buffer, bytes, 0, size);
            Marshal.FreeHGlobal(buffer);
            return bytes;
        }

        /// <summary>
        /// byte数组转int
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int ByteToInt(this byte[] data)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);
            int ilen = BitConverter.ToUInt16(data, 0);
            return ilen;
        }

    }


}
