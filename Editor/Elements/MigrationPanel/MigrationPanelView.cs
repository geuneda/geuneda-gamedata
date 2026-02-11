using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// 마이그레이션 패널 레이아웃을 렌더링하는 UI Toolkit 뷰입니다: 마이그레이션 행 목록,
	/// 전/후 JSON 뷰어가 있는 미리보기 섹션, 그리고 미리보기 및 적용 컨트롤을 포함합니다.
	/// 이 뷰는 순수하게 표현 계층이며, 로직은 <see cref="MigrationPanelController"/>에 있습니다.
	/// </summary>
	internal sealed class MigrationPanelView : VisualElement
	{
		private Label _header;
		private HelpBox _emptyState;
		private VisualElement _contentContainer;
		private ListView _listView;
		private List<MigrationRow> _rows = new List<MigrationRow>();

		private TextField _customJsonInput;
		private DropdownField _migrationDropdown;
		private Button _previewButton;
		private Button _applyButton;
		private JsonViewerElement _inputJson;
		private JsonViewerElement _outputJson;
		private Label _logLabel;

		public event Action PreviewRequested;
		public event Action ApplyRequested;

		public MigrationPanelView()
		{
			style.flexGrow = 1;
			style.paddingLeft = 8;
			style.paddingRight = 8;
			style.paddingTop = 6;
			style.paddingBottom = 6;

			_header = new Label("Migrations");
			_header.style.unityFontStyleAndWeight = FontStyle.Bold;
			_header.style.marginBottom = 6;

			_emptyState = new HelpBox("No migrations available in this project.", HelpBoxMessageType.Info);
			_emptyState.style.display = DisplayStyle.None;

			// 세로 분할: 마이그레이션 목록 (상단) + 미리보기 섹션 (하단)
			var verticalSplit = new TwoPaneSplitView(1, 220, TwoPaneSplitViewOrientation.Vertical);
			verticalSplit.viewDataKey = "ConfigBrowser_MigrationVerticalSplit";
			verticalSplit.style.flexGrow = 1;

			verticalSplit.Add(BuildMigrationsList());
			verticalSplit.Add(BuildPreview());

			_contentContainer = new VisualElement { style = { flexGrow = 1 } };
			_contentContainer.Add(verticalSplit);

			Add(_header);
			Add(_emptyState);
			Add(_contentContainer);
		}

		/// <summary>
		/// 사용자가 입력한 트리밍된 사용자 정의 JSON을 반환하거나, 비어 있으면 null을 반환합니다.
		/// </summary>
		public string CustomJson
		{
			get
			{
				var value = _customJsonInput?.value;
				return value == null ? null : value.Trim();
			}
		}

		/// <summary>
		/// 현재 선택된 마이그레이션 드롭다운 인덱스를 반환하거나, 선택되지 않았으면 -1을 반환합니다.
		/// </summary>
		public int SelectedIndex => _migrationDropdown?.index ?? -1;

		/// <summary>
		/// 헤더 레이블 텍스트를 업데이트합니다.
		/// </summary>
		public void SetHeader(string text)
		{
			_header.text = text;
		}

		/// <summary>
		/// 빈 상태 도움 상자와 콘텐츠 컨테이너 사이를 전환합니다.
		/// </summary>
		public void SetEmptyStateVisible(bool visible)
		{
			_emptyState.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
			_contentContainer.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
		}

		/// <summary>
		/// 표시된 마이그레이션 행을 교체하고 드롭다운 선택지를 새로고침합니다.
		/// </summary>
		public void SetRows(List<MigrationRow> rows)
		{
			_rows = rows ?? new List<MigrationRow>();
			_listView.itemsSource = _rows;
			_listView.RefreshItems();

			var choices = _rows
				.Select(r => $"{r.ConfigType.Name}: v{r.FromVersion} → v{r.ToVersion}")
				.ToList();

			_migrationDropdown.choices = choices;
			if (choices.Count > 0)
			{
				_migrationDropdown.index = 0;
			}
		}

		/// <summary>
		/// 미리보기 및 적용 버튼을 활성화하거나 비활성화합니다.
		/// </summary>
		public void SetButtonsEnabled(bool hasChoices, bool canApply)
		{
			_previewButton.SetEnabled(hasChoices);
			_applyButton.SetEnabled(canApply);
		}

		/// <summary>
		/// 입력(이전) 뷰어에 주어진 JSON 문자열을 표시합니다.
		/// </summary>
		public void SetInputJson(string json)
		{
			_inputJson.SetJson(json);
		}

		/// <summary>
		/// 출력(이후) 뷰어에 주어진 JSON 문자열을 표시합니다.
		/// </summary>
		public void SetOutputJson(string json)
		{
			_outputJson.SetJson(json);
		}

		/// <summary>
		/// 미리보기 아래에 표시되는 마이그레이션 로그 메시지를 설정합니다.
		/// </summary>
		public void SetLog(string message)
		{
			_logLabel.text = message ?? string.Empty;
		}

		private VisualElement BuildMigrationsList()
		{
			var container = new VisualElement { style = { flexGrow = 1, minHeight = 50 } };

			_listView = new ListView
			{
				selectionType = SelectionType.Single,
				showBorder = true,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				style = { flexGrow = 1 }
			};

			_listView.makeItem = MakeRow;
			_listView.bindItem = (e, i) => BindRow(e, i);

			container.Add(_listView);
			return container;
		}

		private VisualElement BuildPreview()
		{
			var container = new VisualElement
			{
				style =
				{
					flexGrow = 1,
					minHeight = 60,
					borderTopWidth = 1,
					borderTopColor = new StyleColor(new Color(0f, 0f, 0f, 0.2f)),
					paddingTop = 4
				}
			};

			var title = new Label("Migration Preview");
			title.style.unityFontStyleAndWeight = FontStyle.Bold;
			title.style.marginBottom = 4;

			// 사용자 정의 JSON 입력 섹션
			var customInputSection = new VisualElement { style = { marginBottom = 6 } };
			var customInputLabel = new Label("Custom Input JSON (paste legacy-schema JSON here):");
			customInputLabel.style.marginBottom = 2;

			_customJsonInput = new TextField
			{
				multiline = true,
				style =
				{
					minHeight = 40,
					maxHeight = 100
				}
			};
			_customJsonInput.AddToClassList("unity-text-field__input");

			// 마이그레이션 선택 및 미리보기 버튼 행
			var previewControlsRow = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					marginTop = 4
				}
			};

			var dropdownLabel = new Label("Target Version:") { style = { marginRight = 4 } };
			_migrationDropdown = new DropdownField { style = { minWidth = 120, marginRight = 8 } };
			_previewButton = new Button(() => PreviewRequested?.Invoke()) { text = "Preview" };
			_applyButton = new Button(() => ApplyRequested?.Invoke()) { text = "Apply Migration" };
			_applyButton.style.marginLeft = 8;

			previewControlsRow.Add(dropdownLabel);
			previewControlsRow.Add(_migrationDropdown);
			previewControlsRow.Add(_previewButton);
			previewControlsRow.Add(_applyButton);

			customInputSection.Add(customInputLabel);
			customInputSection.Add(_customJsonInput);
			customInputSection.Add(previewControlsRow);

			var split = new TwoPaneSplitView(0, 360, TwoPaneSplitViewOrientation.Horizontal);
			split.style.flexGrow = 1;
			split.style.minHeight = 60;

			var left = new VisualElement { style = { flexGrow = 1 } };
			left.Add(new Label("Input"));
			_inputJson = new JsonViewerElement();
			left.Add(_inputJson);

			var right = new VisualElement { style = { flexGrow = 1 } };
			right.Add(new Label("Output"));
			_outputJson = new JsonViewerElement();
			right.Add(_outputJson);

			split.Add(left);
			split.Add(right);

			_logLabel = new Label();
			_logLabel.style.marginTop = 4;
			_logLabel.style.whiteSpace = WhiteSpace.Normal;

			container.Add(title);
			container.Add(customInputSection);
			container.Add(split);
			container.Add(_logLabel);
			return container;
		}

		private static VisualElement MakeRow()
		{
			var row = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					paddingLeft = 6,
					paddingRight = 6,
					paddingTop = 2,
					paddingBottom = 2
				}
			};

			row.Add(new Label { name = "Type", style = { minWidth = 160 } });
			row.Add(new Label { name = "Migration", style = { minWidth = 100 } });
			row.Add(new Label { name = "State", style = { minWidth = 90 } });

			return row;
		}

		private void BindRow(VisualElement row, int index)
		{
			if (index < 0 || index >= _rows.Count) return;
			var data = _rows[index];

			row.Q<Label>("Type").text = data.ConfigType.Name;
			row.Q<Label>("Migration").text = $"v{data.FromVersion} -> v{data.ToVersion}";
			row.Q<Label>("State").text = data.State.ToString();
		}
	}
}
