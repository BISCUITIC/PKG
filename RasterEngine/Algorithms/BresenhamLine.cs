using RasterEngine.Interfaces;

namespace RasterEngine.Algorithms;

public class BresenhamLine : ILineRasterizationAlgorithm
{
    public void Draw(
        IRasterTarget target,
        int x1,
        int y1,
        int x2,
        int y2)
    {
        ArgumentNullException.ThrowIfNull(target);

        long dx = Math.Abs((long)x2 - x1);
        long dy = Math.Abs((long)y2 - y1);

        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;

        long error = dx - dy;

        int x = x1;
        int y = y1;

        while (true)
        {
            target.SetPixel(x, y, 1.0);

            // Достигли конечной точки.
            if (x == x2 && y == y2)
                break;

            long doubledError = 2 * error;

            if (doubledError > -dy)
            {
                error -= dy;
                x += sx;
            }

            if (doubledError < dx)
            {
                error += dx;
                y += sy;
            }
        }
    }
}