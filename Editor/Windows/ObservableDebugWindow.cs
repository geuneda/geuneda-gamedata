using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// <see cref="ObservableDebugRegistry"/>에서 추적하는 모든 활성 Observable 인스턴스를 표시하는 에디터 창입니다.
	/// <c>Tools/Game Data/Observable Debugger</c>를 통해 접근합니다.
	/// </summary>
	/// <remarks>
	/// <para>Observable은 자체 등록 패턴을 통해 에디터에서 생성될 때 자동으로 등록됩니다.</para>
	/// <para>이름, 타입(Field/Computed/List/등), 구독자 활동별 필터링을 지원합니다.</para>
	/// <para>Computed Observable을 선택하면 하단 패널에 의존성 그래프를 표시합니다.</para>
	/// </remarks>
	public sealed class ObservableDebugWindow : EditorWindow
	{
		private static readonly List<string> _kinds = new List<string>
		{
			"All",
			"Field",
			"Computed",
			"List",
			"Dictionary",
			"HashSet"
		};

		private ToolbarSearchField _filterField;
		private ToolbarMenu _kindMenu;
		private Toggle _activeOnlyToggle;
		private ToolbarButton _refreshButton;
		private MultiColumnListView _listView;
		private readonly List<ObservableDebugRegistry.EntrySnapshot> _rows = new List<ObservableDebugRegistry.EntrySnapshot>();
		private Label _headerLabel;
		private DependencyGraphElement _dependencyGraph;

		/// <summary>
		/// Observable Debugger 창을 엽니다.
		/// </summary>
		[MenuItem("Tools/Game Data/Observable Debugger")]
		public static void ShowWindow()
		{
			var window = GetWindow<ObservableDebugWindow>("Observable Debugger");
			window.minSize = new Vector2(720, 420);
			window.Show();
		}

		/// <summary>
		/// 창이 열리거나 리로드될 때 UI Toolkit GUI를 생성합니다.
		/// </summary>
		public void CreateGUI()
		{
			rootVisualElement.Clear();
			rootVisualElement.style.flexGrow = 1;

			rootVisualElement.Add(BuildToolbar());

			var split = new TwoPaneSplitView(1, 180, TwoPaneSplitViewOrientation.Vertical);
			split.style.flexGrow = 1;
			split.Add(BuildList());

			_dependencyGraph = new DependencyGraphElement();
			split.Add(_dependencyGraph);

			rootVisualElement.Add(split);

			// 창이 열려 있는 동안 주기적으로 폴링합니다(에디터 전용 레지스트리, 플레이모드 불필요).
			rootVisualElement.schedule.Execute(RefreshData).Every(250);
			RefreshData();
		}

		private static void OpenSourceFile(ObservableDebugRegistry.EntrySnapshot snapshot)
		{
			var filePath = snapshot.FilePath;
			var lineNumber = snapshot.LineNumber;

			if (string.IsNullOrEmpty(filePath) || lineNumber <= 0)
			{
				return;
			}

			// Unity 내부 유틸리티를 사용하여 특정 라인에서 파일을 엽니다
			// 콘솔 로그 항목 클릭과 동일하게 작동합니다
			InternalEditorUtility.OpenFileAtLineExternal(filePath, lineNumber);
		}

		private static string FormatName(ObservableDebugRegistry.EntrySnapshot s)
		{
			// 제네릭 이름에서 백틱-숫자 패턴(예: `1, `2)을 제거합니다
			var formattedName = Regex.Replace(s.Name ?? string.Empty, @"`\d+", "");

			// 소스 위치가 있으면 추가합니다
			var sourceLocation = s.SourceLocation;
			if (!string.IsNullOrEmpty(sourceLocation))
			{
				return $"{formattedName} ({sourceLocation})";
			}

			// 소스 위치가 없으면 ID로 대체합니다
			return $"{formattedName}#{s.Id}";
		}

		private VisualElement BuildToolbar()
		{
			var toolbar = new Toolbar();
			toolbar.style.flexWrap = Wrap.Wrap; // 작은 화면에서 줄바꿈 허용
			toolbar.style.alignItems = Align.Center; // 모든 항목 세로 중앙 정렬

			_headerLabel = new Label("Observables");
			_headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			_headerLabel.style.marginRight = 8;
			_headerLabel.style.flexShrink = 0; // 카운트 레이블 축소 방지

			_filterField = new ToolbarSearchField();
			_filterField.style.flexGrow = 1;
			_filterField.style.flexShrink = 1;
			_filterField.style.minWidth = 120;
			_filterField.RegisterValueChangedCallback(_ => RefreshData());

			// 컴팩트한 드롭다운을 위해 ToolbarMenu를 사용합니다(레이블 간격 없음)
			_kindMenu = new ToolbarMenu { text = "All" };
			foreach (var kind in _kinds)
			{
				_kindMenu.menu.AppendAction(kind, a =>
				{
					_kindMenu.text = a.name;
					RefreshData();
				}, a => a.name == _kindMenu.text ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
			}

			_activeOnlyToggle = new Toggle("Active only");
			_activeOnlyToggle.labelElement.style.minWidth = 0; // 고정 레이블 너비 제거
			_activeOnlyToggle.style.marginLeft = 4;
			_activeOnlyToggle.style.alignSelf = Align.Center; // 토글 중앙 정렬 보장
			_activeOnlyToggle.RegisterValueChangedCallback(_ => RefreshData());

			_refreshButton = new ToolbarButton(RefreshData)
			{
				text = "↻",
				tooltip = "Refresh"
			};

			toolbar.Add(_headerLabel);
			toolbar.Add(_filterField);
			toolbar.Add(_kindMenu);
			toolbar.Add(_activeOnlyToggle);
			toolbar.Add(_refreshButton);
			return toolbar;
		}

		private VisualElement BuildList()
		{
			_listView = new MultiColumnListView
			{
				columns =
				{
					new Column { name = "name", title = "Observable", width = 280, stretchable = true },
					new Column { name = "value", title = "Value", width = 120 },
					new Column { name = "subs", title = "Subs", width = 50 },
					new Column { name = "kind", title = "Type", width = 80 }
				},
				sortingMode = ColumnSortingMode.Default,
				selectionType = SelectionType.Single,
				showBorder = true,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				style = { flexGrow = 1 }
			};

			_listView.columns["name"].makeCell = () => new Label();
			_listView.columns["name"].bindCell = (e, i) =>
			{
				var s = _rows[i];
				var label = (Label)e;
				label.text = FormatName(s);
				ApplyRowStyle(e, s);
			};

			_listView.columns["value"].makeCell = () => new Label();
			_listView.columns["value"].bindCell = (e, i) =>
			{
				var s = _rows[i];
				((Label)e).text = s.Value;
				ApplyRowStyle(e, s);
			};

			_listView.columns["subs"].makeCell = () => new Label { style = { unityTextAlign = TextAnchor.MiddleRight } };
			_listView.columns["subs"].bindCell = (e, i) =>
			{
				var s = _rows[i];
				((Label)e).text = s.Subscribers.ToString();
				ApplyRowStyle(e, s);
			};

			_listView.columns["kind"].makeCell = () => new Label { style = { unityTextAlign = TextAnchor.MiddleRight } };
			_listView.columns["kind"].bindCell = (e, i) =>
			{
				var s = _rows[i];
				((Label)e).text = s.Kind;
				ApplyRowStyle(e, s);
			};

			_listView.itemsSource = _rows;
			_listView.selectionChanged += OnSelectionChanged;
			_listView.itemsChosen += OnItemsChosen; // 더블클릭으로 소스 열기
			_listView.columnSortingChanged += () => RefreshData();

			return _listView;
		}

		private void OnItemsChosen(IEnumerable<object> items)
		{
			var first = items.FirstOrDefault();
			if (first is ObservableDebugRegistry.EntrySnapshot snapshot)
			{
				OpenSourceFile(snapshot);
			}
		}

		private void ApplyRowStyle(VisualElement element, ObservableDebugRegistry.EntrySnapshot snapshot)
		{
			// MultiColumnListView 셀은 행 컨테이너로 래핑됩니다.
			// 활동에 따라 부모 행의 스타일을 지정할 수 있습니다.
			var row = element.parent;
			if (row != null)
			{
				row.style.backgroundColor = snapshot.Subscribers > 0
					? new StyleColor(new Color(0f, 1f, 0f, 0.05f))
					: new StyleColor(StyleKeyword.Null);
			}
		}

		private void RefreshData()
		{
			_rows.Clear();

			var filter = _filterField?.value?.Trim();
			var hasFilter = !string.IsNullOrEmpty(filter);
			var filterLower = hasFilter ? filter.ToLowerInvariant() : string.Empty;

			var kind = _kindMenu?.text ?? "All";
			var activeOnly = _activeOnlyToggle != null && _activeOnlyToggle.value;

			foreach (var s in ObservableDebugRegistry.EnumerateSnapshots())
			{
				if (activeOnly && s.Subscribers <= 0)
				{
					continue;
				}

				if (kind != "All" && !string.Equals(s.Kind, kind, StringComparison.Ordinal))
				{
					continue;
				}

				if (hasFilter)
				{
					var observableName = s.Name ?? string.Empty;
					if (!observableName.ToLowerInvariant().Contains(filterLower))
					{
						continue;
					}
				}

				_rows.Add(s);
			}

			SortRows();

			_headerLabel.text = $"Observables: {_rows.Count}";
			_listView.RefreshItems();

			if (_rows.Count == 0)
			{
				_dependencyGraph.SetTarget(default);
			}
		}

		private void SortRows()
		{
			var sortDesc = _listView.sortedColumns.FirstOrDefault();
			if (sortDesc == null)
			{
				_rows.Sort((a, b) =>
				{
					var c = string.CompareOrdinal(a.Kind, b.Kind);
					return c != 0 ? c : string.CompareOrdinal(a.Name, b.Name);
				});
				return;
			}

			var dir = sortDesc.direction == SortDirection.Ascending ? 1 : -1;
			_rows.Sort((a, b) =>
			{
				int result;
				switch (sortDesc.columnName)
				{
					case "name": result = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase); break;
					case "value": result = string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase); break;
					case "subs": result = a.Subscribers.CompareTo(b.Subscribers); break;
					case "kind": result = string.Compare(a.Kind, b.Kind, StringComparison.OrdinalIgnoreCase); break;
					default: result = 0; break;
				}
				return result * dir;
			});
		}

		private void OnSelectionChanged(IEnumerable<object> selected)
		{
			var first = selected.FirstOrDefault();
			if (first is ObservableDebugRegistry.EntrySnapshot snapshot)
			{
				_dependencyGraph.SetTarget(snapshot);
			}
			else
			{
				_dependencyGraph.SetTarget(default);
			}
		}
	}
}
