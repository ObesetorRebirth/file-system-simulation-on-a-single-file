using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemSAA
{
    internal class FileATable(MyFS fsys)
    {
        public uint Length { get; } = fsys.SuperBlock.BLOCK_AMOUNT;

        private readonly MyFS fsys = fsys;

        public int this[int index]
        {
            get
            {
                if (index < 0 || index > Length)
                    throw new IndexOutOfRangeException();

                fsys._stream.Seek(fsys.SuperBlock.FAT_POSITION + index * sizeof(int), SeekOrigin.Begin);

                return fsys._reader.ReadInt32();
            }
            set
            {
                if (index < 0 || index > Length)
                    throw new IndexOutOfRangeException();

                fsys._stream.Seek(fsys.SuperBlock.FAT_POSITION + index * sizeof(int), SeekOrigin.Begin);

                fsys._writer.Write(value);
            }
        }
        public int[] ReadFAT(int firstIndex)
        {
            CustomList<int> blocksList = new();

            while (this[firstIndex] != -1)
            {
                blocksList.Add(firstIndex);
                firstIndex = (int)this[firstIndex];
            }
            blocksList.Add(firstIndex);

            return blocksList.ToArray();
        }
        public void WriteFAT(int index) => this[index] = -1;
        public void WriteFAT(int lastIndex, int blockToWrite)
        {
            this[lastIndex] = blockToWrite;
            this[blockToWrite] = -1;
        }
        public void WriteFAT(int firstIndex, int[] blocksToWrite)
        {
            int current = firstIndex;

            if (this[current] != 0)
                while (this[current] != -1)
                    current = (int)this[current];

            for (int i = 0; i < blocksToWrite.Length; i++)
            {
                this[current] = blocksToWrite[i];
                current = blocksToWrite[i];
            }

            this[current] = -1;
        }
        public void ClearFAT(int firstIndex)
        {
            while (this[firstIndex] != -1)
            {
                int temp = (int)this[firstIndex];
                this[firstIndex] = -1;
                firstIndex = temp;
            }
        }
        public ushort GetSize() => (ushort)(Length * sizeof(int));
    }
}
