using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.Symbol
{
    public class RemoteStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position { get; set; }

        private readonly long _startAddress;
        private readonly long _length;
        private readonly IDebugControl _debugControl;

        public RemoteStream(IDebugControl debugControl, long startAddress, long length = 0x1000000)
        {
            _startAddress = startAddress;
            _length = length;
            _debugControl = debugControl;
            Position = 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }
            return Position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - offset < count)
                throw new ArgumentException("Invalid offset and length.");

            long currentAddress = _startAddress + Position;
            int bytesToRead = (int)Math.Min(count, _length - Position);

            if (bytesToRead <= 0)
            {
                return 0; // End of stream
            }
            
            // This is a sync-over-async operation. It blocks the current thread.
            // This is acceptable if symbol loading is on a background thread.
            byte[] data = _debugControl.ReadVirtual(currentAddress, bytesToRead).Result;
            
            if (data == null || data.Length == 0)
            {
                return 0; // GDB failed to read, treat as end of stream
            }

            Array.Copy(data, 0, buffer, offset, data.Length);
            Position += data.Length;
            return data.Length;
        }

        /// <summary>
        /// 暂时不允许写入
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("This stream does not support writing.");
        }

        /// <summary>
        /// 不支持设置长度
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("This stream does not support setting length.");
        }

        /// <summary>
        /// 所有操作都是立即的 无需刷新
        /// </summary>
        public override void Flush()
        {
            // No-op
        }
    }

    /// <summary>
    /// 异步远程内存流，避免死锁问题
    /// </summary>
    public class RemoteStreamAsync : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position { get; set; }

        private readonly long _startAddress;
        private readonly long _length;
        private readonly IDebugControl _debugControl;
        private byte[] _cachedData;
        private long _cachedOffset = -1;
        private const int CACHE_SIZE = 4096; // 4KB缓存

        public RemoteStreamAsync(IDebugControl debugControl, long startAddress, long length = 0x1000000)
        {
            _startAddress = startAddress;
            _length = length;
            _debugControl = debugControl;
            Position = 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }
            return Position;
        }

        /// <summary>
        /// 同步读取方法 - 内部使用异步方法实现，避免在UI线程上调用.Result
        /// 采用ConfigureAwait(false)确保不会尝试回到原始同步上下文
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // 使用异步方法但以非阻塞方式运行，这会略慢些但不会死锁
            return ReadAsync(buffer, offset, count).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步读取方法 - 避免死锁问题
        /// </summary>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken = default)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - offset < count)
                throw new ArgumentException("Invalid offset and length.");

            long currentAddress = _startAddress + Position;
            int bytesToRead = (int)Math.Min(count, _length - Position);

            if (bytesToRead <= 0)
            {
                return 0; // End of stream
            }

            // 实现简单的缓存机制，减少远程读取次数
            if (_cachedData != null && 
                currentAddress >= _cachedOffset && 
                currentAddress + bytesToRead <= _cachedOffset + _cachedData.Length)
            {
                // 缓存命中，直接从缓存读取
                int cacheOffset = (int)(currentAddress - _cachedOffset);
                int bytesToCopy = Math.Min(bytesToRead, _cachedData.Length - cacheOffset);
                Array.Copy(_cachedData, cacheOffset, buffer, offset, bytesToCopy);
                Position += bytesToCopy;
                return bytesToCopy;
            }

            // 缓存未命中，读取新数据并缓存
            // 读取比请求更大的块以提高缓存效率
            int cacheSize = Math.Max(CACHE_SIZE, bytesToRead);
            byte[] data = await _debugControl.ReadVirtual(currentAddress, cacheSize).ConfigureAwait(false);
            
            if (data == null || data.Length == 0)
            {
                return 0; // GDB failed to read, treat as end of stream
            }

            // 更新缓存
            _cachedData = data;
            _cachedOffset = currentAddress;

            // 复制请求的数据
            int bytesToReturn = Math.Min(bytesToRead, data.Length);
            Array.Copy(data, 0, buffer, offset, bytesToReturn);
            Position += bytesToReturn;
            return bytesToReturn;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("This stream does not support writing.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("This stream does not support setting length.");
        }

        public override void Flush()
        {
            // No-op
        }
    }
}
