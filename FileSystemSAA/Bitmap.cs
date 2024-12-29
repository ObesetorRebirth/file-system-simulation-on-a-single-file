using System;
using Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemSAA
{
    internal class Bitmap
    {
        public uint Length { get; }
        public uint ByteArrayLength { get; }
        private readonly BmDesignation designation;
        private readonly MyFS fsys;

        public Bitmap(MyFS fsys, BmDesignation designation)
        {
            Length = designation switch
            {
                BmDesignation.DataBlocks => fsys.SuperBlock.BLOCK_AMOUNT,
                BmDesignation.INodeTable => fsys.SuperBlock.INODE_AMOUNT,
                _ => 0
            };

            ByteArrayLength = (Length + 7) / 8;

            this.fsys = fsys;
            this.designation = designation;
        }
        //ALLOCATION FUNCTIONS
        public void AllocateBlock(int index)
        {
            byte[] bitmap = ReadBitmap();

            int byteIndex = index / 8;
            int bitOffset = index % 8;

            bitmap[byteIndex] |= (byte)(1 << bitOffset);

            WriteBitmap(bitmap);
        }
        public void DeallocateBlock(int index)
        {
            byte[] bitmap = ReadBitmap();

            int byteIndex = index / 8;
            int bitOffset = index % 8;

            bitmap[byteIndex] &= (byte)~(1 << bitOffset);

            WriteBitmap(bitmap);
        }
        public bool IsBlockAllocated(int index)
        {
            byte[] bitmap = ReadBitmap();

            int byteIndex = index / 8;
            int bitOffset = index % 8;

            return (bitmap[byteIndex] & (1 << bitOffset)) != 0;
        }
        //GET FUNCTIONS
        public int GetFreeBit()
        {
            for (int i = 0; i < Length; i++)
            {
                if (!IsBlockAllocated(i))
                {
                    AllocateBlock(i);
                    return i;
                }
            }

            throw new Exception("MyFS has no free space left, initiate defragmentation?");
        }
        public int[] GetFreeBits(int length)
        {
            int[] bits = new int[length];
            int bitsIndex = 0;

            for (int i = 0; i < Length; i++)
            {
                if (!IsBlockAllocated(i))
                {
                    AllocateBlock(i);
                    bits[bitsIndex++] = i;
                    if (bitsIndex >= bits.Length)
                        break;
                }
            }

            for (int i = 0; i < bits.Length; i++)
                if (bits[i] == 0)
                    throw new Exception("MyFS has no free space left, initiate defragmentation?");

            return bits;
        }
        //READ AND WRITE FUNCTIONS
        private byte[] ReadBitmap()
        {
            byte[] bitmap = new byte[ByteArrayLength];

            fsys._stream.Position = designation switch
            {
                BmDesignation.DataBlocks => fsys.SuperBlock.DATABLOCK_BITMAP_POSITION,
                BmDesignation.INodeTable => fsys.SuperBlock.INODE_BITMAP_POSITION,
                _ => 0
            };
            fsys._reader.Read(bitmap, 0, (int)ByteArrayLength);

            return bitmap;
        }
        private void WriteBitmap(byte[] bitmap)
        {
            fsys._stream.Position = designation switch
            {
                BmDesignation.DataBlocks => fsys.SuperBlock.DATABLOCK_BITMAP_POSITION,
                BmDesignation.INodeTable => fsys.SuperBlock.INODE_BITMAP_POSITION,
                _ => 0
            };
            fsys._writer.Write(bitmap);
        }

        public ushort GetSize() => (ushort)ByteArrayLength;
    }
}
