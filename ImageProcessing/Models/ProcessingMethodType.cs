namespace ImageProcessing.Models;

internal enum ProcessingMethodType
{
    Niblack,
    Sauvola,
    AdaptiveMean,
    AdaptiveGaussian,
    Point,
    HorizontalLine,
    VerticalLine,
    DiagonalDownLine,
    DiagonalUpLine,
    Sobel
}
