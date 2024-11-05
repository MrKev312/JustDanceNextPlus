using System.Text;

namespace JustDanceNextPlus;

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
			using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
			SkipElement(reader); // Skip the first element
			SkipElement(reader, false); // Skip the second element without skipping the data

			while (true)
			{
				// Store position before reading the element ID
				long elementIdPos = reader.BaseStream.Position;

				long elementId = ReadVInt(reader, true);
				long dataSize = ReadVInt(reader);

				// Add the size of the element ID and data size ints to the total data size
				long infoSize = reader.BaseStream.Position - elementIdPos;

				if (elementId == 0x1549A966) // Info element
				{
					data.Duration = ExtractDuration(reader, dataSize);
					data.Bitrate = (long)(stream.Length * 8 / data.Duration);
				}
				else if (elementId == 0x1C53BB6B) // Cues element
				{
					// Add the size of everything before the Cues element to the total data size
					dataSize += infoSize + elementIdPos;

					data.Start = elementIdPos;
					data.End = dataSize;

					return data;
				}

				reader.BaseStream.Seek(dataSize, SeekOrigin.Current);
			}
		}
		catch (Exception ex)
		{
			// Handle exceptions (e.g., log the error)
			throw new InvalidOperationException("Failed to extract cues info.", ex);
		}
	}

	private static void SkipElement(BinaryReader reader, bool skipData = true)
	{
		_ = ReadVInt(reader, true);
		long dataSize = ReadVInt(reader);

		if (skipData)
			reader.BaseStream.Seek(dataSize, SeekOrigin.Current);
	}

	private static double ExtractDuration(BinaryReader reader, long dataSize)
	{
		long pos = reader.BaseStream.Position;
		long floatId;
		long floatDataSize;

		do
		{
			floatId = ReadVInt(reader, true);
			floatDataSize = ReadVInt(reader);
			if (floatId != 0x4489)
			{
				reader.BaseStream.Seek(floatDataSize, SeekOrigin.Current);
			}
		} while (floatId != 0x4489 && reader.BaseStream.Position < pos + dataSize);

		byte[] bytes = reader.ReadBytes(8);
		Array.Reverse(bytes);
		double duration = BitConverter.ToDouble(bytes, 0) / 1000;

		reader.BaseStream.Seek(pos, SeekOrigin.Begin);
		return duration;
	}

	private static long ReadVInt(BinaryReader reader, bool isID = false)
	{
		byte firstByte = reader.ReadByte();
		byte mask = 0x80;
		long value = isID ? firstByte : (firstByte & (mask - 1));
		int length = 1;

		while ((firstByte & mask) == 0)
		{
			length++;
			mask >>= 1;
			value <<= 8;
			value |= reader.ReadByte();
		}

		if (!isID && length != 1)
		{
			value &= (1L << ((length * 8) - length)) - 1;
		}

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