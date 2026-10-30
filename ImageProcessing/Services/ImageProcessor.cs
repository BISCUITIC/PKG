using ImageProcessing.Services.Methods;

namespace ImageProcessing.Services;

internal static class ImageProcessor
{
    private static double RectangleSum(double[] integral, int stride, int x0, int y0, int x1, int y1)
    {
        return integral[y1 * stride + x1]
             - integral[y0 * stride + x1]
             - integral[y1 * stride + x0]
             + integral[y0 * stride + x0];
    }

    private static void BuildIntegralImages(byte[] gray, int width, int height, out double[] sum, out double[] squareSum)
    {
        int stride = width + 1;
        int length = checked(stride * (height + 1));
        sum = new double[length];
        squareSum = new double[length];

        for (int y = 1; y <= height; y++)
        {
            double rowSum = 0;
            double rowSquareSum = 0;
            int sourceRow = (y - 1) * width;
            int destinationRow = y * stride;
            int previousRow = (y - 1) * stride;

            for (int x = 1; x <= width; x++)
            {
                double value = gray[sourceRow + x - 1];
                rowSum += value;
                rowSquareSum += value * value;
                sum[destinationRow + x] = sum[previousRow + x] + rowSum;
                squareSum[destinationRow + x] = squareSum[previousRow + x] + rowSquareSum;
            }
        }
    }
    public static int NormalizeWindowSize(int windowSize)
    {
        windowSize = Math.Clamp(windowSize, 3, 101);
        if (windowSize % 2 == 0)
            windowSize = windowSize == 101 ? 101 : windowSize + 1;
        return windowSize;
    }

    public static byte[] LocalThreshold(byte[] gray, int width, int height, int windowSize, double parameter, LocalThresholdKind kind)
    {
        BuildIntegralImages(gray, width, height, out double[] sum, out double[] squareSum);
        int integralWidth = width + 1;
        int radius = windowSize / 2;
        var result = new byte[gray.Length];

        for (int y = 0; y < height; y++)
        {
            int y0 = Math.Max(0, y - radius);
            int y1 = Math.Min(height, y + radius + 1);
            for (int x = 0; x < width; x++)
            {
                int x0 = Math.Max(0, x - radius);
                int x1 = Math.Min(width, x + radius + 1);
                double localSum = RectangleSum(sum, integralWidth, x0, y0, x1, y1);
                double localSquareSum = RectangleSum(squareSum, integralWidth, x0, y0, x1, y1);
                int count = (x1 - x0) * (y1 - y0);
                double mean = localSum / count;
                double variance = Math.Max(0, localSquareSum / count - mean * mean);
                double standardDeviation = Math.Sqrt(variance);

                double threshold = kind switch
                {
                    LocalThresholdKind.Niblack => mean + parameter * standardDeviation,                    
                    LocalThresholdKind.Sauvola => mean * (1 + parameter * (standardDeviation / 128.0 - 1)),
                    _ => mean
                };

                result[y * width + x] = gray[y * width + x] < threshold ? (byte)0 : (byte)255;
            }
        }

        return result;
    }

    public static byte[] DetectEdgesSobel(byte[] gray, int width, int height, double threshold)
    {
        threshold = Math.Max(0, threshold);
        int[,] gxKernel = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
        int[,] gyKernel = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };
        var result = new byte[gray.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int gx = 0;
                int gy = 0;
                for (int ky = -1; ky <= 1; ky++)
                {
                    int sy = y + ky;
                    if (sy < 0 || sy >= height) continue;
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        int sx = x + kx;
                        if (sx < 0 || sx >= width) continue;
                        int value = gray[sy * width + sx];
                        gx += value * gxKernel[ky + 1, kx + 1];
                        gy += value * gyKernel[ky + 1, kx + 1];
                    }
                }

                double magnitude = Math.Sqrt((double)gx * gx + (double)gy * gy);
                result[y * width + x] = magnitude >= threshold ? (byte)255 : (byte)0;
            }
        }

        return result;
    }

    public static byte[] DetectWithKernel(byte[] gray, int width, int height, int[,] kernel, double threshold)
    {
        threshold = Math.Max(0, threshold);
        var result = new byte[gray.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int response = 0;
                for (int ky = -1; ky <= 1; ky++)
                {
                    int sy = y + ky;
                    if (sy < 0 || sy >= height) continue;
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        int sx = x + kx;
                        if (sx < 0 || sx >= width) continue;
                        response += gray[sy * width + sx] * kernel[ky + 1, kx + 1];
                    }
                }
                result[y * width + x] = Math.Abs(response) >= threshold ? (byte)255 : (byte)0;
            }
        }

        return result;
    }

    public static byte[] AdaptiveThreshold(byte[] gray, int width, int height, int windowSize, double constant, bool gaussian)
    {
        byte[] localMeans = gaussian
            ? GaussianBlur(gray, width, height, windowSize)
            : BoxBlur(gray, width, height, windowSize);

        var result = new byte[gray.Length];
        for (int i = 0; i < gray.Length; i++)
        {
            double threshold = localMeans[i] - constant;
            result[i] = gray[i] < threshold ? (byte)0 : (byte)255;
        }
        return result;
    }

    private static byte[] BoxBlur(byte[] gray, int width, int height, int windowSize)
    {
        BuildIntegralImages(gray, width, height, out double[] sum, out _);
        int stride = width + 1;
        int radius = windowSize / 2;
        var result = new byte[gray.Length];

        for (int y = 0; y < height; y++)
        {
            int y0 = Math.Max(0, y - radius);
            int y1 = Math.Min(height, y + radius + 1);
            for (int x = 0; x < width; x++)
            {
                int x0 = Math.Max(0, x - radius);
                int x1 = Math.Min(width, x + radius + 1);
                double total = RectangleSum(sum, stride, x0, y0, x1, y1);
                int count = (x1 - x0) * (y1 - y0);
                result[y * width + x] = (byte)Math.Clamp((int)Math.Round(total / count), 0, 255);
            }
        }
        return result;
    }

    private static byte[] GaussianBlur(byte[] gray, int width, int height, int windowSize)
    {
        int radius = windowSize / 2;
        double sigma = Math.Max(0.8, windowSize / 6.0);
        var kernel = new double[windowSize];
        double kernelSum = 0;
        for (int i = -radius; i <= radius; i++)
        {
            double weight = Math.Exp(-(i * i) / (2 * sigma * sigma));
            kernel[i + radius] = weight;
            kernelSum += weight;
        }
        for (int i = 0; i < kernel.Length; i++) kernel[i] /= kernelSum;

        var horizontal = new double[gray.Length];
        var result = new byte[gray.Length];

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                double total = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int sx = Math.Clamp(x + k, 0, width - 1);
                    total += gray[row + sx] * kernel[k + radius];
                }
                horizontal[row + x] = total;
            }
        }
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double total = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int sy = Math.Clamp(y + k, 0, height - 1);
                    total += horizontal[sy * width + x] * kernel[k + radius];
                }
                result[y * width + x] = (byte)Math.Clamp((int)Math.Round(total), 0, 255);
            }
        }

        return result;
    }

}
