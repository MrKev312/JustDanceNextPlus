using System.Buffers.Binary;

namespace JustDanceNextPlus.Utilities;

static class WebmCuesExtractor
{
	// Simplified version that only gets the Cues info
	public static WebmData GetCuesInfo(string filePath)
	{
		using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, options: FileOptions.Asynchronous);

		WebmData data = new()
		{
			FileName = Path.GetFileNameWithoutExtension(filePath)
		};

		try
		{
			SkipElement(stream); // Skip the first element
			SkipElement(stream, false); // Skip the second element without skipping the data

			while (true)
			{
				// Store position before reading the element ID
				long elementIdPos = stream.Position;

				long elementId = ReadVInt(stream, true);
				long dataSize = ReadVInt(stream);

				// Add the size of the element ID and data size ints to the total data size
				long infoSize = stream.Position - elementIdPos;

				if (elementId == 0x1549A966) // Info element
				{
					data.Duration = ExtractDuration(stream, dataSize);
					data.Bitrate = (long)(8 * stream.Length / data.Duration);
				}
				else if (elementId == 0x1C53BB6B) // Cues element
				{
					// Add the size of everything before the Cues element to the total data size
					dataSize += infoSize + elementIdPos;

					data.Start = elementIdPos;
					data.End = dataSize;

					return data;
				}

				stream.Seek(dataSize, SeekOrigin.Current);
			}
		}
		catch (Exception ex)
		{
			// Handle exceptions (e.g., log the error)
			throw new InvalidOperationException("Failed to extract cues info.", ex);
		}
	}

	private static void SkipElement(Stream stream, bool skipData = true)
	{
		_ = ReadVInt(stream, isID: true);
		long dataSize = ReadVInt(stream);

		if (skipData)
			stream.Seek(dataSize, SeekOrigin.Current);
	}

	private static double ExtractDuration(Stream stream, long dataSize)
	{
		long start = stream.Position;
		long floatId;
		long floatDataSize;

		do
		{
			floatId = ReadVInt(stream, isID: true);
			floatDataSize = ReadVInt(stream);
			if (floatId != 0x4489)
				stream.Seek(floatDataSize, SeekOrigin.Current);
		} while (floatId != 0x4489 && stream.Position < start + dataSize);

		Span<byte> buffer = stackalloc byte[8];
		int bytesRead = stream.Read(buffer);
		if (bytesRead != 8)
			throw new InvalidDataException("Failed to read duration bytes.");

		double duration = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(buffer)) / 1000;

		stream.Seek(start, SeekOrigin.Begin);
		return duration;
	}

	private static long ReadVInt(Stream stream, bool isID = false)
	{
		int firstByte = stream.ReadByte();
		if (firstByte == -1)
			throw new EndOfStreamException();

		byte mask = 0b10000000;
		long value = isID ? firstByte : firstByte & (mask - 1);
		int length = 1;

		while ((firstByte & mask) == 0)
		{
			mask >>= 1;
			int next = stream.ReadByte();
			if (next == -1)
				throw new EndOfStreamException();

			value <<= 8;
			value |= (byte)next;
			length++;
		}

		if (!isID && length != 1)
			value &= (1L << ((8 * length) - length)) - 1;

		return value;
	}
}

public class WebmData
{
	public long Start { get; set; }
	public long End { get; set; }
	public double Duration { get; set; }
	public long Bitrate { get; set; }
	public string FileName { get; set; } = "";
}