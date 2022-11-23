namespace Universalis.Application.Views.V1.Extra.Stats;

public class TradeVolumeView
{
    /// <summary>
    /// The number of units sold over the query interval.
    /// </summary>
    public long Units { get; set; }

    /// <summary>
    /// The total Gil exchanged over the query interval.
    /// </summary>
    public long Gil { get; set; }

    /// <summary>
    /// The start time for the query interval.
    /// </summary>
    public long From { get; set; }

    /// <summary>
    /// The end time for the query interval.
    /// </summary>
    public long To { get; set; }
}
