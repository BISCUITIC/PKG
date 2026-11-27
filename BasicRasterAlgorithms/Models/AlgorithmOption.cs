namespace BasicRasterAlgorithms.Models;

public sealed record AlgorithmOption<TAlgorithm>(
    string Name, 
    TAlgorithm Algorithm);