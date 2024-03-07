using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.PE
{
	/// <summary>
	///     Represents the COFF header format.
	///     See https://msdn.microsoft.com/en-us/library/windows/desktop/ms680313(v=vs.85).aspx for more info.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	// ReSharper disable once InconsistentNaming
	public struct IMAGE_FILE_HEADER
	{
		/// <summary>
		/// 
		/// </summary>
		public uint Unused;

		/// <summary>
		/// The architecture type of the computer. An image file can only be run on the specified computer or a system that emulates the specified computer.
		/// </summary>
		public MACHINE Machine;

		/// <summary>
		/// The number of sections. This indicates the size of the section table, which immediately follows the headers. Note that the Windows loader limits the number of sections to 96.
		/// </summary>
		public ushort NumberOfSections;

		/// <summary>
		/// The low 32 bits of the time stamp of the image. This represents the date and time the image was created by the linker. The value is represented in the number of seconds elapsed since midnight (00:00:00), January 1, 1970, Universal Coordinated Time, according to the system clock.
		/// </summary>
		public uint TimeDateStamp;

		/// <summary>
		/// The offset of the symbol table, in bytes, or zero if no COFF symbol table exists.
		/// </summary>
		public uint PointerToSymbolTable;

		/// <summary>
		/// The number of symbols in the symbol table.
		/// </summary>
		public uint NumberOfSymbols;

		/// <summary>
		/// The size of the optional header, in bytes. This value should be 0 for object files.
		/// </summary>
		public ushort SizeOfOptionalHeader;

		/// <summary>
		/// The characteristics of the image.
		/// </summary>
		public CHARACTERISTICS Characteristics;

		internal static IMAGE_FILE_HEADER Read(Stream reader)
		{
			int size = Marshal.SizeOf(typeof(IMAGE_FILE_HEADER));
			var data = new byte[size];
			reader.Read(data, 0, size);
			return data.ToStruct<IMAGE_FILE_HEADER>();
		}
	}
}
