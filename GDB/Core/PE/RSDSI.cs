using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.PE
{
    public class RSDSI
    {
        /// <summary>
        /// 
        /// </summary>
        public uint Signature { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Guid GUID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public uint Age { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string PDB { get; set; }


        public RSDSI(byte[] data)
        {
            Signature = BitConverter.ToUInt32(data, 0);
            GUID = new Guid(data.Skip(4).Take(16).ToArray());
            Age = BitConverter.ToUInt32(data, 20);
            PDB = Encoding.ASCII.GetString(data, 24, data.Length - 24).TrimEnd('\0');
        }
    }
}
