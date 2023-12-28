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
    /// PredictBody
    /// </summary>
    [DataContract(Name = "PredictBody")]
    public partial class PredictBody : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PredictBody" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected PredictBody() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="PredictBody" /> class.
        /// </summary>
        /// <param name="sessionHash">sessionHash.</param>
        /// <param name="eventId">eventId.</param>
        /// <param name="data">data (required).</param>
        /// <param name="eventData">eventData.</param>
        /// <param name="fnIndex">fnIndex.</param>
        /// <param name="batched">batched (default to false).</param>
        /// <param name="request">request.</param>
        public PredictBody(string sessionHash = default(string), string eventId = default(string), List<Object> data = default(List<Object>), Object eventData = default(Object), int fnIndex = default(int), bool batched = false, Request request = default(Request))
        {
            // to ensure "data" is required (not null)
            if (data == null)
            {
                throw new ArgumentNullException("data is a required property for PredictBody and cannot be null");
            }
            this.Data = data;
            this.SessionHash = sessionHash;
            this.EventId = eventId;
            this.EventData = eventData;
            this.FnIndex = fnIndex;
            this.Batched = batched;
            this.Request = request;
        }

        /// <summary>
        /// Gets or Sets SessionHash
        /// </summary>
        [DataMember(Name = "session_hash", EmitDefaultValue = false)]
        public string SessionHash { get; set; }

        /// <summary>
        /// Gets or Sets EventId
        /// </summary>
        [DataMember(Name = "event_id", EmitDefaultValue = false)]
        public string EventId { get; set; }

        /// <summary>
        /// Gets or Sets Data
        /// </summary>
        [DataMember(Name = "data", IsRequired = true, EmitDefaultValue = true)]
        public List<Object> Data { get; set; }

        /// <summary>
        /// Gets or Sets EventData
        /// </summary>
        [DataMember(Name = "event_data", EmitDefaultValue = true)]
        public Object EventData { get; set; }

        /// <summary>
        /// Gets or Sets FnIndex
        /// </summary>
        [DataMember(Name = "fn_index", EmitDefaultValue = false)]
        public int FnIndex { get; set; }

        /// <summary>
        /// Gets or Sets Batched
        /// </summary>
        [DataMember(Name = "batched", EmitDefaultValue = true)]
        public bool Batched { get; set; }

        /// <summary>
        /// Gets or Sets Request
        /// </summary>
        [DataMember(Name = "request", EmitDefaultValue = false)]
        public Request Request { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class PredictBody {\n");
            sb.Append("  SessionHash: ").Append(SessionHash).Append("\n");
            sb.Append("  EventId: ").Append(EventId).Append("\n");
            sb.Append("  Data: ").Append(Data).Append("\n");
            sb.Append("  EventData: ").Append(EventData).Append("\n");
            sb.Append("  FnIndex: ").Append(FnIndex).Append("\n");
            sb.Append("  Batched: ").Append(Batched).Append("\n");
            sb.Append("  Request: ").Append(Request).Append("\n");
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
