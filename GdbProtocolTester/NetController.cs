using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GdbProtocolTester
{
    public class NetController : TcpClient
    {
        /// <summary>
        /// 开启链路日志
        /// </summary>
        public bool OpenLogger = false;

        /// <summary>
        /// 数据接收事件
        /// </summary>
        public event EventHandler OnMessage;

        /// <summary>
        /// 数据接收线程
        /// </summary>
        private Thread ReceiveThread { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public NetController()
        {
            ReceiveThread = new Thread(ReceiveThreadFunction);
            ReceiveThread.IsBackground = true;
            ReceiveThread.Start(this);
        }

        /// <summary>
        /// 析构
        /// </summary>
        ~NetController()
        {
            if (ReceiveThread != null && ReceiveThread.IsAlive)
                ReceiveThread.Abort();

            if (Client.Connected)
                Client.Disconnect(true);
        }

        /// <summary>
        /// 发送数据到GDB调试器
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public int SendToGDB(string package)
        {
            if (OpenLogger)
            {
                Debugger.Log(0, "", $"Send: {package} \r\n");
            }
            return Client.Send(Encoding.ASCII.GetBytes(package));
        }

        /// <summary>
        /// 单包接收
        /// </summary>
        /// <param name="package"></param>
        private void OnReceivedSingle(string package)
        {
            if (OpenLogger)
            {
                Debugger.Log(0, "", $"Received: {package} \r\n");
            }
            OnMessage.Invoke(package, null);
        }

        /// <summary>
        /// 粘包数据拆解
        /// </summary>
        /// <param name="package"></param>
        private void OnReceivedMultiple(string package)
        {
            if (OnMessage != null)
            {
                if (package == "+")
                {
                    OnReceivedSingle("+");
                    return;
                }

                var index = 0;
                var lastd = 0;
                while (true)
                {
                    index = lastd;
                    index = package.IndexOf('#', index);
                    if (index == -1)
                        break;

                    OnReceivedSingle(package.Substring(lastd, index - lastd + 3));
                    lastd = index + 3;
                }
            }
        }

        /// <summary>
        /// 数据接收处理方法
        /// </summary>
        /// <param name="o"></param>
        private static void ReceiveThreadFunction(object o)
        {
            var mthis = o as NetController;
            do
            {
                if (mthis.Client == null)
                {
                    break;
                }

                if (!mthis.Connected)
                {
                    Thread.Sleep(100);
                    continue;
                }

                var size = 32768;
                var accrualCount = 0;
                var receiveCount = 0;
                var buffer = new byte[size];

            Again:
                receiveCount = mthis.Client.Receive(buffer, accrualCount, size - accrualCount, SocketFlags.None, out SocketError error);
                accrualCount += receiveCount;

                if (receiveCount > 0)
                {
                    if (receiveCount == 1 && accrualCount == 1 && buffer[0] == '+')
                    {
                        mthis.SendToGDB("+");
                        mthis.OnReceivedMultiple("+");
                    }
                    else if (accrualCount > 3 && (buffer[0] == '+' || buffer[0] == '$') && buffer[accrualCount - 3] == '#')
                    {
                        mthis.SendToGDB("+");
                        mthis.OnReceivedMultiple(Encoding.ASCII.GetString(buffer, 0, accrualCount));
                    }
                    else if (accrualCount >= size)
                    {
                        //error package
                        throw new NotImplementedException();
                        //continue;
                    }
                    else
                    {
                        goto Again;
                    }
                }
            } while (true);
        }
    }
}
