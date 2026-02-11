using System;
using UnityEngine;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 이 객체는 한 쌍의 데이터를 포함합니다.
	/// Use <see cref="StructPair{TKey, TValue}"/> 컨테이너 데이터가 값 타입인 경우 사용하세요.
	/// 
	/// 메모리 사용 성능을 향상시키기 위해 컨테이너 데이터가 참조 타입인 경우 
	/// 이 데이터 구조를 사용하세요.
	/// </summary>
	[Serializable]
	public class Pair<TKey, TValue>
	{
		public TKey Key;
		public TValue Value;

		public Pair(TKey key, TValue value)
		{
			Key = key;
			Value = value;
		}

		public override string ToString()
		{
			return $"[{Key.ToString()},{Value.ToString()}]";
		}
	}
	[Serializable]
	public struct StructPair<TKey, TValue>
		where TKey : struct
		where TValue : struct
	{
		public TKey Key;
		public TValue Value;

		public StructPair(TKey key, TValue value)
		{
			Key = key;
			Value = value;
		}

		public override string ToString()
		{
			return $"[{Key.ToString()},{Value.ToString()}]";
		}
	}

	/// <summary>
	/// 4개 컴포넌트 정수 벡터를 나타냅니다.
	/// Unity는 네이티브 Vector4Int 타입을 제공하지 않습니다. 이 구조체는
	/// 인스펙터 직렬화와 JSON/네트워크 직렬화 모두를 위해 그 공백을 채웁니다.
	/// </summary>
	[Serializable]
	public struct Vector4Int
	{
		public int x;
		public int y;
		public int z;
		public int w;

		public Vector4Int(int x, int y, int z, int w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
	}

	/// <summary>
	/// Unity의 Vector4 및 Quaternion 타입을 위한 직렬화 가능한 래퍼입니다.
	/// 주로 Unity의 네이티브 타입이 직접 지원되지 않는
	/// JSON/네트워크 직렬화에 사용됩니다. Unity 6은 인스펙터 필드에서
	/// Vector4/Quaternion을 네이티브로 직렬화합니다 - 이 구조체는 외부 직렬화 형식용입니다.
	/// </summary>
	[Serializable]
	public struct Vector4Serializable
	{
		public float x;
		public float y;
		public float z;
		public float w;

		public Vector4Serializable(float x, float y, float z, float w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		public static implicit operator Vector4Serializable(Vector4 v)
		{
			return new Vector4Serializable(v.x, v.y, v.z, v.w);
		}

		public static implicit operator Vector4Serializable(Quaternion v)
		{
			return new Vector4Serializable(v.x, v.y, v.z, v.w);
		}

		public static implicit operator Vector4(Vector4Serializable v)
		{
			return new Vector4(v.x, v.y, v.z, v.w);
		}

		public static implicit operator Quaternion(Vector4Serializable v)
		{
			return new Quaternion(v.x, v.y, v.z, v.w);
		}
	}

	/// <summary>
	/// Unity의 Vector3 타입을 위한 직렬화 가능한 래퍼입니다.
	/// 주로 Unity의 네이티브 타입이 직접 지원되지 않는
	/// JSON/네트워크 직렬화에 사용됩니다. Unity 6은 인스펙터 필드에서 
	/// Vector3을 네이티브로 직렬화합니다 - 이 구조체는 외부 직렬화 형식용입니다.
	/// </summary>
	[Serializable]
	public struct Vector3Serializable
	{
		public float x;
		public float y;
		public float z;

		public Vector3Serializable(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static implicit operator Vector3Serializable(Vector3 v)
		{
			return new Vector3Serializable(v.x, v.y, v.z);
		}

		public static implicit operator Vector3(Vector3Serializable v)
		{
			return new Vector3(v.x, v.y, v.z);
		}
	}

	/// <summary>
	/// Unity의 Vector2 타입을 위한 직렬화 가능한 래퍼입니다.
	/// 주로 Unity의 네이티브 타입이 직접 지원되지 않는
	/// JSON/네트워크 직렬화에 사용됩니다. Unity 6은 인스펙터 필드에서 
	/// Vector3을 네이티브로 직렬화합니다 - 이 구조체는 외부 직렬화 형식용입니다.
	/// </summary>
	[Serializable]
	public struct Vector2Serializable
	{
		public float x;
		public float y;

		public Vector2Serializable(float x, float y)
		{
			this.x = x;
			this.y = y;
		}

		public static implicit operator Vector2Serializable(Vector2 v)
		{
			return new Vector2Serializable(v.x, v.y);
		}

		public static implicit operator Vector2(Vector2Serializable v)
		{
			return new Vector3(v.x, v.y);
		}
	}

	/// <summary>
	/// Unity의 Vector3Int 타입을 위한 직렬화 가능한 래퍼입니다.
	/// 주로 Unity의 네이티브 타입이 직접 지원되지 않는
	/// JSON/네트워크 직렬화에 사용됩니다. Unity 6은 인스펙터 필드에서 
	/// Vector3을 네이티브로 직렬화합니다 - 이 구조체는 외부 직렬화 형식용입니다.
	/// </summary>
	[Serializable]
	public struct Vector3IntSerializable
	{
		public int x;
		public int y;
		public int z;

		public Vector3IntSerializable(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static implicit operator Vector3IntSerializable(Vector3Int v)
		{
			return new Vector3IntSerializable(v.x, v.y, v.z);
		}

		public static implicit operator Vector3Int(Vector3IntSerializable v)
		{
			return new Vector3Int(v.x, v.y, v.z);
		}
	}

	/// <summary>
	/// Unity의 Vector2Int 타입을 위한 직렬화 가능한 래퍼입니다.
	/// 주로 Unity의 네이티브 타입이 직접 지원되지 않는
	/// JSON/네트워크 직렬화에 사용됩니다. Unity 6은 인스펙터 필드에서 
	/// Vector3을 네이티브로 직렬화합니다 - 이 구조체는 외부 직렬화 형식용입니다.
	/// </summary>
	[Serializable]
	public struct Vector2IntSerializable
	{
		public int x;
		public int y;

		public Vector2IntSerializable(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public static implicit operator Vector2IntSerializable(Vector2Int v)
		{
			return new Vector2IntSerializable(v.x, v.y);
		}

		public static implicit operator Vector2Int(Vector2IntSerializable v)
		{
			return new Vector2Int(v.x, v.y);
		}
	}
}
