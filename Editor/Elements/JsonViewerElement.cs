using UnityEngine;
using UnityEngine.UIElements;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// 에디터 도구용 최소 JSON 뷰어 요소(읽기 전용)입니다.
	/// 의도적으로 단순하게 시작(ScrollView 내 Label)하며, 나중에 트리/비교 뷰로 업그레이드할 수 있습니다.
	/// </summary>
	public sealed class JsonViewerElement : VisualElement
	{
		private readonly ScrollView _scrollView;
		private readonly Label _label;

		public JsonViewerElement()
		{
			style.flexGrow = 1;

			_scrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
			{
				horizontalScrollerVisibility = ScrollerVisibility.Auto,
				verticalScrollerVisibility = ScrollerVisibility.Auto
			};
			_scrollView.style.flexGrow = 1;
			_scrollView.style.backgroundColor = new StyleColor(new Color(0.16f, 0.16f, 0.16f, 1f));
			_scrollView.style.borderTopWidth = 1;
			_scrollView.style.borderBottomWidth = 1;
			_scrollView.style.borderLeftWidth = 1;
			_scrollView.style.borderRightWidth = 1;
			_scrollView.style.borderTopColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 1f));
			_scrollView.style.borderBottomColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 1f));
			_scrollView.style.borderLeftColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 1f));
			_scrollView.style.borderRightColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 1f));
			_scrollView.style.borderTopLeftRadius = 3;
			_scrollView.style.borderTopRightRadius = 3;
			_scrollView.style.borderBottomLeftRadius = 3;
			_scrollView.style.borderBottomRightRadius = 3;

			_label = new Label();
			_label.style.unityFontStyleAndWeight = FontStyle.Normal;
			_label.style.whiteSpace = WhiteSpace.Pre;
			_label.style.paddingLeft = 4;
			_label.style.paddingRight = 4;
			_label.style.paddingTop = 2;
			_label.style.paddingBottom = 2;
			_label.enableRichText = false;
			_label.selection.isSelectable = true;

			_scrollView.Add(_label);
			Add(_scrollView);
		}

		/// <summary>
		/// 뷰어에 표시할 JSON 문자열을 설정합니다.
		/// </summary>
		public void SetJson(string json)
		{
			_label.text = json ?? string.Empty;
		}
	}
}

