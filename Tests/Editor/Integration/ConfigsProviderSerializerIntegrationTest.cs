using System;
using System.Collections.Generic;
using Geuneda.DataExtensions;
using NUnit.Framework;
using UnityEngine;

namespace Geuneda.DataExtensions.Tests.Integration
{
	[TestFixture]
	public class ConfigsProviderSerializerIntegrationTest
	{
		[Serializable]
		public struct HeroConfig
		{
			public int Id;
			public string Name;
			public Color Theme;
		}

		[Serializable]
		[IgnoreServerSerialization]
		public struct LocalConfig
		{
			public bool IsDebug;
		}

		private ConfigsProvider _provider;
		private ConfigsSerializer _serializer;

		[SetUp]
		public void Setup()
		{
			_provider = new ConfigsProvider();
			_serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
		}

		[Test]
		public void FullWorkflow_AddSerializeDeserializeAccess()
		{
			var heroes = new List<HeroConfig>
			{
				new HeroConfig { Id = 1, Name = "Warrior", Theme = Color.red },
				new HeroConfig { Id = 2, Name = "Mage", Theme = Color.blue }
			};
			_provider.AddConfigs(h => h.Id, heroes);
			_provider.AddSingletonConfig(new LocalConfig { IsDebug = true });

			var json = _serializer.Serialize(_provider, "1.0.0");
			
			// LocalConfig가 JSON에 없는지 확인합니다
			Assert.IsFalse(json.Contains("LocalConfig"));
			
			var newProvider = new ConfigsProvider();
			_serializer.Deserialize(json, newProvider);

			Assert.AreEqual(heroes.Count, newProvider.GetConfigsList<HeroConfig>().Count);
			Assert.AreEqual("Warrior", newProvider.GetConfig<HeroConfig>(1).Name);
			Assert.AreEqual(Color.blue, newProvider.GetConfig<HeroConfig>(2).Theme);
			// LocalConfig는 newProvider에 없어야 합니다(타입이 등록되지 않으면 GetConfig가 InvalidOperationException을 발생시킵니다)
			Assert.Throws<InvalidOperationException>(() => newProvider.GetConfig<LocalConfig>());
		}

		[Test]
		public void BackendSync_VersionComparison()
		{
			_provider.UpdateTo(10, new Dictionary<Type, System.Collections.IEnumerable>());
			var jsonV5 = "{\"Version\":\"5\",\"Configs\":{}}";
			var jsonV15 = "{\"Version\":\"15\",\"Configs\":{}}";

			// V5는 더 오래되었지만, ConfigsSerializer.Deserialize는 현재 UpdateTo를 직접 호출합니다.
			// 호출자는 일반적으로 버전을 확인해야 합니다.
			// Deserialize가 버전에 관계없이 버전을 설정하는지 확인합니다.
			_serializer.Deserialize(jsonV5, _provider);
			Assert.AreEqual(5, (int)_provider.Version);

			_serializer.Deserialize(jsonV15, _provider);
			Assert.AreEqual(15, (int)_provider.Version);
		}
	}
}
