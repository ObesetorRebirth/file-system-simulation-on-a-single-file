using Enums;
using System.Text;

namespace FileSystemSAA
{
    internal class MyFS
    {
        public SuperBlock SuperBlock { get; }
        public Bitmap INodeBitmap { get; }
        public Bitmap DataBlockBitmap { get; }
        public INodeTable INodeTable { get; }
        public FileATable FAT { get; }
        public FileStream _stream { get; }
        public BinaryReader _reader { get; }
        public BinaryWriter _writer { get; }
        public string CurrentFilePath { get; private set; }
        //CONSTRUCTOR
        public MyFS(string fsPath, ushort blockSize, uint blockAmount, ushort inodeAmount)
        {
            SuperBlock = new(blockSize, blockAmount, inodeAmount);

            _stream = new(fsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            _reader = new(_stream, Encoding.ASCII);
            _writer = new(_stream, Encoding.ASCII);

            INodeBitmap = new Bitmap(this, BmDesignation.INodeTable);
            DataBlockBitmap = new Bitmap(this, BmDesignation.DataBlocks);
            INodeTable = new INodeTable(this);
            FAT = new FileATable(this);

            SuperBlock.AdjustPositions(this);

            _stream.SetLength(SuperBlock.ROOT_DIR_POSITION + SuperBlock.BLOCK_AMOUNT * SuperBlock.BLOCK_SIZE);

            SuperBlock.Write(this);
            int inodeIndex = INodeBitmap.GetFreeBit();
            int blockIndex = DataBlockBitmap.GetFreeBit();

            DirectoryFile Root = new DirectoryFile(_stream, _reader, _writer, blockIndex);
            Root.SetupDirFile((short)inodeIndex, -1);
            CurrentFilePath = "Root";
            FAT.WriteFAT(blockIndex);
            INodeTable[inodeIndex] = new INode(FileType.DirectoryFile, SuperBlock.BLOCK_SIZE, 0);
            SuperBlock.Update(this, 1, 1, UpdateOperation.Subtract);
        }
        //PROGRAM FUNCTIONS
        public void MakeDirectory(string input)
        {

            string filePath = SetUpFilePath(input);
            DirectoryFile parentDir = GetParentDir(filePath);

            string[] filePathArr = DissectFilePath(filePath);
            string dirName = filePathArr[^1];
            if (!parentDir.ContainsFile(dirName))
            {
                if (!parentDir.HasSpace())
                {
                    Console.WriteLine("Directory is out of space!");
                    return;
                }

                int blockIndex = DataBlockBitmap.GetFreeBit();
                int inodeIndex = INodeBitmap.GetFreeBit();

                parentDir[parentDir.GetFreeIndex()] = new DirectoryEntry(dirName, (short)inodeIndex);

                _stream.Position = 0;
                uint blockSize = (uint)_reader.ReadInt16();
                INodeTable[inodeIndex] = new INode(FileType.DirectoryFile, blockSize, (uint)blockIndex);

                FAT.WriteFAT(blockIndex);

                DirectoryFile newDir = ReadDirBlock(blockIndex);
                newDir.SetupDirFile((short)inodeIndex, parentDir[0].INodeIndex);
                SuperBlock.Update(this, 1, 1, UpdateOperation.Subtract);
            }
            else
            {
                Console.WriteLine("Error");
                return;
            }
        }
        public void RemoveDirectory(string input)
        {
            string filePath = SetUpFilePath(input);
            DirectoryFile dir = GetDir(filePath);

            bool isEmpty = true;
            for (int i = 2; i < dir.Length; i++)
            {
                if (dir[i].FileName != "")
                    isEmpty = false; break;
            }
            if (isEmpty == true)
            {
                string[] filePathArr = DissectFilePath(filePath);
                short iNodeIndex = 0;
                int dirEntryIndex = 0;
                string dirName = filePathArr[^1];
                DirectoryFile parentDir = GetParentDir(filePath);

                for (int i = 2; i < parentDir.Length; i++)
                    if (parentDir[i].FileName == dirName)
                    {
                        dirEntryIndex = i;
                        iNodeIndex = parentDir[i].INodeIndex;
                        break;
                    }
                INodeBitmap.DeallocateBlock(iNodeIndex);
                INodeTable[iNodeIndex] = new INode(default, default, default);
                for (int i = 0; i < dir.Blocks.Length; i++)
                    DataBlockBitmap.DeallocateBlock(dir.Blocks[i]);

                FAT.ClearFAT(dir.Blocks[0]);
                parentDir[dirEntryIndex] = new DirectoryEntry("", default);
                SuperBlock.Update(this, 1, 1, UpdateOperation.Add);
            }
            else
            {
                Console.WriteLine("First, empty the directory!");
                return;
            }
        }
        public void ListDirectoryFiles()
        {
            DirectoryFile currDir = GetDir(CurrentFilePath);

            Console.WriteLine("Files in current directory:");
            for (int i = 0; i < currDir.Length; i++)
                if (currDir[i].FileName != "")
                    Console.WriteLine($"{i + 1}. {currDir[i].FileName}");
        }
        public void ChangeDirectory(string input)
        {
            string filePath = SetUpFilePath(input);

            int block = GetFileBlockIndex(filePath);
            string[] filePathArr = DissectFilePath(filePath);

            switch (filePathArr[^1])
            {
                case ".":
                    return;
                case "..":
                    CurrentFilePath = ExcludeLastFile(CurrentFilePath);
                    break;
                default:
                    if (GetFileType(block) == FileType.DirectoryFile)
                        CurrentFilePath = filePath;
                    else
                        return;
                    break;
            }
        }
        public void ShowFileContent(string input)
        {
            string filePath = SetUpFilePath(input);

            int block = GetFileBlockIndex(filePath);
            int[] blocks = FAT.ReadFAT(block);

            for (int i = 0; i < blocks.Length; i++)
            {
                byte[] data = ReadDataBlock((uint)blocks[i]);

                int length = 0;
                for (int j = 0; j < data.Length; j++)
                    if (data[j] == (byte)'\0')
                    {
                        length = j;
                        break;
                    }

                string content = Encoding.ASCII.GetString(data, 0, length);

                Console.WriteLine(content);
            }
        }
        public void MakeFile(string input, string content)
        {
            string filePath = SetUpFilePath(input);

            DirectoryFile parentDir = GetParentDir(filePath);

            string[] filePathAsArr = DissectFilePath(filePath);
            string fileName = filePathAsArr[^1];

            if (!parentDir.ContainsFile(fileName))
            {
                if (!parentDir.HasSpace())
                    throw new Exception("Not enough storage in the directory");

                byte[] data = Encoding.ASCII.GetBytes(content);

                _stream.Position = 0;
                short blockSize = _reader.ReadInt16();

                int blocksRequired = (data.Length + (blockSize - 1)) / blockSize;

                int[] blocks = DataBlockBitmap.GetFreeBits(blocksRequired);
                int inodeIndex = INodeBitmap.GetFreeBit();

                parentDir[parentDir.GetFreeIndex()] = new DirectoryEntry(fileName, (short)inodeIndex);

                INodeTable[inodeIndex] = new INode(FileType.DataFile, (uint)data.Length, (uint)blocks[0]);

                FAT.WriteFAT(blocks[0], blocks);

                int byteIndex = 0;
                byte[] buffer = new byte[blockSize];
                for (int i = 0; i < blocks.Length; i++)
                {
                    for (int j = 0; j < buffer.Length; j++)
                    {
                        buffer[j] = data[byteIndex++];
                        if (byteIndex >= data.Length)
                            break;
                    }

                    WriteDataBlock((uint)blocks[i], buffer);
                }

                SuperBlock.Update(this, (uint)blocks.Length, 1, UpdateOperation.Subtract);
            }
            else
            {
                int inodeIndex = -1;
                for (int i = 0; i < parentDir.Length; i++)
                    if (parentDir[i].FileName == fileName)
                    {
                        inodeIndex = parentDir[i].INodeIndex;
                        break;
                    }

                byte[] data = Encoding.ASCII.GetBytes(content);

                int blockIndex = (int)INodeTable[inodeIndex].FileBlockIndex;

                INodeTable[inodeIndex] = new INode(FileType.DataFile, (uint)data.Length, (uint)blockIndex);

                int[] blocksToDealocate = FAT.ReadFAT(blockIndex);
                for (int i = 0; i < blocksToDealocate.Length; i++)
                    DataBlockBitmap.DeallocateBlock(blocksToDealocate[i]);

                SuperBlock.Update(this, (uint)blocksToDealocate.Length, 0, UpdateOperation.Add);

                _stream.Position = 0;
                short blockSize = _reader.ReadInt16();

                int blocksRequired = (data.Length + (blockSize - 1)) / blockSize;
                int[] blocks = DataBlockBitmap.GetFreeBits(blocksRequired);

                FAT.ClearFAT(blockIndex);
                FAT.WriteFAT(blockIndex, blocks);

                int byteIndex = 0;
                byte[] buffer = new byte[blockSize];
                for (int i = 0; i < blocks.Length; i++)
                {
                    for (int j = 0; j < buffer.Length; j++)
                    {
                        buffer[j] = data[byteIndex++];
                        if (byteIndex >= data.Length)
                            break;
                    }

                    WriteDataBlock((uint)blocks[i], buffer);
                }

                SuperBlock.Update(this, (uint)blocks.Length, 0, UpdateOperation.Subtract);
            }
        }
        public void AppendToFile(string input, string content)
        {
            string filePath = SetUpFilePath(input);

            DirectoryFile parentDir = GetParentDir(filePath);

            string[] filePathAsArr = DissectFilePath(filePath);
            string fileName = filePathAsArr[^1];

            int inodeIndex = -1;

            for (int i = 0; i < parentDir.Length; i++)
                if (parentDir[i].FileName == fileName)
                {
                    inodeIndex = parentDir[i].INodeIndex;
                    break;
                }

            byte[] data = Encoding.ASCII.GetBytes(content);

            int oldSize = (int)INodeTable[inodeIndex].FileSize;
            int blockIndex = (int)INodeTable[inodeIndex].FileBlockIndex;

            INodeTable[inodeIndex] = new INode(FileType.DataFile, (uint)(oldSize + data.Length), (uint)blockIndex);

            int[] blocks = FAT.ReadFAT(blockIndex);
            byte[] lastBlock = ReadDataBlock((uint)blocks[^1]);
            int dataIndex = 0;

            for (int i = 0; i < lastBlock.Length; i++)
                if (lastBlock[i] == 0)
                {
                    lastBlock[i] = data[dataIndex++];
                    if (dataIndex >= data.Length)
                        break;
                }

            WriteDataBlock((uint)blocks[^1], lastBlock);

            if (dataIndex < data.Length)
            {
                int dataNewSize = data.Length - dataIndex;

                _stream.Position = 0;
                short blockSize = _reader.ReadInt16();

                int blocksRequired = (dataNewSize + (blockSize - 1)) / blockSize;
                int[] newBlocks = DataBlockBitmap.GetFreeBits(blocksRequired);

                FAT.WriteFAT(blocks[^1], newBlocks);

                byte[] buffer = new byte[blockSize];
                for (int i = 0; i < newBlocks.Length; i++)
                {
                    for (int j = 0; j < buffer.Length; j++)
                    {
                        buffer[j] = data[dataIndex++];
                        if (dataIndex >= data.Length)
                            break;
                    }

                    WriteDataBlock((uint)newBlocks[i], buffer);
                }

                SuperBlock.Update(this, (uint)newBlocks.Length, 0, UpdateOperation.Subtract);
            }
        }
        //GET FUNCTIONS
        private DirectoryFile GetDir(string filePath)
        {
            int index = GetFileBlockIndex(filePath);
            int[] blocks = FAT.ReadFAT(index);
            return ReadDirBlocks(blocks);
        }
        private int GetFileBlockIndex(string filePath)
        {
            string[] dirArr = DissectFilePath(filePath);
            int dirArrIndex = 1;

            DirectoryFile currDir = GetDir(0);

            while (dirArrIndex < dirArr.Length)
                for (int i = 0; i < currDir.Length; i++)
                    if (currDir[i].FileName == dirArr[dirArrIndex])
                    {
                        int inodeIndex = currDir[i].INodeIndex;
                        int dirBlockIndex = (int)INodeTable[inodeIndex].FileBlockIndex;
                        int[] blocks = FAT.ReadFAT(dirBlockIndex);
                        currDir = ReadDirBlocks(blocks);
                        dirArrIndex++;
                        break;
                    }

            return currDir.Blocks[0];
        }
        private DirectoryFile GetDir(int index)
        {
            int[] blocks = FAT.ReadFAT(index);
            return ReadDirBlocks(blocks);
        }
        private DirectoryFile GetParentDir(string filePath)
        {
            string parentFilePath = ExcludeLastFile(filePath);
            return GetDir(parentFilePath);
        }
        private FileType GetFileType(int index)
        {
            byte[] data = ReadDataBlock((uint)index);

            int length = 0;
            for (int i = 0; i < data.Length; i++)
                if (data[i] == (byte)'\0')
                {
                    length = i;
                    break;
                }

            string file = Encoding.ASCII.GetString(data, 0, length);

            if (file[0] == '.' && length == 1)
                return FileType.DirectoryFile;
            else
                return FileType.DataFile;
        }
        private int GetFileIndex(string filePath)
        {
            DirectoryFile parentDir = GetParentDir(filePath);

            string[] filePathAsArr = DissectFilePath(filePath);
            string fileName = filePathAsArr[^1];

            for (int i = 0; i < parentDir.Length; i++)
                if (parentDir[i].FileName == fileName)
                    return parentDir[i].INodeIndex;

            return -1;
        }
        //READ AND WRITE FUNCTIONS
        private DirectoryFile ReadDirBlock(int block) => new(_stream, _reader, _writer, block);
        private DirectoryFile ReadDirBlock(int[] blocks) => new DirectoryFile(_stream, _reader, _writer, blocks);
        private DirectoryFile ReadDirBlocks(int[] blocks) => new DirectoryFile(_stream, _reader, _writer, blocks);
        private byte[] ReadDataBlock(uint index)
        {
            _stream.Position = 0;
            short blockSize = _reader.ReadInt16();

            byte[] blockData = new byte[SuperBlock.BLOCK_SIZE];

            _stream.Seek(SuperBlock.ROOT_DIR_POSITION + index * SuperBlock.BLOCK_SIZE, SeekOrigin.Begin);
            _reader.Read(blockData, 0, SuperBlock.BLOCK_SIZE);

            return blockData;
        }
        private void WriteDataBlock(uint index, byte[] data)
        {
            _stream.Seek(SuperBlock.ROOT_DIR_POSITION + index * SuperBlock.BLOCK_SIZE, SeekOrigin.Begin);
            _writer.Write(data);
        }
        //TEXT FUNCTIONS
        private string SetUpFilePath(string input)
        {
            string temp = "";
            
            if (!ContainsFileSeparator(input))
            {
                if (input == "Root")
                {
                    return input;
                }
                
                temp += CurrentFilePath+"\\"+input;
                return temp;
            }
            else
            {
                return input;
            }
        }
        
        private bool ContainsFileSeparator(string input)
        {
            for (int i = 0; i < input.Length; i++)
                if (input[i] == '\\')
                    return true;

            return false;
        }
        public static string[]? DissectCommand(string command)
        {
            string result = "";

            int wordsCounter = 0;

            for (int i = 0; i < command.Length; i++)
                if (command[i] == '"')
                {
                    wordsCounter++;

                    for (int j = i; j < command.Length; j++)
                        if (command[j] == '"' && j != i || j + 1 >= command.Length)
                        {
                            i = j;
                            break;
                        }

                    continue;
                }
                else if (command[i] != ' ')
                {
                    wordsCounter++;

                    for (int j = i; j < command.Length; j++)
                        if (command[j] == ' ' || j + 1 >= command.Length)
                        {
                            i = j;
                            break;
                        }
                }

            string[] commandDissected = new string[wordsCounter];
            int word = 0;

            while (word < commandDissected.Length)
            {
                for (int i = 0; i < command.Length; i++)
                {
                    if (command[i] == ' ')
                    {
                        if (result.Length > 0)
                        {
                            commandDissected[word++] = result;
                            result = "";
                        }
                    }
                    else if (command[i] == '"')
                    {
                        for (int j = i; j < command.Length; j++)
                        {
                            if (command[j] == '"' && i != j && result.Length > 0)
                            {
                                commandDissected[word++] = result;
                                result = "";
                                i = j;
                                break;
                            }

                            if (command[j] != '"')
                                result += command[j];

                            if (j + 1 >= command.Length && command[i] != '"' || j + 1 >= command.Length && command[i] == '"')
                            {
                                Console.WriteLine("Invalid use of <\" \"> for literal argument. ");
                                return null;
                            }
                        }
                    }
                    else
                        result += command[i];
                }
                if (result.Length > 0)
                {
                    commandDissected[word++] = result;
                    result = "";
                }
            }

            return commandDissected;
        }
        private string[] DissectFilePath(string filePath)
        {
            StringBuilder sb = new();

            int filesCounter = 1;

            for (int i = 0; i < filePath.Length; i++)
                if (filePath[i] == '\\')
                    filesCounter++;

            string[] filePathDissected = new string[filesCounter];
            int file = 0;

            for (int i = 0; i < filePath.Length; i++)
            {
                if (filePath[i] == '\\')
                {
                    filePathDissected[file++] = sb.ToString();
                    sb.Clear();
                }
                else
                    sb.Append(filePath[i]);
            }

            filePathDissected[file] = sb.ToString();
            sb.Clear();

            return filePathDissected;
        }
        private string ExcludeLastFile(string filePath)
        {
            StringBuilder sb = new(filePath);
            int slashIndex = filePath.Length - 1;

            for (; slashIndex >= 0; slashIndex--)
                if (filePath[slashIndex] == '\\')
                    break;

            int charsToRemove = filePath.Length - slashIndex;

            sb.Remove(slashIndex, charsToRemove);

            return sb.ToString();
        }
        
    }
}
