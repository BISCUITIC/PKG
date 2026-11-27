using RasterEngine.Interfaces;

namespace RasterEngine.Algorithms;

public class BresenhamCircle : ICircleRasterizationAlgorithm
{
    public void Draw(
        IRasterTarget target,
        int centerX,
        int centerY,
        int radius)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius));

        long x = 0;
        long y = radius;

        long decision = 3 - 2 * (long)radius;

        while (x <= y)
        {
            DrawSymmetricPoints(
                target,
                centerX,
                centerY,
                x,
                y);

            if (decision < 0)
            {
                decision += 4 * x + 6;
            }
            else
            {
                decision += 4 * (x - y) + 10;
                y--;
            }

            x++;
        }
    }

    private static void DrawSymmetricPoints(
        IRasterTarget target,
        int centerX,
        int centerY,
        long x,
        long y)
    {
        target.SetPixel(centerX + (int)x, centerY + (int)y, 1.0);
        target.SetPixel(centerX + (int)y, centerY + (int)x, 1.0);

        target.SetPixel(centerX - (int)y, centerY + (int)x, 1.0);
        target.SetPixel(centerX - (int)x, centerY + (int)y, 1.0);

        target.SetPixel(centerX - (int)x, centerY - (int)y, 1.0);
        target.SetPixel(centerX - (int)y, centerY - (int)x, 1.0);

        target.SetPixel(centerX + (int)y, centerY - (int)x, 1.0);
        target.SetPixel(centerX + (int)x, centerY - (int)y, 1.0);
    }
}