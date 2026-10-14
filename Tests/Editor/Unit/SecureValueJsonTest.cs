using System;
using System.Collections.Generic;
using Geuneda.DataExtensions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests
{
	[TestFixture]
	public class SecureValueJsonTest
	{
		private class SecureSave
		{
			public SecureInt Gold;
			public SecureLong Exp;
			public SecureFloat Speed;
			public SecureDouble Ratio;
		}

		private class PlainSave
		{
			public int Gold;
			public long Exp;
			public float Speed;
			public double Ratio;
		}

		private class FloatPSave
		{
			public SecureFloatP Hp;
		}

		private class PlainFloatSave
		{
			public float Hp;
		}

		private class NullableSave
		{
			public SecureInt? Gold;
			public SecureLong? Exp;
			public SecureFloat? Speed;
			public SecureDouble? Ratio;
			public SecureFloatP? Hp;
			public List<SecureInt?> History;
		}

		private readonly List<Type> _detected = new List<Type>();

		[SetUp]
		public void SetUp()
		{
			_detected.Clear();
			SecureValueMonitor.TamperDetected += OnTamperDetected;
		}

		[TearDown]
		public void TearDown()
		{
			SecureValueMonitor.TamperDetected -= OnTamperDetected;
		}

		private void OnTamperDetected(Type type)
		{
			_detected.Add(type);
		}

		[Test]
		public void Serialize_MatchesPlainFields()
		{
			var secure = new SecureSave { Gold = -42, Exp = long.MaxValue - 1, Speed = 3.14f, Ratio = 0.1 };
			var plain = new PlainSave { Gold = -42, Exp = long.MaxValue - 1, Speed = 3.14f, Ratio = 0.1 };

			Assert.That(JsonConvert.SerializeObject(secure), Is.EqualTo(JsonConvert.SerializeObject(plain)));
		}

		[Test]
		public void Deserialize_ReadsPlainJson()
		{
			var json = JsonConvert.SerializeObject(new PlainSave { Gold = 7, Exp = long.MinValue, Speed = -0.5f, Ratio = 1e300 });

			var secure = JsonConvert.DeserializeObject<SecureSave>(json);

			Assert.That(secure.Gold.Value, Is.EqualTo(7));
			Assert.That(secure.Exp.Value, Is.EqualTo(long.MinValue));
			Assert.That(secure.Speed.Value, Is.EqualTo(-0.5f));
			Assert.That(secure.Ratio.Value, Is.EqualTo(1e300));
			Assert.That(_detected, Is.Empty);
		}

		[Test]
		public void Deserialize_RejectsNullLikePlainField()
		{
			Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<PlainSave>("{\"Gold\":null}"));
			Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<SecureSave>("{\"Gold\":null}"));
		}

		[Test]
		public void TopLevelValue_RoundTrips()
		{
			var json = JsonConvert.SerializeObject(new SecureInt(123));

			Assert.That(json, Is.EqualTo("123"));
			Assert.That(JsonConvert.DeserializeObject<SecureInt>(json).Value, Is.EqualTo(123));
		}

		[Test]
		public void FloatP_WritesFloatNumber()
		{
			var json = JsonConvert.SerializeObject(new FloatPSave { Hp = (floatP)2.5f });

			Assert.That(json, Is.EqualTo("{\"Hp\":2.5}"));
		}

		[Test]
		public void FloatP_RoundTripKeepsRawValue()
		{
			var random = new Random(6);
			var bytes = new byte[4];
			var raws = new List<uint> { 0, 1, 0x80000001, 0x00800000, 0x7F7FFFFF, 0xFF7FFFFF, 0x7F800000, 0xFF800000 };

			for (var i = 0; i < 20000; i++)
			{
				random.NextBytes(bytes);
				var raw = BitConverter.ToUInt32(bytes, 0);

				// NaN 은 JSON 에서 "NaN" 이 되어 payload 가 남지 않고, -0 은 아래 테스트에서 따로 봅니다(float 필드와 같음)
				if (!float.IsNaN(BitConverter.ToSingle(bytes, 0)) && raw != 0x80000000)
				{
					raws.Add(raw);
				}
			}

			foreach (var raw in raws)
			{
				var json = JsonConvert.SerializeObject(new FloatPSave { Hp = floatP.FromRaw(raw) });
				var back = JsonConvert.DeserializeObject<FloatPSave>(json);

				Assert.That(back.Hp.Value.RawValue, Is.EqualTo(raw), json);
			}
		}

		[Test]
		public void FloatP_NegativeZeroBehavesLikeFloatField()
		{
			// Unity(Mono)의 Newtonsoft 는 -0 을 "0.0" 으로 쓰고 "-0.0" 도 +0 으로 읽어 부호가 남지 않습니다. float 필드와 비트 단위로 같아야 합니다
			var json = JsonConvert.SerializeObject(new FloatPSave { Hp = floatP.FromRaw(0x80000000) });
			var back = JsonConvert.DeserializeObject<FloatPSave>(json);
			var plainJson = JsonConvert.SerializeObject(new PlainFloatSave { Hp = -0f });
			var plainBack = JsonConvert.DeserializeObject<PlainFloatSave>(plainJson);

			Assert.That(json, Is.EqualTo(plainJson));
			Assert.That(back.Hp.Value.RawValue, Is.EqualTo(unchecked((uint)BitConverter.SingleToInt32Bits(plainBack.Hp))));
		}

		[Test]
		public void Nullable_NullRoundTrips()
		{
			var json = JsonConvert.SerializeObject(new NullableSave { History = new List<SecureInt?> { 1, null, 3 } });
			var back = JsonConvert.DeserializeObject<NullableSave>(json);

			Assert.That(json, Is.EqualTo("{\"Gold\":null,\"Exp\":null,\"Speed\":null,\"Ratio\":null,\"Hp\":null,\"History\":[1,null,3]}"));
			Assert.That(back.Gold.HasValue || back.Exp.HasValue || back.Speed.HasValue || back.Ratio.HasValue || back.Hp.HasValue, Is.False);
			Assert.That(back.History.Count, Is.EqualTo(3));
			Assert.That(back.History[0].Value.Value, Is.EqualTo(1));
			Assert.That(back.History[1].HasValue, Is.False);
			Assert.That(back.History[2].Value.Value, Is.EqualTo(3));
			Assert.That(JsonConvert.DeserializeObject<SecureInt?>("null").HasValue, Is.False);
		}

		[Test]
		public void Nullable_ValueRoundTrips()
		{
			var save = new NullableSave { Gold = 5, Exp = -6L, Speed = 1.5f, Ratio = 0.25, Hp = (floatP)2.5f };

			var json = JsonConvert.SerializeObject(save);
			var back = JsonConvert.DeserializeObject<NullableSave>(json);

			Assert.That(json, Is.EqualTo("{\"Gold\":5,\"Exp\":-6,\"Speed\":1.5,\"Ratio\":0.25,\"Hp\":2.5,\"History\":null}"));
			Assert.That(back.Gold.Value.Value, Is.EqualTo(5));
			Assert.That(back.Exp.Value.Value, Is.EqualTo(-6L));
			Assert.That(back.Speed.Value.Value, Is.EqualTo(1.5f));
			Assert.That(back.Ratio.Value.Value, Is.EqualTo(0.25));
			Assert.That((float)back.Hp.Value.Value, Is.EqualTo(2.5f));
			Assert.That(JsonConvert.DeserializeObject<SecureLong?>("7").Value.Value, Is.EqualTo(7L));
		}
	}
}
