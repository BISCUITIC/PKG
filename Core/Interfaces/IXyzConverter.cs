using Core.Models.Colors;
using Core.Models.Result;

namespace Core.Interfaces;

public interface IXyzConverter
{
    ConversionResult<RgbColor> ToRgb(XyzColor color);
    LabColor ToLab(XyzColor color);
}
