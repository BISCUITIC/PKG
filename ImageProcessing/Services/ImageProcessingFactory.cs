using ImageProcessing.Models;
using ImageProcessing.Services.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing.Services;

internal static class ImageProcessingFactory
{   
    private static readonly Dictionary<ProcessingMethodType, IProcessintMethod> _strategies = new()
    {
        [ProcessingMethodType.Niblack] = new LocalThresholdStrategy(LocalThresholdKind.Niblack),
        [ProcessingMethodType.Sauvola] = new LocalThresholdStrategy(LocalThresholdKind.Sauvola),
        [ProcessingMethodType.AdaptiveMean] = new LocalThresholdStrategy(gaussian: false),
        [ProcessingMethodType.AdaptiveGaussian] = new LocalThresholdStrategy(gaussian: true),

        [ProcessingMethodType.Point] = new KernelDetectionStrategy(new[,] { { -1, -1, -1 }, { -1, 8, -1 }, { -1, -1, -1 } }),
        [ProcessingMethodType.HorizontalLine] = new KernelDetectionStrategy(new[,] { { -1, -1, -1 }, { 2, 2, 2 }, { -1, -1, -1 } }),
        [ProcessingMethodType.VerticalLine] = new KernelDetectionStrategy(new[,] { { -1, 2, -1 }, { -1, 2, -1 }, { -1, 2, -1 } }),
        [ProcessingMethodType.DiagonalDownLine] = new KernelDetectionStrategy(new[,] { { 2, -1, -1 }, { -1, 2, -1 }, { -1, -1, 2 } }),
        [ProcessingMethodType.DiagonalUpLine] = new KernelDetectionStrategy(new[,] { { -1, -1, 2 }, { -1, 2, -1 }, { 2, -1, -1 } }),

        [ProcessingMethodType.Sobel] = new SobelEdgeDetectionStrategy()
    };
    
    public static IProcessintMethod GetStrategy(ProcessingMethodType method)
    {
        if (!_strategies.TryGetValue(method, out var strategy))
        {
            throw new ArgumentOutOfRangeException(nameof(method), method, "Неизвестный метод обработки.");
        }
        return strategy;
    }
   
    public static bool TryGetStrategyFromTag(string tag, out IProcessintMethod? strategy)
    {
        strategy = null;
        if (Enum.TryParse<ProcessingMethodType>(tag, ignoreCase: true, out var method))
        {
            return _strategies.TryGetValue(method, out strategy);
        }
        return false;
    }
}
