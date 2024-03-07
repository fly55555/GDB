using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core
{
    /// <summary>
    /// 控制中心
    /// </summary>
    public class ControlCenter
    {
        /// <summary>
        /// 调试控制器合集
        /// </summary>
        public Dictionary<DebugMachineType, IDebugControl> DebugControlInstances { get; set; }

        /// <summary>
        /// 当前设定调试控制器实例
        /// </summary>
        public static IDebugControl Instance { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public ControlCenter()
        {
            if (DebugControlInstances == null)
            {
                DebugControlInstances = new Dictionary<DebugMachineType, IDebugControl>();
                DebugControlInstances.Add(DebugMachineType.Qemu, new DebugControlQemu());
                DebugControlInstances.Add(DebugMachineType.Vmware, new DebugControlVmware());
            }
        }
    }
}
