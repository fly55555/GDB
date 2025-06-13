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

        // 中断和停止事件
        public event Action<GdbPacket> OnStopReceived;
        public event Action OnResumed;
        

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

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await _tcpClient.ConnectAsync(host, port);
            _stream = _tcpClient.GetStream();
            
            _ = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token));

            // Query the target's status to get the initial halt reason.
            await SendPacketAsync(new GdbPacket("?"), cancellationToken);
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && IsConnected)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length, token);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    string receivedData = Encoding.ASCII.GetString(_receiveBuffer, 0, bytesRead);
                    _processingBuffer.Append(receivedData);
                    await ProcessBuffer();
                }
                catch (OperationCanceledException) 
                { 
                    break; 
                }
                catch (IOException ex) 
                { 
                    break; 
                }
                catch (Exception ex) 
                { 
                }
            }
        }

        private async Task ProcessBuffer()
        {
            // 存储此次处理过程中提取的所有数据包，用于检测重复
            var processedPacketsInThisRound = new HashSet<string>();

            while (_processingBuffer.Length > 0)
            {
                // 处理ACK信号
                if (_processingBuffer[0] == '+')
                {
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
                    _processingBuffer.Remove(0, 1);
                    // 这里应该重新发送上一个包，但目前实现中未保留这个信息
                    // TODO: 实现包重传机制
                    continue;
                }

                // 查找数据包开始标记
                int startIndex = _processingBuffer.ToString().IndexOf('$');
                if (startIndex == -1)
                {
                    _processingBuffer.Clear();
                    return;
                }
                
                // 移除开始标记前的所有数据
                if (startIndex > 0)
                {
                    _processingBuffer.Remove(0, startIndex);
                }

                // 查找数据包结束标记
                int endIndex = _processingBuffer.ToString().IndexOf('#');
                if (endIndex == -1 || _processingBuffer.Length < endIndex + 3)
                {
                    // 数据包不完整，等待更多数据
                    return;
                }

                // 提取完整的数据包
                string rawPacket = _processingBuffer.ToString(0, endIndex + 3);
                _processingBuffer.Remove(0, endIndex + 3);

                // 检查是否是当前处理循环中的重复包
                if (processedPacketsInThisRound.Contains(rawPacket))
                {
                    continue;
                }
                
                // 将包添加到已处理集合
                processedPacketsInThisRound.Add(rawPacket);

                // 处理完整的数据包
                await HandleFullPacket(rawPacket);
            }
        }

        private async Task HandleFullPacket(string rawPacket)
        {
            try
            {
                var packet = GdbPacket.Deserialize(rawPacket);
                
                // 发送ACK确认
                await _stream.WriteAsync(new byte[] { (byte)'+' }, 0, 1, _cancellationTokenSource.Token);
                // 将处理过的数据包加入队列（用于调试）
                _processedPackets.Enqueue(packet);
                while (_processedPackets.Count > MAX_PROCESSED_PACKETS && _processedPackets.TryDequeue(out _)) { }

                // 检查是否是停止通知（优先处理中断包）
                if (packet.Data.StartsWith("S") || packet.Data.StartsWith("T"))
                {
                    OnStopReceived?.Invoke(packet);
                    return;
                }

                // 如果没有待处理的命令，直接丢弃响应
                if (_pendingCommands.Count == 0)
                {
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
                            tcs.TrySetResult(packet);
                        }
                    }
                }
            }
            catch (Exception ex) 
            { 
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
                return false;
            }
            
            if (!packet.Data.StartsWith("O"))
            {
                return false;
            }
            
            // 查找正在等待响应的qRcmd命令
            var qRcmdCommands = _pendingCommands
                .Where(kv => kv.Key.Substring(kv.Key.IndexOf(':') + 1).StartsWith("qRcmd,"))
                .OrderBy(kv => int.Parse(kv.Key.Split(':')[0]))
                .ToList();
                
            if (qRcmdCommands.Count == 0)
            {
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
                return true;
            }
            
            outputBuffer.Append(packet.Data);
            return true;
        }

        // 处理qRcmd命令的完成信号
        private bool HandleQRcmdCompletion(GdbPacket packet)
        {
            // 查找正在等待响应的qRcmd命令
            var qRcmdCommands = _pendingCommands
                .Where(kv => kv.Key.Substring(kv.Key.IndexOf(':') + 1).StartsWith("qRcmd,"))
                .OrderBy(kv => int.Parse(kv.Key.Split(':')[0]))
                .ToList();
                
            if (qRcmdCommands.Count == 0)
            {
                return false;
            }
            
            // 只处理最早的一个qRcmd命令
            var kvp = qRcmdCommands.First();
            string commandId = kvp.Key;
            
            // 获取此命令累积的输出
            StringBuilder outputBuffer;
            if (_qRcmdOutputBuffers.TryRemove(commandId, out outputBuffer))
            {
                // 创建组合响应包
                string combinedData = outputBuffer.ToString();
                
                if (string.IsNullOrEmpty(combinedData))
                {
                    combinedData = "OK"; // 如果没有输出，就使用OK
                }
                
                GdbPacket combinedPacket = new GdbPacket(combinedData);
                
                // 完成命令
                TaskCompletionSource<GdbPacket> tcs;
                if (_pendingCommands.TryRemove(commandId, out tcs))
                {
                    tcs.TrySetResult(combinedPacket);
                    return true;
                }
            }
            else
            {
                TaskCompletionSource<GdbPacket> tcs;
                if (_pendingCommands.TryRemove(commandId, out tcs))
                {
                    tcs.TrySetResult(packet);
                    return true;
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
                        return true;
                    }
                }
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
            
                // 创建用于等待响应的TaskCompletionSource
                var responseTcs = new TaskCompletionSource<GdbPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingCommands[commandId] = responseTcs;

                // 如果是qRcmd命令，初始化输出缓冲区
                if (command.StartsWith("qRcmd,"))
                {
                    _qRcmdOutputBuffers[commandId] = new StringBuilder();
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
                        var completedTask = await Task.WhenAny(responseTcs.Task, Task.Delay(Timeout.Infinite, linkedCts.Token));
                    
                        if (completedTask == responseTcs.Task)
                        {
                            var response = await responseTcs.Task;
                            return response;
                        }
                        else
                        {
                            // 超时处理
                            _pendingCommands.TryRemove(commandId, out _);
                            _qRcmdOutputBuffers.TryRemove(commandId, out _); // 清除缓冲区
                            throw new TimeoutException($"Command timed out: {command}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 确保在出现异常时移除挂起的命令
                    _pendingCommands.TryRemove(commandId, out _);
                    _qRcmdOutputBuffers.TryRemove(commandId, out _); // 清除缓冲区
                
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
            await _stream.WriteAsync(packetBytes, 0, packetBytes.Length, cancellationToken);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _stream?.Dispose();
            _tcpClient?.Dispose();
            
            // 清除所有挂起的命令
            foreach (var kvp in _pendingCommands)
            {
                kvp.Value.TrySetCanceled();
            }
            _pendingCommands.Clear();
            _qRcmdOutputBuffers.Clear();
        }
    }
} 