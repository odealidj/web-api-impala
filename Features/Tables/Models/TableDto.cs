namespace ImpalaApi.Features.Tables.Models;

public record TableDto
{
    public string Name { get; init; } = string.Empty;
    public string? Type { get; init; }
    public string? Comment { get; init; }
}
