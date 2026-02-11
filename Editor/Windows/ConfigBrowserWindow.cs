using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// 설정 데이터를 탐색, 유효성 검사 및 마이그레이션하기 위한 통합 인터페이스를 제공하는 에디터 창입니다.
	/// <c>Tools/Game Data/Config Browser</c>를 통해 접근합니다.
	/// </summary>
	/// <remarks>
	/// <para>Browse 탭은 할당된 프로바이더의 모든 설정을 JSON 미리보기 및 유효성 검사와 함께 트리 뷰로 표시합니다.</para>
	/// <para>Migrations 탭(마이그레이션이 존재할 때만 표시)은 마이그레이션 상태를 표시하고 인메모리 미리보기를 제공합니다.</para>
	/// </remarks>
	public sealed class ConfigBrowserWindow : EditorWindow
	{
		private const int SingleConfigId = 0;

		private ConfigBrowserView _view;
		private ProviderMenuController _providerMenuController;

		// 변경 감지: 데이터가 변경될 때 자동 새로고침을 위해 프로바이더 데이터 지문을 추적합니다.
		private int _lastConfigTypeCount = -1;
		private int _lastTotalConfigCount = -1;

		private ValidationFilter _validationFilter = ValidationFilter.All();
		private ConfigSelection _selection;

		/// <summary>
		/// Config Browser 창을 엽니다.
		/// </summary>
		[MenuItem("Tools/Game Data/Config Browser")]
		public static void ShowWindow()
		{
			var window = GetWindow<ConfigBrowserWindow>("Config Browser");
			window.minSize = new Vector2(720, 420);
			window.Show();
		}

		private void OnEnable()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		/// <summary>
		/// 창이 열리거나 리로드될 때 UI Toolkit GUI를 생성합니다.
		/// </summary>
		public void CreateGUI()
		{
			_view = new ConfigBrowserView(rootVisualElement);
			_providerMenuController = new ProviderMenuController(_view);
			_providerMenuController.ProviderChanged += OnProviderChanged;

			_view.SearchChanged += _ => RefreshTree();
			_view.ValidateAllRequested += OnValidateAllRequested;
			_view.ExportAllRequested += ExportAllJson;
			_view.ValidateSelectedRequested += OnValidateSelectedRequested;
			_view.ExportSelectedRequested += ExportSelectedJson;
			_view.ClearValidationFilterRequested += OnClearValidationFilterRequested;
			_view.TreeSelectionChanged += OnTreeSelectionChanged;
			_view.ValidationRowClicked += OnValidationRowClicked;
			_view.TabChanged += SetActiveTab;

			rootVisualElement.schedule.Execute(RefreshProviderList).Every(250);
			RefreshProviderList();
			RefreshAll();
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			var isGuiInitialized = _view != null && _view.IsInitialized;

			// 오래된 참조를 방지하기 위해 플레이 모드 진입/종료 시 선택을 초기화
			if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
			{
				_providerMenuController?.ClearSelection();
				_lastConfigTypeCount = -1;
				_lastTotalConfigCount = -1;

				if (isGuiInitialized)
				{
					RefreshAll();
				}
			}
			else if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
			{
				if (isGuiInitialized)
				{
					// 새로 등록된 프로바이더를 가져오거나 오래된 상태를 정리하기 위해 새로고침
					RefreshProviderList();
					RefreshAll();
				}
			}
		}

		private void OnProviderChanged()
		{
			_lastConfigTypeCount = -1;
			_lastTotalConfigCount = -1;
			if (_view != null && _view.IsInitialized)
			{
				RefreshAll();
			}
		}

		private void RefreshProviderList()
		{
			_providerMenuController.RefreshSnapshots();

			// 현재 프로바이더 내 데이터 변경을 감지하고 필요 시 트리를 새로고침합니다.
			var provider = _providerMenuController.Provider;
			if (provider != null)
			{
				var (typeCount, totalCount) = ConfigsEditorUtil.ComputeConfigCounts(provider);
				if (typeCount != _lastConfigTypeCount || totalCount != _lastTotalConfigCount)
				{
					_lastConfigTypeCount = typeCount;
					_lastTotalConfigCount = totalCount;
					RefreshTree();
				}
			}
		}

		private void SetActiveTab(bool isBrowse)
		{
			_view.SetActiveTab(isBrowse);

			// 전환 시 마이그레이션 패널을 업데이트된 상태로 유지합니다.
			if (!isBrowse)
			{
				_view.MigrationPanel.SetProvider(_providerMenuController.Provider);
			}
		}

		private void RefreshAll()
		{
			_selection = ConfigSelection.None();
			_view.SetSelection(_selection, "No selection", string.Empty);

			RefreshTree();
			_view.SetValidationResults(new List<ValidationErrorInfo>(), _validationFilter, _providerMenuController.Provider != null);
			RefreshMigrationsVisibility();
			SetActiveTab(isBrowse: true);
		}

		private void RefreshMigrationsVisibility()
		{
			// Migrations 탭은 항상 표시됩니다; 빈 상태는 MigrationPanelElement에서 처리합니다.
			_view.MigrationPanel.SetProvider(_providerMenuController.Provider);
		}

		private void RefreshTree()
		{
			var items = ConfigTreeBuilder.BuildTreeItems(_providerMenuController.Provider, _view.SearchText);
			_view.SetTreeItems(items);
		}

		private void OnTreeSelectionChanged(IEnumerable<object> selected)
		{
			var first = selected.FirstOrDefault();
			if (first is not ConfigNode node || node.Kind != ConfigNodeKind.Entry)
			{
				_selection = ConfigSelection.None();
				_view.SetSelection(_selection, "No selection", string.Empty);
				return;
			}

			_selection = new ConfigSelection(node.ConfigType, node.ConfigId, node.Value);
			_view.SetSelection(_selection, node.DisplayName, ConfigExportService.ToJson(node.Value));
		}

		private void OnValidateAllRequested()
		{
			_validationFilter = ValidationFilter.All();
			var errors = ConfigValidationService.ValidateAll(_providerMenuController.Provider);
			_view.SetValidationResults(errors, _validationFilter, _providerMenuController.Provider != null);
		}

		private void OnValidateSelectedRequested()
		{
			if (!_selection.IsValid || _providerMenuController.Provider == null) return;
			_validationFilter = ValidationFilter.Single(_selection.ConfigType, _selection.ConfigId);
			var errors = ConfigValidationService.ValidateSingle(_selection);
			_view.SetValidationResults(errors, _validationFilter, _providerMenuController.Provider != null);
		}

		private void OnClearValidationFilterRequested()
		{
			_validationFilter = ValidationFilter.All();
			var errors = ConfigValidationService.ValidateAll(_providerMenuController.Provider);
			_view.SetValidationResults(errors, _validationFilter, _providerMenuController.Provider != null);
		}

		private void OnValidationRowClicked(string configTypeName, int? configId)
		{
			// 최선의 노력: 표시된 항목을 스캔하여 트리에서 일치하는 노드를 선택합니다.
			var provider = _providerMenuController.Provider;
			if (provider == null) return;
			var targetType = provider.GetAllConfigs().Keys.FirstOrDefault(t => t.Name == configTypeName);
			if (targetType == null) return;

			// 결정적으로 검색할 수 있도록 트리 항목 데이터를 다시 빌드합니다.
			var roots = ConfigTreeBuilder.BuildTreeItems(provider, _view.SearchText);
			_view.SetTreeItems(roots);

			var itemId = ConfigTreeBuilder.FindTreeItemIdForEntry(roots, targetType, configId ?? SingleConfigId);
			if (itemId.HasValue)
			{
				_view.SelectTreeItem(itemId.Value);
			}
		}

		private void ExportAllJson()
		{
			var provider = _providerMenuController.Provider;
			if (provider == null)
			{
				EditorUtility.DisplayDialog("Export JSON", "No provider selected.\nEnter Play Mode and create a ConfigsProvider.", "OK");
				return;
			}

			var json = ConfigExportService.ExportProviderToJson(provider);
			var path = EditorUtility.SaveFilePanel("Export All Configs JSON", Application.dataPath, "configs.json", "json");
			if (string.IsNullOrWhiteSpace(path))
			{
				return;
			}

			System.IO.File.WriteAllText(path, json);
			EditorUtility.RevealInFinder(path);
		}

		private void ExportSelectedJson()
		{
			if (!_selection.IsValid)
			{
				EditorUtility.DisplayDialog("Export JSON", "No config selected.", "OK");
				return;
			}

			var typeName = _selection.ConfigType.Name;
			var configId = _selection.ConfigId;
			var isSingleton = configId == SingleConfigId;
			var fileName = isSingleton ? $"{typeName}.json" : $"{typeName}_{configId}.json";

			var json = ConfigExportService.ToJson(_selection.Value);
			var path = EditorUtility.SaveFilePanel("Export Config JSON", Application.dataPath, fileName, "json");
			if (string.IsNullOrWhiteSpace(path))
			{
				return;
			}

			System.IO.File.WriteAllText(path, json);
			EditorUtility.RevealInFinder(path);
		}
	}
}
