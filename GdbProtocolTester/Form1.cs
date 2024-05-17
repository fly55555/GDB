using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GdbProtocolTester
{
    public partial class Form1 : Form
    {
        NetController netController;

        private bool Connected { get; set; }

        private List<string> History { get; set; }

        public Form1()
        {
            //Maybe you need this
            History = new List<string>();
            InitializeComponent();
        }

        private string BuilderProtocol(string command)
        {
            byte sum = 0;
            for (int i = 0; i < command.Length; i++)
            {
                sum += (byte)command[i];
            }
            var builder = $"${command}#{Convert.ToString(sum, 16).PadLeft(2, '0')}";
            return builder;
        }

        private void OnReceivedMessage(object o, EventArgs e)
        {
            Invoke(new Action(() =>
            {
                var str = o as string;
                if (radioButton1.Checked)
                {
                    if (str.Length >=4)
                    {
                        var nacked = str.TrimStart(new char[] { '+', '$', 'O' });
                        nacked = nacked.Substring(0, nacked.Length - 3);
                        if (nacked.Length > 1)
                        {
                            var monmsg = Encoding.ASCII.GetString(nacked.ToBin());
                            richTextBox1.SelectionColor = Color.Sienna;
                            richTextBox1.AppendText(monmsg);
                        }
                    }
                }
                else 
                {
                    richTextBox1.SelectionColor = Color.Sienna;
                    richTextBox1.AppendText(str.ToString());
                }
            }));
        }

        private async void button1_ClickAsync(object sender, EventArgs e)
        {
            var connectionstring = textBox1.Text;
            if (!string.IsNullOrEmpty(connectionstring))
            {
                var spiltstr = connectionstring.Split(':');
                if (spiltstr.Length == 2)
                {
                    try
                    {
                        netController = new NetController();
                        netController.OnMessage += OnReceivedMessage;

                        await netController.ConnectAsync(spiltstr[0], int.Parse(spiltstr[1]));
                        Connected = netController.Connected;
                    }
                    catch (Exception err)
                    {
                        richTextBox1.AppendText("\r\n");
                        richTextBox1.SelectionColor = Color.Red;
                        richTextBox1.AppendText(err.ToString());
                        richTextBox1.AppendText("\r\n");
                    }

                    if (Connected)
                    {
                        label3.ForeColor = Color.Green;
                        label3.Text = "已连接";
                        button1.Enabled = false;
                        button2.Enabled = true;
                    }
                    else
                    {
                        label3.ForeColor = Color.Chocolate;
                        label3.Text = "未连接";
                        button1.Enabled = true;
                        button2.Enabled = false;
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (Connected)
            {
                netController.Close();
                netController = null;

                label3.ForeColor = Color.Chocolate;
                label3.Text = "未连接";

                button1.Enabled = true;
                button2.Enabled = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((Keys)e.KeyChar == Keys.Enter)
            {
                var command = richTextBox2.Text.Trim(new char[] { '\r','\n'});
                richTextBox2.Clear();

                if (!Connected)
                {
                    return;
                }

                //监控指令
                if (radioButton1.Checked)
                {
                    var cmdStr = BuilderProtocol($"qRcmd,{command.ToBytes().ToHex()}");
                    netController.SendToGDB(cmdStr);
                }

                //原始指令
                if (radioButton2.Checked)
                {
                    netController.SendToGDB(BuilderProtocol(command));
                }

                //十六进制数据
                if (radioButton3.Checked)
                {
                    netController.Client.Send(command.ToBin());
                }
            }
        }
    }
}
