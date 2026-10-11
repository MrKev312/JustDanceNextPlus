using JustDanceServer.JustDanceClasses.Endpoints;

using System.Buffers;
using System.Text.Json;

namespace JustDanceServer.Tests.Controllers;

public sealed class JduHomeRequestTests
{
    private static readonly string[] TileTimeFields = ["creationTime", "lockDuration", "contentExpiry"];

    [Fact]
    public void HomeRequest_AcceptsClientTileHistoryWithDecimalTimes()
    {
        const string json = """
			{
			  "tileHistory": [
			    {
			      "__class": "HomeService::HomeTile",
			      "type": 0,
			      "creationTime": 1786626724717.0,
			      "locked": false,
			      "new": false,
			      "lockDuration": 0.0,
			      "contentExpiry": 0.0,
			      "uuid": "00000000-0000-0000-0000-000000000000",
			      "mapTileInfo": {
			        "__class": "HomeService::MapTileInfo",
			        "type": 3,
			        "mapName": "TestMap"
			      }
			    }
			  ],
			  "requestedTileCount": 5,
			  "timestampLastRequest": 1786626724,
			  "timestampLastManualContent": 1786626724,
			  "timestampLastMapContent": 0,
			  "timestampLastVideoContent": 0,
			  "timestampLastPlaylistContent": 0,
			  "timestampLastLocalTrack": 0,
			  "uplayConnected": true
			}
			""";

        JduHomeRequest request = JsonSerializer.Deserialize<JduHomeRequest>(json, JsonSerializerOptions.Web)!;

        JduHomeTile tile = Assert.Single(request.TileHistory);
        Assert.Equal(1786626724717L, tile.CreationTime);
        Assert.Equal(0, tile.LockDuration);
        Assert.Equal(0, tile.ContentExpiry);
        Assert.Equal("TestMap", tile.MapTileInfo?.MapName);
        Assert.Equal(5, request.RequestedTileCount);
        Assert.Equal(1786626724L, request.TimestampLastRequest);
        Assert.True(request.UplayConnected);
    }

    [Theory]
    [InlineData("0", 0L)]
    [InlineData("0.0", 0L)]
    [InlineData("1786626724717", 1786626724717L)]
    [InlineData("1786626724717.0", 1786626724717L)]
    [InlineData("1.786626724717e12", 1786626724717L)]
    [InlineData("9007199254740993.0", 9007199254740993L)]
    [InlineData("9223372036854775807", long.MaxValue)]
    [InlineData("9223372036854775807.0", long.MaxValue)]
    [InlineData("-9223372036854775808", long.MinValue)]
    [InlineData("-9223372036854775808.0", long.MinValue)]
    [InlineData("\"1786626724717\"", 1786626724717L)]
    public void TileTimes_AcceptWholeNumbersWithoutLosingPrecision(string number, long expected)
    {
        string json = $$"""
			{"tileHistory":[{"creationTime":{{number}},"lockDuration":{{number}},"contentExpiry":{{number}}}]}
			""";

        JduHomeRequest request = JsonSerializer.Deserialize<JduHomeRequest>(json, JsonSerializerOptions.Web)!;

        JduHomeTile tile = Assert.Single(request.TileHistory);
        Assert.Equal(expected, tile.CreationTime);
        Assert.Equal(expected, tile.LockDuration);
        Assert.Equal(expected, tile.ContentExpiry);
    }

    [Theory]
    [InlineData("0.5")]
    [InlineData("-0.5")]
    [InlineData("1786626724717.5")]
    [InlineData("1e-1")]
    [InlineData("1e-100")]
    [InlineData("1.00000000000000000000000000001")]
    [InlineData("9223372036854775807.000000000001")]
    [InlineData("9223372036854775808")]
    [InlineData("9223372036854775808.0")]
    [InlineData("-9223372036854775809.0")]
    [InlineData("1e100")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("\"1786626724717.0\"")]
    [InlineData("\"not a number\"")]
    public void TileTimes_RejectFractionsOverflowAndNonNumericValues(string value)
    {
        foreach (string field in TileTimeFields)
        {
            string json = $$"""{"tileHistory":[{"{{field}}":{{value}}}]}""";

            JsonException exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<JduHomeRequest>(json, JsonSerializerOptions.Web));

            Assert.Equal($"$.tileHistory[0].{field}", exception.Path);
        }
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1786626724717L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void TileTimes_StillSerializeAsIntegers(long value)
    {
        JduHomeTile tile = new() { CreationTime = value, LockDuration = value, ContentExpiry = value };

        JsonElement json = JsonSerializer.SerializeToElement(tile, JsonSerializerOptions.Web);

        foreach (string field in TileTimeFields)
            Assert.Equal(value, json.GetProperty(field).GetInt64());
    }

    [Fact]
    public void HomeRequest_OtherIntegerFieldsStillRejectDecimalNotation()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JduHomeRequest>(
            """{"timestampLastRequest":1786626724.0}""", JsonSerializerOptions.Web));
    }

    [Fact]
    public void HomeRequest_ReadsTileTimeAcrossJsonBufferSegments()
    {
        JsonSegment first = new("{\"tileHistory\":[{\"creationTime\":1786626"u8.ToArray());
        JsonSegment last = first.Append("724717.0}]}"u8.ToArray());
        ReadOnlySequence<byte> json = new(first, 0, last, last.Memory.Length);
        Utf8JsonReader reader = new(json);

        JduHomeRequest request = JsonSerializer.Deserialize<JduHomeRequest>(ref reader, JsonSerializerOptions.Web)!;

        Assert.Equal(1786626724717L, Assert.Single(request.TileHistory).CreationTime);
    }

    private sealed class JsonSegment : ReadOnlySequenceSegment<byte>
    {
        public JsonSegment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public JsonSegment Append(ReadOnlyMemory<byte> memory)
        {
            JsonSegment next = new(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}
