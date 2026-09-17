using Core.Interfaces;
using Core.Models.Colors;
using static Core.Services.ColorMath;

namespace Core.Services;

public sealed class RgbConverter : IRgbConverter
{
    public XyzColor ToXyz(RgbColor color)
    {
        ValidateRgb(color);

        double r = DecodeSrgb(color.R / 255.0);
        double g = DecodeSrgb(color.G / 255.0);
        double b = DecodeSrgb(color.B / 255.0);

        double x = 100 * (
            0.4123907993 * r +
            0.3575843394 * g +
            0.1804807884 * b);

        double y = 100 * (
            0.2126390059 * r +
            0.7151686788 * g +
            0.0721923154 * b);

        double z = 100 * (
            0.0193308187 * r +
            0.1191947798 * g +
            0.9505321522 * b);

        return new XyzColor(x, y, z);
    }

    public LabColor ToLab(RgbColor color)
    {
        ValidateRgb(color);

        double r = DecodeSrgb(color.R / 255.0);
        double g = DecodeSrgb(color.G / 255.0);
        double b = DecodeSrgb(color.B / 255.0);

        double x = 100 * (
            0.4123907993 * r +
            0.3575843394 * g +
            0.1804807884 * b);

        double y = 100 * (
            0.2126390059 * r +
            0.7151686788 * g +
            0.0721923154 * b);

        double z = 100 * (
            0.0193308187 * r +
            0.1191947798 * g +
            0.9505321522 * b);

        double fx = LabFunction(x / WhiteX);
        double fy = LabFunction(y / WhiteY);
        double fz = LabFunction(z / WhiteZ);

        return new LabColor(
            116 * fy - 16,
            500 * (fx - fy),
            200 * (fy - fz));
    }
}
