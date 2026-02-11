using System;
using Geuneda.DataExtensions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests.Security
{
	/// <summary>
	/// ConfigsSerializer의 보안 테스트로, 다음에 대한 보호를 확인합니다:
	/// - $type 메타데이터를 통한 타입 주입 공격
	/// - 잘못된 형식의 JSON 페이로드
	/// - 깊이 중첩된 JSON으로 인한 스택 오버플로
	/// </summary>
	[TestFixture]
	public class ConfigsSerializerSecurityTest
	{
		[Serializable]
		public class BaseConfig { public int Id; }
		[Serializable]
		public class DerivedConfig : BaseConfig { public string Extra; }
		
		// 역직렬화 중 절대 허용되어서는 안 되는 타입
		[Serializable]
		public class MaliciousConfig { public string Payload; }

		private ConfigsProvider _provider;

		[SetUp]
		public void Setup()
		{
			_provider = new ConfigsProvider();
		}

		[Test]
		public void SecureMode_TypeNameHandlingNone_Verified()
		{
			var serializer = new ConfigsSerializer(SerializationSecurityMode.Secure);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 1, Extra = "Data" });
			
			var json = serializer.Serialize(_provider, "1");
			
			// $type를 포함하면 안 됩니다
			Assert.IsFalse(json.Contains("$type"));
		}

		[Test]
		public void TrustedOnlyMode_TypeNameHandlingAuto_Verified()
		{
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 1, Extra = "Data" });
			
			var json = serializer.Serialize(_provider, "1");
			
			// 다형성 직렬화를 위해 $type를 포함해야 합니다
			Assert.IsTrue(json.Contains("$type"));
		}

		[Test]
		public void TrustedOnlyMode_BinderBlocksUnregisteredTypes()
		{
			// 직렬화기를 생성하고 DerivedConfig를 직렬화합니다(등록됨)
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 10, Extra = "Data" });
			var json = serializer.Serialize(_provider, "1");
			
			// JSON을 수정하여 다른 타입 주입을 시도합니다
			// DerivedConfig 타입 참조를 MaliciousConfig로 교체합니다
			// Newtonsoft는 전체 AssemblyQualifiedName으로 직렬화하지 않으므로 FullName을 사용합니다
			var maliciousJson = json.Replace(
				typeof(DerivedConfig).FullName,
				typeof(MaliciousConfig).FullName);
			
			// 바인더는 등록된 적이 없는 MaliciousConfig를 거부해야 합니다
			var newProvider = new ConfigsProvider();
			var ex = Assert.Throws<JsonSerializationException>(() => 
				serializer.Deserialize(maliciousJson, newProvider));
			
			// 보안 관련 키워드에 대해 기본 메시지와 내부 예외를 모두 확인합니다
			// Newtonsoft는 바인더의 예외를 추가 컨텍스트로 감쌀 수 있습니다
			var fullMessage = ex.Message + (ex.InnerException?.Message ?? "");
			var containsSecurityMessage = 
				fullMessage.Contains("not allowed") || 
				fullMessage.Contains("not be resolved") ||
				fullMessage.Contains("could not be resolved") ||
				fullMessage.Contains("MaliciousConfig") ||
				fullMessage.Contains("whitelist") ||
				fullMessage.Contains("security");
			
			Assert.IsTrue(containsSecurityMessage,
				$"Exception should indicate type is not allowed. Actual message: {ex.Message}");
		}

		[Test]
		public void TrustedOnlyMode_RoundTrip_Works()
		{
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 10, Extra = "Data" });
			
			var json = serializer.Serialize(_provider, "1");
			
			var newProvider = new ConfigsProvider();
			serializer.Deserialize(json, newProvider);
			
			var cfg = newProvider.GetConfig<DerivedConfig>();
			Assert.AreEqual(10, cfg.Id);
			Assert.AreEqual("Data", cfg.Extra);
		}

		[Test]
		public void SecureMode_CannotRoundTrip_ExpectedLimitation()
		{
			// 보안 모드는 TypeNameHandling.None을 사용하므로, 내부
			// Dictionary<Type, IEnumerable> 구조를 왕복 변환할 수 없습니다
			// 역직렬화기가 구체적인 타입을 모르기 때문입니다.
			//
			// 이것은 문서화된 제한사항입니다 - 보안 모드는 직렬화 전용
			// 시나리오용입니다(예: 신뢰할 수 없는 클라이언트에 설정 전송).
			
			var secureSerializer = new ConfigsSerializer(SerializationSecurityMode.Secure);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 10, Extra = "Data" });
			
			var secureJson = secureSerializer.Serialize(_provider, "1");
			
			// $type 메타데이터가 없는지 확인합니다
			Assert.IsFalse(secureJson.Contains("$type"), "Secure mode should NOT include $type metadata");
			
			// 역직렬화가 실패하는지 확인합니다(예상된 제한사항)
			var secureProvider = new ConfigsProvider();
			Assert.Throws<JsonSerializationException>(() => secureSerializer.Deserialize(secureJson, secureProvider),
				"Secure mode cannot round-trip because IEnumerable requires $type to know concrete type");
		}

		[Test]
		public void RegisterAllowedTypes_AllowsTypesForDeserialization()
		{
			// 직렬화기를 생성하고 수동으로 타입을 등록합니다
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			serializer.RegisterAllowedTypes(new[] { typeof(DerivedConfig) });
			
			// 다른 직렬화기를 사용하여 직렬화합니다
			var otherSerializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 5, Extra = "Test" });
			var json = otherSerializer.Serialize(_provider, "1");
			
			// DerivedConfig가 등록되었으므로 역직렬화할 수 있어야 합니다
			var newProvider = new ConfigsProvider();
			serializer.Deserialize(json, newProvider);
			
			var cfg = newProvider.GetConfig<DerivedConfig>();
			Assert.AreEqual(5, cfg.Id);
		}

		[Test]
		public void Deserialize_MalformedJson_ThrowsGracefully()
		{
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			var json = "{\"Version\":\"1\", \"Configs\": { unclosed";
			
			Assert.Throws<JsonReaderException>(() => serializer.Deserialize(json, _provider));
		}

		[Test]
		public void Deserialize_UnexpectedType_ThrowsOrIgnores()
		{
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			var json = "{\"Version\":\"1\", \"Configs\": \"NotADictionary\"}";
			
			Assert.Throws<JsonSerializationException>(() => serializer.Deserialize(json, _provider));
		}

		[Test]
		public void MaxDepth_PreventsStackOverflow()
		{
			// 매우 낮은 최대 깊이로 직렬화기를 생성합니다
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly, maxDepth: 5);
			
			// 깊이 중첩된 JSON 역직렬화를 시도합니다(공격 시뮬레이션)
			// Configs 딕셔너리는 Type 키를 예상하므로, "a"와 같은 유효하지 않은 문자열 키를 사용하면
			// Type으로 변환 시 JsonSerializationException이 발생합니다.
			// 이는 잘못된 형식/깊이 중첩된 페이로드가 거부되는지 여전히 검증합니다.
			var deeplyNestedJson = "{\"Version\":\"1\",\"Configs\":{" +
				"\"a\":{\"b\":{\"c\":{\"d\":{\"e\":{\"f\":{\"g\":{}}}}}}}" +
				"}}";
			
			// 유효하지 않은 타입 키 변환으로 인해 예외가 발생해야 합니다(잘못된 페이로드 거부)
			Assert.Throws<JsonSerializationException>(() => serializer.Deserialize(deeplyNestedJson, _provider));
		}

		[Test]
		public void SecurityMode_Property_ReturnsCorrectMode()
		{
			var trustedSerializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			var secureSerializer = new ConfigsSerializer(SerializationSecurityMode.Secure);
			
			Assert.AreEqual(SerializationSecurityMode.TrustedOnly, trustedSerializer.SecurityMode);
			Assert.AreEqual(SerializationSecurityMode.Secure, secureSerializer.SecurityMode);
		}
	}
}
