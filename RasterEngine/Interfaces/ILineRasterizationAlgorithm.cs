namespace RasterEngine.Interfaces;

public interface ILineRasterizationAlgorithm
{
    void Draw(
        IRasterTarget target,
        int x1,
        int y1,
        int x2,
        int y2);
}