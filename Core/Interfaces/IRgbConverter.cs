using Core.Models.Colors;

namespace Core.Interfaces;

public interface IRgbConverter
{
    XyzColor ToXyz(RgbColor color);
    LabColor ToLab(RgbColor color);
}
