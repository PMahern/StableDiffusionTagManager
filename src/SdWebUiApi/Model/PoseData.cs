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
    /// PoseData
    /// </summary>
    [DataContract(Name = "PoseData")]
    public partial class PoseData : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PoseData" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected PoseData() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="PoseData" /> class.
        /// </summary>
        /// <param name="people">people (required).</param>
        /// <param name="canvasWidth">canvasWidth (required).</param>
        /// <param name="canvasHeight">canvasHeight (required).</param>
        public PoseData(List<Person> people = default(List<Person>), int canvasWidth = default(int), int canvasHeight = default(int))
        {
            // to ensure "people" is required (not null)
            if (people == null)
            {
                throw new ArgumentNullException("people is a required property for PoseData and cannot be null");
            }
            this.People = people;
            this.CanvasWidth = canvasWidth;
            this.CanvasHeight = canvasHeight;
        }

        /// <summary>
        /// Gets or Sets People
        /// </summary>
        [DataMember(Name = "people", IsRequired = true, EmitDefaultValue = true)]
        public List<Person> People { get; set; }

        /// <summary>
        /// Gets or Sets CanvasWidth
        /// </summary>
        [DataMember(Name = "canvas_width", IsRequired = true, EmitDefaultValue = true)]
        public int CanvasWidth { get; set; }

        /// <summary>
        /// Gets or Sets CanvasHeight
        /// </summary>
        [DataMember(Name = "canvas_height", IsRequired = true, EmitDefaultValue = true)]
        public int CanvasHeight { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class PoseData {\n");
            sb.Append("  People: ").Append(People).Append("\n");
            sb.Append("  CanvasWidth: ").Append(CanvasWidth).Append("\n");
            sb.Append("  CanvasHeight: ").Append(CanvasHeight).Append("\n");
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