using Core.Models.Colors;
using Core.Models.Result;

namespace Core.Interfaces;

public interface ILabConverter
{
    ConversionResult<RgbColor> ToRgb(LabColor color);
    XyzColor ToXyz(LabColor color);
}
