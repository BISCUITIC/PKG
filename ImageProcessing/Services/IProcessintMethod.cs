using ImageProcessing.Models;

namespace ImageProcessing.Services;

internal interface IProcessintMethod
{
    GrayImage Process(GrayImage image, int windowSize, double parameter);
}
