using System.Runtime.Serialization;
using System.Text.Json.Serialization;

#pragma warning disable CS1591

namespace WebApi.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerializableLinkType
{
	[EnumMember(Value = "mirror")]
	Mirror = 1,
	[EnumMember(Value = "download")]
	Download = 2
}