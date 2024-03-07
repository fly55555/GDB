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
	///     DOS .EXE header
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	// ReSharper disable once InconsistentNaming
	public struct IMAGE_DOS_HEADER
	{
		/// <summary>
		///     Magic number
		/// </summary>
		public ushort e_magic;

		/// <summary>
		///     Bytes on last page of file
		/// </summary>
		public ushort e_cblp;

		/// <summary>
		///     Pages in file
		/// </summary>
		public ushort e_cp;

		/// <summary>
		///     Relocations
		/// </summary>
		public ushort e_crlc;

		/// <summary>
		///     Size of header in paragraphs
		/// </summary>
		public ushort e_cparhdr;

		/// <summary>
		///     Minimum extra paragraphs needed
		/// </summary>
		public ushort e_minalloc;

		/// <summary>
		///     Maximum extra paragraphs needed
		/// </summary>
		public ushort e_maxalloc;

		/// <summary>
		///     Initial (relative) SS value
		/// </summary>
		public ushort e_ss;

		/// <summary>
		///     Initial SP value
		/// </summary>
		public ushort e_sp;

		/// <summary>
		///     Checksum
		/// </summary>
		public ushort e_csum;

		/// <summary>
		///     Initial IP value
		/// </summary>
		public ushort e_ip;

		/// <summary>
		///     Initial (relative) CS value
		/// </summary>
		public ushort e_cs;

		/// <summary>
		///     File address of relocation table
		/// </summary>
		public ushort e_lfarlc;

		/// <summary>
		///     Overlay number
		/// </summary>
		public ushort e_ovno;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res_0;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res_1;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res_2;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res_3;

		/// <summary>
		///     OEM identifier (for e_oeminfo)
		/// </summary>
		public ushort e_oemid;

		/// <summary>
		///     OEM information; e_oemid specific
		/// </summary>
		public ushort e_oeminfo;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res2_0;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res2_1;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res2_2;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res2_3;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res2_4;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res2_5;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res2_6;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res2_7;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res2_8;

		/// <summary>
		///     Reserved words
		/// </summary>
		public ushort e_res2_9;

		/// <summary>
		///     File address of new exe header
		/// </summary>
		public uint e_lfanew;

		internal static IMAGE_DOS_HEADER Read(Stream reader)
		{
			int size = Marshal.SizeOf(typeof(IMAGE_DOS_HEADER));
			var data = new byte[size];
			reader.Read(data, 0, size);
			return data.ToStruct<IMAGE_DOS_HEADER>();
		}
	}
}
