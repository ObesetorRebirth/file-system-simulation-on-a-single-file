namespace FileSystemSAA
{
    internal class DirectoryFile
    {
        public int Length { get; }
        public int[] Blocks { get; private set; }
        private int blocksNum;
        public uint DIRFILE_BLOCK_INDEX { get; }

        public static ushort FILENAME_SIZE { get; } = 30;
        private readonly ushort DIRENTRY_SIZE = DirectoryEntry.GetSize();
        private readonly int BLOCK_MAX_ENTRIES;

        private readonly MyFS fsys;
        private readonly FileStream _stream;
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;

        public DirectoryFile(FileStream _stream, BinaryReader _reader, BinaryWriter _writer, int block)
        {
            this._stream = _stream;
            this._reader = _reader;
            this._writer = _writer;

            _stream.Position = 0;
            short blockSize = _reader.ReadInt16();

            Length = blockSize / DIRENTRY_SIZE;
            Blocks = [block];
            BLOCK_MAX_ENTRIES = blockSize / DIRENTRY_SIZE;
        }
        public DirectoryFile(FileStream _stream, BinaryReader _reader, BinaryWriter _writer, int[] blocks)
        {
            this._stream = _stream;
            this._reader = _reader;
            this._writer = _writer;

            _stream.Position = 0;
            short blockSize = _reader.ReadInt16();

            Length = blocks.Length * blockSize / DIRENTRY_SIZE;
            Blocks = blocks;
            BLOCK_MAX_ENTRIES = blockSize / DIRENTRY_SIZE;
        }
        //INDEXER
        public DirectoryEntry this[int index]
        {
            get
            {
                if (index < 0 || index > Length)
                    throw new IndexOutOfRangeException();

                blocksNum = 0;
                int actualIndex = GetBlockIndex(index);

                _stream.Position = 0;
                short blockSize = _reader.ReadInt16();

                _stream.Seek(sizeof(short) * 7 + sizeof(int) * 4, SeekOrigin.Begin);
                short rootPos = _reader.ReadInt16();

                _stream.Seek(rootPos + Blocks[blocksNum] * blockSize + actualIndex * DIRENTRY_SIZE, SeekOrigin.Begin);

                return new DirectoryEntry
                    (
                    CharsToString(_reader.ReadChars(FILENAME_SIZE)),
                    _reader.ReadInt16()
                    );
            }
            set
            {
                if (index < 0 || index > Length)
                    throw new IndexOutOfRangeException();

                blocksNum = 0;
                int actualIndex = GetBlockIndex(index);

                _stream.Position = 0;
                short blockSize = _reader.ReadInt16();

                _stream.Seek(sizeof(short) * 7 + sizeof(int) * 4, SeekOrigin.Begin);
                short rootPos = _reader.ReadInt16();

                _stream.Seek(rootPos + Blocks[blocksNum] * blockSize + actualIndex * DIRENTRY_SIZE, SeekOrigin.Begin);

                _writer.Write(StringToChars(value.FileName, FILENAME_SIZE));
                _writer.Write(value.INodeIndex);
            }
        }
        public bool ContainsFile(string fileName)
        {
            for (int i = 2; i < Length; i++)
                if (this[i].FileName == fileName)
                    return true;

            return false;
        }
        public bool HasSpace()
        {
            for (int i = 2; i < Length; i++)
                if (this[i].FileName == "")
                    return true;
            return false;
        }
        private int GetBlockIndex(int index)
        {
            if (index >= BLOCK_MAX_ENTRIES)
            {
                blocksNum++;
                GetBlockIndex(index -= BLOCK_MAX_ENTRIES);
            }
            return index;
        }
        //. .. Directory and Parent Directory Setup
        public void SetupDirFile(short inodeIndex, short parentINodeIndex)
        {
            this[0] = new DirectoryEntry(".", inodeIndex);
            this[1] = new DirectoryEntry("..", parentINodeIndex);
        }
        //Tursene na SvobodenSlot v Directory
        public int GetFreeIndex()
        {
            for (int i = 0; i < Length; i++)
                if (this[i].FileName == "")
                    return i;

            throw new Exception($"No space in directory. Current num of files is {Length}");
        }
        //Utility Methodi
        private static string CharsToString(char[] chars)
        {
            int i;
            for (i = 0; i < chars.Length && chars[i] != 0; i++) ;
            return new string(chars, 0, i);
        }
        private static char[] StringToChars(string str, int length)
        {
            char[] chars = new char[length];
            char[] info = str.ToCharArray();
            for (int i = 0; i < Math.Min(length, info.Length); i++)
            {
                chars[i] = info[i];
            }
            return chars;
        }
    }
    public class DirectoryEntry
    {
        public string FileName { get; }
        public short INodeIndex { get; }
        public DirectoryEntry(string fileName, short inodeIndex)
        {
            FileName = fileName;
            INodeIndex = inodeIndex;
        }
        public static ushort GetSize() => 30 + sizeof(short);
    }

}
