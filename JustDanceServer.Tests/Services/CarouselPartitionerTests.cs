using JustDanceServer.Services;

namespace JustDanceServer.Tests.Services;

public sealed class CarouselPartitionerTests
{
    [Fact]
    public void Create_BalancesAlphabetRowsAndPreservesEveryItem()
    {
        string[] items = [.. Enumerable.Range(0, 97).Select(index => $"Song {index:D3}")];

        IReadOnlyList<CarouselPartition<string>> rows = CarouselPartitioner.Create(items, value => value);

        Assert.Equal(items.Length, rows.Sum(row => row.Items.Count));
        Assert.All(rows, row => Assert.InRange(row.Items.Count, 10, 30));
        Assert.InRange(rows.Average(row => row.Items.Count), 15, 25);
        Assert.Equal(items.Order(StringComparer.OrdinalIgnoreCase),
            rows.SelectMany(row => row.Items));
    }

    [Fact]
    public void Create_KeepsNumbersSeparateAndFoldsLatinDiacritics()
    {
        string[] items = ["24K Magic", "8Bit", "Água", "Ça Plane", "Zulu", "東京"];

        IReadOnlyList<CarouselPartition<string>> rows = CarouselPartitioner.Create(items, value => value);

        Assert.Equal(["24K Magic", "8Bit"], rows.Single(row => row.Title == "0-9").Items);
        Assert.Contains("Água", rows.Single(row => row.Title.StartsWith('A')).Items);
        Assert.Contains("Ça Plane", rows.Single(row => row.Title.StartsWith('A')).Items);
        Assert.Equal(["東京"], rows.Single(row => row.Title == "Misc").Items);
    }

    [Fact]
    public void Create_UsesWholeLetterNameWhenSparseTailFitsUnderMaximum()
    {
        string[] items =
        [
            .. Enumerable.Range(0, 25).Select(index => $"Aa Song {index:D2}"),
            "Ay Song",
            "Az Song"
        ];

        CarouselPartition<string> row = Assert.Single(CarouselPartitioner.Create(items, value => value));

        Assert.Equal("A", row.Title);
        Assert.Equal(27, row.Items.Count);
    }

    [Fact]
    public void Create_UsesAtMostThreeCharactersPerAlphabetBoundary()
    {
        string[] items =
        [
            .. Enumerable.Range(0, 40).Select(index => $"Aaa Song {index:D2}"),
            .. Enumerable.Range(0, 40).Select(index => $"Aaf Song {index:D2}"),
            .. Enumerable.Range(0, 40).Select(index => $"Ab Song {index:D2}")
        ];

        IReadOnlyList<CarouselPartition<string>> rows = CarouselPartitioner.Create(items, value => value);

        Assert.All(rows, row =>
        {
            string range = row.Title.Split(' ')[0];
            Assert.All(range.Split('-'), boundary => Assert.InRange(boundary.Length, 1, 3));
            Assert.InRange(row.Items.Count, 10, 30);
        });
    }

    [Fact]
    public void Create_ShiftsBoundaryRatherThanSplittingIndistinguishablePrefixes()
    {
        string[] items =
        [
            .. Enumerable.Range(0, 20).Select(index => $"Alpha {index:D2}"),
            "Long title here",
            "Long title herf",
            .. Enumerable.Range(0, 20).Select(index => $"Zulu {index:D2}")
        ];

        IReadOnlyList<CarouselPartition<string>> rows = CarouselPartitioner.Create(items, value => value);
        CarouselPartition<string> firstLongTitleRow = rows.Single(row => row.Items.Contains("Long title here"));

        Assert.Contains("Long title herf", firstLongTitleRow.Items);
        Assert.Equal(["A-L", "Z"], rows.Select(row => row.Title));
        Assert.Equal([22, 20], rows.Select(row => row.Items.Count));
    }

    [Fact]
    public void Create_SortsUsingTheSameCompactKeyAsRangeLabels()
    {
        string[] items =
        [
            .. Enumerable.Range(0, 20).Select(index => $"A La Folie {index:D2}"),
            .. Enumerable.Range(0, 20).Select(index => $"After Party {index:D2}")
        ];

        IReadOnlyList<CarouselPartition<string>> rows = CarouselPartitioner.Create(items, value => value);

        Assert.Equal(["AF", "AL"], rows.Select(row => row.Title));
        Assert.All(rows[0].Items, item => Assert.StartsWith("After Party", item));
        Assert.All(rows[1].Items, item => Assert.StartsWith("A La Folie", item));
    }

    [Fact]
    public void Create_OrdersLettersThenNumbersThenMisc()
    {
        string[] items =
        [
            .. Enumerable.Range(0, 10).Select(index => $"Zulu {index:D2}"),
            "24K", "8Bit", "!Bang", "東京"
        ];

        IReadOnlyList<CarouselPartition<string>> rows = CarouselPartitioner.Create(items, value => value);

        Assert.StartsWith("Z", rows[0].Title);
        Assert.Equal("0-9", rows[1].Title);
        Assert.Equal("Misc", rows[2].Title);
        Assert.Equal(["!Bang", "東京"], rows[2].Items);
    }

    [Fact]
    public void Create_NeverMakesAnOversizedNumberOrMiscRow()
    {
        string[] items =
        [
            .. Enumerable.Range(0, 65).Select(index => $"{index:D3}"),
            .. Enumerable.Range(0, 35).Select(index => $"!misc-{index:D2}")
        ];

        IReadOnlyList<CarouselPartition<string>> rows = CarouselPartitioner.Create(items, value => value);

        Assert.All(rows, row => Assert.InRange(row.Items.Count, 10, 30));
        Assert.All(rows.Where(row => row.Title.StartsWith("0-9")), row =>
            Assert.All(row.Items, item => Assert.True(char.IsDigit(item[0]))));
    }
}
