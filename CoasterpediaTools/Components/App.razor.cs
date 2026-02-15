namespace CoasterpediaTools.Components;

public partial class App
{
    public static string SetPageTitle(string? pageTitle = null)
    {
        const string baseTitle = "Coasterpedia Tools";

        if (string.IsNullOrEmpty(pageTitle))
        {
            return baseTitle;
        }

        return $"{pageTitle} - {baseTitle}";
    }
}