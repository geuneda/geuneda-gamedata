using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// 단일 유효성 검사 오류 행을 표시하기 위한 UI Toolkit 요소입니다.
	/// Config Browser 유효성 검사 결과 패널에서 사용하기 위한 것입니다.
	/// </summary>
	public sealed class ValidationErrorElement : VisualElement
	{
		private readonly Label _title;
		private readonly Label _detail;

		private string _configType;
		private int? _configId;

		public event Action<string, int?> Clicked;

		public ValidationErrorElement()
		{
			style.flexDirection = FlexDirection.Column;
			style.paddingLeft = 6;
			style.paddingRight = 6;
			style.paddingTop = 4;
			style.paddingBottom = 4;
			style.borderBottomWidth = 1;
			style.borderBottomColor = new StyleColor(new Color(0f, 0f, 0f, 0.15f));

			_title = new Label
			{
				style =
				{
					unityFontStyleAndWeight = FontStyle.Bold,
					marginBottom = 2
				}
			};

			_detail = new Label
			{
				style =
				{
					whiteSpace = WhiteSpace.Normal
				}
			};

			Add(_title);
			Add(_detail);

			RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(_configType, _configId));
		}

		/// <summary>
		/// 요소를 특정 유효성 검사 오류에 바인딩합니다.
		/// </summary>
		public void Bind(string configType, int? configId, string fieldName, string message)
		{
			_configType = configType;
			_configId = configId;

			var idStr = configId.HasValue ? $" (ID:{configId.Value})" : string.Empty;
			_title.text = $"[!] {configType}{idStr}";
			_detail.text = $"{fieldName}: {message}";
		}
	}
}

