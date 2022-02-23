using WebApi.Models;
#pragma warning disable CS1591

namespace WebApi.Util;

public class LinkTypeHelpers
{
	public static SerializableLinkType ParseToSerializableLinkType(short rawLinkType) => rawLinkType switch
	{
		1 => SerializableLinkType.Mirror,
		2 => SerializableLinkType.Download,
		_ => throw new ArgumentOutOfRangeException(nameof(rawLinkType), rawLinkType, "Unknown link type")
	};
}