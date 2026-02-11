using System;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// Config Browser 트리에서 현재 선택된 설정 항목을 나타냅니다.
	/// 유효하지 않은 선택(설정이 선택되지 않음)은 null <see cref="ConfigType"/>을 가집니다.
	/// </summary>
	internal readonly struct ConfigSelection
	{
		public readonly Type ConfigType;
		public readonly int ConfigId;
		public readonly object Value;

		/// <summary>설정 항목이 실제로 선택되었을 때 true를 반환합니다.</summary>
		public bool IsValid => ConfigType != null;

		public ConfigSelection(Type configType, int configId, object value)
		{
			ConfigType = configType;
			ConfigId = configId;
			Value = value;
		}

		/// <summary>빈 선택을 생성합니다.</summary>
		public static ConfigSelection None() => new ConfigSelection(null, 0, null);
	}

	/// <summary>
	/// 표시해야 할 유효성 검사 오류의 하위 집합을 설명합니다.
	/// 모든 오류 또는 타입과 ID로 식별되는 단일 설정 항목의 오류입니다.
	/// </summary>
	internal readonly struct ValidationFilter
	{
		public readonly bool IsAll;
		public readonly Type ConfigType;
		public readonly int ConfigId;

		private ValidationFilter(bool isAll, Type configType, int configId)
		{
			IsAll = isAll;
			ConfigType = configType;
			ConfigId = configId;
		}

		/// <summary>모든 유효성 검사 오류를 표시하는 필터를 생성합니다.</summary>
		public static ValidationFilter All() => new ValidationFilter(true, null, 0);

		/// <summary>단일 설정 항목에 대한 필터를 생성합니다.</summary>
		public static ValidationFilter Single(Type type, int id) => new ValidationFilter(false, type, id);
	}

	/// <summary>
	/// <see cref="ConfigValidationService"/>에서 생성된 단일 유효성 검사 오류의 세부 정보를 보유하는
	/// 불변 레코드입니다. 싱글톤 설정은 null <see cref="ConfigId"/>를 사용합니다.
	/// </summary>
	internal readonly struct ValidationErrorInfo
	{
		public readonly string ConfigTypeName;
		public readonly int? ConfigId;
		public readonly string FieldName;
		public readonly string Message;

		public ValidationErrorInfo(string configTypeName, int? configId, string fieldName, string message)
		{
			ConfigTypeName = configTypeName;
			ConfigId = configId;
			FieldName = fieldName;
			Message = message;
		}
	}

	/// <summary>
	/// Config Browser 트리 뷰에서 노드의 종류를 구분합니다.
	/// </summary>
	internal enum ConfigNodeKind
	{
		Header, // 다른 노드를 그룹화하는 헤더 노드입니다.
		Type, // 항목 자식을 포함하는 설정 타입 노드입니다.
		Entry // 선택 가능한 설정 항목 노드입니다.
	}

	/// <summary>
	/// Config Browser <see cref="UnityEngine.UIElements.TreeView"/>의 단일 노드 데이터 페이로드입니다.
	/// 노드는 정적 팩토리 메서드 <see cref="Header"/>, <see cref="Type"/>, <see cref="Entry"/>를 통해 생성됩니다.
	/// </summary>
	internal readonly struct ConfigNode
	{
		public readonly ConfigNodeKind Kind;
		public readonly string DisplayName;
		public readonly Type ConfigType;
		public readonly int ConfigId;
		public readonly object Value;

		private ConfigNode(ConfigNodeKind kind, string displayName, Type configType, int configId, object value)
		{
			Kind = kind;
			DisplayName = displayName;
			ConfigType = configType;
			ConfigId = configId;
			Value = value;
		}

		/// <summary>주어진 표시 <paramref name="name"/>으로 헤더 노드를 생성합니다.</summary>
		public static ConfigNode Header(string name) => new ConfigNode(ConfigNodeKind.Header, name, null, 0, null);

		/// <summary>주어진 설정 <paramref name="type"/>에 대한 타입 노드를 생성합니다.</summary>
		public static ConfigNode Type(Type type, string name) => new ConfigNode(ConfigNodeKind.Type, name, type, 0, null);

		/// <summary>단일 설정 인스턴스를 나타내는 항목 노드를 생성합니다.</summary>
		public static ConfigNode Entry(Type type, int id, object value, string name) => new ConfigNode(ConfigNodeKind.Entry, name, type, id, value);
	}
}
