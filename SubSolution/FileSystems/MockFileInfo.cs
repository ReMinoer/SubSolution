using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace SubSolution.FileSystems
{
    internal class MockFileInfo : FileInfoBase
    {
        public override string FullName { get; }
        public override string Name { get; }
        public override DirectoryInfoBase ParentDirectory { get; }

        public MockFileInfo(MockDirectoryInfo parentDirectory, string name)
        {
            Name = name;
            FullName = parentDirectory.FileSystem.Combine(parentDirectory.FullName, name);
            ParentDirectory = parentDirectory;
        }
    }
}