using ImageProcessing.Models;
using static ImageProcessing.Services.ImageProcessor;

namespace ImageProcessing.Services.Methods;

internal class KernelDetectionStrategy : IProcessintMethod
{
    private readonly int[,] _kernel;

    public KernelDetectionStrategy(int[,] kernel)
    {
        _kernel = kernel;
    }

    public GrayImage Process(GrayImage image, int windowSize, double parameter)
    {        
        byte[] resultPixels = DetectWithKernel(image.Pixels, image.Width, image.Height, _kernel, parameter);
        return new GrayImage(resultPixels, image.Width, image.Height);
    }
}
