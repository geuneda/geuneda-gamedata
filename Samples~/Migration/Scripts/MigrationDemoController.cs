using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Geuneda.DataExtensions.Samples.Migration
{
	/// <summary>
	/// 스키마 마이그레이션을 위한 에디터 전용 데모(플레이 모드)입니다.
	/// Config Browser 창을 사용한 실제 개발자 워크플로우를 시연합니다.
	/// </summary>
	public sealed class MigrationDemoController : MonoBehaviour
	{
		[SerializeField] private Button _openBrowserButton;
		[SerializeField] private TMP_Text _output;

		private ConfigsProvider _provider;

		private void Awake()
		{
			InitializeProvider();

			if (_openBrowserButton != null)
			{
				_openBrowserButton.onClick.AddListener(OpenConfigBrowser);
			}

			SetOutput(BuildInitialOutput());
		}

		private void OnDestroy()
		{
			_provider = null;
		}

		private void InitializeProvider()
		{
			_provider = new ConfigsProvider();

			// SampleEnemyConfig 인스턴스 추가
			// 참고: 이것은 v3 스키마 인스턴스이지만, 프로바이더 버전은 1로 설정되어
			// Config Browser에서 마이그레이션이 "Pending"으로 표시됩니다.
			var enemyConfigs = new List<SampleEnemyConfig>
			{
				new SampleEnemyConfig
				{
					Id = 1,
					Name = "Orc Warlord",
					AttackDamage = 25,
					ArmorType = "Heavy",
					BaseHealth = 120,
					BonusHealth = 30,
					Stats = new EnemyStats
					{
						DamageReduction = 20,
						CritChance = 15,
						MoveSpeedMultiplier = 0.85f
					},
					Abilities = new[] { "Charge", "Battlecry" }
				}
			};
			_provider.AddConfigs(c => c.Id, enemyConfigs);

			// 마이그레이션이 Pending으로 표시되도록 프로바이더 버전을 1(레거시)로 설정
			_provider.UpdateTo(version: 1, new Dictionary<Type, System.Collections.IEnumerable>());
		}

		private void OpenConfigBrowser()
		{
#if UNITY_EDITOR
			EditorApplication.ExecuteMenuItem("Tools/Game Data/Config Browser");
#else
			Debug.Log("Config Browser is only available in the Unity Editor.");
#endif
		}

		private static string BuildInitialOutput()
		{
			var sampleJson =
				"{\n" +
				"  \"Id\": 1,\n" +
				"  \"Name\": \"Orc Warlord\",\n" +
				"  \"Health\": 150,\n" +
				"  \"Damage\": 25\n" +
				"}";

			var sb = new StringBuilder(2048);
			sb.AppendLine("═════════════════════════════════════════");
			sb.AppendLine("SCHEMA MIGRATION DEMO");
			sb.AppendLine("═════════════════════════════════════════");
			sb.AppendLine("This sample demonstrates the real developer workflow");
			sb.AppendLine("for migrating config schemas using the Config Browser.");
			sb.AppendLine();
			sb.AppendLine("SCHEMA EVOLUTION:");
			sb.AppendLine("─────────────────────────────────────────");
			sb.AppendLine("v1: Id, Name, Health, Damage");
			sb.AppendLine("v2: +ArmorType, Damage → AttackDamage");
			sb.AppendLine("v3: Health → BaseHealth + BonusHealth, +Stats, +Abilities[]");
			sb.AppendLine();
			sb.AppendLine("WORKFLOW:");
			sb.AppendLine("─────────────────────────────────────────");
			sb.AppendLine("1. Enter Play Mode (registers the provider)");
			sb.AppendLine("2. Open Config Browser (Tools > Game Data > Config Browser)");
			sb.AppendLine("3. Go to the 'Migrations' tab");
			sb.AppendLine("4. Paste the v1 JSON below into 'Custom Input JSON'");
			sb.AppendLine("5. Select target version and click 'Preview Migration'");
			sb.AppendLine();
			sb.AppendLine("V1 SAMPLE JSON (copy this):");
			sb.AppendLine("─────────────────────────────────────────");
			sb.AppendLine(sampleJson);
			sb.AppendLine();
			sb.AppendLine("─────────────────────────────────────────");
			sb.AppendLine("Note: The provider version is currently set to 1.");

			return sb.ToString();
		}

		private void SetOutput(string text)
		{
			if (_output != null)
			{
				_output.text = text;
			}
		}
	}
}
