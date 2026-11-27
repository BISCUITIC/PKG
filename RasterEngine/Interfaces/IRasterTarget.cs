namespace RasterEngine.Interfaces;

public interface IRasterTarget
{
    void SetPixel(
        int x, 
        int y, 
        double intensity);
}
