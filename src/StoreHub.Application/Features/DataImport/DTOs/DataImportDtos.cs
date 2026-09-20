namespace StoreHub.Application.Features.DataImport.DTOs;

public enum DataImportKind
{
    Categories = 1,
    Products = 2,
    Stores = 3,
    Users = 4,
    /// <summary>Single workbook with Stores + Categories + Products + Users sheets.</summary>
    All = 5
}

public sealed class DataImportResultDto
{
    public int Created { get; init; }

    public int Skipped { get; init; }

    public int Failed { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
