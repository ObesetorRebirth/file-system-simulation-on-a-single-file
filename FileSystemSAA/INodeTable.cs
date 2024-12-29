using System;
using Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemSAA
{
    internal class INodeTable(MyFS fsys)
    {
        public uint Length { get; } = fsys.SuperBlock.INODE_AMOUNT;

        private readonly ushort INODE_SIZE = INode.GetSize();
        private readonly MyFS fsys = fsys;

        public INode this[int index]
        {
            get
            {
                if (index < 0 || index > Length)
                    throw new IndexOutOfRangeException();

                fsys._stream.Seek(fsys.SuperBlock.INODE_TABLE_POSITION + index * INODE_SIZE, SeekOrigin.Begin);

                return new INode
                    (
                    (FileType)fsys._reader.ReadByte(),
                    (uint)fsys._reader.ReadInt32(),
                    (uint)fsys._reader.ReadInt32()
                    );
            }
            set
            {
                if (index < 0 || index > Length)
                    throw new IndexOutOfRangeException();

                fsys._stream.Seek(fsys.SuperBlock.INODE_TABLE_POSITION + index * INODE_SIZE, SeekOrigin.Begin);

                fsys._writer.Write(value.FileType);
                fsys._writer.Write(value.FileSize);
                fsys._writer.Write(value.FileBlockIndex);
            }
        }

        public ushort GetSize() => (ushort)(INODE_SIZE * Length);
    }

    public class INode(FileType fileType, uint fileSize, uint fileBlockIndex)
    {
        public byte FileType { get; } = (byte)fileType;
        public uint FileSize { get; } = fileSize;
        public uint FileBlockIndex { get; } = fileBlockIndex;

        public static ushort GetSize() => sizeof(byte) + sizeof(int) + sizeof(int);
    }
}

