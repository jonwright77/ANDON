namespace AndonApp.Services;

public class GeneralSettings
{
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Percentage at or above which a cell scores green (on / above target).</summary>
    public int AdherenceGreenThreshold { get; set; } = 100;

    /// <summary>Percentage at or above which a cell scores amber (below target but acceptable).
    /// Anything below this threshold scores red.</summary>
    public int AdherenceAmberThreshold { get; set; } = 80;
}
