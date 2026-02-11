using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Geuneda.DataExtensions;

// ReSharper disable once CheckNamespace

namespace GeunedaEditor.GameData
{
	/// <summary>
	/// ReadOnly 어트리뷰트를 위한 커스텀 드로어를 포함하는 클래스입니다.
	/// 커스텀 인스펙터 호환성을 위해 UI Toolkit(CreatePropertyGUI)과 IMGUI(OnGUI)를 모두 지원합니다.
	/// </summary>
	[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnlyPropertyDrawer : PropertyDrawer
	{
		/// <summary>
		/// Unity 6+의 기본 인스펙터를 위한 UI Toolkit 구현입니다.
		/// </summary>
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var field = new PropertyField(property);

			field.SetEnabled(false);

			return field;
		}

		/// <summary>
		/// OnInspectorGUI를 사용하는 커스텀 인스펙터의 IMGUI 구현입니다.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// 이전 GUI 활성화 값 저장
			var previousGUIState = GUI.enabled;
			// 프로퍼티 편집 비활성화
			GUI.enabled = false;
			// 프로퍼티 그리기
			EditorGUI.PropertyField(position, property, label, true);
			// 이전 GUI 활성화 값 설정
			GUI.enabled = previousGUIState;
		}

		/// <summary>
		/// IMGUI 모드에서 프로퍼티의 적절한 높이를 반환합니다.
		/// </summary>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}
	}
}
