using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Geuneda.DataExtensions;
using GeunedaEditor.GameData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// 마이그레이션 패널 UI를 구동하는 컨트롤러입니다.
	/// <see cref="MigrationRunner"/>에서 사용 가능한 마이그레이션을 검색하고,
	/// <see cref="MigrationPanelView"/>에 행을 채우며, 미리보기/적용 작업을 처리합니다.
	/// </summary>
	internal sealed class MigrationPanelController
	{
		private readonly MigrationPanelView _view;
		private readonly List<MigrationRow> _rows = new List<MigrationRow>();
		private IConfigsProvider _provider;

		public MigrationPanelController(MigrationPanelView view)
		{
			_view = view;
			_view.PreviewRequested += OnPreviewRequested;
			_view.ApplyRequested += OnApplyRequested;
		}

		/// <summary>
		/// 마이그레이션 검사 대상으로 <paramref name="provider"/>를 할당하고 패널을 다시 빌드합니다.
		/// </summary>
		public void SetProvider(IConfigsProvider provider)
		{
			_provider = provider;
			Rebuild();
		}

		/// <summary>
		/// 마이그레이션 행 목록을 다시 빌드하고 뷰를 새로고침합니다.
		/// </summary>
		public void Rebuild()
		{
			_rows.Clear();
			_view.SetInputJson(string.Empty);
			_view.SetOutputJson(string.Empty);
			_view.SetLog(string.Empty);

			if (_provider == null)
			{
				_view.SetHeader("Migrations (no provider)");
				_view.SetEmptyStateVisible(false);
				_view.SetRows(_rows);
				var hasChoices = _rows.Count > 0;
				_view.SetButtonsEnabled(hasChoices, hasChoices && _provider != null);
				return;
			}

			var providerTypes = _provider.GetAllConfigs().Keys.ToHashSet();
			var migratableTypes = MigrationRunner.GetConfigTypesWithMigrations()
				.Where(providerTypes.Contains)
				.OrderBy(t => t.Name)
				.ToList();

			// 사용 가능한 마이그레이션이 없을 때 빈 상태를 표시합니다.
			if (migratableTypes.Count == 0)
			{
				_view.SetHeader("Migrations");
				_view.SetEmptyStateVisible(true);
				_view.SetRows(_rows);
				_view.SetButtonsEnabled(false, false);
				return;
			}

			_view.SetEmptyStateVisible(false);

			var currentVersion = _provider.Version;
			var latestVersion = migratableTypes.Max(t => MigrationRunner.GetLatestVersion(t));
			_view.SetHeader($"Current Config Version: {currentVersion}    Latest Available: {latestVersion}");

			foreach (var type in migratableTypes)
			{
				var migrations = MigrationRunner.GetAvailableMigrations(type);
				for (int i = 0; i < migrations.Count; i++)
				{
					var m = migrations[i];
					var state = GetState(currentVersion, m.FromVersion, m.ToVersion);
					_rows.Add(new MigrationRow(type, m.FromVersion, m.ToVersion, m.MigrationType, state));
				}
			}

			_view.SetRows(_rows);
			var hasRows = _rows.Count > 0;
			_view.SetButtonsEnabled(hasRows, hasRows && _provider != null);
		}

		private void OnPreviewRequested()
		{
			var selectedIndex = _view.SelectedIndex;
			if (selectedIndex < 0 || selectedIndex >= _rows.Count)
			{
				return;
			}

			RunPreview(_rows[selectedIndex]);
		}

		private void OnApplyRequested()
		{
			var selectedIndex = _view.SelectedIndex;
			if (selectedIndex < 0 || selectedIndex >= _rows.Count || _provider == null)
			{
				return;
			}

			var row = _rows[selectedIndex];
			ApplyMigration(row);
		}

		private void ApplyMigration(MigrationRow row)
		{
			// UpdateTo는 인터페이스가 아닌 ConfigsProvider에서만 사용 가능합니다
			if (!(_provider is ConfigsProvider concreteProvider))
			{
				_view.SetLog("Apply Failed: Provider does not support UpdateTo (must be ConfigsProvider).");
				return;
			}

			var currentVersion = _provider.Version;

			// 프로바이더에서 이 타입의 모든 설정을 가져옵니다
			var allConfigs = _provider.GetAllConfigs();
			if (!allConfigs.TryGetValue(row.ConfigType, out var container))
			{
				_view.SetLog($"Apply Failed: No configs of type {row.ConfigType.Name} found in provider.");
				return;
			}

			// 설정을 (id, value) 쌍의 목록으로 읽어옵니다
			if (!ConfigsEditorUtil.TryReadConfigs(container, out var entries) || entries.Count == 0)
			{
				_view.SetLog($"Apply Failed: Could not read configs of type {row.ConfigType.Name}.");
				return;
			}

			try
			{
				// 각 설정을 마이그레이션하고 결과를 수집합니다
				var migratedEntries = new List<(int Id, object Value)>();
				int totalApplied = 0;

				foreach (var entry in entries)
				{
					var inputJson = JObject.FromObject(entry.Value);
					var outputJson = (JObject)inputJson.DeepClone();

					var applied = MigrationRunner.Migrate(row.ConfigType, outputJson, currentVersion, row.ToVersion);
					totalApplied += applied;

					// 설정 타입으로 다시 역직렬화합니다
					var migratedValue = outputJson.ToObject(row.ConfigType);
					migratedEntries.Add((entry.Id, migratedValue));
				}

				// 리플렉션을 사용하여 마이그레이션된 값으로 새 딕셔너리를 생성합니다
				var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(int), row.ConfigType);
				var newDictionary = (IDictionary)Activator.CreateInstance(dictionaryType);

				foreach (var (id, value) in migratedEntries)
				{
					newDictionary.Add(id, value);
				}

				// 프로바이더를 업데이트합니다
				var updateDict = new Dictionary<Type, IEnumerable> { { row.ConfigType, newDictionary } };
				concreteProvider.UpdateTo(row.ToVersion, updateDict);

				_view.SetLog($"Apply Success: Migrated {entries.Count} config(s), {totalApplied} migration step(s) applied. Provider now at v{row.ToVersion}.");

				// 새로운 상태를 반영하기 위해 다시 빌드합니다
				Rebuild();
			}
			catch (Exception ex)
			{
				_view.SetLog($"Apply Failed: {ex.Message}");
			}
		}

		private void RunPreview(MigrationRow row)
		{
			if (_provider == null)
			{
				return;
			}

			var currentVersion = _provider.Version;
			JObject inputJson;
			string instanceLabel;

			// 우선순위 1: 텍스트 필드에서 입력된 사용자 정의 JSON
			var customJson = _view.CustomJson;
			if (!string.IsNullOrEmpty(customJson))
			{
				try
				{
					inputJson = JObject.Parse(customJson);
					instanceLabel = $"{row.ConfigType.Name} (custom input)";
				}
				catch (Exception ex)
				{
					_view.SetInputJson($"// Invalid JSON: {ex.Message}");
					_view.SetOutputJson(string.Empty);
					_view.SetLog(string.Empty);
					return;
				}
			}
			// 우선순위 2: 프로바이더 데이터 (현재 스키마)
			else if (TryGetFirstInstance(row.ConfigType, out var id, out var instance))
			{
				inputJson = JObject.FromObject(instance);
				var idStr = id == 0 ? "singleton" : id.ToString();
				instanceLabel = $"{row.ConfigType.Name} ({idStr})";
			}
			else
			{
				_view.SetInputJson("// No instance found for this config type in the provider.");
				_view.SetOutputJson(string.Empty);
				_view.SetLog(string.Empty);
				return;
			}

			var outputJson = (JObject)inputJson.DeepClone();

			int applied;
			try
			{
				applied = MigrationRunner.Migrate(row.ConfigType, outputJson, currentVersion, row.ToVersion);
			}
			catch (Exception ex)
			{
				_view.SetInputJson(inputJson.ToString(Formatting.Indented));
				_view.SetOutputJson($"// Migration failed: {ex.Message}");
				_view.SetLog($"Migration Log: {row.MigrationType.Name} - FAILED");
				return;
			}

			_view.SetInputJson(inputJson.ToString(Formatting.Indented));
			_view.SetOutputJson(outputJson.ToString(Formatting.Indented));
			_view.SetLog($"Migration Log: {row.MigrationType.Name} - SUCCESS (Applied: {applied})  Preview Instance: {instanceLabel}");
		}

		private bool TryGetFirstInstance(Type configType, out int id, out object instance)
		{
			id = 0;
			instance = null;

			var all = _provider.GetAllConfigs();
			if (!all.TryGetValue(configType, out var container))
			{
				return false;
			}

			if (!ConfigsEditorUtil.TryReadConfigs(container, out var entries) || entries.Count == 0)
			{
				return false;
			}

			id = entries[0].Id;
			instance = entries[0].Value;
			return instance != null;
		}

		private static MigrationState GetState(ulong currentVersion, ulong from, ulong to)
		{
			if (currentVersion >= to) return MigrationState.Applied;
			if (currentVersion == from) return MigrationState.Current;
			return MigrationState.Pending;
		}
	}
}
