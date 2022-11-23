namespace Universalis.Application.Views.V1.Extra.Stats;

public class UnitTradeVolumeView
{
    /// <summary>
    /// The number of units sold over the query interval.
    /// </summary>
    public long Quantity { get; set; }

    /// <summary>
    /// The start time for the query interval.
    /// </summary>
    public long From { get; set; }

    /// <summary>
    /// The end time for the query interval.
    /// </summary>
    public long To { get; set; }
}
