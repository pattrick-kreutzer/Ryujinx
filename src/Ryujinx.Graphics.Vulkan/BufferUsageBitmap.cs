﻿namespace Ryujinx.Graphics.Vulkan
{
    internal class BufferUsageBitmap
    {
        private BitMap _bitmap;
        private int _size;
        private int _granularity;
        private int _bits;
        private int _writeBitOffset;

        private int _intsPerCb;
        private int _bitsPerCb;

        public BufferUsageBitmap(int size, int granularity)
        {
            _size = size;
            _granularity = granularity;

            // There are two sets of bits - one for read tracking, and the other for write.
            int bits = (size + (granularity - 1)) / granularity;
            _writeBitOffset = bits;
            _bits = bits << 1;

            _intsPerCb = (_bits + (BitMap.IntSize - 1)) / BitMap.IntSize;
            _bitsPerCb = _intsPerCb * BitMap.IntSize;

            _bitmap = new BitMap(_bitsPerCb * CommandBufferPool.MaxCommandBuffers);
        }

        public void Add(int cbIndex, int offset, int size, bool write)
        {
            if (size == 0)
            {
                return;
            }

            // Some usages can be out of bounds (vertex buffer on amd), so bound if necessary.
            if (offset + size > _size)
            {
                size = _size - offset;
            }

            int cbBase = cbIndex * _bitsPerCb + (write ? _writeBitOffset : 0);
            int start = cbBase + offset / _granularity;
            int end = cbBase + (offset + size - 1) / _granularity;

            _bitmap.SetRange(start, end);
        }

        public bool OverlapsWith(int cbIndex, int offset, int size, bool write = false)
        {
            if (size == 0)
            {
                return false;
            }

            int cbBase = cbIndex * _bitsPerCb + (write ? _writeBitOffset : 0);
            int start = cbBase + offset / _granularity;
            int end = cbBase + (offset + size - 1) / _granularity;

            return _bitmap.IsSet(start, end);
        }

        public bool OverlapsWith(int offset, int size, bool write = false)
        {
            for (int i = 0; i < CommandBufferPool.MaxCommandBuffers; i++)
            {
                if (OverlapsWith(i, offset, size, write))
                {
                    return true;
                }
            }

            return false;
        }

        public void Clear(int cbIndex)
        {
            _bitmap.ClearInt(cbIndex * _intsPerCb, (cbIndex + 1) * _intsPerCb - 1);
        }
    }
}
