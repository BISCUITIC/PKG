using ImageProcessing.Models;
using static ImageProcessing.Services.ImageProcessor;

namespace ImageProcessing.Services.Methods;

internal enum LocalThresholdKind { Niblack, Sauvola }

internal class LocalThresholdStrategy : IProcessintMethod
{    
    private readonly LocalThresholdKind _kind;
    private readonly bool _isAdaptive;
    private readonly bool _gaussian;

    public LocalThresholdStrategy(LocalThresholdKind kind)
    {
        _kind = kind;
        _isAdaptive = false;
    }
    
    public LocalThresholdStrategy(bool gaussian)
    {
        _gaussian = gaussian;
        _isAdaptive = true;
    }

    public GrayImage Process(GrayImage image, int windowSize, double parameter)
    {
        byte[] resultPixels = _isAdaptive
            ? AdaptiveThreshold(image.Pixels, image.Width, image.Height, windowSize, parameter, _gaussian)
            : LocalThreshold(image.Pixels, image.Width, image.Height, windowSize, parameter, _kind);

        return new GrayImage(resultPixels, image.Width, image.Height);
    }
}
