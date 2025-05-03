namespace JustDanceNextPlus.Utilities;

public static class PathExtensions
{
	public static string? GetFirstFile(this string directoryPath)
	{
		string[] files = Directory.GetFiles(directoryPath);

		return files.FirstOrDefault();
	}

	public static (string name, long size)? GetLargestFile(this string directoryPath)
	{
		string[] files = Directory.GetFiles(directoryPath);

		return files.Select(file => new FileInfo(file))
			.Select(fileInfo => (fileInfo.Name, fileInfo.Length))
			.OrderByDescending(file => file.Length)
			.FirstOrDefault();
	}
}