using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// Designer Workflow 샘플의 진입점 MonoBehaviour입니다.
	/// ScriptableObject 설정을 로드하고 런타임에 표시하며, 다시 로드 버튼을 포함합니다.
	/// </summary>
	public sealed class DesignerWorkflowDemoController : MonoBehaviour
	{
		[SerializeField] private TMP_Text _displayText;
		[SerializeField] private Button _reloadButton;

		private ConfigLoader _loader;
		private LoadedConfigs _loadedConfigs;

		private void Awake()
		{
			_loader = new ConfigLoader();
		}

		private void Start()
		{
			if (_reloadButton != null)
			{
				_reloadButton.onClick.AddListener(ReloadAndRender);
			}

			ReloadAndRender();
		}

		private void ReloadAndRender()
		{
			_loadedConfigs = _loader.Load();
			Render(_loadedConfigs);
		}

		private void Render(LoadedConfigs data)
		{
			if (_displayText == null)
			{
				return;
			}

			var sb = new StringBuilder(1024);

			sb.AppendLine("═══════════════════════════════════════════");
			sb.AppendLine("DESIGNER WORKFLOW DEMO");
			sb.AppendLine("═══════════════════════════════════════════");
			sb.AppendLine();

			sb.AppendLine("Assets (Resources)");
			sb.AppendLine($"- GameSettingsAsset: {(data.SettingsAsset != null ? "Loaded" : "Missing")}");
			sb.AppendLine($"- EnemyConfigsAsset: {(data.EnemiesAsset != null ? "Loaded" : "Missing")}");
			sb.AppendLine($"- LootTableAsset: {(data.LootTableAsset != null ? "Loaded" : "Missing")}");
			sb.AppendLine();

			sb.AppendLine("GameSettings (singleton)");
			sb.AppendLine($"- Difficulty: {data.Provider.GetConfig<GameSettingsConfig>().Difficulty}");
			sb.AppendLine($"- MasterVolume: {data.Provider.GetConfig<GameSettingsConfig>().MasterVolume:0.00}");
			sb.AppendLine();

			sb.AppendLine("Enemies (id-keyed)");
			for (var i = 0; i < data.Enemies.Count; i++)
			{
				var e = data.Enemies[i];
				sb.AppendLine($"- [{e.Id}] {e.Name} HP:{e.Health} DMG:{e.Damage}");
			}
			sb.AppendLine();

			sb.AppendLine("LootTable (UnitySerializedDictionary)");
			if (data.LootTable.Count == 0)
			{
				sb.AppendLine("- (empty)");
			}
			else
			{
				foreach (var pair in data.LootTable)
				{
					var key = pair.Key != null ? pair.Key.GetSelectionString() : "<null>";
					sb.AppendLine($"- {key}: {pair.Value:0.00}");
				}
			}

			_displayText.text = sb.ToString();
		}
	}
}

