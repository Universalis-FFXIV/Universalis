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
    /// MinimizedSaleView
    /// </summary>
    [DataContract]
    public partial class MinimizedSaleView :  IEquatable<MinimizedSaleView>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MinimizedSaleView" /> class.
        /// </summary>
        /// <param name="hq">Whether or not the item was high-quality..</param>
        /// <param name="pricePerUnit">The price per unit sold..</param>
        /// <param name="quantity">The stack size sold..</param>
        /// <param name="timestamp">The sale time, in seconds since the UNIX epoch..</param>
        /// <param name="worldName">The world name, if applicable..</param>
        /// <param name="worldID">The world ID, if applicable..</param>
        public MinimizedSaleView(bool? hq = default(bool?), int? pricePerUnit = default(int?), int? quantity = default(int?), long? timestamp = default(long?), string worldName = default(string), int? worldID = default(int?))
        {
            this.Hq = hq;
            this.PricePerUnit = pricePerUnit;
            this.Quantity = quantity;
            this.Timestamp = timestamp;
            this.WorldName = worldName;
            this.WorldID = worldID;
        }
        
        /// <summary>
        /// Whether or not the item was high-quality.
        /// </summary>
        /// <value>Whether or not the item was high-quality.</value>
        [DataMember(Name="hq", EmitDefaultValue=false)]
        public bool? Hq { get; set; }

        /// <summary>
        /// The price per unit sold.
        /// </summary>
        /// <value>The price per unit sold.</value>
        [DataMember(Name="pricePerUnit", EmitDefaultValue=false)]
        public int? PricePerUnit { get; set; }

        /// <summary>
        /// The stack size sold.
        /// </summary>
        /// <value>The stack size sold.</value>
        [DataMember(Name="quantity", EmitDefaultValue=false)]
        public int? Quantity { get; set; }

        /// <summary>
        /// The sale time, in seconds since the UNIX epoch.
        /// </summary>
        /// <value>The sale time, in seconds since the UNIX epoch.</value>
        [DataMember(Name="timestamp", EmitDefaultValue=false)]
        public long? Timestamp { get; set; }

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        /// <value>The world name, if applicable.</value>
        [DataMember(Name="worldName", EmitDefaultValue=false)]
        public string WorldName { get; set; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        /// <value>The world ID, if applicable.</value>
        [DataMember(Name="worldID", EmitDefaultValue=false)]
        public int? WorldID { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class MinimizedSaleView {\n");
            sb.Append("  Hq: ").Append(Hq).Append("\n");
            sb.Append("  PricePerUnit: ").Append(PricePerUnit).Append("\n");
            sb.Append("  Quantity: ").Append(Quantity).Append("\n");
            sb.Append("  Timestamp: ").Append(Timestamp).Append("\n");
            sb.Append("  WorldName: ").Append(WorldName).Append("\n");
            sb.Append("  WorldID: ").Append(WorldID).Append("\n");
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
            return this.Equals(input as MinimizedSaleView);
        }

        /// <summary>
        /// Returns true if MinimizedSaleView instances are equal
        /// </summary>
        /// <param name="input">Instance of MinimizedSaleView to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(MinimizedSaleView input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Hq == input.Hq ||
                    (this.Hq != null &&
                    this.Hq.Equals(input.Hq))
                ) && 
                (
                    this.PricePerUnit == input.PricePerUnit ||
                    (this.PricePerUnit != null &&
                    this.PricePerUnit.Equals(input.PricePerUnit))
                ) && 
                (
                    this.Quantity == input.Quantity ||
                    (this.Quantity != null &&
                    this.Quantity.Equals(input.Quantity))
                ) && 
                (
                    this.Timestamp == input.Timestamp ||
                    (this.Timestamp != null &&
                    this.Timestamp.Equals(input.Timestamp))
                ) && 
                (
                    this.WorldName == input.WorldName ||
                    (this.WorldName != null &&
                    this.WorldName.Equals(input.WorldName))
                ) && 
                (
                    this.WorldID == input.WorldID ||
                    (this.WorldID != null &&
                    this.WorldID.Equals(input.WorldID))
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
                if (this.Hq != null)
                    hashCode = hashCode * 59 + this.Hq.GetHashCode();
                if (this.PricePerUnit != null)
                    hashCode = hashCode * 59 + this.PricePerUnit.GetHashCode();
                if (this.Quantity != null)
                    hashCode = hashCode * 59 + this.Quantity.GetHashCode();
                if (this.Timestamp != null)
                    hashCode = hashCode * 59 + this.Timestamp.GetHashCode();
                if (this.WorldName != null)
                    hashCode = hashCode * 59 + this.WorldName.GetHashCode();
                if (this.WorldID != null)
                    hashCode = hashCode * 59 + this.WorldID.GetHashCode();
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
