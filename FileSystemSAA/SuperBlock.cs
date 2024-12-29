using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemSAA
{
    internal class SuperBlock
    {
        public ushort BLOCK_SIZE { get; }
        public uint BLOCK_AMOUNT { get; }
        public uint FREE_BLOCKS { get; private set; }
        public ushort INODE_AMOUNT { get; }
        public ushort FREE_INODES { get; private set; }
        public uint FSYS_TOTAL_STORAGE { get; }
        public uint FSYS_AVAILABLE_STORAGE { get; private set; }
        public ushort INODE_BITMAP_POSITION { get; private set; }
        public ushort DATABLOCK_BITMAP_POSITION { get; private set; }
        public ushort INODE_TABLE_POSITION { get; private set; }
        public ushort FAT_POSITION { get; private set; }
        public ushort ROOT_DIR_POSITION { get; private set; }

        public SuperBlock(ushort blockSize, uint blockAmount, ushort inodeAmount)
        {
            BLOCK_SIZE = blockSize;
            BLOCK_AMOUNT = blockAmount;
            INODE_AMOUNT = inodeAmount;

            FREE_BLOCKS = blockAmount;
            FREE_INODES = inodeAmount;

            FSYS_TOTAL_STORAGE = blockAmount * blockSize;
            FSYS_AVAILABLE_STORAGE = FSYS_TOTAL_STORAGE;
        }

        public void AdjustPositions(MyFS fsys)
        {
            INODE_BITMAP_POSITION = GetSize();
            DATABLOCK_BITMAP_POSITION = (ushort)(INODE_BITMAP_POSITION + fsys.INodeBitmap.GetSize());
            INODE_TABLE_POSITION = (ushort)(DATABLOCK_BITMAP_POSITION + fsys.DataBlockBitmap.GetSize());
            FAT_POSITION = (ushort)(INODE_TABLE_POSITION + fsys.INodeTable.GetSize());
            ROOT_DIR_POSITION = (ushort)(FAT_POSITION + fsys.FAT.GetSize());
        }
        public void Write(MyFS fsys)
        {
            fsys._stream.Seek(0, SeekOrigin.Begin);

            fsys._writer.Write(BLOCK_SIZE);
            fsys._writer.Write(BLOCK_AMOUNT);
            fsys._writer.Write(FREE_BLOCKS);
            fsys._writer.Write(INODE_AMOUNT);
            fsys._writer.Write(FREE_INODES);
            fsys._writer.Write(FSYS_TOTAL_STORAGE);
            fsys._writer.Write(FSYS_AVAILABLE_STORAGE);
            fsys._writer.Write(INODE_BITMAP_POSITION);
            fsys._writer.Write(DATABLOCK_BITMAP_POSITION);
            fsys._writer.Write(INODE_TABLE_POSITION);
            fsys._writer.Write(FAT_POSITION);
            fsys._writer.Write(ROOT_DIR_POSITION);
        }
        public void Update(MyFS fsys, uint blocks, ushort inodes, UpdateOperation updateOperation)
        {
            switch (updateOperation)
            {
                case UpdateOperation.Add:
                    FREE_BLOCKS += blocks;
                    FREE_INODES += inodes;
                    break;
                case UpdateOperation.Subtract:
                    FREE_BLOCKS -= blocks;
                    FREE_INODES -= inodes;
                    break;
            }

            FSYS_AVAILABLE_STORAGE = FREE_BLOCKS * BLOCK_SIZE;

            long fsysLastPos = fsys._stream.Position;

            fsys._stream.Seek(sizeof(ushort) + sizeof(uint), SeekOrigin.Begin);
            fsys._writer.Write(FREE_BLOCKS);

            fsys._stream.Seek(sizeof(ushort), SeekOrigin.Current);
            fsys._writer.Write(FREE_INODES);

            fsys._stream.Seek(sizeof(uint), SeekOrigin.Current);
            fsys._writer.Write(FSYS_AVAILABLE_STORAGE);

            fsys._stream.Position = fsysLastPos;
        }
        public static ushort GetSize() => sizeof(short) * 8 + sizeof(int) * 4;
    }
}
