namespace JustDanceNextPlus.Services;

/// <summary>
/// Defines an abstraction for file system operations, including methods for checking file and directory existence,
/// reading files, and retrieving file system metadata.
/// </summary>
/// <remarks>Implementations of this interface provide access to file and directory information without exposing
/// underlying storage details. This allows consumers to interact with files and directories in a platform-agnostic
/// manner, which is useful for testing, dependency injection, or supporting multiple storage backends.</remarks>
public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    Stream OpenRead(string path);
    string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    long GetFileLength(string path);
    string[] GetDirectories(string path);
    DateTime GetDirectoryCreationTime(string path);
}

/// <summary>
/// Provides an implementation of the <see cref="IFileSystem"/> interface that interacts with the system's physical file
/// system using standard .NET file and directory operations.
/// </summary>
/// <remarks>This class enables file and directory access through the system's underlying file system APIs. It is
/// suitable for scenarios where direct access to the operating system's file system is required, such as reading,
/// writing, or querying files and directories. For unit testing or environments where file system access should be
/// abstracted or mocked, consider using a different implementation of <see cref="IFileSystem"/>.</remarks>
public class SystemFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public Stream OpenRead(string path) => File.OpenRead(path);
    public string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => Directory.GetFiles(path, searchPattern, searchOption);
    public long GetFileLength(string path) => new FileInfo(path).Length;
    public string[] GetDirectories(string path) => Directory.GetDirectories(path);
    public DateTime GetDirectoryCreationTime(string path) => Directory.GetCreationTime(path);
}