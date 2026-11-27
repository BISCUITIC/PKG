namespace RasterEngine.Interfaces;

public interface ICircleRasterizationAlgorithm
{
    void Draw(
        IRasterTarget target,
        int centerX,
        int centerY,
        int radius);
}
