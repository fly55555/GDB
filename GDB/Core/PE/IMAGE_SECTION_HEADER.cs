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
	///     Represents the image section header format.
	///     See https://msdn.microsoft.com/en-us/library/windows/desktop/ms680341(v=vs.85).aspx for more.
	/// </summary>
	/// <remarks>
	///     From http://www.pinvoke.net/default.aspx/Structures/IMAGE_SECTION_HEADER.html
	///     via https://gist.github.com/caioproiete/b51f29f74f5f5b2c59c39e47a8afc3a3
	/// </remarks>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	// ReSharper disable once InconsistentNaming
	public struct IMAGE_SECTION_HEADER
	{
		/// <summary>
		///     First character of the name.
		/// </summary>
		public char Char0;

		/// <summary>
		///     Second character of the name.
		/// </summary>
		public char Char1;

		/// <summary>
		///     Third character of the name.
		/// </summary>
		public char Char2;

		/// <summary>
		///     4th character of the name.
		/// </summary>
		public char Char3;

		/// <summary>
		///     5th character of the name.
		/// </summary>
		public char Char4;

		/// <summary>
		///     6th character of the name.
		/// </summary>
		public char Char5;

		/// <summary>
		///     7th character of the name.
		/// </summary>
		public char Char6;

		/// <summary>
		///     8th character of the name.
		/// </summary>
		public char Char7;

		/// <summary>
		///     The total size of the section when loaded into memory, in bytes. If this value is greater than the SizeOfRawData
		///     member, the section is filled with zeroes. This field is valid only for executable images and should be set to 0
		///     for object files.
		/// </summary>
		public uint VirtualSize;

		/// <summary>
		///     The address of the first byte of the section when loaded into memory, relative to the image base. For object files,
		///     this is the address of the first byte before relocation is applied.
		/// </summary>
		public uint VirtualAddress;

		/// <summary>
		///     The size of the initialized data on disk, in bytes. This value must be a multiple of the FileAlignment member of
		///     the IMAGE_OPTIONAL_HEADER structure. If this value is less than the VirtualSize member, the remainder of the
		///     section is filled with zeroes. If the section contains only uninitialized data, the member is zero.
		/// </summary>
		public uint SizeOfRawData;

		/// <summary>
		///     A file pointer to the first page within the COFF file. This value must be a multiple of the FileAlignment member of
		///     the IMAGE_OPTIONAL_HEADER structure. If a section contains only uninitialized data, set this member is zero.
		/// </summary>
		public uint PointerToRawData;

		/// <summary>
		///     A file pointer to the beginning of the relocation entries for the section. If there are no relocations, this value
		///     is zero.
		/// </summary>
		public uint PointerToRelocations;

		/// <summary>
		///     A file pointer to the beginning of the line-number entries for the section. If there are no COFF line numbers, this
		///     value is zero.
		/// </summary>
		public uint PointerToLinenumbers;

		/// <summary>
		///     The number of relocation entries for the section. This value is zero for executable images.
		/// </summary>
		public ushort NumberOfRelocations;

		/// <summary>
		///     The number of line-number entries for the section.
		/// </summary>
		public ushort NumberOfLinenumbers;

		/// <summary>
		///     The characteristics of the image.
		/// </summary>
		public DataSectionFlags Characteristics;

		/// <summary>
		///     An 8-byte, null-padded UTF-8 string. There is no terminating null character if the string is exactly eight
		///     characters long. For longer names, this member contains a forward slash (/) followed by an ASCII representation of
		///     a decimal number that is an offset into the string table. Executable images do not use a string table and do not
		///     support section names longer than eight characters.
		/// </summary>
		public string Name => new string(new[] { Char0, Char1, Char2, Char3, Char4, Char5, Char6, Char7 });

		internal static IMAGE_SECTION_HEADER Read(Stream reader)
		{
			int size = Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER));
			var data = new byte[size];
			reader.Read(data, 0, size);
			return data.ToStruct<IMAGE_SECTION_HEADER>();
		}
	}
}
