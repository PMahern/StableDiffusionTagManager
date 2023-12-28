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
    /// Person
    /// </summary>
    [DataContract(Name = "Person")]
    public partial class Person : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Person" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected Person() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Person" /> class.
        /// </summary>
        /// <param name="poseKeypoints2d">poseKeypoints2d (required).</param>
        /// <param name="handRightKeypoints2d">handRightKeypoints2d.</param>
        /// <param name="handLeftKeypoints2d">handLeftKeypoints2d.</param>
        /// <param name="faceKeypoints2d">faceKeypoints2d.</param>
        public Person(List<decimal> poseKeypoints2d = default(List<decimal>), List<decimal> handRightKeypoints2d = default(List<decimal>), List<decimal> handLeftKeypoints2d = default(List<decimal>), List<decimal> faceKeypoints2d = default(List<decimal>))
        {
            // to ensure "poseKeypoints2d" is required (not null)
            if (poseKeypoints2d == null)
            {
                throw new ArgumentNullException("poseKeypoints2d is a required property for Person and cannot be null");
            }
            this.PoseKeypoints2d = poseKeypoints2d;
            this.HandRightKeypoints2d = handRightKeypoints2d;
            this.HandLeftKeypoints2d = handLeftKeypoints2d;
            this.FaceKeypoints2d = faceKeypoints2d;
        }

        /// <summary>
        /// Gets or Sets PoseKeypoints2d
        /// </summary>
        [DataMember(Name = "pose_keypoints_2d", IsRequired = true, EmitDefaultValue = true)]
        public List<decimal> PoseKeypoints2d { get; set; }

        /// <summary>
        /// Gets or Sets HandRightKeypoints2d
        /// </summary>
        [DataMember(Name = "hand_right_keypoints_2d", EmitDefaultValue = false)]
        public List<decimal> HandRightKeypoints2d { get; set; }

        /// <summary>
        /// Gets or Sets HandLeftKeypoints2d
        /// </summary>
        [DataMember(Name = "hand_left_keypoints_2d", EmitDefaultValue = false)]
        public List<decimal> HandLeftKeypoints2d { get; set; }

        /// <summary>
        /// Gets or Sets FaceKeypoints2d
        /// </summary>
        [DataMember(Name = "face_keypoints_2d", EmitDefaultValue = false)]
        public List<decimal> FaceKeypoints2d { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class Person {\n");
            sb.Append("  PoseKeypoints2d: ").Append(PoseKeypoints2d).Append("\n");
            sb.Append("  HandRightKeypoints2d: ").Append(HandRightKeypoints2d).Append("\n");
            sb.Append("  HandLeftKeypoints2d: ").Append(HandLeftKeypoints2d).Append("\n");
            sb.Append("  FaceKeypoints2d: ").Append(FaceKeypoints2d).Append("\n");
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
