using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// UI Toolkit을 사용하여 전체 Config Browser UI 레이아웃을 구성하고 관리합니다.
	/// 사용자 상호작용(검색, 유효성 검사, 내보내기, 탭 전환, 트리 선택)에 대한 이벤트를 노출하며
	/// <see cref="ConfigBrowserWindow"/>가 구독합니다. 이 클래스는 순수하게 표현 계층입니다.
	/// </summary>
	internal sealed class ConfigBrowserView
	{
		private Toolbar _toolbar;
		private ToolbarMenu _providerMenu;
		private ToolbarToggle _browseTab;
		private ToolbarToggle _migrationsTab;

		private Toolbar _browseActionBar;
		private ToolbarSearchField _searchField;
		private Button _validateAllButton;
		private Button _exportAllButton;

		private VisualElement _browseRoot;
		private VisualElement _migrationsRoot;
		private MigrationPanelElement _migrationPanel;

		private TreeView _treeView;
		private JsonViewerElement _jsonViewer;
		private Label _detailsHeader;
		private Button _validateSelectedButton;
		private Button _exportSelectedButton;

		private VisualElement _validationPanel;
		private Label _validationHeaderLabel;
		private Button _clearValidationFilterButton;
		private ScrollView _validationList;

		public event Action<string> SearchChanged;
		public event Action ValidateAllRequested;
		public event Action ExportAllRequested;
		public event Action ValidateSelectedRequested;
		public event Action ExportSelectedRequested;
		public event Action ClearValidationFilterRequested;
		public event Action<IEnumerable<object>> TreeSelectionChanged;
		public event Action<string, int?> ValidationRowClicked;
		public event Action<bool> TabChanged;

		public ConfigBrowserView(VisualElement root)
		{
			Build(root);
		}

		/// <summary><see cref="Build"/>가 완료된 후 true를 반환합니다.</summary>
		public bool IsInitialized => _detailsHeader != null;

		/// <summary>최상위 도구 모음 요소입니다.</summary>
		public Toolbar Toolbar => _toolbar;
		/// <summary>도구 모음의 프로바이더 선택 드롭다운 메뉴입니다.</summary>
		public ToolbarMenu ProviderMenu => _providerMenu;
		/// <summary>Browse 탭 콘텐츠의 루트 컨테이너입니다.</summary>
		public VisualElement BrowseRoot => _browseRoot;
		/// <summary>Migrations 탭 콘텐츠의 루트 컨테이너입니다.</summary>
		public VisualElement MigrationsRoot => _migrationsRoot;
		/// <summary>Migrations 탭 내부의 내장된 <see cref="MigrationPanelElement"/>입니다.</summary>
		public MigrationPanelElement MigrationPanel => _migrationPanel;
		/// <summary>검색 필드의 현재 텍스트입니다.</summary>
		public string SearchText => _searchField?.value;

		/// <summary>
		/// 도구 모음의 현재 프로바이더 드롭다운 메뉴를 <paramref name="newMenu"/>로 교체합니다.
		/// </summary>
		public void ReplaceProviderMenu(ToolbarMenu newMenu)
		{
			if (_toolbar == null || _providerMenu == null || newMenu == null) return;
			_toolbar.Insert(_toolbar.IndexOf(_providerMenu), newMenu);
			_toolbar.Remove(_providerMenu);
			_providerMenu = newMenu;
		}

		/// <summary>
		/// 표시되는 탭을 전환합니다. <paramref name="isBrowse"/>가 true이면 Browse 탭이 표시되고,
		/// 그렇지 않으면 Migrations 탭이 표시됩니다.
		/// </summary>
		public void SetActiveTab(bool isBrowse)
		{
			_browseRoot.style.display = isBrowse ? DisplayStyle.Flex : DisplayStyle.None;
			_migrationsRoot.style.display = isBrowse ? DisplayStyle.None : DisplayStyle.Flex;
			_browseTab.SetValueWithoutNotify(isBrowse);
			_migrationsTab.SetValueWithoutNotify(!isBrowse);
		}

		/// <summary>
		/// 트리 뷰 루트 항목을 교체하고 시각적 트리를 다시 빌드합니다.
		/// </summary>
		public void SetTreeItems(IList<TreeViewItemData<ConfigNode>> items)
		{
			_treeView.SetRootItems(items);
			_treeView.Rebuild();
		}

		/// <summary>
		/// 주어진 <paramref name="itemId"/>로 트리 항목을 프로그래밍 방식으로 선택하고 뷰로 스크롤합니다.
		/// </summary>
		public void SelectTreeItem(int itemId)
		{
			_treeView.SetSelection(new List<int> { itemId });
			_treeView.ScrollToItem(itemId);
		}

		/// <summary>
		/// 주어진 <paramref name="selection"/>을 표시하도록 상세 패널을 업데이트합니다.
		/// 헤더에 설정 <paramref name="displayName"/>을 표시하고 뷰어에 직렬화된 <paramref name="json"/>을 표시합니다.
		/// </summary>
		public void SetSelection(ConfigSelection selection, string displayName, string json)
		{
			if (!selection.IsValid)
			{
				_detailsHeader.text = "No selection";
				_validateSelectedButton.SetEnabled(false);
				_exportSelectedButton.SetEnabled(false);
				_jsonViewer.SetJson(string.Empty);
				return;
			}

			_detailsHeader.text = displayName;
			_validateSelectedButton.SetEnabled(true);
			_exportSelectedButton.SetEnabled(true);
			_jsonViewer.SetJson(json);
		}

		/// <summary>
		/// 주어진 <paramref name="errors"/> 목록으로 유효성 검사 패널을 채우고,
		/// 활성 <paramref name="filter"/> 범위를 반영하도록 헤더를 업데이트합니다.
		/// <paramref name="providerExists"/>가 false이면 안내 메시지를 표시합니다.
		/// </summary>
		public void SetValidationResults(List<ValidationErrorInfo> errors, ValidationFilter filter, bool providerExists)
		{
			_validationList.Clear();

			var showing = filter.IsAll ? "All" : $"{filter.ConfigType?.Name} (ID:{filter.ConfigId})";
			_validationHeaderLabel.text = $"Validation Results  Showing: {showing}  Errors: {errors.Count}";
			_clearValidationFilterButton.SetEnabled(!filter.IsAll);

			if (!providerExists)
			{
				_validationList.Add(new HelpBox("No provider selected.\nEnter Play Mode and create a ConfigsProvider.", HelpBoxMessageType.Info));
				return;
			}

			if (errors.Count == 0)
			{
				_validationList.Add(new HelpBox("No validation errors.", HelpBoxMessageType.Info));
				return;
			}

			for (int i = 0; i < errors.Count; i++)
			{
				var e = errors[i];
				var row = new ValidationErrorElement();
				row.Bind(e.ConfigTypeName, e.ConfigId, e.FieldName, e.Message);
				row.Clicked += (typeName, configId) => ValidationRowClicked?.Invoke(typeName, configId);
				_validationList.Add(row);
			}
		}

		/// <summary>
		/// 유효성 검사 결과 목록에서 모든 요소를 제거합니다.
		/// </summary>
		public void ClearValidationList()
		{
			_validationList.Clear();
		}

		private void Build(VisualElement root)
		{
			root.Clear();
			root.style.flexGrow = 1;

			// 전역 도구 모음: 프로바이더 + 탭
			root.Add(BuildToolbar());

			// 탭 콘텐츠 영역
			_browseRoot = BuildBrowseRoot();
			_migrationsRoot = BuildMigrationsRoot();

			var content = new VisualElement { style = { flexGrow = 1 } };
			content.Add(_browseRoot);
			content.Add(_migrationsRoot);
			root.Add(content);
		}

		private VisualElement BuildToolbar()
		{
			_toolbar = new Toolbar();

			_providerMenu = new ToolbarMenu { text = "No providers" };
			_providerMenu.style.minWidth = 280;

			// 전역 도구 모음에 통합된 탭
			_browseTab = new ToolbarToggle { text = "Browse", value = true };
			_migrationsTab = new ToolbarToggle { text = "Migrations", value = false };

			_browseTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					_migrationsTab.SetValueWithoutNotify(false);
					TabChanged?.Invoke(true);
				}
			});

			_migrationsTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					_browseTab.SetValueWithoutNotify(false);
					TabChanged?.Invoke(false);
				}
			});

			// 프로바이더 메뉴 뒤에 탭을 왼쪽으로 밀기 위한 스페이서
			var spacer = new VisualElement { style = { flexGrow = 1 } };

			_toolbar.Add(_providerMenu);
			_toolbar.Add(_browseTab);
			_toolbar.Add(_migrationsTab);
			_toolbar.Add(spacer);

			return _toolbar;
		}

		private VisualElement BuildBrowseActionBar()
		{
			_browseActionBar = new Toolbar();

			_searchField = new ToolbarSearchField();
			_searchField.style.flexGrow = 1;
			_searchField.RegisterValueChangedCallback(evt => SearchChanged?.Invoke(evt.newValue));

			_validateAllButton = new Button(() => ValidateAllRequested?.Invoke())
			{
				text = "Validate All"
			};

			_exportAllButton = new Button(() => ExportAllRequested?.Invoke())
			{
				text = "Export All"
			};

			_browseActionBar.Add(_searchField);
			_browseActionBar.Add(_validateAllButton);
			_browseActionBar.Add(_exportAllButton);

			return _browseActionBar;
		}

		private VisualElement BuildBrowseRoot()
		{
			var root = new VisualElement { style = { flexGrow = 1 } };

			// Browse 액션 바: 검색 + 전체 검증 + 전체 내보내기
			root.Add(BuildBrowseActionBar());

			// 가로 분할: 트리 뷰 (왼쪽) + 상세 패널 (오른쪽)
			var horizontalSplit = new TwoPaneSplitView(0, 260, TwoPaneSplitViewOrientation.Horizontal);
			horizontalSplit.viewDataKey = "ConfigBrowser_HorizontalSplit_v2"; // 지속된 상태를 초기화하기 위해 키를 변경함
			horizontalSplit.style.flexGrow = 1;
			horizontalSplit.style.minHeight = 200;

			_treeView = new TreeView
			{
				selectionType = SelectionType.Single,
				showBorder = true,
				style = { flexGrow = 1, minWidth = 200, minHeight = 100 },
				makeItem = () => new Label { style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 4 } },
				bindItem = (element, index) =>
				{
					var node = _treeView.GetItemDataForIndex<ConfigNode>(index);
					var label = (Label)element;
					label.text = node.DisplayName;

					// 노드 종류에 따른 스타일
					label.style.unityFontStyleAndWeight = node.Kind == ConfigNodeKind.Header
						? FontStyle.Bold
						: FontStyle.Normal;
				}
			};
			_treeView.selectionChanged += selection => TreeSelectionChanged?.Invoke(selection);

			// 축소를 방지하기 위해 최소 크기의 컨테이너로 TreeView를 래핑
			var treeContainer = new VisualElement { style = { flexGrow = 1, minWidth = 200 } };
			treeContainer.Add(_treeView);
			horizontalSplit.Add(treeContainer);
			horizontalSplit.Add(BuildDetailsPanel());

			// 세로 분할: 콘텐츠 (상단) + 유효성 검사 패널 (하단)
			var verticalSplit = new TwoPaneSplitView(1, 180, TwoPaneSplitViewOrientation.Vertical);
			verticalSplit.viewDataKey = "ConfigBrowser_BrowseVerticalSplit_v2"; // 지속된 상태를 초기화하기 위해 키를 변경함
			verticalSplit.style.flexGrow = 1;

			verticalSplit.Add(horizontalSplit);
			verticalSplit.Add(BuildValidationPanel());

			root.Add(verticalSplit);
			return root;
		}

		private VisualElement BuildMigrationsRoot()
		{
			var root = new VisualElement { style = { flexGrow = 1 } };
			_migrationPanel = new MigrationPanelElement();
			root.Add(_migrationPanel);
			return root;
		}

		private VisualElement BuildDetailsPanel()
		{
			var panel = new VisualElement
			{
				style =
				{
					flexGrow = 1,
					paddingLeft = 8,
					paddingRight = 8,
					paddingTop = 6,
					paddingBottom = 6
				}
			};

			var header = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					justifyContent = Justify.SpaceBetween,
					marginBottom = 6
				}
			};

			_detailsHeader = new Label("No selection");
			_detailsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;

			// 설정별 작업을 위한 버튼 컨테이너
			var buttonContainer = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center
				}
			};

			_validateSelectedButton = new Button(() => ValidateSelectedRequested?.Invoke())
			{
				text = "Validate"
			};
			_validateSelectedButton.SetEnabled(false);

			_exportSelectedButton = new Button(() => ExportSelectedRequested?.Invoke())
			{
				text = "Export"
			};
			_exportSelectedButton.SetEnabled(false);

			buttonContainer.Add(_validateSelectedButton);
			buttonContainer.Add(_exportSelectedButton);

			header.Add(_detailsHeader);
			header.Add(buttonContainer);

			_jsonViewer = new JsonViewerElement();

			panel.Add(header);
			panel.Add(_jsonViewer);

			return panel;
		}

		private VisualElement BuildValidationPanel()
		{
			_validationPanel = new VisualElement
			{
				style =
				{
					flexGrow = 1,
					minHeight = 100,
					borderTopWidth = 1,
					borderTopColor = new StyleColor(new Color(0f, 0f, 0f, 0.2f))
				}
			};

			var header = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					justifyContent = Justify.SpaceBetween,
					paddingLeft = 8,
					paddingRight = 8,
					paddingTop = 4,
					paddingBottom = 4
				}
			};

			_validationHeaderLabel = new Label("Validation Results");
			_validationHeaderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

			_clearValidationFilterButton = new Button(() => ClearValidationFilterRequested?.Invoke())
			{
				text = "Clear"
			};

			header.Add(_validationHeaderLabel);
			header.Add(_clearValidationFilterButton);

			_validationList = new ScrollView(ScrollViewMode.Vertical);
			_validationList.style.flexGrow = 1;

			_validationPanel.Add(header);
			_validationPanel.Add(_validationList);

			return _validationPanel;
		}
	}
}
