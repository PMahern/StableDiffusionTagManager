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
    /// CreateResponse
    /// </summary>
    [DataContract(Name = "CreateResponse")]
    public partial class CreateResponse : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateResponse" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected CreateResponse() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateResponse" /> class.
        /// </summary>
        /// <param name="info">Response string from create embedding or hypernetwork task. (required).</param>
        public CreateResponse(string info = default(string))
        {
            // to ensure "info" is required (not null)
            if (info == null)
            {
                throw new ArgumentNullException("info is a required property for CreateResponse and cannot be null");
            }
            this.Info = info;
        }

        /// <summary>
        /// Response string from create embedding or hypernetwork task.
        /// </summary>
        /// <value>Response string from create embedding or hypernetwork task.</value>
        [DataMember(Name = "info", IsRequired = true, EmitDefaultValue = true)]
        public string Info { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class CreateResponse {\n");
            sb.Append("  Info: ").Append(Info).Append("\n");
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
