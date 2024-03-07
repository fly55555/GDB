using GDB.Core.Disassembly;
using GDB.Core.Register;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core
{
    public interface IDebugControl
    {
        /// <summary>
        /// 目标当前是否为暂停状态
        /// </summary>
        bool IsHalt { get; set; }

        /// <summary>
        /// 目标暂停 事件接收器
        /// </summary>
        event EventHandler OnHaltHandler;

        /// <summary>
        /// Sword Art Online
        /// </summary>
        /// <param name="connectionstring"></param>
        /// <returns></returns>
        bool LinkStart(string connectionstring);

        /// <summary>
        /// 测试执行命令
        /// </summary>
        /// <param name="command">命令内容</param>
        /// <param name="monitor">是否为监控命令</param>
        /// <returns></returns>
        string Execute(string command, bool monitor = false);

        /// <summary>
        /// 单步步入
        /// </summary>
        /// <returns>true:成功 false:失败</returns>
        bool Step();

        /// <summary>
        /// 立刻中断
        /// </summary>
        /// <returns>true:成功 false:失败</returns>
        bool Break();

        /// <summary>
        /// 继续执行
        /// </summary>
        /// <returns>true:成功 false:失败</returns>
        bool Continue();

        /// <summary>
        /// 单步步过(对于call一类直接越过)
        /// </summary>
        /// <returns>true:成功 false:失败</returns>
        bool StepOver();

        /// <summary>
        /// 获取当前寄存器信息
        /// </summary>
        /// <param name="context">寄存器返回内容</param>
        /// <returns>true:成功 false:失败</returns>
        bool GetContext(out CommonRegister_x64 context);

        /// <summary>
        /// 添加断点
        /// </summary>
        /// <param name="type">断点类型</param>
        /// <param name="addr">断点地址</param>
        /// <param name="size">断点范围</param>
        /// <returns>true:成功 false:失败</returns>
        bool BreakPointAdd(int type, long addr, int size);

        /// <summary>
        /// 删除断点
        /// </summary>
        /// <param name="type">断点类型</param>
        /// <param name="addr">断点地址</param>
        /// <param name="size">断点范围</param>
        /// <returns>true:成功 false:失败</returns>
        bool BreakPointDel(int type, long addr, int size);

        /// <summary>
        /// 读取内存
        /// </summary>
        /// <param name="addr">内存地址</param>
        /// <param name="size">读取长度</param>
        /// <returns>返回读取到的内存原始数据</returns>
        byte[] ReadVirtual(long addr, int size);

        /// <summary>
        /// 反汇编
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        List<CommonInstruction> Disassembly(long addr, int size);
    }
}
