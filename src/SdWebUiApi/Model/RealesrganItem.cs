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
    /// RealesrganItem
    /// </summary>
    [DataContract(Name = "RealesrganItem")]
    public partial class RealesrganItem : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RealesrganItem" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected RealesrganItem() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="RealesrganItem" /> class.
        /// </summary>
        /// <param name="name">name (required).</param>
        /// <param name="path">path.</param>
        /// <param name="scale">scale.</param>
        public RealesrganItem(string name = default(string), string path = default(string), int scale = default(int))
        {
            // to ensure "name" is required (not null)
            if (name == null)
            {
                throw new ArgumentNullException("name is a required property for RealesrganItem and cannot be null");
            }
            this.Name = name;
            this.Path = path;
            this.Scale = scale;
        }

        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets Path
        /// </summary>
        [DataMember(Name = "path", EmitDefaultValue = false)]
        public string Path { get; set; }

        /// <summary>
        /// Gets or Sets Scale
        /// </summary>
        [DataMember(Name = "scale", EmitDefaultValue = false)]
        public int Scale { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class RealesrganItem {\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Path: ").Append(Path).Append("\n");
            sb.Append("  Scale: ").Append(Scale).Append("\n");
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
