using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.PE
{
	/// <summary>
	///     Represents the data directory.
	///     See https://msdn.microsoft.com/en-us/library/windows/desktop/ms680305(v=vs.85).aspx for more info.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	// ReSharper disable once InconsistentNaming
	public struct IMAGE_DATA_DIRECTORY
	{
		/// <summary>
		///     The relative virtual address of the table.
		/// </summary>
		public uint VirtualAddress;

		/// <summary>
		///     The size of the table, in bytes.
		/// </summary>
		public uint Size;
	}
}
