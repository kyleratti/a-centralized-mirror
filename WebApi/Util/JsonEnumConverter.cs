using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FruityFoundation.Base.Structures;

namespace WebApi.Util;

/// <summary>
/// A serializable and deserializable value for an enum value
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class JsonEnumValueAttribute : Attribute
{
	/// <summary>
	/// The string value.
	/// </summary>
	public string Value { get; }
	/// <summary>
	/// The comparison mode to use if performing string comparisons against <see cref="Value"/>.
	/// </summary>
	public StringComparison ComparisonMode { get; }

	/// <summary>
	/// A serializable and deserializable value for an enum value
	/// </summary>
	/// <param name="value">The string value of this member</param>
	/// <param name="comparisonMode">The comparison mode to use.</param>
	public JsonEnumValueAttribute(string value, StringComparison comparisonMode = StringComparison.OrdinalIgnoreCase)
	{
		Value = value;
		ComparisonMode = comparisonMode;
	}
}

/// <summary>
/// Serialize and deserialize enums using string members
/// </summary>
public class JsonEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
	/// <inheritdoc />
	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var jsonValue = reader.GetString();

		if (jsonValue is null)
			throw new SerializationException("value was null");

		if (!FindEnumByJsonValue(jsonValue).Try(out var enumValue))
			throw new SerializationException($"Unable to map value to type ({typeof(T).FullName}): {jsonValue}");

		return enumValue;
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(GetJsonValueForEnum(value));
	}

	/// <inheritdoc />
	public override bool CanConvert(Type typeToConvert) =>
		typeToConvert.IsEnum;

	private static string GetJsonValueForEnum(T enumValue) =>
		FindJsonValueForEnum(enumValue)
			.Map(x => x.Value)
			.OrThrow($"Missing {nameof(JsonEnumValueAttribute)} on member: {enumValue.GetType().FullName}.{enumValue.ToString("G")}");

	private static Maybe<JsonEnumValueAttribute> FindJsonValueForEnum(T enumValue)
	{
		var enumName = Enum.GetName(enumValue.GetType(), enumValue) ?? throw new ApplicationException($"Unable to find enum member {enumValue} on type {enumValue.GetType().FullName}");

		return enumValue.GetType()
			.GetMember(enumName)
			.Select(x => x.GetCustomAttribute<JsonEnumValueAttribute>(inherit: true))
			.Where(x => x is not null)
			.Cast<JsonEnumValueAttribute>()
			.FirstOrEmpty();
	}

	private static Maybe<T> FindEnumByJsonValue(string input) =>
		Enum.GetValues<T>()
			.Select(x => (EnumValue: x, JsonValue: FindJsonValueForEnum(x)))
			.FirstOrEmpty(x => x.JsonValue.Try(out var jsonValue) && jsonValue.Value.Equals(input, jsonValue.ComparisonMode))
			.Map(x => x.EnumValue);
}