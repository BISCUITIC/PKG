using Core.Interfaces;
using Core.Models.Colors;
using Core.Models.Result;
using static Core.Services.ColorMath;

namespace Core.Services;

public sealed class LabConverter : ILabConverter
{
    public ConversionResult<RgbColor> ToRgb(LabColor color)
    {
        ValidateFinite(color.L, color.A, color.B);

        double fy = (color.L + 16) / 116.0;
        double fx = fy + color.A / 500.0;
        double fz = fy - color.B / 200.0;

        double x = WhiteX * InverseLabFunction(fx) / 100.0;
        double y = WhiteY * InverseLabFunction(fy) / 100.0;
        double z = WhiteZ * InverseLabFunction(fz) / 100.0;

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

    public XyzColor ToXyz(LabColor color)
    {
        ValidateFinite(color.L, color.A, color.B);

        double fy = (color.L + 16) / 116.0;
        double fx = fy + color.A / 500.0;
        double fz = fy - color.B / 200.0;

        double x = WhiteX * InverseLabFunction(fx);
        double y = WhiteY * InverseLabFunction(fy);
        double z = WhiteZ * InverseLabFunction(fz);

        ValidateFinite(x, y, z);

        return new XyzColor(x, y, z);
    }
}
