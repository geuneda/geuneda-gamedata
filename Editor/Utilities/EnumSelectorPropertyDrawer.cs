using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace

namespace GeunedaEditor.GameData
{
	/// <summary>
	/// 주어진 <typeparamref name="T"/> 열거형 타입에 대해
	/// 자체 커스텀 EnumSelectorPropertyDrawer 구현으로 이 프로퍼티 드로어를 구현하세요.
	/// 커스텀 인스펙터 호환성을 위해 UI Toolkit(CreatePropertyGUI)과 IMGUI(OnGUI)를 모두 지원합니다.
	/// 
	/// Ex:
	/// [CustomPropertyDrawer(typeof(EnumSelectorExample))]
	/// public class EnumSelectorExamplePropertyDrawer : EnumSelectorPropertyDrawer{EnumExample}
	/// {
	/// }
	/// </summary>
	public abstract class EnumSelectorPropertyDrawer<T> : PropertyDrawer
		where T : Enum
	{
		private static readonly Dictionary<Type, GUIContent[]> _sortedEnums = new Dictionary<Type, GUIContent[]>();

		private bool _errorFound;

		/// <summary>
		/// Unity 6+의 기본 인스펙터를 위한 UI Toolkit 구현입니다.
		/// </summary>
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var container = new VisualElement();
			var enumType = typeof(T);
			var enumNames = Enum.GetNames(enumType).OrderBy(n => n).ToList();
			var selectionProperty = property.FindPropertyRelative("_selection");
			var currentString = selectionProperty.stringValue;
			var currentIndex = enumNames.IndexOf(currentString);

			if (currentIndex == -1 && !string.IsNullOrWhiteSpace(currentString))
			{
				enumNames.Insert(0, $"Invalid: {currentString}");
				currentIndex = 0;
			}
			else if (currentIndex == -1)
			{
				currentIndex = 0;
			}

			var dropdown = new DropdownField(property.displayName, enumNames, currentIndex);

			dropdown.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue.StartsWith("Invalid: "))
				{
					return;
				}

				selectionProperty.stringValue = evt.newValue;
				selectionProperty.serializedObject.ApplyModifiedProperties();
			});

			container.Add(dropdown);

			return container;
		}

		/// <summary>
		/// OnInspectorGUI를 사용하는 커스텀 인스펙터의 IMGUI 구현입니다.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var enumType = typeof(T);
			var enumValues = GetSortedEnumConstants(enumType);
			var selectionProperty = property.FindPropertyRelative("_selection");
			var currentString = selectionProperty.stringValue;
			var currentIndex = string.IsNullOrWhiteSpace(currentString) ? 0 : Array.FindIndex(enumValues, s => s.text == currentString);

			if (currentIndex != -1)
			{
				selectionProperty.stringValue = enumValues[EditorGUI.Popup(position, label, currentIndex, enumValues)].text;

				_errorFound = false;
			}
			else
			{
				// 문자열이 유효한 열거형 상수가 아닙니다. 이름이 변경되었거나 제거되었기 때문입니다
				if (!_errorFound)
				{
					var targetObject = selectionProperty.serializedObject.targetObject;

					Debug.LogError($"Invalid enum constant: {enumType.Name}.{currentString} in object {targetObject.name} of type: {targetObject.GetType().Name}");

					_errorFound = true;
				}

				var color = GUI.contentColor;
				var finalArray = new[] { new GUIContent("Invalid: " + currentString) }.Concat(enumValues).ToArray();

				GUI.contentColor = Color.red;
				var newSelection = EditorGUI.Popup(position, label, 0, finalArray);
				GUI.contentColor = color;

				if (newSelection > 0)
				{
					selectionProperty.stringValue = finalArray[newSelection].text;
				}
			}

			EditorGUI.EndProperty();
		}

		private GUIContent[] GetSortedEnumConstants(Type enumType)
		{
			if (!_sortedEnums.TryGetValue(enumType, out var content))
			{
				var values = Enum.GetNames(enumType);

				content = new GUIContent[values.Length];

				Array.Sort(values);

				for (var i = 0; i < values.Length; i++)
				{
					content[i] = new GUIContent(values[i]);
				}

				_sortedEnums.Add(enumType, content);
			}
			return content;
		}
	}
}
