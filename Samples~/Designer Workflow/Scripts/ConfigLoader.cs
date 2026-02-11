using System.Collections.Generic;
using UnityEngine;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// 디자이너가 작성한 ScriptableObject 에셋을 런타임 <see cref="ConfigsProvider"/>에 로드합니다.
	/// 이 샘플은 씬 참조가 필요하지 않도록 Resources를 사용합니다.
	/// </summary>
	public sealed class ConfigLoader
	{
		public const string GameSettingsResourcePath = "SampleGameSettings";
		public const string EnemyConfigsResourcePath = "SampleEnemyConfigs";
		public const string LootTableResourcePath = "SampleLootTable";

		public LoadedConfigs Load()
		{
			var settingsAsset = Resources.Load<GameSettingsAsset>(GameSettingsResourcePath);
			var enemiesAsset = Resources.Load<EnemyConfigsAsset>(EnemyConfigsResourcePath);
			var lootAsset = Resources.Load<LootTableAsset>(LootTableResourcePath);

			var provider = new ConfigsProvider();

			// 설정: 단일 항목으로 저장됨 (키 0)
			var settings = default(GameSettingsConfig);
			if (settingsAsset != null && settingsAsset.ConfigsDictionary != null &&
			    settingsAsset.ConfigsDictionary.TryGetValue(GameSettingsAsset.SingletonKey, out var loadedSettings))
			{
				settings = loadedSettings;
			}
			provider.AddSingletonConfig(settings);

			// 적: 쌍 목록 -> 목록 -> ID 키 프로바이더
			var enemies = new List<EnemyConfig>();
			if (enemiesAsset != null && enemiesAsset.Configs != null)
			{
				for (var i = 0; i < enemiesAsset.Configs.Count; i++)
				{
					enemies.Add(enemiesAsset.Configs[i].Value);
				}
			}
			provider.AddConfigs(e => e.Id, enemies);

			// 전리품 테이블: UnitySerializedDictionary 파생 구체 타입에 저장됨
			var lootTable = lootAsset != null ? lootAsset.DropRates : new LootTable();

			return new LoadedConfigs(provider, settingsAsset, enemiesAsset, lootAsset, enemies, lootTable);
		}
	}

	public readonly struct LoadedConfigs
	{
		public ConfigsProvider Provider { get; }
		public GameSettingsAsset SettingsAsset { get; }
		public EnemyConfigsAsset EnemiesAsset { get; }
		public LootTableAsset LootTableAsset { get; }
		public IReadOnlyList<EnemyConfig> Enemies { get; }
		public LootTable LootTable { get; }

		public LoadedConfigs(
			ConfigsProvider provider,
			GameSettingsAsset settingsAsset,
			EnemyConfigsAsset enemiesAsset,
			LootTableAsset lootTableAsset,
			IReadOnlyList<EnemyConfig> enemies,
			LootTable lootTable)
		{
			Provider = provider;
			SettingsAsset = settingsAsset;
			EnemiesAsset = enemiesAsset;
			LootTableAsset = lootTableAsset;
			Enemies = enemies;
			LootTable = lootTable;
		}
	}
}

