using RasterEngine.Interfaces;

namespace RasterEngine.Algorithms;

public class DdaLine : ILineRasterizationAlgorithm
{
    public void Draw(
        IRasterTarget target,
        int x1,
        int y1,
        int x2,
        int y2)
    {
        ArgumentNullException.ThrowIfNull(target);

        long dx = (long)x2 - x1;
        long dy = (long)y2 - y1;

        long steps = Math.Max(
            Math.Abs(dx),
            Math.Abs(dy));

        // Вырожденный отрезок.
        if (steps == 0)
        {
            target.SetPixel(x1, y1, 1.0);
            return;
        }

        double xIncrement = (double)dx / steps;
        double yIncrement = (double)dy / steps;

        double x = x1;
        double y = y1;

        for (long i = 0; i <= steps; i++)
        {
            int pixelX = (int)Math.Round(
                x,
                MidpointRounding.AwayFromZero);

            int pixelY = (int)Math.Round(
                y,
                MidpointRounding.AwayFromZero);

            // Гарантируем точное попадание в конечную точку.
            if (i == steps)
            {
                pixelX = x2;
                pixelY = y2;
            }

            target.SetPixel(pixelX, pixelY, 1.0);

            x += xIncrement;
            y += yIncrement;
        }
    }
}