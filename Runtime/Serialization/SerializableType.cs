using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// <see cref="Type"/> 인스펙터에서 보고, 수정하고, 저장할 수 있는 타입입니다.
	/// </summary>
	/// <remarks>
	/// <see cref="FilterTypesAttributeBase"/> 어트리뷰트와 호환됩니다.
	/// </remarks>
	/// <author>Bruno Coimbra, https://github.com/coimbrastudios/framework/blob/master/Coimbra/SerializableType%601.cs</author>
	/// <typeparam name="T">타입이 이것에 할당 가능해야 합니다.</typeparam>
	/// <seealso cref="FilterTypesAttributeBase"/>
	/// <seealso cref="FilterTypesByAccessibilityAttribute"/>
	/// <seealso cref="FilterTypesByMethodAttribute"/>
	/// <seealso cref="FilterTypesByAssignableFromAttribute"/>
	/// <seealso cref="FilterTypesBySpecificTypeAttribute"/>
	[Serializable]
	public struct SerializableType<T> : IEquatable<SerializableType<T>>, IEquatable<Type>, ISerializationCallbackReceiver
	{
		[SerializeField]
		private string _className;

		[SerializeField]
		private string _assemblyName;

		private Type _value;

		public SerializableType(Type type)
			: this()
		{
			Value = type;
		}

		/// <summary>
		/// 타입의 어셈블리를 가져옵니다.
		/// </summary>
		public string AssemblyName => _assemblyName;

		/// <summary>
		/// 타입의 이름을 가져옵니다.
		/// </summary>
		public string ClassName => _className;

		/// <summary>
		/// 직렬화된 타입을 가져오거나 설정합니다.
		/// </summary>
		public Type Value
		{
			get => _value ?? typeof(T);
			set
			{
				_value = value;

				if (_value != null && typeof(T).IsAssignableFrom(_value))
				{
					_assemblyName = _value.Assembly.FullName;
					_className = _value.FullName;
				}
				else
				{
					_assemblyName = typeof(T).Assembly.FullName;
					_className = typeof(T).FullName;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Type(SerializableType<T> type)
		{
			return type.Value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator SerializableType<T>(Type type)
		{
			return new SerializableType<T>(type);
		}

		/// <inheritdoc/>
		public bool Equals(SerializableType<T> other)
		{
			return Value == other.Value;
		}

		/// <inheritdoc/>
		public bool Equals(Type other)
		{
			return Value == other;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return Value.GetDisplayString();
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize() { }

		/// <summary>
		/// 역직렬화 후 타입을 재구성합니다.
		/// 코드 스트리핑 문제를 방지하기 위해 로드된 어셈블리를 검색하는 AOT 안전 해석 패턴을 사용합니다.
		/// </summary>
		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			try
			{
				if (string.IsNullOrEmpty(_assemblyName) || string.IsNullOrEmpty(_className))
				{
					Value = null;
					return;
				}

				// AOT 안전: Assembly.Load 대신 로드된 어셈블리를 검색합니다
				Value = Type.GetType($"{_className}, {_assemblyName}");

				if (Value != null)
				{
					return;
				}

				// 대체: 모든 로드된 어셈블리를 검색합니다
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					if (assembly.FullName == _assemblyName)
					{
						Value = assembly.GetType(_className);
						break;
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}
}
