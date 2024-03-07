using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GDB.Core
{
    public class CommandInstance : NetController
    {
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler OnHalt;

        /// <summary>
        /// 
        /// </summary>
        private Thread HaltThread { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private AutoResetEvent HaltEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private AutoResetEvent ReceivedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private ConcurrentQueue<GdbMessage> MessageQueue { get; set; }


        private bool NoAckMode { get; set; }


        public int PacketSize { get; set; }

        private bool SupportedFeatures { get; set; }

        private bool SupportedMultiProcessor { get; set; }

        private bool SupportedVCont { get; set; }

        private bool SupportedQStartNoAckMode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public CommandInstance()
        {
            PacketSize = 400;

            OnMessage += OnReceivedMessage;
            HaltEvent = new AutoResetEvent(false);
            ReceivedEvent = new AutoResetEvent(false);
            MessageQueue = new ConcurrentQueue<GdbMessage>();

            HaltThread = new Thread(() =>
            {
                while (HaltEvent.WaitOne())
                {
                    if (OnHalt != null)
                        OnHalt.Invoke(null, null);
                }
            });
            HaltThread.IsBackground = true;
            HaltThread.Start();
        }

        /// <summary>
        /// Receive Single package GDB binary protocol data
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void OnReceivedMessage(object o, EventArgs e)
        {
            var message = new GdbMessage(o as string);
            MessageQueue.Enqueue(message);
            ReceivedEvent.Set();
            if (message.Naked.Length > 10 && message.Naked[0] == 'T')
            {
                HaltEvent.Set();
            }
        }

        /// <summary>
        /// Package GDB remote serial protocol 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private GdbMessage DequeueMessage()
        {
            GdbMessage message;
            while (!MessageQueue.TryDequeue(out message))
            {
                ReceivedEvent.WaitOne();
            }
            return message;
        }

        //var cxx = ExecuteCommand("g");
        //var xxx = MonitorCommand("info");//info registers -a


        private void ParseBasic()
        {
            var message = default(GdbMessage);
            SendToGDB(BuilderProtocol("qSupported"));
            Thread.Sleep(300);
            while (MessageQueue.TryDequeue(out message))
            {
                if (message.Naked.Length < 4) continue;
                var subcat = message.Naked.Split(';');
                foreach (var item in subcat)
                {
                    if (item.StartsWith("PacketSize"))
                    {
                        PacketSize = int.Parse(item.Substring(11));
                    }

                    if (item.StartsWith("qXfer:features:read"))
                    {
                        SupportedFeatures = true;
                    }

                    if (item.StartsWith("multiprocess"))
                    {
                        SupportedMultiProcessor = true;
                    }

                    if (item.StartsWith("vContSupported"))
                    {
                        SupportedVCont = true;
                    }

                    if (item.StartsWith("QStartNoAckMode"))
                    {
                        SupportedQStartNoAckMode = true;
                    }
                }
            }
        }

        private void SwitchMode()
        {
            if (SupportedQStartNoAckMode)
            {
                var message = default(GdbMessage);
                SendToGDB(BuilderProtocol("QStartNoAckMode"));
                Thread.Sleep(100);
                while (MessageQueue.TryDequeue(out message))
                {
                    if (message.Naked.Length == 2 && message.Naked == "OK")
                    {
                        NoAckMode = true;
                    }
                }
            }
            else 
            {
                NoAckMode = false;
            }
        }




        private void ReadTarget()
        {
            if (!SupportedFeatures) return;
            var offset = 0;
            var xmlstr = "";

            do
            {
                var cmdstr = string.Format("qXfer:features:read:target.xml:{0:X3},{1:X3}", offset, PacketSize);
                var xmlmsg = ExecuteCommand(cmdstr);
                if (xmlmsg.Naked.Length > 0 && xmlmsg.Naked == "E00") break;
                xmlstr += xmlmsg.Nakedxml;
                offset += PacketSize;
            } while (true);

            xmlstr = "";
        }

        public void CommandInit()
        {
            ParseBasic();
            SwitchMode();
            //ReadTarget();
        }

        /// <summary>
        /// Execute command (GDB protocol)
        /// </summary>
        /// <param name="command">指令内容</param>
        /// <param name="halt">指令执行后将会进入暂停状态 否则无限等待!</param>
        /// <returns></returns>
        public GdbMessage ExecuteCommand11(string command, bool halt = false)
        {
            GdbMessage result = default(GdbMessage);
            SendToGDB(BuilderProtocol(command));
            result = DequeueMessage();
            //if (halt)
            //{
            //    SendToGDB(BuilderProtocol(command));
            //    do
            //    {
            //        result = DequeueMessage();
            //    } while (!string.IsNullOrEmpty(result.Naked) && result.Naked[0] != 'T');
            //}
            //else
            //{
            //    SendToGDB(BuilderProtocol(command));
            //    do
            //    {
            //        result = DequeueMessage();
            //    } while (!string.IsNullOrEmpty(result.Naked) && result.Naked[0] == 'T');
            //}
            return result;
        }

        public GdbMessage ExecuteCommand(string command, bool halt = false)
        {
            var result = default(GdbMessage);
            SendToGDB(BuilderProtocol(command));
            do
            {
                result = DequeueMessage();
                if (result.Original == "$#00") break;
                if (!string.IsNullOrEmpty(result.Naked))
                {
                    if (halt)
                    {
                        if (NoAckMode)
                        {
                            if (result.Naked[0] == 'T') break;
                        }
                        else
                        {
                            if (result.Naked != "+") break;
                        }
                    }
                    else
                    {
                        if (NoAckMode)
                        {
                            if (result.Naked[0] != 'T') break;
                        }
                        else
                        {
                            if (result.Naked != "+") break;
                        }
                    }
                }
            } while (true);
            return result;
        }

        public GdbMessage ExecuteCommand2(string command, bool halt = false)
        {
            var result = default(GdbMessage);
            SendToGDB(BuilderProtocol(command));
            Thread.Sleep(300);
            result = DequeueMessage();
            return result;
        }

        /// <summary>
        /// Execute monitor command (GDB protocol)
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public string MonitorCommand(string command)
        {
            var cmdStr = BuilderProtocol($"qRcmd,{command.ToBytes().ToHex()}");
            SendToGDB(cmdStr);
            GdbMessage message;
            var list = new List<GdbMessage>();
            do
            {
                message = DequeueMessage();
                list.Add(message);
            } while (message.Naked != "OK");

            var protolines = new List<string>();
            foreach (var item in list)
            {
                //filter "OK" message
                if (item.Naked != "OK")
                {
                    protolines.Add(item.Naked.Substring(1, item.Naked.Length - 1));
                }
            }
            var result = "";
            var xlines = string.Join("", protolines);
            if (!string.IsNullOrEmpty(xlines))
            {
                result = xlines.ToBin().ToTextA();
            }

            return result;
        }
    }
}
