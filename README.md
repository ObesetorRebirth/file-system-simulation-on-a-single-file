# File System Simulation on a single binary file

A simple simulated file system on a single binary file implemented in C#. A project I did for Data Structures and Algorithms course. The main goal was to implement the file system without using any of the integrated C# structures (Stack, List, etc.) except array, and also without using LINQ. This project provides basic file system functionalities such as creating directories, listing files, writing files, reading files, and copying files from the host system. The file system has the functionality to split the data of a file on two or more separate block of data, based on Unix's iNode.

## Features
- Create and remove directories
- Navigate directories
- List files and directories
- Write and append to files
- Read file content
- Copy files from the host system to the simulated file system

## Installation & Setup

1. Clone this repository:
   ```sh
   git clone https://github.com/ObesetorRebirth/file-system-simulation-on-a-single-file.git
   cd file-system-simulation-on-a-single-file
   ```
2. Open the project in your preferred C# IDE (e.g., Visual Studio).
3. Build and run the project.

## Usage
Upon running the program, the system prompts for commands. The available commands are:

### Commands List:
- **mkdir** `<directory_path>` - Create a new directory
- **rmdir** `<directory_path>` - Remove an empty directory
- **cd** `<directory_path>` - Change directory
- **ls** - List files and directories in the current directory
- **write** `<file_path>` `"<content>"` - Create or overwrite a file with content
- **write append** `<file_path>` `"<content>"` - Append content to an existing file
- **cat** `<file_path>` - Display file content
- **cpin** `<source_path>` `<destination_file_name>` - Copy a file from the host system to the simulated file system
- **exit** - Exit the file system

## Example Usage
```sh
mkdir \MyFolder
cd \MyFolder
write file.txt "Hello, File System!"
ls
cat file.txt
write append file.txt " Appended content."
cat file.txt
```

## Project Structure
```
Bitmap.cs - the project stores 2 bitmaps, one to keep track of the free iNodes and one to keep track of the allocated datablocks
CustomList.cs - a custom made list to avoid using integrated data structures
DirectoryFile.cs - directory files are also directory entries, a directory only stores the iNodes for the files that are in it.
FileATable.cs - the File Allocation Table (FAT) comes in handy when dealing with larger files that need more than one data block of storage, the FAT is a list of integers, with the same length as the number of datablocks in the file system, that stores either: 0 for an empty data block, an index of a data block, that points to the next data block that stores the remaining bytes of the same file, or -1 if the datablock is the last datablock that contains file data of the same file.  
INodeTable.cs - an array storing iNodes/ the metadata of all of the files
MyFS.cs - All of the components of the file system and the file stream, with the program functions and some utility functions
SuperBlock.cs - stores important information regarding the file system: size of datablock, number of datablocks, positions of the components etc.

```



