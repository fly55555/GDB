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
}
