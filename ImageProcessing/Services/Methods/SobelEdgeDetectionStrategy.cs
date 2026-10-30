using ImageProcessing.Models;
using static ImageProcessing.Services.ImageProcessor;

namespace ImageProcessing.Services.Methods;

internal class SobelEdgeDetectionStrategy : IProcessintMethod
{
    public GrayImage Process(GrayImage image, int windowSize, double parameter)
    {
        byte[] resultPixels = DetectEdgesSobel(image.Pixels, image.Width, image.Height, parameter);
        return new GrayImage(resultPixels, image.Width, image.Height);
    }
}
