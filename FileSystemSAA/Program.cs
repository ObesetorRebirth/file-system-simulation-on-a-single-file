using FileSystemSAA;

internal class Program
{
    private static void Main(string[] args)
    {
        MyFS fsys = new MyFS(@"C:\Users\vikdi\SAA\fsys", 512, 200, 20);

        Console.WriteLine("mkdir (creates an empty directory) filepath \\ Directory name");
        Console.WriteLine("--------------------------------------------------------------");
        Console.WriteLine("rmdir (removes an empty directory) filepath \\ Directory name");
        Console.WriteLine("--------------------------------------------------------------");
        Console.WriteLine("cd (changes the current directory)filepath \\ Directory name ");
        Console.WriteLine("--------------------------------------------------------------");
        Console.WriteLine("ls (lists all directories or files in the current directory)");
        Console.WriteLine("--------------------------------------------------------------");
        Console.WriteLine("write filepath \"<content>\" or write append <filepath> \"<content>\"");
        Console.WriteLine("--------------------------------------------------------------");
        Console.WriteLine("cat (shows file content) filepath");
        Console.WriteLine("--------------------------------------------------------------");

        bool inFileSys = true;
        while (inFileSys)
        {
            Console.Write($"{fsys.CurrentFilePath} : ");

            string[] commandDissected;
            string? command = Console.ReadLine();
            if (command != null || command != "")
            {
                commandDissected = MyFS.DissectCommand(command);

                if (commandDissected.Length > 1 || commandDissected[0] == "ls")
                {
                    switch (commandDissected[0])
                    {
                        case "mkdir":
                            fsys.MakeDirectory(commandDissected[1]);
                            break;
                        case "rmdir":
                            fsys.RemoveDirectory(commandDissected[1]);
                            break;
                        case "ls":
                            fsys.ListDirectoryFiles();
                            break;
                        case "cd":
                            fsys.ChangeDirectory(commandDissected[1]);
                            break;
                        case "write":
                            if (commandDissected[1] == "append")
                                fsys.AppendToFile(commandDissected[2], commandDissected[3]);
                            else
                                fsys.MakeFile(commandDissected[1], commandDissected[2]);
                            break;
                        case "cat":
                            fsys.ShowFileContent(commandDissected[1]);
                            break;
                        default:
                            Console.WriteLine("Invalid input.");
                            continue;
                    }
                }
                else if (command == "exit")
                    inFileSys = false;
                else
                    continue;
            }
        }
    }
}