using MudBlazor;

namespace CoasterpediaTools.Components.Shared;

public record MultiSelectChip<T>
{
    public required T Value { get; init; }
    public string? Text { get; init; }
    public Color? Color { get; init; }
}