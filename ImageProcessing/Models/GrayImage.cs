using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageProcessing.Models;

internal class GrayImage
{
    public byte[] Pixels { get; }
    public int Width { get; }
    public int Height { get; }
    
    public GrayImage(byte[] pixels, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        if (width <= 0 || height <= 0 || pixels.Length != checked(width * height))
            throw new ArgumentException("Размер массива не соответствует размерам изображения.");

        Pixels = pixels;
        Width = width;
        Height = height;
    }
    
    public static GrayImage FromBitmapSource(BitmapSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        FormatConvertedBitmap converted = 
            new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

        int width = converted.PixelWidth;
        int height = converted.PixelHeight;

        int stride = checked(width * 4);

        byte[] bgra = new byte[checked(stride * height)];

        converted.CopyPixels(bgra, stride, 0);

        byte[] gray = new byte[checked(width * height)];

        for (int i = 0, p = 0; i < gray.Length; i++, p += 4)
        {
            int blue = bgra[p];
            int green = bgra[p + 1];
            int red = bgra[p + 2];

            gray[i] = (byte)((299 * red + 587 * green + 114 * blue + 500) / 1000);
        }

        return new GrayImage(gray, width, height);
    }
    
    public BitmapSource ToBitmapSource()
    {
        int stride = Width; // 1 байт на пиксель для формата Gray8
        return BitmapSource.Create(
            Width,
            Height,
            96, 96,
            PixelFormats.Gray8,
            null,
            Pixels,
            stride);
    }
}
