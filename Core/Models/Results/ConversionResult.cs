namespace Core.Models.Result;

public readonly record struct ConversionResult<T>(T Value, bool WasClipped);
