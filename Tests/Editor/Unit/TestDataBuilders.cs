using System;
using System.Collections.Generic;
using UnityEngine;

namespace Geuneda.DataExtensions.Tests
{
	/// <summary>
	/// 공통 설정으로 테스트 ConfigsProvider 인스턴스를 생성하기 위한 빌더 패턴입니다.
	/// 테스트 파일의 상용구를 줄이고 일관된 테스트 데이터를 보장합니다.
	/// </summary>
	public class ConfigsProviderBuilder
	{
		private readonly ConfigsProvider _provider = new ConfigsProvider();

		public ConfigsProviderBuilder WithSingleton<T>(T config)
		{
			_provider.AddSingletonConfig(config);
			return this;
		}

		public ConfigsProviderBuilder WithCollection<T>(Func<T, int> idResolver, IEnumerable<T> configs)
		{
			_provider.AddConfigs(idResolver, new List<T>(configs));
			return this;
		}

		public ConfigsProviderBuilder WithCollection<T>(Func<T, int> idResolver, params T[] configs)
		{
			_provider.AddConfigs(idResolver, new List<T>(configs));
			return this;
		}

		public ConfigsProviderBuilder WithVersion(ulong version)
		{
			_provider.UpdateTo(version, new Dictionary<Type, System.Collections.IEnumerable>());
			return this;
		}

		public ConfigsProvider Build() => _provider;

		/// <summary>
		/// 공통 테스트 설정이 미리 채워진 기본 프로바이더를 생성합니다.
		/// </summary>
		public static ConfigsProviderBuilder Default()
		{
			return new ConfigsProviderBuilder()
				.WithSingleton(new MockSingletonConfig { Value = 42 })
				.WithCollection(c => c.Id, 
					new MockCollectionConfig { Id = 1, Name = "First" },
					new MockCollectionConfig { Id = 2, Name = "Second" });
		}
	}

	/// <summary>
	/// 유효성 검사 어트리뷰트를 가진 모크 설정을 생성하기 위한 빌더입니다.
	/// </summary>
	public class MockValidatableConfigBuilder
	{
		private string _name = "DefaultName";
		private int _health = 100;
		private string _tag = "ABC";

		public MockValidatableConfigBuilder WithName(string name)
		{
			_name = name;
			return this;
		}

		public MockValidatableConfigBuilder WithHealth(int health)
		{
			_health = health;
			return this;
		}

		public MockValidatableConfigBuilder WithTag(string tag)
		{
			_tag = tag;
			return this;
		}

		public MockValidatableConfigBuilder Invalid()
		{
			_name = "";
			_health = 150; // 범위 0-100 초과
			_tag = "A";    // 너무 짧음, 최소 3
			return this;
		}

		public MockValidatableConfig Build() => new MockValidatableConfig
		{
			Name = _name,
			Health = _health,
			Tag = _tag
		};
	}

	/// <summary>
	/// 합리적인 기본값을 가진 Unity 타입 설정 빌더입니다.
	/// </summary>
	public class UnityTypesConfigBuilder
	{
		private Color _color = Color.white;
		private Vector2 _vec2 = Vector2.zero;
		private Vector3 _vec3 = Vector3.zero;
		private Vector4 _vec4 = Vector4.zero;
		private Quaternion _quat = Quaternion.identity;

		public UnityTypesConfigBuilder WithColor(Color color)
		{
			_color = color;
			return this;
		}

		public UnityTypesConfigBuilder WithVector2(Vector2 vec)
		{
			_vec2 = vec;
			return this;
		}

		public UnityTypesConfigBuilder WithVector3(Vector3 vec)
		{
			_vec3 = vec;
			return this;
		}

		public UnityTypesConfigBuilder WithVector4(Vector4 vec)
		{
			_vec4 = vec;
			return this;
		}

		public UnityTypesConfigBuilder WithQuaternion(Quaternion quat)
		{
			_quat = quat;
			return this;
		}

		public UnityTypesConfigBuilder WithRandomValues()
		{
			_color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
			_vec2 = new Vector2(1.1f, 2.2f);
			_vec3 = new Vector3(3.3f, 4.4f, 5.5f);
			_vec4 = new Vector4(6.6f, 7.7f, 8.8f, 9.9f);
			_quat = new Quaternion(0.1f, 0.2f, 0.3f, 0.4f);
			return this;
		}

		public MockUnityTypesConfig Build() => new MockUnityTypesConfig
		{
			Color = _color,
			Vec2 = _vec2,
			Vec3 = _vec3,
			Vec4 = _vec4,
			Quat = _quat
		};
	}

	// ════════════════════════════════════════════════════════════════════════
	// 공통 테스트 설정 타입
	// ════════════════════════════════════════════════════════════════════════

	/// <summary>
	/// 일반 테스트를 위한 간단한 싱글톤 설정입니다.
	/// </summary>
	[Serializable]
	public struct MockSingletonConfig
	{
		public int Value;
	}

	/// <summary>
	/// 키 기반 컬렉션 테스트를 위한 ID를 가진 간단한 컬렉션 설정입니다.
	/// </summary>
	[Serializable]
	public struct MockCollectionConfig
	{
		public int Id;
		public string Name;
	}

	/// <summary>
	/// EditorConfigValidator 테스트를 위한 유효성 검사 어트리뷰트를 가진 설정입니다.
	/// </summary>
	[Serializable]
	public class MockValidatableConfig
	{
		[Required]
		public string Name;
		[Range(0, 100)]
		public int Health;
		[MinLength(3)]
		public string Tag;
	}

	/// <summary>
	/// 직렬화 테스트를 위한 Unity 타입을 가진 설정입니다.
	/// </summary>
	[Serializable]
	public struct MockUnityTypesConfig
	{
		public Color Color;
		public Vector2 Vec2;
		public Vector3 Vec3;
		public Vector4 Vec4;
		public Quaternion Quat;
	}

	/// <summary>
	/// 서버 직렬화 중 무시되도록 표시된 설정입니다.
	/// </summary>
	[Serializable]
	[IgnoreServerSerialization]
	public struct MockIgnoredConfig
	{
		public int Value;
	}

	/// <summary>
	/// 오류 테스트를 위한 직렬화 불가 설정([Serializable] 누락)입니다.
	/// </summary>
	public struct MockNonSerializableConfig
	{
		public int Value;
	}
}
