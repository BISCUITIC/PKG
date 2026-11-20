using RasterEngine.Interfaces;

namespace RasterEngine.Algorithms;

public static class StepByStepLine
{
    public static void Draw(
        IRasterTarget target,
        int x1,
        int y1,
        int x2,
        int y2)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        long dx = (long)x2 - x1;
        long dy = (long)y2 - y1;

        long absDx = Math.Abs(dx);
        long absDy = Math.Abs(dy);

        // Вырожденный отрезок.
        if (absDx == 0 && absDy == 0)
        {
            target.SetPixel(x1, y1, 1.0);
            return;
        }

        // Преимущественно горизонтальный отрезок.
        if (absDx >= absDy)
        {
            long direction = Math.Sign(dx);

            for (long i = 0; i <= absDx; i++)
            {
                int x = (int)(x1 + direction * i);

                double exactY = y1 + (double)dy * i / absDx;

                int y = (int)Math.Round(
                    exactY,
                    MidpointRounding.AwayFromZero);

                target.SetPixel(x, y, 1.0);
            }
        }
        // Преимущественно вертикальный отрезок.
        else
        {
            long direction = Math.Sign(dy);

            for (long i = 0; i <= absDy; i++)
            {
                int y = (int)(y1 + direction * i);

                double exactX = x1 + (double)dx * i / absDy;

                int x = (int)Math.Round(
                    exactX,
                    MidpointRounding.AwayFromZero);

                target.SetPixel(x, y, 1.0);
            }
        }
    }
}