using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.PE
{
	/// <summary>
	///     The architecture type of the computer. An image file can only be run on the specified computer or a system that
	///     emulates the specified computer. This member can be one of the following values.
	///     See https://msdn.microsoft.com/en-us/library/windows/desktop/ms680313(v=vs.85).aspx for more.
	/// </summary>
	// ReSharper disable InconsistentNaming
	public enum MACHINE : ushort
	{
		/// <summary>
		///     x86
		/// </summary>
		IMAGE_FILE_MACHINE_I386 = 0x014c,

		/// <summary>
		///     Intel Itanium
		/// </summary>
		IMAGE_FILE_MACHINE_IA64 = 0x0200,

		/// <summary>
		///     x64
		/// </summary>
		IMAGE_FILE_MACHINE_AMD64 = 0x8664
	}
	// ReSharper restore InconsistentNaming
}
