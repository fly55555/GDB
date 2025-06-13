using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace GDB.Core.Protocol
{
    public class GdbClient : IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource;
        
        // 使用字典来存储发送的命令和对应的响应通道
        private readonly ConcurrentDictionary<string, TaskCompletionSource<GdbPacket>> _pendingCommands = 
            new ConcurrentDictionary<string, TaskCompletionSource<GdbPacket>>();
        
        // 存储已处理的数据包，用于调试和诊断
        private readonly ConcurrentQueue<GdbPacket> _processedPackets = new ConcurrentQueue<GdbPacket>();
        private const int MAX_PROCESSED_PACKETS = 100;
        
        // 存储qRcmd命令的输出缓冲区
        private readonly ConcurrentDictionary<string, StringBuilder> _qRcmdOutputBuffers = 
            new ConcurrentDictionary<string, StringBuilder>();
        
        // 用于强制命令串行执行的信号量
        private readonly SemaphoreSlim _commandSemaphore = new SemaphoreSlim(1, 1);
        
        // 当前命令ID，用于唯一标识每个命令
        private int _commandId = 0;

        private readonly byte[] _receiveBuffer = new byte[4096];
        private readonly StringBuilder _processingBuffer = new StringBuilder();

        // 日志记录器
        private readonly List<string> _logs = new List<string>();
        private const int MAX_LOG_ENTRIES = 1000;
        
        // 是否启用详细日志
        public bool EnableVerboseLogging { get; set; } = true;

        // 中断和停止事件
        public event Action<GdbPacket> OnStopReceived;
        public event Action OnResumed;
        
        // 日志记录事件
        public event Action<string> OnLogMessage;
        
        // 定义不同类型命令的响应特征
        private static readonly Dictionary<string, string[]> CommandResponsePrefixes = new Dictionary<string, string[]>
        {
            { "qRcmd,", new[] { "O", "OK" } },    // 监控命令
            { "m", new[] { "" } },               // 内存读取
            { "g", new[] { "" } },               // 获取寄存器
            { "c", new[] { "S", "T", "W", "X", "" } }, // 继续执行 - 注意：ACK信号已在ProcessBuffer中单独处理
            { "s", new[] { "S", "T", "W", "X", "" } }, // 单步执行 - 注意：ACK信号已在ProcessBuffer中单独处理
            { "?", new[] { "S", "T", "W", "X" } }, // 获取状态
            { "Z", new[] { "OK", "E" } },        // 添加断点
            { "z", new[] { "OK", "E" } }         // 删除断点
        };

        // 错误响应前缀
        private static readonly string[] ErrorPrefixes = new[] { "E", "e" };

        public bool IsConnected => _tcpClient?.Connected ?? false;

        public GdbClient()
        {
            _tcpClient = new TcpClient();
        }

        // 记录日志
        private void LogMessage(string message, bool isError = false)
        {
            return;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] {(isError ? "[ERROR] " : "")}GdbClient: {message}";
            
            lock (_logs)
            {
                _logs.Add(formattedMessage);
                while (_logs.Count > MAX_LOG_ENTRIES)
                {
                    _logs.RemoveAt(0);
                }
            }
            
            OnLogMessage?.Invoke(formattedMessage);
            
            // 使用System.Diagnostics.Debug或Trace输出到VS输出窗口
            System.Diagnostics.Debug.WriteLine(formattedMessage);
        }

        // 获取所有日志
        public List<string> GetLogs()
        {
            lock (_logs)
            {
                return new List<string>(_logs);
            }
        }

        // 清除日志
        public void ClearLogs()
        {
            lock (_logs)
            {
                _logs.Clear();
            }
        }

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            LogMessage($"正在连接到 {host}:{port}...");
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await _tcpClient.ConnectAsync(host, port);
            _stream = _tcpClient.GetStream();
            LogMessage($"已连接到 {host}:{port}");
            
            _ = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token));

            // Query the target's status to get the initial halt reason.
            LogMessage("发送初始状态查询...");
            await SendPacketAsync(new GdbPacket("?"), cancellationToken);
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            LogMessage("接收循环已启动");
            while (!token.IsCancellationRequested && IsConnected)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length, token);
                    if (bytesRead == 0)
                    {
                        LogMessage("连接已关闭（读取0字节）", true);
                        break;
                    }

                    string receivedData = Encoding.ASCII.GetString(_receiveBuffer, 0, bytesRead);
                    if (EnableVerboseLogging)
                    {
                        LogMessage($"接收原始数据: {ByteArrayToHexString(_receiveBuffer, 0, bytesRead)} (ASCII: {EscapeNonPrintable(receivedData)})");
                    }

                    _processingBuffer.Append(receivedData);
                    await ProcessBuffer();
                }
                catch (OperationCanceledException) 
                { 
                    LogMessage("接收循环已取消");
                    break; 
                }
                catch (IOException ex) 
                { 
                    LogMessage($"IO异常: {ex.Message}", true);
                    break; 
                }
                catch (Exception ex) 
                { 
                    LogMessage($"接收循环异常: {ex.Message}", true);
                }
            }
            LogMessage("接收循环已退出");
        }

        private async Task ProcessBuffer()
        {
            if (EnableVerboseLogging)
            {
                LogMessage($"处理缓冲区, 当前长度: {_processingBuffer.Length}, 内容: {EscapeNonPrintable(_processingBuffer.ToString())}");
            }

            // 存储此次处理过程中提取的所有数据包，用于检测重复
            var processedPacketsInThisRound = new HashSet<string>();

            while (_processingBuffer.Length > 0)
            {
                // 处理ACK信号
                if (_processingBuffer[0] == '+')
                {
                    if (EnableVerboseLogging)
                    {
                        LogMessage("接收到ACK信号(+)");
                    }
                    _processingBuffer.Remove(0, 1);
                    
                    // 检查是否有待处理的继续执行或单步命令
                    // 使用ToList()创建一个副本，避免在迭代过程中修改集合
                    var pendingCommands = _pendingCommands.ToList();
                    foreach (var kvp in pendingCommands)
                    {
                        string commandId = kvp.Key;
                        string command = commandId.Substring(commandId.IndexOf(':') + 1);
                        
                        // 如果是继续执行命令或单步命令，则完成响应
                        if (command == "c" || command == "s" || command == "vCont;c" || command == "vCont;s")
                        {
                            TaskCompletionSource<GdbPacket> tcs;
                            if (_pendingCommands.TryRemove(commandId, out tcs))
                            {
                                LogMessage($"接收到ACK信号，完成命令 {commandId}");
                                tcs.TrySetResult(new GdbPacket("OK"));
                                OnResumed?.Invoke();
                            }
                        }
                    }
                    
                    continue;
                }
                
                // 处理NACK信号
                if (_processingBuffer[0] == '-')
                {
                    LogMessage("接收到NACK信号(-)，需要重传");
                    _processingBuffer.Remove(0, 1);
                    // 这里应该重新发送上一个包，但目前实现中未保留这个信息
                    // TODO: 实现包重传机制
                    continue;
                }

                // 查找数据包开始标记
                int startIndex = _processingBuffer.ToString().IndexOf('$');
                if (startIndex == -1)
                {
                    // 没有找到开始标记，清空缓冲区
                    if (EnableVerboseLogging)
                    {
                        LogMessage($"缓冲区中未找到起始符号($)，丢弃缓冲区: {EscapeNonPrintable(_processingBuffer.ToString())}");
                    }
                    _processingBuffer.Clear();
                    return;
                }
                
                // 移除开始标记前的所有数据
                if (startIndex > 0)
                {
                    if (EnableVerboseLogging)
                    {
                        LogMessage($"丢弃起始符号前的数据: {EscapeNonPrintable(_processingBuffer.ToString(0, startIndex))}");
                    }
                    _processingBuffer.Remove(0, startIndex);
                }

                // 查找数据包结束标记
                int endIndex = _processingBuffer.ToString().IndexOf('#');
                if (endIndex == -1 || _processingBuffer.Length < endIndex + 3)
                {
                    // 数据包不完整，等待更多数据
                    if (EnableVerboseLogging)
                    {
                        LogMessage("数据包不完整，等待更多数据");
                    }
                    return;
                }

                // 提取完整的数据包
                string rawPacket = _processingBuffer.ToString(0, endIndex + 3);
                _processingBuffer.Remove(0, endIndex + 3);

                // 检查是否是当前处理循环中的重复包
                if (processedPacketsInThisRound.Contains(rawPacket))
                {
                    LogMessage($"跳过重复数据包: {rawPacket}");
                    continue;
                }
                
                // 将包添加到已处理集合
                processedPacketsInThisRound.Add(rawPacket);

                LogMessage($"提取完整数据包: {rawPacket}");

                // 处理完整的数据包
                await HandleFullPacket(rawPacket);
            }
        }

        private async Task HandleFullPacket(string rawPacket)
        {
            try
            {
                var packet = GdbPacket.Deserialize(rawPacket);
                LogMessage($"解析数据包: {rawPacket} -> 数据: {packet.Data}, 校验和: {packet.Checksum:X2}");
                
                // 发送ACK确认
                await _stream.WriteAsync(new byte[] { (byte)'+' }, 0, 1, _cancellationTokenSource.Token);
                if (EnableVerboseLogging)
                {
                    LogMessage("发送ACK确认(+)");
                }

                // 将处理过的数据包加入队列（用于调试）
                _processedPackets.Enqueue(packet);
                while (_processedPackets.Count > MAX_PROCESSED_PACKETS && _processedPackets.TryDequeue(out _)) { }

                // 检查是否是停止通知（优先处理中断包）
                if (packet.Data.StartsWith("S") || packet.Data.StartsWith("T"))
                {
                    LogMessage($"接收到停止通知: {packet.Data}");
                    OnStopReceived?.Invoke(packet);
                    return;
                }

                // 如果没有待处理的命令，直接丢弃响应
                if (_pendingCommands.Count == 0)
                {
                    LogMessage($"没有待处理的命令，丢弃响应: {packet.Data}", true);
                    return;
                }

                // 首先尝试处理qRcmd命令的响应
                // 检查是否是qRcmd命令的"OK"完成信号
                if (packet.Data == "OK")
                {
                    // 检查是否是"c"或"s"命令的响应
                    if(TryCompleteResumedCommand())
                    {
                        return;
                    }
                    if (TryFindQRcmdCommand() && HandleQRcmdCompletion(packet))
                    {
                        return; // 已处理，不需要继续
                    }
                }
                // 检查是否是qRcmd命令的"O"输出数据
                else if (packet.Data.StartsWith("O") && TryFindQRcmdCommand())
                {
                    LogMessage("检测到qRcmd命令的O输出数据");
                    if (HandleQRcmdOutput(packet))
                    {
                        return; // 已处理，不需要继续
                    }
                }

                // 尝试匹配等待响应的命令
                bool matched = false;
                foreach (var kvp in _pendingCommands)
                {
                    string commandId = kvp.Key;
                    string command = commandId.Substring(commandId.IndexOf(':') + 1);
                    
                    if (IsMatchingResponse(command, packet.Data))
                    {
                        TaskCompletionSource<GdbPacket> tcs;
                        if (_pendingCommands.TryRemove(commandId, out tcs))
                        {
                            LogMessage($"已匹配命令 ID {commandId} 的响应: {packet.Data}");
                            tcs.TrySetResult(packet);
                            matched = true;
                            break;
                        }
                    }
                }

                // 如果没有匹配到任何命令，检查是否有任何待处理的命令
                if (!matched && _pendingCommands.Count > 0)
                {
                    // 取最早的命令
                    var oldestCommand = _pendingCommands.Keys.OrderBy(k => int.Parse(k.Split(':')[0])).FirstOrDefault();
                    if (oldestCommand != null)
                    {
                        TaskCompletionSource<GdbPacket> tcs;
                        if (_pendingCommands.TryRemove(oldestCommand, out tcs))
                        {
                            LogMessage($"未能精确匹配，将响应 {packet.Data} 分配给最早的命令 {oldestCommand}");
                            tcs.TrySetResult(packet);
                        }
                    }
                    else
                    {
                        LogMessage($"未匹配任何命令，丢弃响应: {packet.Data}", true);
                    }
                }
                else if (!matched)
                {
                    LogMessage($"未匹配任何命令，丢弃响应: {packet.Data}", true);
                }
            }
            catch (Exception ex) 
            { 
                LogMessage($"处理数据包异常: {ex.Message}", true); 
            }
        }

        private bool TryCompleteResumedCommand()
        {
            // 查找c或s命令
            var commandKvp = _pendingCommands.FirstOrDefault(kvp =>
            {
                var command = kvp.Key.Substring(kvp.Key.IndexOf(':') + 1);
                return command == "c" || command == "s";
            });

            if (commandKvp.Key != null)
            {
                if (_pendingCommands.TryRemove(commandKvp.Key, out var tcs))
                {
                    LogMessage($"继续或单步命令 '{commandKvp.Key}' 已被确认为 'OK'.");
                    tcs.TrySetResult(new GdbPacket("OK"));
                    OnResumed?.Invoke();
                    return true;
                }
            }
            return false;
        }

        // 检查是否有待处理的qRcmd命令
        private bool TryFindQRcmdCommand()
        {
            foreach (var kvp in _pendingCommands)
            {
                string command = kvp.Key.Substring(kvp.Key.IndexOf(':') + 1);
                if (command.StartsWith("qRcmd,"))
                {
                    return true;
                }
            }
            return false;
        }

        // 处理qRcmd命令的输出数据包
        private bool HandleQRcmdOutput(GdbPacket packet)
        {
            // 验证是否是O开头但不是OK的数据包
            if (packet.Data == "OK")
            {
                LogMessage("这是OK数据包，应由HandleQRcmdCompletion处理");
                return false;
            }
            
            if (!packet.Data.StartsWith("O"))
            {
                LogMessage($"这不是输出数据包: {packet.Data}");
                return false;
            }
            
            // 查找正在等待响应的qRcmd命令
            var qRcmdCommands = _pendingCommands
                .Where(kv => kv.Key.Substring(kv.Key.IndexOf(':') + 1).StartsWith("qRcmd,"))
                .OrderBy(kv => int.Parse(kv.Key.Split(':')[0]))
                .ToList();
                
            if (qRcmdCommands.Count == 0)
            {
                LogMessage("未找到等待响应的qRcmd命令，无法处理输出数据");
                return false;
            }
            
            // 只处理最早的一个qRcmd命令
            var kvp = qRcmdCommands.First();
            string commandId = kvp.Key;
            
            // 将输出添加到此命令的缓冲区
            var outputBuffer = _qRcmdOutputBuffers.GetOrAdd(commandId, new StringBuilder());
            
            // 检查是否已经存在相同的输出，避免重复添加
            if (outputBuffer.ToString().Contains(packet.Data))
            {
                LogMessage($"命令 {commandId} 已存在相同输出，跳过: {packet.Data}");
                return true;
            }
            
            outputBuffer.Append(packet.Data);
            LogMessage($"为命令 {commandId} 添加输出: {packet.Data}");
            return true;
        }

        // 处理qRcmd命令的完成信号
        private bool HandleQRcmdCompletion(GdbPacket packet)
        {
            LogMessage("尝试处理qRcmd命令完成信号");
            
            // 查找正在等待响应的qRcmd命令
            var qRcmdCommands = _pendingCommands
                .Where(kv => kv.Key.Substring(kv.Key.IndexOf(':') + 1).StartsWith("qRcmd,"))
                .OrderBy(kv => int.Parse(kv.Key.Split(':')[0]))
                .ToList();
                
            if (qRcmdCommands.Count == 0)
            {
                LogMessage("未找到等待完成的qRcmd命令");
                return false;
            }
            
            // 只处理最早的一个qRcmd命令
            var kvp = qRcmdCommands.First();
            string commandId = kvp.Key;
            
            LogMessage($"找到等待完成的qRcmd命令: {commandId}");
            
            // 获取此命令累积的输出
            StringBuilder outputBuffer;
            if (_qRcmdOutputBuffers.TryRemove(commandId, out outputBuffer))
            {
                // 创建组合响应包
                string combinedData = outputBuffer.ToString();
                LogMessage($"获取到命令 {commandId} 的累积输出: {combinedData}");
                
                if (string.IsNullOrEmpty(combinedData))
                {
                    combinedData = "OK"; // 如果没有输出，就使用OK
                }
                
                GdbPacket combinedPacket = new GdbPacket(combinedData);
                
                // 完成命令
                TaskCompletionSource<GdbPacket> tcs;
                if (_pendingCommands.TryRemove(commandId, out tcs))
                {
                    LogMessage($"完成qRcmd命令 {commandId}，组合响应: {combinedData}");
                    tcs.TrySetResult(combinedPacket);
                    return true;
                }
                else
                {
                    LogMessage($"无法完成命令 {commandId}，未找到TaskCompletionSource", true);
                }
            }
            else
            {
                // 没有找到缓冲区，可能是第一个响应就是OK
                LogMessage($"未找到命令 {commandId} 的输出缓冲区，直接返回OK");
                
                TaskCompletionSource<GdbPacket> tcs;
                if (_pendingCommands.TryRemove(commandId, out tcs))
                {
                    LogMessage($"完成qRcmd命令 {commandId}，直接响应: OK");
                    tcs.TrySetResult(packet);
                    return true;
                }
                else
                {
                    LogMessage($"无法完成命令 {commandId}，未找到TaskCompletionSource", true);
                }
            }
            
            // 即使找不到TaskCompletionSource，也认为处理了这个OK包
            return true;
        }
        
        private bool IsMatchingResponse(string command, string response)
        {
            // 检查是否是错误响应
            foreach (var errorPrefix in ErrorPrefixes)
            {
                if (response.StartsWith(errorPrefix))
                {
                    if (EnableVerboseLogging)
                    {
                        LogMessage($"匹配到错误响应: {response} (命令: {command})");
                    }
                    return true;
                }
            }
            
            // 获取命令前缀
            string commandPrefix = GetCommandPrefix(command);
            
            // 查找命令对应的响应前缀
            if (!string.IsNullOrEmpty(commandPrefix) && CommandResponsePrefixes.TryGetValue(commandPrefix, out var responsePrefixes))
            {
                foreach (var prefix in responsePrefixes)
                {
                    if (string.IsNullOrEmpty(prefix) || response.StartsWith(prefix))
                    {
                        if (EnableVerboseLogging)
                        {
                            LogMessage($"匹配响应: {response} 对应命令: {command} (前缀: {commandPrefix}, 响应前缀: {prefix})");
                        }
                        return true;
                    }
                }
            }
            
            // 如果找不到匹配规则，默认接受响应
            if (EnableVerboseLogging)
            {
                LogMessage($"无法确定匹配规则，默认接受响应: {response} 对应命令: {command}");
            }
            return true;
        }
        
        private string GetCommandPrefix(string command)
        {
            foreach (var prefix in CommandResponsePrefixes.Keys)
            {
                if (command.StartsWith(prefix))
                {
                    return prefix;
                }
            }
            return null;
        }
        
        public async Task<GdbPacket> SendCommandAndReceiveResponseAsync(string command, CancellationToken cancellationToken = default)
        {
            // 等待信号量，确保命令串行执行
            await _commandSemaphore.WaitAsync(cancellationToken);
            try
            {
                // 创建命令ID
                string commandId = $"{Interlocked.Increment(ref _commandId)}:{command}";
                LogMessage($"发送命令 ID {commandId}");
            
                // 创建用于等待响应的TaskCompletionSource
                var responseTcs = new TaskCompletionSource<GdbPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingCommands[commandId] = responseTcs;

                // 如果是qRcmd命令，初始化输出缓冲区
                if (command.StartsWith("qRcmd,"))
                {
                    _qRcmdOutputBuffers[commandId] = new StringBuilder();
                    LogMessage($"初始化qRcmd命令 {commandId} 的输出缓冲区");
                }
            
                try
                {
                    // 发送命令
                    var packet = new GdbPacket(command);
                    await SendPacketAsync(packet, cancellationToken);
                
                    // 对于单步命令和继续执行命令，使用更短的超时时间
                    int timeoutSeconds = (command == "s" || command == "c" || command == "vCont;s" || command == "vCont;c") ? 3 : 10;
                
                    // 设置超时
                    using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                    {
                        LogMessage($"等待命令 ID {commandId} 的响应，超时时间: {timeoutSeconds}秒");
                        var completedTask = await Task.WhenAny(responseTcs.Task, Task.Delay(Timeout.Infinite, linkedCts.Token));
                    
                        if (completedTask == responseTcs.Task)
                        {
                            var response = await responseTcs.Task;
                            LogMessage($"命令 ID {commandId} 已收到响应: {response.Data}");
                            return response;
                        }
                        else
                        {
                            // 超时处理
                            _pendingCommands.TryRemove(commandId, out _);
                            _qRcmdOutputBuffers.TryRemove(commandId, out _); // 清除缓冲区
                            LogMessage($"命令 ID {commandId} 超时", true);
                            throw new TimeoutException($"Command timed out: {command}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 确保在出现异常时移除挂起的命令
                    _pendingCommands.TryRemove(commandId, out _);
                    _qRcmdOutputBuffers.TryRemove(commandId, out _); // 清除缓冲区
                
                    // 记录异常
                    LogMessage($"命令 ID {commandId} 执行失败: {ex.Message}", true);
                
                    // 重新抛出原始异常
                    throw new Exception($"Failed to execute command: {command}", ex);
                }
            }
            finally
            {
                // 释放信号量
                _commandSemaphore.Release();
            }
        }

        public async Task SendPacketAsync(GdbPacket packet, CancellationToken cancellationToken = default)
        {
            var packetBytes = packet.Serialize();
            string packetString = Encoding.ASCII.GetString(packetBytes);
            LogMessage($"发送数据包: {packetString} (十六进制: {ByteArrayToHexString(packetBytes, 0, packetBytes.Length)})");
            await _stream.WriteAsync(packetBytes, 0, packetBytes.Length, cancellationToken);
        }

        public void Dispose()
        {
            LogMessage("关闭GdbClient连接");
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _stream?.Dispose();
            _tcpClient?.Dispose();
            
            // 清除所有挂起的命令
            foreach (var kvp in _pendingCommands)
            {
                LogMessage($"取消命令 ID {kvp.Key}");
                kvp.Value.TrySetCanceled();
            }
            _pendingCommands.Clear();
            _qRcmdOutputBuffers.Clear();
            LogMessage("GdbClient已关闭");
        }

        // 辅助方法：将字节数组转换为十六进制字符串
        private static string ByteArrayToHexString(byte[] bytes, int offset, int count)
        {
            StringBuilder hex = new StringBuilder(count * 3);
            for (int i = offset; i < offset + count; i++)
            {
                hex.AppendFormat("{0:X2} ", bytes[i]);
            }
            return hex.ToString().TrimEnd();
        }

        // 辅助方法：转义不可打印字符
        private static string EscapeNonPrintable(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                if (c < 32 || c > 126)
                {
                    sb.Append($"\\x{(int)c:X2}");
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
} 