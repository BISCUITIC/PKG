namespace ReadingInformationGraphicFiles;

public sealed record ImageInfo
{
    public string FileName { get; init; } = "";
    public string Format { get; init; } = "";
    public string Dimensions { get; init; } = "";
    public string Resolution { get; init; } = "";
    public string ColorDepth { get; init; } = "";
    public string Compression { get; init; } = "";
    public string FileSize { get; init; } = "";
    public string FullPath { get; init; } = "";
    public string Error { get; init; } = "";    

    public bool IsError { get; init; } = false;
}
