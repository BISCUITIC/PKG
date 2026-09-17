using Core.Interfaces;
using Core.Models.Colors;
using Core.Models.Result;
using static Core.Services.ColorMath;

namespace Core.Services;

public sealed class XyzConverter : IXyzConverter
{
    public ConversionResult<RgbColor> ToRgb(XyzColor color)
    {
        ValidateFinite(color.X, color.Y, color.Z);

        double x = color.X / 100.0;
        double y = color.Y / 100.0;
        double z = color.Z / 100.0;

        double r = 3.2409699419 * x
                  - 1.5373831776 * y
                  - 0.4986107603 * z;

        double g = -0.9692436363 * x
                  + 1.8759675015 * y
                  + 0.0415550574 * z;

        double b = 0.0556300797 * x
                  - 0.2039769589 * y
                  + 1.0569715142 * z;

        return CreateRgbResult(r, g, b);
    }

    public LabColor ToLab(XyzColor color)
    {
        ValidateFinite(color.X, color.Y, color.Z);

        double fx = LabFunction(color.X / WhiteX);
        double fy = LabFunction(color.Y / WhiteY);
        double fz = LabFunction(color.Z / WhiteZ);

        double l = 116 * fy - 16;
        double a = 500 * (fx - fy);
        double b = 200 * (fy - fz);

        ValidateFinite(l, a, b);

        return new LabColor(l, a, b);
    }
}
