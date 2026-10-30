namespace ImageProcessing.Models;

internal class MethodMetadata
{
    public bool UsesWindow { get; init; }
    public string ParameterLabel { get; init; } = string.Empty;
    public string DefaultParameter { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
