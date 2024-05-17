using GDB.Core;
using GDB.Core.Disassembly;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GDB
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// 缓存页数据
        /// </summary>
        public byte[] MemoryCachePage { get; set; }



        public MainForm()
        {
            InitializeComponent();
            DoubleBufferedListView(listView_Registers, true);
            DoubleBufferedListView(listView_Disassembly, true);
        }

        public void DoubleBufferedListView(ListView listView, bool flag)
        {
            Type type = listView.GetType();
            System.Reflection.PropertyInfo pi = type.GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pi.SetValue(listView, flag, null);
        }


        private void TestSum()
        {
            var command = "qXfer:threads:read::0,100";

            byte sum = 0;
            for (int i = 0; i < command.Length; i++)
            {
                sum += (byte)command[i];
            }
            var builder = $"${command}#{Convert.ToString(sum, 16).PadLeft(2, '0')}";
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            TestSum();
            ControlCenter.Instance = new ControlCenter().DebugControlInstances[DebugMachineType.Vmware];
            ControlCenter.Instance.OnHaltHandler += TargetOnHalt;
            ControlCenter.Instance.LinkStart("LocalHost:8864");

            listView_Disassembly.OnReadEvent += (object o, EventArgs readevent) =>
            {
                var args = (UI.ListViewEx.ReadEvent)readevent;
                args.Result = ControlCenter.Instance.ReadVirtual(args.Address, args.Size);
            };
            listView_Disassembly.OnDisassemblyEvent += (object o, EventArgs readevent) =>
            {
                var args = (UI.ListViewEx.DisassemblyEvent)readevent;
                var instructions = CommonDisassembly_x64.GetResult(args.CodeData, args.Address);
                foreach (var item in instructions)
                {
                    args.Result.Add(((long)item.IP, string.Format("{0:X16}", item.IP), item.CodeStr, item.Describe));
                }
            };

        }

        private void ListView_Disassembly_OnReadAction(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Halt Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TargetOnHalt(object sender, EventArgs e)
        {
            Invoke(new Action(() =>
            {
                ShowContext();
                ShowDisassembly();
                KeyStateUpdate();
            }));
        }

        /// <summary>
        /// 时钟执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (ControlCenter.Instance != null && ControlCenter.Instance.IsHalt)
            {

                
            }
        }

        private void ShowContext()
        {
            if (!ControlCenter.Instance.GetContext(out Core.Register.CommonRegister_x64 context))
                return;

            var orgItems = listView_Registers.Items;
            if (orgItems.Count > 0)
            {
                foreach (var item in context.GetType().GetProperties())
                {
                    var name = item.Name;
                    var value = item.GetValue(context);
                    orgItems[name].UseItemStyleForSubItems = true;
                    orgItems[name].SubItems[1].ForeColor = Color.Black;
                    if (orgItems[name].SubItems[1].Text != string.Format("{0:X16}", value))
                    {
                        orgItems[name].UseItemStyleForSubItems = false;
                        orgItems[name].SubItems[1].ForeColor = Color.Red;
                    }
                    orgItems[name].SubItems[1].Text = string.Format("{0:X16}", value);
                }
            }
            else
            {
                foreach (var item in context.GetType().GetProperties())
                {
                    var name = item.Name;
                    var value = item.GetValue(context);
                    orgItems.Add(name, name, name).SubItems.Add(string.Format("{0:X16}", value));
                }
            }

        }

        private void ShowDisassembly()
        {
            if (ControlCenter.Instance.GetContext(out Core.Register.CommonRegister_x64 context))
            {
                listView_Disassembly.RefreshDataDebug((long)context.RIP);
            }
        }

        private void KeyStateUpdate()
        {
            if (ControlCenter.Instance.IsHalt)
            {
                stepIntoToolStripMenuItem.Enabled = true;
                stepOverToolStripMenuItem.Enabled = true;
                runToolStripMenuItem1.Enabled = true;

                breakToolStripMenuItem.Enabled = false;
            }
            else
            {
                stepIntoToolStripMenuItem.Enabled = false;
                stepOverToolStripMenuItem.Enabled = false;
                runToolStripMenuItem1.Enabled = false;

                breakToolStripMenuItem.Enabled = true;
            }
        }

        /// <summary>
        /// 立即中断
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void breakToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ControlCenter.Instance.Break();
            KeyStateUpdate();
        }

        /// <summary>
        /// 继续执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ControlCenter.Instance.Continue();
            KeyStateUpdate();
        }

        /// <summary>
        /// 单步步入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stepOverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ControlCenter.Instance.Step();
            KeyStateUpdate();
        }

        /// <summary>
        /// 单步步过
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ControlCenter.Instance.Step();
            KeyStateUpdate();
        }

        /// <summary>
        /// 执行到上一帧
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void executeTillReturnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            KeyStateUpdate();
        }

        /// <summary>
        /// 测试命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExecuteTest_Click(object sender, EventArgs e)
        {
            textBox_GDBOutput.Text = ControlCenter.Instance.Execute(textBox_GDBCommand.Text);
            KeyStateUpdate();
        }

        /// <summary>
        /// 更新断点信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateBreakPoint_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow item in dataGridView_BreakPoint.Rows)
            {
                if (item.Cells.Count == 3)
                {
                    if (!string.IsNullOrEmpty((string)item.Cells[0].Value) &&
                        !string.IsNullOrEmpty((string)item.Cells[1].Value) &&
                        !string.IsNullOrEmpty((string)item.Cells[2].Value))
                    {
                        var addr = (string)item.Cells[0].Value;
                        var type = (string)item.Cells[1].Value;
                        var open = (string)item.Cells[2].Value;
                        if (open == "1")
                        {
                            ControlCenter.Instance.BreakPointAdd(Convert.ToInt32(type), Convert.ToInt64(addr, 16), 1);
                        }
                        else
                        {
                            ControlCenter.Instance.BreakPointDel(Convert.ToInt32(type), Convert.ToInt64(addr, 16), 1);
                        }
                    }
                }
            }
        }
    }
}
