using System;
using System.Collections.Generic;
using UnityEngine;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// Unity는 기본적으로 Dictionary 직렬화를 지원하지 않지만,
	/// Unity의 직렬화 프로토콜을 활용하면 가능합니다.
	/// Dictionary와 Unity의 ISerializationCallbackReceiver 인터페이스를 모두 상속하는 새 클래스를 만들면,
	/// Dictionary 데이터를 Unity가 직렬화할 수 있는 형식으로 변환할 수 있습니다.
	///
	/// 중요: Unity는 제네릭 타입을 직렬화하지 않으므로, UnitySerializedDictionary를 상속하여
	/// 구체적인 Dictionary 타입을 만들어야 합니다.
	/// </summary>
	[Serializable]
	public abstract class UnitySerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>,
																	ISerializationCallbackReceiver
	{
		[SerializeField]
		private List<TKey> _keyData = new();

		[SerializeField]
		private List<TValue> _valueData = new();

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			Clear();
			for (var i = 0; i < _keyData.Count && i < _valueData.Count; i++)
			{
				this[_keyData[i]] = _valueData[i];
			}
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			_keyData.Clear();
			_valueData.Clear();

			foreach (var item in this)
			{
				_keyData.Add(item.Key);
				_valueData.Add(item.Value);
			}
		}
	}
}
