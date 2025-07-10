using System.Buffers.Binary;
using System.Text;

namespace JustDanceNextPlus.Utilities;

static class WebmCuesExtractor
{
	public static WebmData GetCuesInfo(string filePath)
	{
		using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, options: FileOptions.Asynchronous);

		WebmData data = new()
		{
			FileName = Path.GetFileNameWithoutExtension(filePath)
		};

		try
		{
			SkipElement(stream);        // Skip first element
			SkipElement(stream, false); // Skip second, leave position

			while (true)
			{
				long elementIdPos = stream.Position;

				long elementId = ReadVInt(stream, out int idLength, isID: true);
				long dataSize = ReadVInt(stream, out int sizeLength);

				long infoSize = idLength + sizeLength;

				if (elementId == 0x1549A966) // Info
				{
					data.Duration = ExtractDuration(stream, dataSize);
					data.Bitrate = (long)(8 * stream.Length / data.Duration);
				}
				else if (elementId == 0x1C53BB6B) // Cues
				{
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
			throw new InvalidOperationException("Failed to extract cues info.", ex);
		}
	}

	private static void SkipElement(Stream stream, bool skipData = true)
	{
		_ = ReadVInt(stream, out _); // element ID
		long dataSize = ReadVInt(stream, out _);

		if (skipData)
			stream.Seek(dataSize, SeekOrigin.Current);
	}

	private static double ExtractDuration(Stream stream, long dataSize)
	{
		long pos = stream.Position;
		long end = pos + dataSize;

		long floatId;
		long floatDataSize;

		do
		{
			floatId = ReadVInt(stream, out _);
			floatDataSize = ReadVInt(stream, out _);

			if (floatId != 0x4489)
				stream.Seek(floatDataSize, SeekOrigin.Current);

		} while (floatId != 0x4489 && stream.Position < end);

		Span<byte> buffer = stackalloc byte[8];
		stream.ReadExactly(buffer[..8]);
		double duration = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(buffer)) / 1000;

		stream.Seek(pos, SeekOrigin.Begin);
		return duration;
	}

	private static long ReadVInt(Stream stream, out int length, bool isID = false)
	{
		Span<byte> firstByteSpan = stackalloc byte[1];
		stream.ReadExactly(firstByteSpan);
		byte firstByte = firstByteSpan[0];

		byte mask = 0b1000_0000;
		length = 1;

		while ((firstByte & mask) == 0)
		{
			mask >>= 1;
			length++;
		}

		if (length > 8)
			throw new InvalidOperationException("Invalid VINT length");

		Span<byte> buffer = stackalloc byte[8];
		buffer[0] = firstByte;

		if (length > 1)
			stream.ReadExactly(buffer[1..length]);

		long value = isID ? firstByte : firstByte & (mask - 1);

		for (int i = 1; i < length; i++)
			value = (value << 8) | buffer[i];

		if (!isID && length != 1)
			value &= (1L << ((8 * length) - length)) - 1;

		return value;
	}

	private static void ReadExactly(this Stream stream, Span<byte> buffer)
	{
		int totalRead = 0;
		while (totalRead < buffer.Length)
		{
			int read = stream.Read(buffer[totalRead..]);
			if (read == 0)
				throw new EndOfStreamException("Unexpected end of stream while reading bytes.");
			totalRead += read;
		}
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