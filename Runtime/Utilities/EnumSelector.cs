using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 현재 객체를 다음으로 태그합니다: <seealso cref="EnumSelector{T}"/>
	/// </summary>
	public interface IEnumSelector
	{
		/// <summary>
		/// 현재 선택된 열거형의 인덱스 번호를 요청합니다
		/// </summary>
		int GetSelectedIndex();

		/// <summary>
		/// 선택된 열거형이 유효한 열거형인지 요청합니다.
		/// 열거형 값이 변경되고 선택된 문자열이 제거되면, 선택된 열거형은 유효하지 않게 됩니다.
		/// </summary>
		bool HasValidSelection();

		/// <summary>
		/// 선택된 열거형 값을 문자열로 요청합니다
		/// </summary>
		string GetSelectionString();
	}

	/// <summary>
	/// The EnumSelector <typeparamref name="T"/> 의 모든 열거형 값을 제공하는 드롭다운 선택 필드 역할을 합니다. <typeparamref name="T"/>. 
	/// 새 값이 추가되거나 제거될 때 잘못된 열거형을 가리키는 것을 방지하기 위해 열거형 값 대신 열거형 이름을 저장합니다
	/// </summary>
	[Serializable]
	public class EnumSelector<T> : IEnumSelector where T : Enum
	{
		[SerializeField, HideInInspector] private string _selection = "";

		public static readonly string[] EnumNames = Enum.GetNames(typeof(T));
		public static readonly T[] EnumValues = (T[])Enum.GetValues(typeof(T));
		public static readonly Dictionary<string, T> EnumDictionary = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

		static EnumSelector()
		{
			// 정적 딕셔너리 캐시(EnumDictionary)를 도입하여 조회를 O(n)에서 O(1)로 최적화했습니다
			for (var i = 0; i < EnumNames.Length; i++)
			{
				EnumDictionary[EnumNames[i]] = EnumValues[i];
			}
		}

		private EnumSelector()
		{
			SetSelection(EnumValues[0]);
		}

		protected EnumSelector(T data)
		{
			SetSelection(data);
		}

		/// <inheritdoc />
		public int GetSelectedIndex()
		{
			if (EnumDictionary.TryGetValue(_selection, out var value))
			{
				return Array.IndexOf(EnumValues, value);
			}

			Debug.LogError($"Could not load enum for string: {_selection}");

			return -1;
		}

		/// <inheritdoc />
		public bool HasValidSelection()
		{
			return GetSelectedIndex() != -1;
		}

		/// <inheritdoc />
		public string GetSelectionString()
		{
			return _selection;
		}

		/// <summary>
		/// 선택된 열거형 값을 요청합니다
		/// </summary>
		public T GetSelection()
		{
			if (EnumDictionary.TryGetValue(_selection, out var enumConstant))
			{
				return enumConstant;
			}

			var index = GetSelectedIndex();

			return index == -1 ? EnumValues[0] : EnumValues[index];
		}

		/// <summary>
		/// 열거형 값을 다음으로 설정합니다: <paramref name="data"/>
		/// </summary>
		public void SetSelection(T data)
		{
			_selection = Enum.GetName(typeof(T), data);
		}

		public static implicit operator T(EnumSelector<T> d)
		{
			return d.GetSelection();
		}
	}
}