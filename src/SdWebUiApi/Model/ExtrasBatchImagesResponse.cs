/*
 * FastAPI
 *
 * No description provided (generated by Openapi Generator https://github.com/openapitools/openapi-generator)
 *
 * The version of the OpenAPI document: 0.1.0
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using OpenAPIDateConverter = SdWebUiApi.Client.OpenAPIDateConverter;

namespace SdWebUiApi.Model
{
    /// <summary>
    /// ExtrasBatchImagesResponse
    /// </summary>
    [DataContract(Name = "ExtrasBatchImagesResponse")]
    public partial class ExtrasBatchImagesResponse : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtrasBatchImagesResponse" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected ExtrasBatchImagesResponse() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtrasBatchImagesResponse" /> class.
        /// </summary>
        /// <param name="htmlInfo">A series of HTML tags containing the process info. (required).</param>
        /// <param name="images">The generated images in base64 format. (required).</param>
        public ExtrasBatchImagesResponse(string htmlInfo = default(string), List<string> images = default(List<string>))
        {
            // to ensure "htmlInfo" is required (not null)
            if (htmlInfo == null)
            {
                throw new ArgumentNullException("htmlInfo is a required property for ExtrasBatchImagesResponse and cannot be null");
            }
            this.HtmlInfo = htmlInfo;
            // to ensure "images" is required (not null)
            if (images == null)
            {
                throw new ArgumentNullException("images is a required property for ExtrasBatchImagesResponse and cannot be null");
            }
            this.Images = images;
        }

        /// <summary>
        /// A series of HTML tags containing the process info.
        /// </summary>
        /// <value>A series of HTML tags containing the process info.</value>
        [DataMember(Name = "html_info", IsRequired = true, EmitDefaultValue = true)]
        public string HtmlInfo { get; set; }

        /// <summary>
        /// The generated images in base64 format.
        /// </summary>
        /// <value>The generated images in base64 format.</value>
        [DataMember(Name = "images", IsRequired = true, EmitDefaultValue = true)]
        public List<string> Images { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class ExtrasBatchImagesResponse {\n");
            sb.Append("  HtmlInfo: ").Append(HtmlInfo).Append("\n");
            sb.Append("  Images: ").Append(Images).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
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
