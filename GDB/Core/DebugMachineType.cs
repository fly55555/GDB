using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core
{
    public enum DebugMachineType
    {
        /// <summary>
        /// QEMU虚拟机
        /// </summary>
        Qemu,
        /// <summary>
        /// Vmware虚拟机
        /// </summary>
        Vmware,
        /// <summary>
        /// Hyperdbg类型调试机
        /// </summary>
        HyperDbg,
        /// <summary>
        /// VirtualBox虚拟机
        /// </summary>
        VirtualBox
    }
}
