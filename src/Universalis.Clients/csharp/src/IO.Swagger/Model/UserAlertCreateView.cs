/* 
 * Universalis
 *
 * Welcome to the Universalis documentation page.  <br />  <br />  There is a rate limit of 20 req/s (40 req/s burst) on the API, and 10 req/s (20 req/s burst) on the website itself, if you're scraping instead.  The number of simultaneous connections per IP is capped to 8.    To map item IDs to item names or vice versa, use <a href=\"https://xivapi.com/docs/Search#search\">XIVAPI</a>.  In addition to XIVAPI, you can also get item ID mappings from <a href=\"https://lumina.xiv.dev/docs/intro.html\">Lumina</a>,  <a href=\"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Item.csv\">this sheet</a>, or  <a href=\"https://raw.githubusercontent.com/ffxiv-teamcraft/ffxiv-teamcraft/master/apps/client/src/assets/data/items.json\">this</a> pre-made dump.    To get a mapping of world IDs to world names, use <a href=\"https://xivapi.com/World\">XIVAPI</a> or  <a href=\"https://github.com/xivapi/ffxiv-datamining/blob/master/csv/World.csv\">this sheet</a>.  The <code>key</code> column represents the world ID, and the <code>Name</code> column represents the world name.  Note that not all listed worlds are available to be used &#8212; many of the worlds in this sheet are test worlds,  or Korean worlds (Korea is unsupported at this time).    <br />  <br />  If you use this API heavily for your projects, please consider supporting the website on  <a href=\"https://liberapay.com/karashiiro\">Liberapay</a>, <a href=\"https://ko-fi.com/karashiiro\">Ko-fi</a>, or  <a href=\"https://patreon.com/universalis\">Patreon</a>, or making a one-time donation on  <a href=\"https://ko-fi.com/karashiiro\">Ko-fi</a>. Any support is appreciated!  
 *
 * OpenAPI spec version: v2
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using SwaggerDateConverter = IO.Swagger.Client.SwaggerDateConverter;

namespace IO.Swagger.Model
{
    /// <summary>
    /// UserAlertCreateView
    /// </summary>
    [DataContract]
    public partial class UserAlertCreateView :  IEquatable<UserAlertCreateView>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserAlertCreateView" /> class.
        /// </summary>
        /// <param name="alertItemId">The ID of the item that the new alert should apply to..</param>
        /// <param name="alertName">The alert name..</param>
        /// <param name="alertNq">Whether or not this alert should apply to NQ items..</param>
        /// <param name="alertHq">Whether or not this alert should apply to HQ items..</param>
        /// <param name="alertDc">Whether or not this alert should apply to data on all worlds on the data center..</param>
        /// <param name="alertNotifyDiscord">Whether or not this alert should send notifications via Discord..</param>
        /// <param name="alertNotifyEmail">Whether or not this alert should send notifications via email..</param>
        /// <param name="alertTriggers">The alert triggers..</param>
        /// <param name="alertType">The type of the alert..</param>
        public UserAlertCreateView(int? alertItemId = default(int?), string alertName = default(string), bool? alertNq = default(bool?), bool? alertHq = default(bool?), bool? alertDc = default(bool?), bool? alertNotifyDiscord = default(bool?), bool? alertNotifyEmail = default(bool?), List<string> alertTriggers = default(List<string>), string alertType = default(string))
        {
            this.AlertItemId = alertItemId;
            this.AlertName = alertName;
            this.AlertNq = alertNq;
            this.AlertHq = alertHq;
            this.AlertDc = alertDc;
            this.AlertNotifyDiscord = alertNotifyDiscord;
            this.AlertNotifyEmail = alertNotifyEmail;
            this.AlertTriggers = alertTriggers;
            this.AlertType = alertType;
        }
        
        /// <summary>
        /// The ID of the item that the new alert should apply to.
        /// </summary>
        /// <value>The ID of the item that the new alert should apply to.</value>
        [DataMember(Name="alert_item_id", EmitDefaultValue=false)]
        public int? AlertItemId { get; set; }

        /// <summary>
        /// The alert name.
        /// </summary>
        /// <value>The alert name.</value>
        [DataMember(Name="alert_name", EmitDefaultValue=false)]
        public string AlertName { get; set; }

        /// <summary>
        /// Whether or not this alert should apply to NQ items.
        /// </summary>
        /// <value>Whether or not this alert should apply to NQ items.</value>
        [DataMember(Name="alert_nq", EmitDefaultValue=false)]
        public bool? AlertNq { get; set; }

        /// <summary>
        /// Whether or not this alert should apply to HQ items.
        /// </summary>
        /// <value>Whether or not this alert should apply to HQ items.</value>
        [DataMember(Name="alert_hq", EmitDefaultValue=false)]
        public bool? AlertHq { get; set; }

        /// <summary>
        /// Whether or not this alert should apply to data on all worlds on the data center.
        /// </summary>
        /// <value>Whether or not this alert should apply to data on all worlds on the data center.</value>
        [DataMember(Name="alert_dc", EmitDefaultValue=false)]
        public bool? AlertDc { get; set; }

        /// <summary>
        /// Whether or not this alert should send notifications via Discord.
        /// </summary>
        /// <value>Whether or not this alert should send notifications via Discord.</value>
        [DataMember(Name="alert_notify_discord", EmitDefaultValue=false)]
        public bool? AlertNotifyDiscord { get; set; }

        /// <summary>
        /// Whether or not this alert should send notifications via email.
        /// </summary>
        /// <value>Whether or not this alert should send notifications via email.</value>
        [DataMember(Name="alert_notify_email", EmitDefaultValue=false)]
        public bool? AlertNotifyEmail { get; set; }

        /// <summary>
        /// The alert triggers.
        /// </summary>
        /// <value>The alert triggers.</value>
        [DataMember(Name="alert_triggers", EmitDefaultValue=false)]
        public List<string> AlertTriggers { get; set; }

        /// <summary>
        /// The type of the alert.
        /// </summary>
        /// <value>The type of the alert.</value>
        [DataMember(Name="alert_type", EmitDefaultValue=false)]
        public string AlertType { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class UserAlertCreateView {\n");
            sb.Append("  AlertItemId: ").Append(AlertItemId).Append("\n");
            sb.Append("  AlertName: ").Append(AlertName).Append("\n");
            sb.Append("  AlertNq: ").Append(AlertNq).Append("\n");
            sb.Append("  AlertHq: ").Append(AlertHq).Append("\n");
            sb.Append("  AlertDc: ").Append(AlertDc).Append("\n");
            sb.Append("  AlertNotifyDiscord: ").Append(AlertNotifyDiscord).Append("\n");
            sb.Append("  AlertNotifyEmail: ").Append(AlertNotifyEmail).Append("\n");
            sb.Append("  AlertTriggers: ").Append(AlertTriggers).Append("\n");
            sb.Append("  AlertType: ").Append(AlertType).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as UserAlertCreateView);
        }

        /// <summary>
        /// Returns true if UserAlertCreateView instances are equal
        /// </summary>
        /// <param name="input">Instance of UserAlertCreateView to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(UserAlertCreateView input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.AlertItemId == input.AlertItemId ||
                    (this.AlertItemId != null &&
                    this.AlertItemId.Equals(input.AlertItemId))
                ) && 
                (
                    this.AlertName == input.AlertName ||
                    (this.AlertName != null &&
                    this.AlertName.Equals(input.AlertName))
                ) && 
                (
                    this.AlertNq == input.AlertNq ||
                    (this.AlertNq != null &&
                    this.AlertNq.Equals(input.AlertNq))
                ) && 
                (
                    this.AlertHq == input.AlertHq ||
                    (this.AlertHq != null &&
                    this.AlertHq.Equals(input.AlertHq))
                ) && 
                (
                    this.AlertDc == input.AlertDc ||
                    (this.AlertDc != null &&
                    this.AlertDc.Equals(input.AlertDc))
                ) && 
                (
                    this.AlertNotifyDiscord == input.AlertNotifyDiscord ||
                    (this.AlertNotifyDiscord != null &&
                    this.AlertNotifyDiscord.Equals(input.AlertNotifyDiscord))
                ) && 
                (
                    this.AlertNotifyEmail == input.AlertNotifyEmail ||
                    (this.AlertNotifyEmail != null &&
                    this.AlertNotifyEmail.Equals(input.AlertNotifyEmail))
                ) && 
                (
                    this.AlertTriggers == input.AlertTriggers ||
                    this.AlertTriggers != null &&
                    this.AlertTriggers.SequenceEqual(input.AlertTriggers)
                ) && 
                (
                    this.AlertType == input.AlertType ||
                    (this.AlertType != null &&
                    this.AlertType.Equals(input.AlertType))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.AlertItemId != null)
                    hashCode = hashCode * 59 + this.AlertItemId.GetHashCode();
                if (this.AlertName != null)
                    hashCode = hashCode * 59 + this.AlertName.GetHashCode();
                if (this.AlertNq != null)
                    hashCode = hashCode * 59 + this.AlertNq.GetHashCode();
                if (this.AlertHq != null)
                    hashCode = hashCode * 59 + this.AlertHq.GetHashCode();
                if (this.AlertDc != null)
                    hashCode = hashCode * 59 + this.AlertDc.GetHashCode();
                if (this.AlertNotifyDiscord != null)
                    hashCode = hashCode * 59 + this.AlertNotifyDiscord.GetHashCode();
                if (this.AlertNotifyEmail != null)
                    hashCode = hashCode * 59 + this.AlertNotifyEmail.GetHashCode();
                if (this.AlertTriggers != null)
                    hashCode = hashCode * 59 + this.AlertTriggers.GetHashCode();
                if (this.AlertType != null)
                    hashCode = hashCode * 59 + this.AlertType.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

}
