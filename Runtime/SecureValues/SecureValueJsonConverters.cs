using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 보안 값을 평문 숫자로 읽고 씁니다. 같은 이름의 int/long/float/double 필드와 JSON 형식이 같아 세이브를 그대로 옮길 수 있습니다.
	/// nullable(<c>SecureInt?</c> 등)의 <c>null</c> 은 그대로 왕복하고, nullable 이 아닌 타입의 <c>null</c> 은 원래 타입처럼 거절합니다.
	/// </summary>
	/// <remarks>
	/// 보안 값 타입의 <see cref="JsonConverterAttribute"/> 로만 쓰입니다. Newtonsoft 는 nullable 에도 기반 타입의 속성 변환기를 쓰므로
	/// <c>objectType</c> 으로 nullable 을 구별합니다. 변환기는 리플렉션으로 만들어지므로 코드 제거(managed stripping)에서 생성자를 지킵니다.
	/// </remarks>
	internal abstract class SecureValueJsonConverter<TSecure, TPlain> : JsonConverter where TSecure : struct
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(TSecure) || objectType == typeof(TSecure?);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(ToPlain((TSecure)value));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null && objectType != typeof(TSecure))
			{
				return null;
			}

			return ToSecure(serializer.Deserialize<TPlain>(reader));
		}

		protected abstract TPlain ToPlain(TSecure value);

		protected abstract TSecure ToSecure(TPlain value);
	}

	internal sealed class SecureIntJsonConverter : SecureValueJsonConverter<SecureInt, int>
	{
		[Preserve]
		public SecureIntJsonConverter() { }

		protected override int ToPlain(SecureInt value) => value.Value;

		protected override SecureInt ToSecure(int value) => value;
	}

	internal sealed class SecureLongJsonConverter : SecureValueJsonConverter<SecureLong, long>
	{
		[Preserve]
		public SecureLongJsonConverter() { }

		protected override long ToPlain(SecureLong value) => value.Value;

		protected override SecureLong ToSecure(long value) => value;
	}

	internal sealed class SecureFloatJsonConverter : SecureValueJsonConverter<SecureFloat, float>
	{
		[Preserve]
		public SecureFloatJsonConverter() { }

		protected override float ToPlain(SecureFloat value) => value.Value;

		protected override SecureFloat ToSecure(float value) => value;
	}

	internal sealed class SecureDoubleJsonConverter : SecureValueJsonConverter<SecureDouble, double>
	{
		[Preserve]
		public SecureDoubleJsonConverter() { }

		protected override double ToPlain(SecureDouble value) => value.Value;

		protected override SecureDouble ToSecure(double value) => value;
	}

	/// <summary>
	/// floatP 는 IEEE binary32 와 비트가 같으므로 float 숫자로 읽고 씁니다
	/// </summary>
	internal sealed class SecureFloatPJsonConverter : SecureValueJsonConverter<SecureFloatP, float>
	{
		[Preserve]
		public SecureFloatPJsonConverter() { }

		protected override float ToPlain(SecureFloatP value) => value.Value;

		protected override SecureFloatP ToSecure(float value) => new SecureFloatP(value);
	}
}
