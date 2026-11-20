using RasterEngine.Interfaces;

namespace RasterAlgorithms;

public sealed class VirtualGrid : IRasterTarget
{
    private readonly double[,] _intensities;

    public int Width { get; }
    public int Height { get; }

    public int OriginColumn { get; }
    public int OriginRow { get; }

    public VirtualGrid(int width, int height)
        : this(width, height, width / 2, height / 2) { }

    private VirtualGrid(
        int width,
        int height,
        int originColumn,
        int originRow)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        if (originColumn < 0 || originColumn >= width)
            throw new ArgumentOutOfRangeException(
                nameof(originColumn));

        if (originRow < 0 || originRow >= height)
            throw new ArgumentOutOfRangeException(
                nameof(originRow));

        Width = width;
        Height = height;

        OriginColumn = originColumn;
        OriginRow = originRow;

        _intensities = new double[height, width];
    }

    public void SetPixel(int x, int y, double intensity)
    {
        if (double.IsNaN(intensity) || double.IsInfinity(intensity) || 
            intensity < 0.0 || intensity > 1.0)        
            throw new ArgumentOutOfRangeException(nameof(intensity));        

        long column = (long)x + OriginColumn;
        long row = (long)OriginRow - y;

        if (column < 0 || column >= Width || row < 0 || row >= Height)        
            return;        

        int c = (int)column;
        int r = (int)row;

        _intensities[r, c] = Math.Max(_intensities[r, c], intensity);
    }

    public bool TryGetPixel(
        int x,
        int y,
        out double intensity)
    {
        long column = (long)x + OriginColumn;
        long row = (long)OriginRow - y;

        intensity = default;

        if (column < 0 || column >= Width || row < 0 || row >= Height)
            return false;        

        intensity = _intensities[(int)row, (int)column];

        return true;
    }

    public double GetIntensityAtIndex(int row, int column)
    {
        return _intensities[row, column];
    }

    public int GetGridXFromColumn(int column)
    {
        return column - OriginColumn;
    }

    public int GetGridYFromRow(int row)
    {
        return OriginRow - row;
    }

    public void Clear()
    {
        Array.Clear(_intensities, 0, _intensities.Length);
    }
}
