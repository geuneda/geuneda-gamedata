using System;
using System.Collections.Generic;
using Geuneda.DataExtensions;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace Geuneda.DataExtensions.Tests
{
	[TestFixture]
	public class ConfigsSerializerTest
	{
		private ConfigsSerializer _serializer;
		private ConfigsProvider _provider;

		[Serializable]
		public struct TestConfig
		{
			public int Id;
			public string Name;
		}

		[Serializable]
		[IgnoreServerSerialization]
		public struct IgnoredConfig
		{
			public int Value;
		}

		public struct NonSerializableConfig
		{
			public int Value;
		}

		[Serializable]
		public struct UnityTypesConfig
		{
			public Color Color;
			public Vector2 Vec2;
			public Vector3 Vec3;
			public Vector4 Vec4;
			public Quaternion Quat;
		}

		public enum TestEnum
		{
			Value1,
			Value2
		}

		[Serializable]
		public struct EnumConfig
		{
			public TestEnum Selection;
		}

		[SetUp]
		public void Setup()
		{
			_serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			_provider = new ConfigsProvider();
		}

		[Test]
		public void Serialize_ValidProvider_ReturnsJsonString()
		{
			_provider.AddSingletonConfig(new TestConfig { Id = 1, Name = "Test" });
			var json = _serializer.Serialize(_provider, "1");

			Assert.IsNotEmpty(json);
			Assert.IsTrue(json.Contains("TestConfig"));
			Assert.IsTrue(json.Contains("\"Version\":\"1\""));
		}

		[Test]
		public void Serialize_IgnoreServerSerialization_ExcludesMarkedTypes()
		{
			_provider.AddSingletonConfig(new TestConfig { Id = 1 });
			_provider.AddSingletonConfig(new IgnoredConfig { Value = 100 });
			
			var json = _serializer.Serialize(_provider, "1");

			Assert.IsTrue(json.Contains("TestConfig"));
			Assert.IsFalse(json.Contains("IgnoredConfig"));
		}

		[Test]
		public void Serialize_NonSerializableWithoutAttribute_ThrowsException()
		{
			// 참고: ConfigsProvider는 직렬화 불가 타입 추가를 허용하지만, 
			// ConfigsSerializer.Serialize는 type.IsSerializable을 확인합니다
			_provider.AddSingletonConfig(new NonSerializableConfig { Value = 1 });
			
			Assert.Throws<Exception>(() => _serializer.Serialize(_provider, "1"));
		}

		[Test]
		public void Serialize_UnityTypes_SerializesCorrectly()
		{
			var config = new UnityTypesConfig
			{
				Color = Color.red,
				Vec2 = Vector2.one,
				Vec3 = Vector3.up,
				Vec4 = new Vector4(1, 2, 3, 4),
				Quat = Quaternion.identity
			};
			_provider.AddSingletonConfig(config);

			var json = _serializer.Serialize(_provider, "1");

			// Color는 ColorJsonConverter에 의해 16진수 문자열로 직렬화됩니다
			Assert.IsTrue(json.Contains("#FF0000FF") || json.Contains("\"Color\":"));
			// Vector는 x,y,z,w 속성을 가진 객체로 직렬화됩니다
			Assert.IsTrue(json.Contains("\"x\":"));
		}

		[Test]
		public void Serialize_EnumValues_SerializedAsStrings()
		{
			_provider.AddSingletonConfig(new EnumConfig { Selection = TestEnum.Value2 });
			var json = _serializer.Serialize(_provider, "1");

			Assert.IsTrue(json.Contains("\"Selection\":\"Value2\""));
		}

		[Test]
		public void Deserialize_ValidJson_IntoExistingProvider()
		{
			// 먼저 올바른 형식을 얻기 위해 직렬화한 다음 역직렬화합니다
			var sourceProvider = new ConfigsProvider();
			sourceProvider.AddSingletonConfig(new TestConfig { Id = 10, Name = "Deserialized" });
			var json = _serializer.Serialize(sourceProvider, "5");
			
			_serializer.Deserialize(json, _provider);

			Assert.AreEqual(5, (int)_provider.Version);
			Assert.AreEqual(10, _provider.GetConfig<TestConfig>().Id);
			Assert.AreEqual("Deserialized", _provider.GetConfig<TestConfig>().Name);
		}

		[Test]
		public void Deserialize_MalformedJson_ThrowsJsonException()
		{
			var json = "{ invalid json }";
			Assert.Throws<JsonReaderException>(() => _serializer.Deserialize(json, _provider));
		}

		[Test]
		public void RoundTrip_AllConfigTypes_PreservesData()
		{
			// ID 0에 "싱글톤 유사" 항목을 포함하는 컬렉션을 사용합니다
			// 참고: ConfigsProvider는 동일 타입의 싱글톤과 컬렉션을 동시에 지원하지 않습니다
			_provider.AddConfigs(c => c.Id, new List<TestConfig> 
			{ 
				new TestConfig { Id = 0, Name = "First" },
				new TestConfig { Id = 2, Name = "Collection1" },
				new TestConfig { Id = 3, Name = "Collection2" }
			});

			var json = _serializer.Serialize(_provider, "10");
			var newProvider = new ConfigsProvider();
			_serializer.Deserialize(json, newProvider);

			Assert.AreEqual(10, (int)newProvider.Version);
			Assert.AreEqual("First", newProvider.GetConfig<TestConfig>(0).Name);
			Assert.AreEqual("Collection1", newProvider.GetConfig<TestConfig>(2).Name);
			Assert.AreEqual("Collection2", newProvider.GetConfig<TestConfig>(3).Name);
		}

		[Test]
		public void SecureMode_TypeNameHandlingNone_Verified()
		{
			var secureSerializer = new ConfigsSerializer(SerializationSecurityMode.Secure);
			_provider.AddSingletonConfig(new TestConfig { Id = 1 });
			
			var json = secureSerializer.Serialize(_provider, "1");

			// 보안 모드에서는 JSON 값 부분에 타입 이름이 존재하면 안 됩니다
			// 구조에 따라 다릅니다. 실제로 SerializedConfigs는 Dictionary<Type, IEnumerable>을 사용합니다
			// 여전히 타입 키를 포함할 수 있습니다. 확인해 봅시다.
			Assert.IsFalse(json.Contains("$type"));
		}

		[Test]
		public void RoundTrip_UnityTypes_PreservesValues()
		{
			var config = new UnityTypesConfig
			{
				Color = new Color(0.1f, 0.2f, 0.3f, 0.4f),
				Vec2 = new Vector2(1.1f, 2.2f),
				Vec3 = new Vector3(3.3f, 4.4f, 5.5f),
				Vec4 = new Vector4(6.6f, 7.7f, 8.8f, 9.9f),
				Quat = new Quaternion(0.1f, 0.2f, 0.3f, 0.4f)
			};
			_provider.AddSingletonConfig(config);

			var json = _serializer.Serialize(_provider, "1");
			var newProvider = new ConfigsProvider();
			_serializer.Deserialize(json, newProvider);

			var result = newProvider.GetConfig<UnityTypesConfig>();
			// Color는 16진수 문자열 표현(채널당 8비트)으로 인해 일부 정밀도 손실이 있습니다
			Assert.AreEqual(config.Color.r, result.Color.r, 0.01f);
			Assert.AreEqual(config.Color.g, result.Color.g, 0.01f);
			Assert.AreEqual(config.Color.b, result.Color.b, 0.01f);
			Assert.AreEqual(config.Color.a, result.Color.a, 0.01f);
			Assert.AreEqual(config.Vec2, result.Vec2);
			Assert.AreEqual(config.Vec3, result.Vec3);
			Assert.AreEqual(config.Vec4, result.Vec4);
			// Quaternion 동등성은 부동소수점에서 까다로울 수 있지만, 이것들은 정확한 왕복 변환입니다
			Assert.AreEqual(config.Quat.x, result.Quat.x, 0.0001f);
			Assert.AreEqual(config.Quat.y, result.Quat.y, 0.0001f);
			Assert.AreEqual(config.Quat.z, result.Quat.z, 0.0001f);
			Assert.AreEqual(config.Quat.w, result.Quat.w, 0.0001f);
		}
	}
}
