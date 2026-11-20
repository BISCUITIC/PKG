using RasterAlgorithms;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BasicRasterAlgorithms.Controls;

public sealed class GridPainter : FrameworkElement
{
    private const double BASE_CELL_SIZE = 24.0; 

    private double CellSize => BASE_CELL_SIZE * Zoom;

    private const double MIN_ZOOM = 0.25;
    private const double MAX_ZOOM = 4.0;
    private const double ZOOM_FACTOR = 1.1;

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(
            nameof(Source),
            typeof(VirtualGrid),
            typeof(GridPainter),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public VirtualGrid Source
    {
        get => (VirtualGrid)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(
            nameof(Zoom),
            typeof(double),
            typeof(GridPainter),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender),
            ValidateZoom);

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public static readonly DependencyProperty OffsetProperty =
        DependencyProperty.Register(
            nameof(Offset),
            typeof(Vector),
            typeof(GridPainter),
            new FrameworkPropertyMetadata(new Vector(), FrameworkPropertyMetadataOptions.AffectsRender));

    public Vector Offset
    {
        get => (Vector)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    public GridPainter()
    {
        ClipToBounds = true;
    }

    private static bool ValidateZoom(object value)
    {
        double zoom = (double)value;

        return !double.IsNaN(zoom)
            && !double.IsInfinity(zoom)
            && zoom >= MIN_ZOOM
            && zoom <= MAX_ZOOM;
    }

    private Point GetOrigin()
    {
        return new Point(
            ActualWidth / 2 + Offset.X,
            ActualHeight / 2 + Offset.Y);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (e.Delta == 0)
            return;

        Point mousePosition = e.GetPosition(this);

        Point origin = GetOrigin();

        double oldZoom = Zoom;

        // Вычисляем новый масштаб.
        double factor = Math.Pow(
            ZOOM_FACTOR,
            e.Delta / 120.0);

        double newZoom = Math.Clamp(
            oldZoom * factor,
            MIN_ZOOM,
            MAX_ZOOM);

        // Если масштаб не изменился, ничего не делаем.
        if (newZoom == oldZoom)
        {
            e.Handled = true;
            return;
        }

        // Фактический коэффициент изменения масштаба.
        double scaleFactor = newZoom / oldZoom;

        // Корректируем положение координатной плоскости.
        Vector cursorOffset = mousePosition - origin;

        Offset = new Vector(
            Offset.X + cursorOffset.X * (1 - scaleFactor),
            Offset.Y + cursorOffset.Y * (1 - scaleFactor));

        Zoom = newZoom;

        e.Handled = true;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        Point origin = GetOrigin();

        dc.DrawRectangle(
            Brushes.White,
            null,
            new Rect(0, 0, ActualWidth, ActualHeight));

        DrawGrid(dc, origin);
        DrawAxes(dc, origin);

        if (Source != null)
            DrawPixels(dc, origin);
    }

    private static readonly Pen GridPen = CreateGridPen();

    private static Pen CreateGridPen()
    {
        Pen pen = new Pen(new SolidColorBrush(Color.FromRgb(220, 225, 232)), 1.0);

        pen.Freeze();

        return pen;
    }

    private void DrawGrid(DrawingContext dc, Point origin)
    {
        double cellSize = CellSize;

        // Вертикальные линии справа.
        for (double x = origin.X;
             x <= ActualWidth;
             x += cellSize)
        {
            dc.DrawLine(
                GridPen,
                new Point(x, 0),
                new Point(x, ActualHeight));
        }

        // Вертикальные линии слева.
        for (double x = origin.X - cellSize;
             x >= 0;
             x -= cellSize)
        {
            dc.DrawLine(
                GridPen,
                new Point(x, 0),
                new Point(x, ActualHeight));
        }

        // Горизонтальные линии снизу.
        for (double y = origin.Y;
             y <= ActualHeight;
             y += cellSize)
        {
            dc.DrawLine(
                GridPen,
                new Point(0, y),
                new Point(ActualWidth, y));
        }

        // Горизонтальные линии сверху.
        for (double y = origin.Y - cellSize;
             y >= 0;
             y -= cellSize)
        {
            dc.DrawLine(
                GridPen,
                new Point(0, y),
                new Point(ActualWidth, y));
        }
    }

    private static readonly Pen AxisPen = CreateAxisPen();

    private static Pen CreateAxisPen()
    {
        Pen pen = new Pen(new SolidColorBrush(Color.FromRgb(71, 85, 105)), 1.5);

        pen.Freeze();

        return pen;
    }

    private void DrawAxes(DrawingContext dc, Point origin)
    {
        dc.DrawLine(AxisPen, new Point(0, origin.Y), new Point(ActualWidth, origin.Y));
        dc.DrawLine(AxisPen, new Point(origin.X, 0), new Point(origin.X, ActualHeight));
    }

    private static readonly Brush PixelBrush = CreatePixelBrush();

    private static Brush CreatePixelBrush()
    {
        Brush brush = new SolidColorBrush(
            Color.FromRgb(37, 99, 235));

        brush.Freeze();

        return brush;
    }

    private void DrawPixels(DrawingContext dc, Point origin)
    {
        double cellSize = CellSize;

        for (int row = 0; row < Source.Height; row++)
        {
            for (int column = 0; column < Source.Width; column++)
            {
                double intensity = Source.GetIntensityAtIndex(row, column);

                if (intensity <= 0)
                    continue;

                int x = Source.GetGridXFromColumn(column);
                int y = Source.GetGridYFromRow(row);

                double screenX = origin.X + x * cellSize;
                double screenY = origin.Y - (y + 1) * cellSize;
                
                if (screenX + cellSize <= 0 ||
                    screenX >= ActualWidth ||
                    screenY + cellSize <= 0 ||
                    screenY >= ActualHeight)
                {
                    continue;
                }


                double inset = Math.Min(1.0, cellSize * 0.08);

                Rect pixel = new Rect(
                    screenX + inset,
                    screenY + inset,
                    cellSize - 2 * inset,
                    cellSize - 2 * inset);
                
                dc.PushOpacity(intensity);
                dc.DrawRectangle(PixelBrush, null, pixel);
                dc.Pop();
            }
        }
    }
}