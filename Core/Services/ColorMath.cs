using Core.Models.Colors;
using Core.Models.Result;

namespace Core.Services;

internal static class ColorMath
{
    internal const double WhiteX = 95.045592705167;
    internal const double WhiteY = 100.0;
    internal const double WhiteZ = 108.905775075988;

    private const double Delta = 6.0 / 29.0;
    private const double GamutTolerance = 1e-7;

    internal static double DecodeSrgb(double value)
    {
        return value <= 0.04045
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static double EncodeSrgb(double value)
    {
        return value <= 0.0031308
            ? 12.92 * value
            : 1.055 * Math.Pow(value, 1.0 / 2.4) - 0.055;
    }

    internal static double LabFunction(double value)
    {
        return value > Delta * Delta * Delta
            ? Math.Cbrt(value)
            : value / (3 * Delta * Delta) + 4.0 / 29.0;
    }

    internal static double InverseLabFunction(double value)
    {
        return value > Delta
            ? value * value * value
            : 3 * Delta * Delta * (value - 4.0 / 29.0);
    }

    internal static ConversionResult<RgbColor> CreateRgbResult(
        double r,
        double g,
        double b)
    {
        ValidateFinite(r, g, b);

        bool wasClipped =
            IsOutsideGamut(r) ||
            IsOutsideGamut(g) ||
            IsOutsideGamut(b);

        r = Math.Clamp(r, 0.0, 1.0);
        g = Math.Clamp(g, 0.0, 1.0);
        b = Math.Clamp(b, 0.0, 1.0);

        var color = new RgbColor(
            Math.Clamp(EncodeSrgb(r) * 255, 0.0, 255.0),
            Math.Clamp(EncodeSrgb(g) * 255, 0.0, 255.0),
            Math.Clamp(EncodeSrgb(b) * 255, 0.0, 255.0)
        );

        return new ConversionResult<RgbColor>(color, wasClipped);
    }

    private static bool IsOutsideGamut(double value)
    {
        return value < -GamutTolerance || value > 1.0 + GamutTolerance;
    }

    internal static void ValidateRgb(RgbColor color)
    {
        ValidateFinite(color.R, color.G, color.B);

        if (color.R < 0 || color.R > 255 ||
            color.G < 0 || color.G > 255 ||
            color.B < 0 || color.B > 255)
        {
            throw new ArgumentOutOfRangeException(
                nameof(color), "Компоненты RGB должны находиться в диапазоне 0–255."
            );
        }
    }

    internal static void ValidateFinite(
        double first,
        double second,
        double third)
    {
        if (!double.IsFinite(first) ||
            !double.IsFinite(second) ||
            !double.IsFinite(third))
        {
            throw new ArgumentException(
                "Координаты должны быть конечными числами. " +
                "Возможно, введены слишком большие значения."
            );
        }
    }
}