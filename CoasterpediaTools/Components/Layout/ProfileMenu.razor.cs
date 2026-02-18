using Microsoft.AspNetCore.Components;

namespace CoasterpediaTools.Components.Layout;

public partial class ProfileMenu
{
    [Parameter] public string? ScreenMode { get; set; }

    [Parameter] public EventCallback<string?> ScreenModeChanged { get; set; }

    private async Task OnScreenModeChanged(string value)
    {
        ScreenMode = value;
        await ScreenModeChanged.InvokeAsync(value);
        StateHasChanged();
    }
}