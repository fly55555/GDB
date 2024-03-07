using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.PE
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_DEBUG_DIRECTORY
    {
		/// <summary>
		///     Size of the structure.
		/// </summary>
		public uint Characteristics;

		/// <summary>
		/// 
		/// </summary>
		public uint TimeDateStamp;

		/// <summary>
		///     Major Runtime Version of the CLR Runtime.
		/// </summary>
		public ushort MajorVersion;

		/// <summary>
		///     Minor Runtime Version of the CLR Runtime.
		/// </summary>
		public ushort MinorVersion;

		/// <summary>
		/// 
		/// </summary>
		public uint Type;

		/// <summary>
		/// 
		/// </summary>
		public uint SizeOfData;

		/// <summary>
		/// 
		/// </summary>
		public uint AddressOfRawData;

		/// <summary>
		/// 
		/// </summary>
		public uint PointerToRawData;

		internal static IMAGE_DEBUG_DIRECTORY Read(Stream reader)
		{
			int size = Marshal.SizeOf(typeof(IMAGE_DEBUG_DIRECTORY));
			var data = new byte[size];
			reader.Read(data, 0, size);
			return data.ToStruct<IMAGE_DEBUG_DIRECTORY>();
		}
	}
}
