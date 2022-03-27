using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V2;

public class UserAlertView
{
    /// <summary>
    /// The alert's ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The alert's item ID.
    /// </summary>
    [JsonPropertyName("itemID")]
    public int ItemId { get; set; }

    /// <summary>
    /// The time that this alert was created, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("created")]
    public string CreatedTimestampMs { get; set; }

    /// <summary>
    /// The last time that this alert was checked, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("lastChecked")]
    public string LastCheckedTimestampMs { get; set; }

    /// <summary>
    /// The alert's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The alert's server.
    /// </summary>
    [JsonPropertyName("server")]
    public string Server { get; set; }

    /// <summary>
    /// The expiry time of this alert, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("expiry")]
    public string ExpiryTimestampMs { get; set; }

    /// <summary>
    /// The trigger conditions for this alert.
    /// </summary>
    [JsonPropertyName("triggerConditions")]
    public string[] TriggerConditions { get; set; }

    /// <summary>
    /// The trigger type of this alert.
    /// </summary>
    [JsonPropertyName("triggerType")]
    public string TriggerType { get; set; }

    /// <summary>
    /// The last time this alert was triggered, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("triggerLastSent")]
    public string TriggerLastSentTimestampMs { get; set; }

    /// <summary>
    /// Whether or not this alert should trigger on the entire data center.
    /// </summary>
    [JsonPropertyName("triggerDataCenter")]
    public bool TriggerDataCenter { get; set; }

    /// <summary>
    /// Whether or not this alert should trigger on HQ items.
    /// </summary>
    [JsonPropertyName("triggerHQ")]
    public bool TriggerHq { get; set; }

    /// <summary>
    /// Whether or not this alert should trigger on NQ items.
    /// </summary>
    [JsonPropertyName("triggerNQ")]
    public bool TriggerNq { get; set; }

    /// <summary>
    /// Whether or not this alert is active.
    /// </summary>
    [JsonPropertyName("triggerActive")]
    public bool TriggerActive { get; set; }
}