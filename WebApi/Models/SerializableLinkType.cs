using System.Text.Json.Serialization;
using WebApi.Util;

#pragma warning disable CS1591

namespace WebApi.Models;

[JsonConverter(typeof(JsonEnumConverter<SerializableLinkType>))]
public enum SerializableLinkType
{
	[JsonEnumValue("mirror")]
	Mirror = 1,
	[JsonEnumValue("download")]
	Download = 2
}