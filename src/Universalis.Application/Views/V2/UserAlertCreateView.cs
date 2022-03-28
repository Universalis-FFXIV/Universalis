using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V2;

public class UserAlertCreateView
{
    /// <summary>
    /// The ID of the item that the new alert should apply to.
    /// </summary>
    [JsonPropertyName("alert_item_id")]
    public int? AlertItemId { get; set; }

    /// <summary>
    /// The alert name.
    /// </summary>
    [JsonPropertyName("alert_name")]
    public string? AlertName { get; set; }

    /// <summary>
    /// Whether or not this alert should apply to NQ items.
    /// </summary>
    [JsonPropertyName("alert_nq")]
    public bool? AlertNq { get; set; }

    /// <summary>
    /// Whether or not this alert should apply to HQ items.
    /// </summary>
    [JsonPropertyName("alert_hq")]
    public bool? AlertHq { get; set; }

    /// <summary>
    /// Whether or not this alert should apply to data on all worlds on the data center.
    /// </summary>
    [JsonPropertyName("alert_dc")]
    public bool? AlertDc { get; set; }

    /// <summary>
    /// Whether or not this alert should send notifications via Discord.
    /// </summary>
    [JsonPropertyName("alert_notify_discord")]
    public bool? AlertNotifyDiscord { get; set; }

    /// <summary>
    /// Whether or not this alert should send notifications via email.
    /// </summary>
    [JsonPropertyName("alert_notify_email")]
    public bool? AlertNotifyEmail { get; set; }

    /// <summary>
    /// The alert triggers.
    /// </summary>
    [JsonPropertyName("alert_triggers")]
    public string[]? AlertTriggers { get; set; }

    /// <summary>
    /// The type of the alert.
    /// </summary>
    [JsonPropertyName("alert_type")]
    public string? AlertType { get; set; }
}