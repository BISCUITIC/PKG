using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace ReadingInformationGraphicFiles;

public static class ImageMetadataReader
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".gif", ".tif", ".tiff", ".bmp", ".png", ".pcx"
    };

    private static readonly byte[] PngSignature =
    {
        137, 80, 78, 71, 13, 10, 26, 10
    };

    public static bool IsSupported(string path) =>
        SupportedExtensions.Contains(Path.GetExtension(path));

    public static ImageInfo Read(string path)
    {
        try
        {
            string extension = Path.GetExtension(path);
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.SequentialScan);

            ImageInfo result = extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => ParseJpeg(stream),
                ".png" => ParsePng(stream),
                ".gif" => ParseGif(stream),
                ".bmp" => ParseBmp(stream),
                ".tif" or ".tiff" => ParseTiff(stream),
                ".pcx" => ParsePcx(stream),
                _ => throw new NotSupportedException("Неподдерживаемый формат.")
            };

            return result with
            {
                FileName = Path.GetFileName(path),
                FullPath = path,
                FileSize = FormatFileSize(stream.Length)
            };
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            InvalidDataException or
            EndOfStreamException or
            NotSupportedException or
            FormatException or
            OverflowException or
            ArgumentOutOfRangeException)
        {
            return new ImageInfo
            {
                FileName = Path.GetFileName(path),
                FullPath = path,
                FileSize = TryGetFileSize(path),
                Format = "Ошибка",
                Dimensions = "—",
                Resolution = "—",
                ColorDepth = "—",
                Compression = "—",
                Error = ex.Message,
                IsError = true
            };
        }
    }

    private static ImageInfo ParsePng(Stream s)
    {
        Span<byte> header = stackalloc byte[33];
        ReadExactly(s, header[..8]);
        if (!header[..8].SequenceEqual(PngSignature))
            throw new InvalidDataException("Файл не является корректным PNG.");

        ReadExactly(s, header[8..33]);
        uint ihdrLength = BinaryPrimitives.ReadUInt32BigEndian(header[..4]);
        if (ihdrLength != 13 || Encoding.ASCII.GetString(header[4..8]) != "IHDR")
            throw new InvalidDataException("Повреждённый PNG: отсутствует IHDR.");

        uint width = BinaryPrimitives.ReadUInt32BigEndian(header[8..12]);
        uint height = BinaryPrimitives.ReadUInt32BigEndian(header[12..16]);
        int bitDepth = header[16];
        int colorType = header[17];

        if (width == 0 || height == 0)
            throw new InvalidDataException("PNG содержит нулевой размер изображения.");
        if (!IsValidPngBitDepth(colorType, bitDepth))
            throw new InvalidDataException("Некорректная комбинация глубины цвета и типа PNG.");

        double? xDpi = null;
        double? yDpi = null;

        // pHYs располагается до IDAT, поэтому достаточно просмотреть заголовки чанков до начала данных.
        while (s.Position + 8 <= s.Length)
        {
            Span<byte> chunkHeader = stackalloc byte[8];
            ReadExactly(s, chunkHeader);

            uint length = BinaryPrimitives.ReadUInt32BigEndian(chunkHeader[..4]);
            string type = Encoding.ASCII.GetString(chunkHeader[4..8]);

            if ((long)length > s.Length - s.Position - 4)
                throw new InvalidDataException("Повреждённый PNG: некорректная длина чанка.");

            if (type == "pHYs" && length == 9)
            {
                Span<byte> phys = stackalloc byte[9];
                ReadExactly(s, phys);
                uint xPpm = BinaryPrimitives.ReadUInt32BigEndian(phys[..4]);
                uint yPpm = BinaryPrimitives.ReadUInt32BigEndian(phys[4..8]);
                byte unit = phys[8];

                if (unit == 1)
                {
                    xDpi = xPpm * 0.0254;
                    yDpi = yPpm * 0.0254;
                }
            }
            else
            {
                s.Position += length;
            }

            s.Position += 4; // CRC

            if (type is "IDAT" or "IEND")
                break;
        }

        string depth = colorType switch
        {
            0 => $"{bitDepth} бит/пиксель (градации серого)",
            2 => $"{bitDepth * 3} бит/пиксель ({bitDepth} бит/канал, RGB)",
            3 => $"{bitDepth} бит/пиксель (индекс палитры)",
            4 => $"{bitDepth * 2} бит/пиксель ({bitDepth} бит/канал, Gray+Alpha)",
            6 => $"{bitDepth * 4} бит/пиксель ({bitDepth} бит/канал, RGBA)",
            _ => $"{bitDepth} бит/пиксель"
        };

        return new ImageInfo
        {
            Format = "PNG",
            Dimensions = $"{width} × {height}",
            Resolution = FormatDpi(xDpi, yDpi),
            ColorDepth = depth,
            Compression = "Deflate (zlib)"
        };
    }

    private static bool IsValidPngBitDepth(int colorType, int bitDepth) => colorType switch
    {
        0 => bitDepth is 1 or 2 or 4 or 8 or 16,
        2 => bitDepth is 8 or 16,
        3 => bitDepth is 1 or 2 or 4 or 8,
        4 => bitDepth is 8 or 16,
        6 => bitDepth is 8 or 16,
        _ => false
    };

    private static ImageInfo ParseGif(Stream s)
    {
        Span<byte> header = stackalloc byte[13];
        ReadExactly(s, header);

        string signature = Encoding.ASCII.GetString(header[..6]);
        if (signature is not ("GIF87a" or "GIF89a"))
            throw new InvalidDataException("Файл не является корректным GIF.");

        int width = BinaryPrimitives.ReadUInt16LittleEndian(header[6..8]);
        int height = BinaryPrimitives.ReadUInt16LittleEndian(header[8..10]);
        byte packed = header[10];

        bool hasGlobalColorTable = (packed & 0x80) != 0;
        int globalColorBits = (packed & 0x07) + 1;
        int? pixelBits = hasGlobalColorTable ? globalColorBits : null;

        if (hasGlobalColorTable)
        {
            long tableBytes = 3L * (1L << globalColorBits);
            EnsureAvailable(s, tableBytes);
            s.Position += tableBytes;
        }

        while (s.Position < s.Length)
        {
            int introducer = s.ReadByte();
            if (introducer < 0)
                break;

            switch (introducer)
            {
                case 0x2C: // Image Descriptor
                {
                    Span<byte> descriptor = stackalloc byte[9];
                    ReadExactly(s, descriptor);
                    byte imagePacked = descriptor[8];

                    bool hasLocalColorTable = (imagePacked & 0x80) != 0;
                    if (hasLocalColorTable)
                        pixelBits = (imagePacked & 0x07) + 1;

                    // Дальше начинается LZW-данные изображения; заголовка достаточно.
                    s.Position = s.Length;
                    break;
                }

                case 0x21: // Extension
                    if (s.ReadByte() < 0)
                        throw new EndOfStreamException();
                    SkipGifSubBlocks(s);
                    break;

                case 0x3B: // Trailer
                    s.Position = s.Length;
                    break;

                default:
                    throw new InvalidDataException("Повреждённый GIF: неизвестный блок.");
            }
        }

        string depth = pixelBits.HasValue
            ? $"{pixelBits.Value} бит/пиксель (палитра, до {1 << pixelBits.Value} цветов)"
            : "не определено (палитра отсутствует)";

        return new ImageInfo
        {
            Format = "GIF",
            Dimensions = $"{width} × {height}",
            Resolution = "не задано в стандарте GIF",
            ColorDepth = depth,
            Compression = "LZW"
        };
    }

    private static void SkipGifSubBlocks(Stream s)
    {
        while (true)
        {
            int blockSize = s.ReadByte();
            if (blockSize < 0)
                throw new EndOfStreamException();
            if (blockSize == 0)
                return;

            EnsureAvailable(s, blockSize);
            s.Position += blockSize;
        }
    }

    private static ImageInfo ParseBmp(Stream s)
    {
        Span<byte> fileHeader = stackalloc byte[26];
        ReadExactly(s, fileHeader);
        if (fileHeader[0] != (byte)'B' || fileHeader[1] != (byte)'M')
            throw new InvalidDataException("Файл не является корректным BMP.");

        uint dibSize = BinaryPrimitives.ReadUInt32LittleEndian(fileHeader[14..18]);
        if (dibSize == 12)
        {
            int widthBmp = BinaryPrimitives.ReadUInt16LittleEndian(fileHeader[18..20]);
            int heightBmp = BinaryPrimitives.ReadUInt16LittleEndian(fileHeader[20..22]);
            int bits = BinaryPrimitives.ReadUInt16LittleEndian(fileHeader[24..26]);

            if (widthBmp <= 0 || heightBmp <= 0 || bits <= 0)
                throw new InvalidDataException("Повреждённый BMP: некорректные размеры или глубина цвета.");

            return new ImageInfo
            {
                Format = "BMP",
                Dimensions = $"{widthBmp} × {heightBmp}",
                Resolution = "не задано",
                ColorDepth = $"{bits} бит/пиксель",
                Compression = "нет (BI_RGB)"
            };
        }

        if (dibSize < 40 || dibSize > 124)
            throw new InvalidDataException("Неподдерживаемый или повреждённый DIB-заголовок BMP.");

        byte[] dib = new byte[checked((int)dibSize)];
        s.Position = 14;
        ReadExactly(s, dib);

        int widthRaw = BinaryPrimitives.ReadInt32LittleEndian(dib.AsSpan(4, 4));
        int heightRaw = BinaryPrimitives.ReadInt32LittleEndian(dib.AsSpan(8, 4));
        int bitsPerPixel = BinaryPrimitives.ReadUInt16LittleEndian(dib.AsSpan(14, 2));
        uint compressionCode = BinaryPrimitives.ReadUInt32LittleEndian(dib.AsSpan(16, 4));
        int xPpm = BinaryPrimitives.ReadInt32LittleEndian(dib.AsSpan(24, 4));
        int yPpm = BinaryPrimitives.ReadInt32LittleEndian(dib.AsSpan(28, 4));

        if (widthRaw == 0 || heightRaw == 0 || bitsPerPixel == 0)
            throw new InvalidDataException("Повреждённый BMP: некорректные размеры или глубина цвета.");

        int width = Math.Abs(widthRaw);
        int height = Math.Abs(heightRaw);
        double? xDpi = xPpm > 0 ? xPpm * 0.0254 : null;
        double? yDpi = yPpm > 0 ? yPpm * 0.0254 : null;

        return new ImageInfo
        {
            Format = "BMP",
            Dimensions = $"{width} × {height}",
            Resolution = FormatDpi(xDpi, yDpi),
            ColorDepth = $"{bitsPerPixel} бит/пиксель",
            Compression = BmpCompressionName(compressionCode)
        };
    }

    private static ImageInfo ParsePcx(Stream s)
    {
        Span<byte> header = stackalloc byte[128];
        ReadExactly(s, header);

        if (header[0] != 0x0A || header[2] != 1)
            throw new InvalidDataException("Файл не является корректным PCX (ожидалась RLE-кодировка).");

        int bitsPerPlane = header[3];
        int xmin = BinaryPrimitives.ReadUInt16LittleEndian(header[4..6]);
        int ymin = BinaryPrimitives.ReadUInt16LittleEndian(header[6..8]);
        int xmax = BinaryPrimitives.ReadUInt16LittleEndian(header[8..10]);
        int ymax = BinaryPrimitives.ReadUInt16LittleEndian(header[10..12]);
        int hres = BinaryPrimitives.ReadUInt16LittleEndian(header[12..14]);
        int vres = BinaryPrimitives.ReadUInt16LittleEndian(header[14..16]);
        int planes = header[65];

        if (bitsPerPlane is not (1 or 2 or 4 or 8) || planes is < 1 or > 4)
            throw new InvalidDataException("PCX содержит неподдерживаемые параметры глубины цвета.");

        int width = xmax - xmin + 1;
        int height = ymax - ymin + 1;
        if (width <= 0 || height <= 0)
            throw new InvalidDataException("Повреждённый PCX: некорректные размеры.");

        int depth = checked(bitsPerPlane * planes);

        return new ImageInfo
        {
            Format = "PCX",
            Dimensions = $"{width} × {height}",
            Resolution = FormatDpi(hres > 0 ? hres : null, vres > 0 ? vres : null),
            ColorDepth = $"{depth} бит/пиксель ({bitsPerPlane} бит × {planes} пл.)",
            Compression = "RLE"
        };
    }

    private static ImageInfo ParseJpeg(Stream s)
    {
        Span<byte> soi = stackalloc byte[2];
        ReadExactly(s, soi);
        if (soi[0] != 0xFF || soi[1] != 0xD8)
            throw new InvalidDataException("Файл не является корректным JPEG.");

        int? width = null;
        int? height = null;
        int? bits = null;
        int? components = null;
        double? xDpi = null;
        double? yDpi = null;
        string compression = "JPEG";
        bool hasHuffman = false;
        bool hasArithmetic = false;

        while (s.Position < s.Length)
        {
            int prefix = s.ReadByte();
            if (prefix < 0)
                break;
            if (prefix != 0xFF)
                continue;

            int marker;
            do
            {
                marker = s.ReadByte();
            }
            while (marker == 0xFF);

            if (marker < 0)
                break;

            if (marker == 0xD9 || marker == 0xDA)
                break;

            if (marker is >= 0xD0 and <= 0xD7 || marker == 0x01)
                continue;

            if (marker == 0x00)
                throw new InvalidDataException("Повреждённый JPEG: неожиданный маркер FF00.");

            ushort length = ReadUInt16BE(s);
            if (length < 2)
                throw new InvalidDataException("Повреждённый JPEG: неверная длина сегмента.");

            long dataStart = s.Position;
            int dataLength = length - 2;
            long dataEnd = checked(dataStart + dataLength);
            if (dataEnd > s.Length)
                throw new InvalidDataException("Повреждённый JPEG: сегмент выходит за пределы файла.");

            if (marker == 0xE0 && dataLength >= 14)
            {
                byte[] app0 = new byte[Math.Min(dataLength, 32)];
                ReadExactly(s, app0);

                if (Encoding.ASCII.GetString(app0, 0, 5) == "JFIF\0")
                {
                    byte units = app0[7];
                    ushort xd = BinaryPrimitives.ReadUInt16BigEndian(app0.AsSpan(8, 2));
                    ushort yd = BinaryPrimitives.ReadUInt16BigEndian(app0.AsSpan(10, 2));

                    if (xd > 0 && yd > 0)
                    {
                        if (units == 1)
                        {
                            xDpi = xd;
                            yDpi = yd;
                        }
                        else if (units == 2)
                        {
                            xDpi = xd * 2.54;
                            yDpi = yd * 2.54;
                        }
                    }
                }

                s.Position = dataEnd;
                continue;
            }

            if (marker == 0xE1 && dataLength >= 8 && dataLength <= 65533)
            {
                byte[] app1 = new byte[dataLength];
                ReadExactly(s, app1);

                if (Encoding.ASCII.GetString(app1, 0, 6) == "Exif\0\0" && (xDpi is null || yDpi is null))
                {
                    var exif = TryReadExifDpi(app1, 6);
                    xDpi ??= exif.x;
                    yDpi ??= exif.y;
                }

                s.Position = dataEnd;
                continue;
            }

            if (marker == 0xC4)
                hasHuffman = true;
            if (marker == 0xCC)
                hasArithmetic = true;

            if (IsStartOfFrame(marker))
            {
                Span<byte> sof = stackalloc byte[6];
                if (dataLength < 6)
                    throw new InvalidDataException("Повреждённый JPEG: короткий SOF.");

                ReadExactly(s, sof);
                bits = sof[0];
                height = BinaryPrimitives.ReadUInt16BigEndian(sof[1..3]);
                width = BinaryPrimitives.ReadUInt16BigEndian(sof[3..5]);
                components = sof[5];

                if (bits is <= 0 or > 16 || width <= 0 || height <= 0 || components <= 0)
                    throw new InvalidDataException("Повреждённый JPEG: некорректный SOF.");

                compression = marker is 0xC3 or 0xC7 or 0xCB or 0xCF
                    ? "JPEG (lossless)"
                    : "JPEG (DCT)";

                s.Position = dataEnd;
                continue;
            }

            s.Position = dataEnd;
        }

        if (width is null || height is null || bits is null || components is null)
            throw new InvalidDataException("В JPEG не найден корректный кадр SOF.");

        if (hasArithmetic)
            compression += ", arithmetic coding";
        else if (hasHuffman)
            compression += ", Huffman coding";

        string depth = $"{bits.Value * components.Value} бит/пиксель ({bits.Value} бит/канал, {components.Value} кан.)";

        return new ImageInfo
        {
            Format = "JPEG",
            Dimensions = $"{width} × {height}",
            Resolution = FormatDpi(xDpi, yDpi),
            ColorDepth = depth,
            Compression = compression
        };
    }

    private static ImageInfo ParseTiff(Stream s)
    {
        if (s.Length < 8)
            throw new InvalidDataException("Файл слишком короткий для TIFF.");

        Span<byte> header = stackalloc byte[8];
        ReadExactly(s, header);

        bool littleEndian = header[0] == 'I' && header[1] == 'I';
        bool bigEndian = header[0] == 'M' && header[1] == 'M';
        if (!littleEndian && !bigEndian)
            throw new InvalidDataException("Файл не является TIFF.");

        ushort magic = U16(header[2..4], littleEndian);
        if (magic != 42)
            throw new InvalidDataException("Поддерживается классический TIFF (magic = 42). BigTIFF не поддерживается.");

        uint ifdOffset = U32(header[4..8], littleEndian);
        if (ifdOffset > s.Length - 2)
            throw new InvalidDataException("Повреждённый TIFF: неверное смещение IFD.");

        s.Position = ifdOffset;
        ushort entryCount = ReadU16(s, littleEndian);
        if ((long)entryCount * 12 > s.Length - s.Position)
            throw new InvalidDataException("Повреждённый TIFF: IFD выходит за пределы файла.");

        int? width = null;
        int? height = null;
        double? xRes = null;
        double? yRes = null;
        ushort resolutionUnit = 2;
        string compression = "не задано";
        List<ushort> bitsPerSample = new();

        for (int i = 0; i < entryCount; i++)
        {
            Span<byte> entry = stackalloc byte[12];
            ReadExactly(s, entry);

            ushort tag = U16(entry[..2], littleEndian);
            ushort type = U16(entry[2..4], littleEndian);
            uint count = U32(entry[4..8], littleEndian);
            uint valueOrOffset = U32(entry[8..12], littleEndian);

            switch (tag)
            {
                case 256:
                    width = ReadSingleIntTag(s, type, count, valueOrOffset, littleEndian);
                    break;
                case 257:
                    height = ReadSingleIntTag(s, type, count, valueOrOffset, littleEndian);
                    break;
                case 258:
                    bitsPerSample = ReadShortArray(s, type, count, valueOrOffset, littleEndian);
                    break;
                case 259:
                    compression = TiffCompressionName(ReadSingleUIntTag(s, type, count, valueOrOffset, littleEndian));
                    break;
                case 282:
                    xRes = ReadRational(s, type, count, valueOrOffset, littleEndian);
                    break;
                case 283:
                    yRes = ReadRational(s, type, count, valueOrOffset, littleEndian);
                    break;
                case 296:
                    resolutionUnit = (ushort)ReadSingleUIntTag(s, type, count, valueOrOffset, littleEndian);
                    break;
            }
        }

        if (width is not > 0 || height is not > 0)
            throw new InvalidDataException("В TIFF не найдены корректные теги ImageWidth/ImageLength.");

        if (resolutionUnit == 3)
        {
            if (xRes.HasValue)
                xRes *= 2.54;
            if (yRes.HasValue)
                yRes *= 2.54;
        }
        else if (resolutionUnit != 2)
        {
            xRes = null;
            yRes = null;
        }

        string depth = bitsPerSample.Count > 0
            ? $"{bitsPerSample.Sum((ushort a) => a)} бит/пиксель ({string.Join(", ", bitsPerSample)} бит/канал)"
            : "не задано";

        return new ImageInfo
        {
            Format = "TIFF",
            Dimensions = $"{width} × {height}",
            Resolution = FormatDpi(xRes, yRes),
            ColorDepth = depth,
            Compression = compression
        };
    }

    private static (double? x, double? y) TryReadExifDpi(byte[] data, int tiffStart)
    {
        try
        {
            if (data.Length < tiffStart + 8)
                return (null, null);

            bool little = data[tiffStart] == 'I' && data[tiffStart + 1] == 'I';
            bool big = data[tiffStart] == 'M' && data[tiffStart + 1] == 'M';
            if (!little && !big)
                return (null, null);

            ushort magic = U16(data.AsSpan(tiffStart + 2, 2), little);
            if (magic != 42)
                return (null, null);

            uint ifdOffset = U32(data.AsSpan(tiffStart + 4, 4), little);
            int ifd = checked(tiffStart + checked((int)ifdOffset));
            if (ifd < 0 || ifd + 2 > data.Length)
                return (null, null);

            ushort count = U16(data.AsSpan(ifd, 2), little);
            double? x = null;
            double? y = null;
            ushort unit = 2;

            for (int i = 0; i < count; i++)
            {
                int p = checked(ifd + 2 + i * 12);
                if (p < 0 || p + 12 > data.Length)
                    break;

                ushort tag = U16(data.AsSpan(p, 2), little);
                ushort type = U16(data.AsSpan(p + 2, 2), little);
                uint cnt = U32(data.AsSpan(p + 4, 4), little);
                uint off = U32(data.AsSpan(p + 8, 4), little);

                if (tag == 282)
                    x = ReadRationalFromArray(data, tiffStart, type, cnt, off, little);
                else if (tag == 283)
                    y = ReadRationalFromArray(data, tiffStart, type, cnt, off, little);
                else if (tag == 296)
                    unit = (ushort)ReadInlineUIntFromArray(data, p, type, cnt, off, little);
            }

            if (unit == 3)
            {
                if (x.HasValue)
                    x *= 2.54;
                if (y.HasValue)
                    y *= 2.54;
            }
            else if (unit != 2)
            {
                x = null;
                y = null;
            }

            return (x, y);
        }
        catch
        {
            return (null, null);
        }
    }

    private static double? ReadRational(Stream s, ushort type, uint count, uint offset, bool little)
    {
        if (type != 5 || count < 1)
            return null;

        if ((long)offset > s.Length - 8)
            return null;

        long original = s.Position;
        try
        {
            s.Position = offset;
            uint numerator = ReadU32(s, little);
            uint denominator = ReadU32(s, little);
            return denominator == 0 ? null : (double)numerator / denominator;
        }
        finally
        {
            s.Position = original;
        }
    }

    private static double? ReadRationalFromArray(
        byte[] data,
        int tiffStart,
        ushort type,
        uint count,
        uint offset,
        bool little)
    {
        if (type != 5 || count < 1)
            return null;

        int p = checked(tiffStart + checked((int)offset));
        if (p < 0 || p + 8 > data.Length)
            return null;

        uint numerator = U32(data.AsSpan(p, 4), little);
        uint denominator = U32(data.AsSpan(p + 4, 4), little);
        return denominator == 0 ? null : (double)numerator / denominator;
    }

    private static List<ushort> ReadShortArray(
        Stream s,
        ushort type,
        uint count,
        uint valueOrOffset,
        bool little)
    {
        var result = new List<ushort>();
        if (type != 3 || count == 0 || count > 1000)
            return result;

        long bytes = checked((long)count * 2);
        if (bytes <= 4)
        {
            result.Add(little
                ? (ushort)(valueOrOffset & 0xFFFF)
                : (ushort)(valueOrOffset >> 16));

            if (count > 1)
            {
                result.Add(little
                    ? (ushort)(valueOrOffset >> 16)
                    : (ushort)(valueOrOffset & 0xFFFF));
            }

            return result;
        }

        if ((long)valueOrOffset > s.Length - bytes)
            return result;

        long original = s.Position;
        try
        {
            s.Position = valueOrOffset;
            byte[] data = new byte[checked((int)bytes)];
            ReadExactly(s, data);

            for (int i = 0; i + 1 < data.Length; i += 2)
                result.Add(U16(data.AsSpan(i, 2), little));

            return result;
        }
        finally
        {
            s.Position = original;
        }
    }

    private static int ReadSingleIntTag(Stream s, ushort type, uint count, uint valueOrOffset, bool little)
    {
        if (count < 1)
            return 0;

        uint value = ReadSingleUIntTag(s, type, count, valueOrOffset, little);
        if (value > int.MaxValue)
            throw new InvalidDataException("TIFF содержит размер изображения, превышающий поддерживаемый диапазон.");
        return (int)value;
    }

    private static uint ReadSingleUIntTag(Stream s, ushort type, uint count, uint valueOrOffset, bool little)
    {
        if (count < 1)
            return 0;

        return type switch
        {
            3 => little
                ? valueOrOffset & 0xFFFF
                : valueOrOffset >> 16,
            4 => valueOrOffset,
            _ => 0
        };
    }

    private static long ReadInlineUIntFromArray(
        byte[] data,
        int entryStart,
        ushort type,
        uint count,
        uint value,
        bool little)
    {
        if (count < 1)
            return 0;

        return type switch
        {
            3 => little
                ? U16(data.AsSpan(entryStart + 8, 2), true)
                : U16(data.AsSpan(entryStart + 8, 2), false),
            4 => value,
            _ => 0
        };
    }

    private static bool IsStartOfFrame(int marker) =>
        marker is
            0xC0 or 0xC1 or 0xC2 or 0xC3 or
            0xC5 or 0xC6 or 0xC7 or
            0xC9 or 0xCA or 0xCB or
            0xCD or 0xCE or 0xCF;

    private static string BmpCompressionName(uint code) => code switch
    {
        0 => "нет (BI_RGB)",
        1 => "RLE8",
        2 => "RLE4",
        3 => "BITFIELDS",
        4 => "JPEG",
        5 => "PNG",
        6 => "ALPHABITFIELDS",
        _ => $"код {code}"
    };

    private static string TiffCompressionName(uint code) => code switch
    {
        1 => "нет",
        2 => "CCITT RLE",
        3 => "CCITT Group 3",
        4 => "CCITT Group 4",
        5 => "LZW",
        6 => "JPEG (старый TIFF/JPEG)",
        7 => "JPEG",
        8 => "Deflate",
        32773 => "PackBits",
        32946 => "Deflate",
        _ => $"код {code}"
    };

    private static string FormatDpi(double? x, double? y)
    {
        if (!x.HasValue && !y.HasValue)
            return "не задано";

        if (x.HasValue && y.HasValue)
        {
            return $"{x.Value.ToString("0.##", CultureInfo.InvariantCulture)} × {y.Value.ToString("0.##", CultureInfo.InvariantCulture)} dpi";
        }

        return $"{(x ?? y)!.Value.ToString("0.##", CultureInfo.InvariantCulture)} dpi";
    }

    private static string FormatFileSize(long bytes)
    {
        const double k = 1024;
        return bytes switch
        {
            < 1024 => $"{bytes} Б",
            < 1024 * 1024 => $"{bytes / k:0.##} КБ",
            < 1024L * 1024 * 1024 => $"{bytes / (k * k):0.##} МБ",
            _ => $"{bytes / (k * k * k):0.##} ГБ"
        };
    }

    private static string TryGetFileSize(string path)
    {
        try
        {
            return FormatFileSize(new FileInfo(path).Length);
        }
        catch
        {
            return "—";
        }
    }

    private static ushort ReadUInt16BE(Stream s)
    {
        Span<byte> buffer = stackalloc byte[2];
        ReadExactly(s, buffer);
        return BinaryPrimitives.ReadUInt16BigEndian(buffer);
    }

    private static ushort ReadU16(Stream s, bool little)
    {
        Span<byte> buffer = stackalloc byte[2];
        ReadExactly(s, buffer);
        return U16(buffer, little);
    }

    private static uint ReadU32(Stream s, bool little)
    {
        Span<byte> buffer = stackalloc byte[4];
        ReadExactly(s, buffer);
        return U32(buffer, little);
    }

    private static ushort U16(ReadOnlySpan<byte> bytes, bool little) =>
        little
            ? BinaryPrimitives.ReadUInt16LittleEndian(bytes)
            : BinaryPrimitives.ReadUInt16BigEndian(bytes);

    private static uint U32(ReadOnlySpan<byte> bytes, bool little) =>
        little
            ? BinaryPrimitives.ReadUInt32LittleEndian(bytes)
            : BinaryPrimitives.ReadUInt32BigEndian(bytes);

    private static void EnsureAvailable(Stream s, long bytes)
    {
        if (bytes < 0 || bytes > s.Length - s.Position)
            throw new EndOfStreamException();
    }

    private static void ReadExactly(Stream s, Span<byte> buffer)
    {
        int read = 0;
        while (read < buffer.Length)
        {
            int n = s.Read(buffer[read..]);
            if (n <= 0)
                throw new EndOfStreamException();
            read += n;
        }
    }

    private static void ReadExactly(Stream s, byte[] buffer) =>
        ReadExactly(s, buffer.AsSpan());
}
