using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.Symbole
{
    public class RemoteStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => Size;

        public override long Position { get; set; }


        /// <summary>
        /// 总大小
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 起始地址
        /// </summary>
        public long StartPoint { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IDebugControl DebugControl { get; set; }


        public RemoteStream(IDebugControl debugControl, long offset, long size = 0x1000000)
        {
            Size = size;
            StartPoint = offset;
            DebugControl = debugControl;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
                Position = offset;

            if (origin == SeekOrigin.Current)
                Position += offset;

            if (origin == SeekOrigin.End)
                Position = Length - offset;

            return Position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var data = DebugControl.ReadVirtual(offset + Position + StartPoint, count);
            Array.Copy(data, buffer, data.Length);
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
            return;
        }

        /// <summary>
        /// 不支持设置长度
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            return;
        }

        /// <summary>
        /// 所有操作都是立即的 无需刷新
        /// </summary>
        public override void Flush()
        {
            return;
        }
    }
}
