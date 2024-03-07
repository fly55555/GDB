using Microsoft.VisualBasic.Compatibility.VB6;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GDB.UI
{
    public class ListViewEx : ListView
    {
        /// <summary>
        /// 读取事件
        /// </summary>
        public class ReadEvent : EventArgs
        {
            /// <summary>
            /// 读取长度
            /// </summary>
            public int Size { get; set; }
            /// <summary>
            /// 读取地址
            /// </summary>
            public long Address { get; set; }
            /// <summary>
            /// 返回内容
            /// </summary>
            public byte[] Result { get; set; }
        }

        /// <summary>
        /// 反汇编事件
        /// </summary>
        public class DisassemblyEvent : EventArgs
        {
            /// <summary>
            /// 反汇编地址
            /// </summary>
            public long Address { get; set; }
            /// <summary>
            /// 代码内容
            /// </summary>
            public byte[] CodeData { get; set; }

            /// <summary>
            /// 返回内容
            /// </summary>
            public List<(long, string, string, string)> Result { get; set; }

            /// <summary>
            /// 实例化
            /// </summary>
            public DisassemblyEvent()
            {
                Result = new List<(long, string, string, string)>();
            }

        }



        /// <summary>
        /// 最大可视项数量(用于实现地址反汇编前后无限滚动)
        /// </summary>
        private int VisibleCount
        {
            get
            {
                if (TopItem != null)
                {
                    return (Height - TopItem.Bounds.Y) / TopItem.Bounds.Height;
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// 实际展示数量(用于实现地址反汇编前后无限滚动)
        /// </summary>
        private int DisplayCount { get; set; }

        /// <summary>
        /// 当前位置(0-VisibleCount 用于仿CE调试滚动效果)
        /// </summary>
        private int CurrentScroll { get; set; }

        /// <summary>
        /// 上一次反汇编更新的顶部(用于实现地址反汇编前后无限滚动)
        /// </summary>
        private int LastRefreshTopIndex { get; set; }

        /// <summary>
        /// 上一次反汇编更新的底部(用于实现地址反汇编前后无限滚动)
        /// </summary>
        private int LastRefreshBottomIndex { get; set; }

        /// <summary>
        /// 可视区最底部项
        /// </summary>
        public ListViewItem BottomItem
        {
            get
            {
                if (TopItem != null && Items.Count >= TopItem.Index + VisibleCount + 1)
                {
                    return Items[TopItem.Index + VisibleCount];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 读取事件
        /// </summary>
        public event EventHandler OnReadEvent;

        /// <summary>
        /// 反汇编事件
        /// </summary>
        public event EventHandler OnDisassemblyEvent;




        /// <summary>
        /// 页面对齐大小
        /// </summary>
        private int PageSize { get; set; }

        /// <summary>
        /// 当前指令在ListView中的位置
        /// </summary>
        private int InstructionIndex { get; set; }

        /// <summary>
        /// 当前指令IP地址
        /// </summary>
        private long InstructionPointer { get; set; }

        /// <summary>
        /// 记录最后一次选中的地址(由于是前后无限的反汇编窗口 需要记住选中)
        /// </summary>
        private List<long> LastSelectedAddress { get; set; }


        /// <summary>
        /// 实例化
        /// </summary>
        public ListViewEx()
        {
            //确定非预览模式再生成项目 防止多次进入生成无效代码
            if (!DesignMode)
            {
                PageSize = 120;
                DisplayCount = 500;
                for (int i = 0; i < DisplayCount; i++)
                {
                    Items.Add(string.Empty);
                    //Items.Add($"{i}");
                }
            }
        }

        /// <summary>
        /// 滚动条变化事件
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnScroll(ScrollEventArgs e)
        {
            //System.Diagnostics.Debugger.Log(0, "OnScroll", $"{e.Type} \r\n");
            //System.Diagnostics.Debugger.Log(0, "Up", $"{(InstructionPointer - PageSize * 2).ToString("X16")} \r\n");
            //System.Diagnostics.Debugger.Log(0, "Down", $"{(InstructionPointer - PageSize * 2).ToString("X16")} \r\n");

            if (TopItem.Tag == null || BottomItem.Tag == null || (long)TopItem.Tag > (long)BottomItem.Tag)
            {

            }

            int index = 0;
            bool istop = false;
            switch (e.Type)
            {
                case ScrollEventType.SmallDecrement://向上
                    {
                        //if (e.NewValue == 2 && TopItem == FocusedItem)
                        //{
                        //    SelectedItems.Clear();
                        //    TopItem.Focused = true;
                        //    TopItem.Selected = true;
                        //}
                        istop = true;
                        if (TopItem.Index == LastRefreshTopIndex + VisibleCount)
                            index = TopItem.Index;
                    }
                    break;
                case ScrollEventType.SmallIncrement://向下
                    {
                        istop = false;

                        //单击进度条按钮
                        if (e.NewValue == 0)
                        {
                            if (BottomItem.Index >= LastRefreshBottomIndex - VisibleCount)
                                index = BottomItem.Index;
                        }

                        //滚轮
                        if (e.NewValue == 1)
                        {
                            if (BottomItem.Index >= LastRefreshBottomIndex - VisibleCount)
                                index = BottomItem.Index;
                        }

                        //按键
                        if (e.NewValue == 2)
                        {
                            if (BottomItem.Index >= LastRefreshBottomIndex - VisibleCount)
                            {
                                index = BottomItem.Index;
                            }
                        }
                    }
                    break;
                case ScrollEventType.LargeDecrement://向上
                    istop = true;
                    //index = TopItem.Index;
                    break;
                case ScrollEventType.LargeIncrement://向下
                    istop = false;
                    //index = TopItem.Index;
                    break;
                case ScrollEventType.ThumbPosition:

                    break;
                case ScrollEventType.ThumbTrack:


                    break;
                case ScrollEventType.First:
                    break;
                case ScrollEventType.Last:
                    break;
                case ScrollEventType.EndScroll:
                    break;
                default:
                    break;
            }



            if (index > 0)
            {
                InstructionPointer = (long)Items[index].Tag;
                RefreshDataDefault(InstructionPointer, istop, e.NewValue);
            }
        }

        /// <summary>
        /// 重写窗口消息处理方法
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            //WM_KEYDOWN
            if (m.Msg == 0x0100) 
            {
                if ((Keys)m.WParam == Keys.Up)
                {
                    OnScroll(new ScrollEventArgs(ScrollEventType.SmallDecrement, 2));
                }

                if ((Keys)m.WParam == Keys.Down)
                {
                    OnScroll(new ScrollEventArgs(ScrollEventType.SmallIncrement, 2));
                }
            }

            //WM_MOUSEWHEEL 
            if (m.Msg == 0x020A)
            {
                int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                if (delta == 120)
                {
                    OnScroll(new ScrollEventArgs(ScrollEventType.SmallDecrement, 1));
                }

                if (delta == -120)
                {
                    OnScroll(new ScrollEventArgs(ScrollEventType.SmallIncrement, 1));
                }
            }

            //WM_VSCROLL 
            if (m.Msg == 0x0115)
            {
                OnScroll(new ScrollEventArgs((ScrollEventType)(m.WParam.ToInt64() & 0xFFFF), 0));
            }
        }

        /// <summary>
        /// 保持可见性(仅不可见时设置)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="top"></param>
        public void EnsureVisibleEx(int index, bool top = true)
        {
            if (VisibleCount > 0 && TopItem != null && BottomItem != null)
            {
                var topindex = TopItem.Index;
                var bottomindex = BottomItem.Index;
                if (index < topindex || index > bottomindex)
                {
                    if (top)
                        EnsureVisibleToTop(index);
                    else
                        EnsureVisibleToBottom(index);
                }
            }
        }


        /// <summary>
        /// 保持可见性并设置到可视区最顶层
        /// </summary>
        /// <param name="index"></param>
        public void EnsureVisibleToTop(int index)
        {
            if (VisibleCount > 0)
            {
                var subvalue = index - BottomItem.Index + VisibleCount;
                if (subvalue > 0)
                {
                    EnsureVisible(index + VisibleCount - 1);
                }
                else
                {
                    EnsureVisible(index);
                }
            }
        }

        /// <summary>
        /// 保持可见性并设置到可视区最底层
        /// </summary>
        /// <param name="index"></param>
        public void EnsureVisibleToBottom(int index)
        {
            if (VisibleCount > 0 && TopItem != null)
            {
                var subvalue = index - TopItem.Index - VisibleCount;
                if (subvalue > 0)
                {
                    EnsureVisible(index);
                }
                else
                {
                    if (index >= VisibleCount)
                        EnsureVisible(index - VisibleCount);
                    EnsureVisible(index);
                }
            }
        }


        /// <summary>
        /// 刷新内容(仅用于单步调试或断点命中)
        /// </summary>
        public void RefreshDataDebug(long address = -1)
        {
            SelectedItems.Clear();

            if (address != -1)
                InstructionPointer = address;

            if (OnReadEvent != null)
            {
                var readEvent = new ReadEvent();
                readEvent.Size = PageSize * 3;
                readEvent.Address = InstructionPointer - PageSize;
                OnReadEvent(null, readEvent);
                if (readEvent.Result != null && readEvent.Result.Length == readEvent.Size)
                {
                    if (OnDisassemblyEvent != null)
                    {
                        var disassemblyEvent = new DisassemblyEvent();
                        disassemblyEvent.Address = readEvent.Address;
                        disassemblyEvent.CodeData = readEvent.Result;
                        OnDisassemblyEvent(null, disassemblyEvent);
                        RefreshDisassemblyDebug(disassemblyEvent);
                    }
                }
            }
        }

        /// <summary>
        /// 刷新反汇编
        /// </summary>
        /// <param name="disassemblyEvent"></param>
        private void RefreshDisassemblyDebug(DisassemblyEvent disassemblyEvent)
        {
            var ipinx = 0;
            foreach (var item in disassemblyEvent.Result)
            {
                if (item.Item1 == InstructionPointer)
                    break;
                ipinx++;
            }

            //仿CE代码自动滚动
            var halfx = DisplayCount / 2;
            var index = halfx - ipinx;
            if (Items[halfx + CurrentScroll].SubItems != null && 
                Items[halfx + CurrentScroll].SubItems[0].Text == disassemblyEvent.Result[ipinx - 1].Item2)
            {
                if (CurrentScroll > VisibleCount)
                {
                    CurrentScroll = 0;
                }
                else
                {
                    CurrentScroll++;
                }
            }
            else
            {
                CurrentScroll = 0;
            }
            index += CurrentScroll;


            LastRefreshTopIndex = index;
            foreach (var item in disassemblyEvent.Result)
            {
                if (Items[index].SubItems != null)
                    Items[index].SubItems.Clear();

                Items[index].Tag = item.Item1;
                Items[index].SubItems[0].Text = item.Item2;
                Items[index].SubItems.Add(item.Item3);
                Items[index].SubItems.Add(item.Item4);
                if (item.Item1 == InstructionPointer)
                {
                    InstructionIndex = index;
                    EnsureVisibleEx(index);
                    Items[index].Selected = true;
                    Items[index].Focused = true;
                    Focus();
                }
                index++;
            }
            LastRefreshBottomIndex = index;
        }

        /// <summary>
        /// 普通翻动更新
        /// </summary>
        /// <param name="address"></param>
        public void RefreshDataDefault(long address, bool istop, int type)
        {
            if (OnReadEvent != null)
            {
                var readEvent = new ReadEvent();
                readEvent.Size = PageSize * 4;
                readEvent.Address = address - PageSize * 2;
                OnReadEvent(null, readEvent);
                if (readEvent.Result != null && readEvent.Result.Length == readEvent.Size)
                {
                    if (OnDisassemblyEvent != null)
                    {
                        var disassemblyEvent = new DisassemblyEvent();
                        disassemblyEvent.Address = readEvent.Address;
                        disassemblyEvent.CodeData = readEvent.Result;
                        OnDisassemblyEvent(null, disassemblyEvent);

                        if (SelectedItems != null && SelectedItems.Count > 0)
                        {
                            LastSelectedAddress = new List<long>();
                            foreach (ListViewItem item in SelectedItems)
                            {
                                LastSelectedAddress.Add((long)item.Tag);
                            }
                            SelectedItems.Clear();
                        }

                        var index = DisplayCount / 2 - disassemblyEvent.Result.Count / 2;

                        LastRefreshTopIndex = index;
                        foreach (var item in disassemblyEvent.Result)
                        {
                            if (Items[index].SubItems != null)
                                Items[index].SubItems.Clear();

                            Items[index].Tag = item.Item1;
                            Items[index].SubItems[0].Text = item.Item2;
                            Items[index].SubItems.Add(item.Item3);
                            Items[index].SubItems.Add(item.Item4);
                            if (item.Item1 == address)
                            {
                                InstructionIndex = index;
                                EnsureVisibleEx(index, istop);
                                if (type == 2)
                                {
                                    Items[index].Selected = true;
                                    Items[index].Focused = true;
                                    Focus();
                                }
                            }

                            if (LastSelectedAddress != null && LastSelectedAddress.Contains(item.Item1))
                            {
                                Items[index].Selected = true;
                            }

                            index++;
                        }
                        LastRefreshBottomIndex = index;
                    }
                }
            }
        }

        private void RefreshDisassemblyDefault(DisassemblyEvent disassemblyEvent)
        {
            
        }

    }
}
