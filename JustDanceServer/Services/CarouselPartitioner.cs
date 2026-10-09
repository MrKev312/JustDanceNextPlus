using System.Globalization;
using System.Text;

namespace JustDanceServer.Services;

public sealed record CarouselPartition<T>(string Title, IReadOnlyList<T> Items);

public static class CarouselPartitioner
{
	public const int TargetSize = 20;
	public const int MinimumSize = 10;
	public const int MaximumSize = 30;

	public static IReadOnlyList<CarouselPartition<T>> Create<T>(
		IEnumerable<T> source,
		Func<T, string> nameSelector)
	{
		Entry<T>[] entries =
		[
			.. source.Select(item => new Entry<T>(item, Normalize(nameSelector(item))))
				.OrderBy(entry => entry.SortName, StringComparer.OrdinalIgnoreCase)
		];

		Entry<T>[] numbers =
		[
			.. entries.Where(entry => entry.Kind == EntryKind.Number)
				.OrderBy(entry => entry.RangeKey, StringComparer.Ordinal)
				.ThenBy(entry => entry.SortName, StringComparer.Ordinal)
		];
		Entry<T>[] letters =
		[
			.. entries.Where(entry => entry.Kind == EntryKind.Letter)
				.OrderBy(entry => entry.RangeKey, StringComparer.Ordinal)
				.ThenBy(entry => entry.SortName, StringComparer.Ordinal)
		];
		Entry<T>[] misc = [.. entries.Where(entry => entry.Kind == EntryKind.Misc)];

		List<CarouselPartition<T>> result = [];
		result.AddRange(CreateLetterPartitions(letters));
		result.AddRange(CreateNamedPartitions(numbers, "0-9"));
		result.AddRange(CreateNamedPartitions(misc, "Misc"));
		return result;
	}

	private static IEnumerable<CarouselPartition<T>> CreateLetterPartitions<T>(Entry<T>[] entries)
	{
		Entry<T>[][] partitions = SplitLetters(entries);
		string[] titles = new string[partitions.Length];
		for (int index = 0; index < partitions.Length; index++)
		{
			Entry<T>[] partition = partitions[index];
			string firstName = partition[0].RangeKey;
			string lastName = partition[^1].RangeKey;
			bool continuesPreviousLetter = index > 0
				&& partitions[index - 1][^1].RangeKey[0] == firstName[0];
			bool continuesNextLetter = index < partitions.Length - 1
				&& partitions[index + 1][0].RangeKey[0] == lastName[0];

			int firstLength = continuesPreviousLetter
				? GetLabelLength(partitions[index - 1][^1].RangeKey, firstName)
				: 1;
			int lastLength = continuesNextLetter
				? GetLabelLength(lastName, partitions[index + 1][0].RangeKey)
				: 1;

			if (firstName[0] == lastName[0] && (continuesPreviousLetter || continuesNextLetter))
				firstLength = lastLength = Math.Max(2, Math.Max(firstLength, lastLength));

			string first = GetPrefix(firstName, firstLength);
			string last = GetPrefix(lastName, lastLength);
			titles[index] = string.Equals(first, last, StringComparison.Ordinal)
				? first
				: $"{first}-{last}";
		}

		Dictionary<string, int> titleCounts = titles
			.GroupBy(title => title, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
		Dictionary<string, int> titleIndexes = [with(StringComparer.Ordinal)];
		for (int index = 0; index < partitions.Length; index++)
		{
			string title = titles[index];
			if (titleCounts[title] > 1)
			{
				titleIndexes[title] = titleIndexes.GetValueOrDefault(title) + 1;
				title = $"{title} {titleIndexes[title]}";
			}

			yield return new CarouselPartition<T>(
				title,
				[.. partitions[index].Select(entry => entry.Item)]);
		}
	}

	private static IEnumerable<CarouselPartition<T>> CreateNamedPartitions<T>(
		Entry<T>[] entries,
		string title)
	{
		Entry<T>[][] partitions = Split(entries);
		for (int index = 0; index < partitions.Length; index++)
		{
			string partitionTitle = partitions.Length == 1
				? title
				: $"{title} {index + 1}";
			yield return new CarouselPartition<T>(
				partitionTitle,
				[.. partitions[index].Select(entry => entry.Item)]);
		}
	}

	private static Entry<T>[][] Split<T>(Entry<T>[] entries)
	{
		if (entries.Length == 0)
			return [];

		int minimumPartitions = (int)Math.Ceiling(entries.Length / (double)MaximumSize);
		int maximumPartitions = Math.Max(1, entries.Length / MinimumSize);
		int preferredPartitions = Math.Max(1, (int)Math.Round(
			entries.Length / (double)TargetSize,
			MidpointRounding.AwayFromZero));
		int partitionCount = Math.Clamp(preferredPartitions, minimumPartitions, maximumPartitions);

		int smallSize = entries.Length / partitionCount;
		int largerPartitions = entries.Length % partitionCount;
		Entry<T>[][] result = new Entry<T>[partitionCount][];
		int offset = 0;
		for (int index = 0; index < partitionCount; index++)
		{
			int size = smallSize + (index < largerPartitions ? 1 : 0);
			result[index] = entries[offset..(offset + size)];
			offset += size;
		}

		return result;
	}

	private static Entry<T>[][] SplitLetters<T>(Entry<T>[] entries)
	{
		if (entries.Length <= MaximumSize)
			return entries.Length == 0 ? [] : [entries];

		int minimumPartitions = (int)Math.Ceiling(entries.Length / (double)MaximumSize);
		int maximumPartitions = entries.Length / MinimumSize;
		int partitionCount = Math.Clamp(
			(int)Math.Round(entries.Length / (double)TargetSize, MidpointRounding.AwayFromZero),
			minimumPartitions,
			maximumPartitions);

		long[,] costs = new long[partitionCount + 1, entries.Length + 1];
		int[,] previous = new int[partitionCount + 1, entries.Length + 1];
		for (int group = 0; group <= partitionCount; group++)
			for (int item = 0; item <= entries.Length; item++)
				costs[group, item] = long.MaxValue;
		costs[0, 0] = 0;

		for (int group = 1; group <= partitionCount; group++)
		{
			int minimumEnd = group * MinimumSize;
			int maximumEnd = Math.Min(entries.Length, group * MaximumSize);
			for (int end = minimumEnd; end <= maximumEnd; end++)
			{
				for (int size = MinimumSize; size <= MaximumSize; size++)
				{
					int start = end - size;
					if (start < 0 || costs[group - 1, start] == long.MaxValue)
						continue;

					long cost = costs[group - 1, start]
						+ Math.Abs(size - TargetSize);
					if (start > 0)
						cost += GetBoundaryPenalty(entries[start - 1].RangeKey, entries[start].RangeKey);

					if (cost >= costs[group, end])
						continue;

					costs[group, end] = cost;
					previous[group, end] = start;
				}
			}
		}

		Entry<T>[][] result = new Entry<T>[partitionCount][];
		int currentEnd = entries.Length;
		for (int group = partitionCount; group > 0; group--)
		{
			int start = previous[group, currentEnd];
			result[group - 1] = entries[start..currentEnd];
			currentEnd = start;
		}

		return result;
	}

	private static long GetBoundaryPenalty(string first, string second)
		=> GetDistinguishingLength(first, second) switch
		{
			1 => 0,
			2 => 1_000_000,
			3 => 2_000_000,
			_ => 1_000_000_000_000
		};

	private static int GetLabelLength(string first, string second)
		=> Math.Min(GetDistinguishingLength(first, second), 3);

	private static int GetDistinguishingLength(string first, string second)
	{
		int sharedLength = 0;
		int maximum = Math.Min(first.Length, second.Length);
		while (sharedLength < maximum && first[sharedLength] == second[sharedLength])
			sharedLength++;
		return Math.Max(sharedLength + 1, 1);
	}

	private static string GetPrefix(string value, int length)
		=> value[..Math.Min(length, Math.Min(value.Length, 3))];

	private static string Normalize(string value)
	{
		StringBuilder builder = new();
		foreach (char character in (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormD))
		{
			UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
			if (category != UnicodeCategory.NonSpacingMark)
				builder.Append(char.ToUpperInvariant(character));
		}

		return builder.ToString().Normalize(NormalizationForm.FormC);
	}

	private enum EntryKind
	{
		Letter,
		Number,
		Misc
	}

	private sealed record Entry<T>(T Item, string SortName)
	{
		public string RangeKey { get; } = string.Concat(
			SortName.Where(character => character is (>= 'A' and <= 'Z') or (>= '0' and <= '9')));

		public EntryKind Kind => SortName.FirstOrDefault() switch
		{
			>= 'A' and <= 'Z' => EntryKind.Letter,
			>= '0' and <= '9' => EntryKind.Number,
			_ => EntryKind.Misc
		};
	}
}
