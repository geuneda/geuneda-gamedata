using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Geuneda.DataExtensions
{

	/// <summary>
	/// 직렬화할 데이터를 나타내는 클래스입니다.
	/// </summary>
	internal class SerializedConfigs
	{
		public string Version;
		
		public Dictionary<Type, IEnumerable> Configs;
	}

	/// <summary>
	/// 직렬화기에 의해 무시되도록 설정 구조체에 추가할 수 있는 어트리뷰트입니다.
	/// 이렇게 하면 필요하지 않을 때 서버로 전송되지 않습니다.
	/// </summary>
	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
	public class IgnoreServerSerialization : Attribute { }

	/// <summary>
	/// 직렬화의 보안 모드를 정의합니다.
	/// </summary>
	public enum SerializationSecurityMode
	{
		/// <summary>
		/// 기본 모드입니다. 다형성 직렬화를 위해 TypeNameHandling.Auto를 사용합니다.
		/// 보안을 위해 SerializationBinder를 사용하여 허용된 타입을 제한합니다.
		/// 신뢰할 수 있는 데이터 소스에만 사용해야 합니다.
		/// </summary>
		TrustedOnly,
		/// <summary>
		/// 보안 모드입니다. TypeNameHandling.None을 사용합니다 - 타입 메타데이터가 생성되거나 처리되지 않습니다.
		/// 참고: 이 모드는 내부
		/// Dictionary&lt;Type, IEnumerable&gt; 구조에 타입 메타데이터가 필요하므로 왕복 직렬화/역직렬화가 불가능합니다.
		/// 신뢰할 수 없는 대상으로 설정을 직렬화할 때 사용합니다(예: 클라이언트에 전송).
		/// </summary>
		Secure
	}

	/// <summary>
	/// 보안 보호 기능이 있는 게임 설정 데이터 직렬화기입니다.
	/// 
	/// 보안 기능:
	/// - TrustedOnly 모드는 SerializationBinder를 사용하여 허용된 타입을 화이트리스트에 등록
	/// - Secure 모드는 TypeNameHandling을 완전히 비활성화(직렬화 전용)
	/// - 깊이 중첩된 JSON의 스택 오버플로 공격을 방지하기 위해 MaxDepth를 설정
	/// 
	/// 이러한 설정은 클라이언트와 서버 간에 공유되도록 설계되었습니다.
	/// </summary>
	public class ConfigsSerializer : IConfigsSerializer
	{
		/// <summary>
		/// 스택 오버플로 공격을 방지하기 위한 최대 JSON 중첩 깊이입니다.
		/// </summary>
		public const int DefaultMaxDepth = 128;
		
		private readonly JsonSerializerSettings _settings;
		private readonly SerializationSecurityMode _securityMode;
		private ConfigTypesBinder _binder;

		/// <summary>
		/// 현재 보안 모드를 가져옵니다.
		/// </summary>
		public SerializationSecurityMode SecurityMode => _securityMode;

		/// <summary>
		/// 지정된 보안 모드로 새 ConfigsSerializer를 생성합니다.
		/// </summary>
		/// <param name="mode">사용할 보안 모드입니다. 기본값은 TrustedOnly입니다.</param>
		/// <param name="maxDepth">최대 JSON 중첩 깊이입니다. 기본값은 128입니다.</param>
		public ConfigsSerializer(SerializationSecurityMode mode = SerializationSecurityMode.TrustedOnly, int maxDepth = DefaultMaxDepth)
		{
			_securityMode = mode;
			_binder = new ConfigTypesBinder(null);
			
			_settings = new JsonSerializerSettings()
			{
				TypeNameHandling = mode == SerializationSecurityMode.Secure ? TypeNameHandling.None : TypeNameHandling.Auto,
				SerializationBinder = mode == SerializationSecurityMode.TrustedOnly ? _binder : null,
				MaxDepth = maxDepth,
				Converters = new List<JsonConverter>()
				{
					new StringEnumConverter(),
					new ColorJsonConverter(),
					new Vector2JsonConverter(),
					new Vector3JsonConverter(),
					new Vector4JsonConverter(),
					new QuaternionJsonConverter()
				}
			};
		}

		/// <inheritdoc />
		public string Serialize(IConfigsProvider cfg, string version)
		{
			var configs = cfg.GetAllConfigs();
			var serializedConfig = new SerializedConfigs()
			{
				Version = version,
				Configs = new Dictionary<Type, IEnumerable>()
			};
			
			foreach (var type in configs.Keys)
			{
				if (type.CustomAttributes.Any(c => c.AttributeType == typeof(IgnoreServerSerialization)))
				{
					continue;
				}
				if (!type.IsSerializable)
				{
					throw new Exception(@$"Config {type} could not be serialized.
						 If this is not used in game logic please add [IgnoreServerSerialization]");
				}

				serializedConfig.Configs[type] = configs[type];
				
				// 바인더를 위한 타입 자동 등록 (TrustedOnly 모드)
				if (_securityMode == SerializationSecurityMode.TrustedOnly)
				{
					_binder.AddAllowedType(type);
					var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), type);
					_binder.AddAllowedType(dictType);
				}
			}
			
			return JsonConvert.SerializeObject(serializedConfig, _settings);
		}

		/// <summary>
		/// 역직렬화 중 허용할 추가 타입을 등록합니다.
		/// TrustedOnly 모드에서만 유효합니다.
		/// 직렬화된 데이터에 없는 타입을 허용해야 하는 경우 Deserialize 전에 호출하세요.
		/// </summary>
		/// <param name="types">허용할 타입입니다.</param>
		public void RegisterAllowedTypes(IEnumerable<Type> types)
		{
			if (_securityMode != SerializationSecurityMode.TrustedOnly || types == null)
			{
				return;
			}
			
			foreach (var type in types)
			{
				_binder.AddAllowedType(type);
				var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), type);
				_binder.AddAllowedType(dictType);
			}
		}

		/// <summary>
		/// 역직렬화 중 허용할 타입을 기존 프로바이더에서 등록합니다.
		/// TrustedOnly 모드에서만 유효합니다.
		/// </summary>
		/// <param name="provider">등록할 타입을 포함하는 프로바이더입니다.</param>
		public void RegisterAllowedTypesFromProvider(IConfigsProvider provider)
		{
			if (_securityMode != SerializationSecurityMode.TrustedOnly || provider == null)
			{
				return;
			}
			
			foreach (var type in provider.GetAllConfigs().Keys)
			{
				_binder.AddAllowedType(type);
				var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), type);
				_binder.AddAllowedType(dictType);
			}
		}

		/// <inheritdoc />
		public T Deserialize<T>(string serialized) where T : IConfigsAdder
		{
			var cfg = Activator.CreateInstance(typeof(T)) as IConfigsAdder;
			Deserialize(serialized, cfg);
			return (T)cfg;
		}
		
		/// <inheritdoc />
		public void Deserialize(string serialized, IConfigsAdder cfg)
		{
			var configs = JsonConvert.DeserializeObject<SerializedConfigs>(serialized, _settings);
			if (!ulong.TryParse(configs.Version, out var versionNumber))
			{
				versionNumber = 0;
			}
			cfg.UpdateTo(versionNumber, configs?.Configs);
		}
	}
}
