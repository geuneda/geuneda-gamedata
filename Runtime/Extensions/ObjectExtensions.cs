using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#nullable enable
// ReSharper disable once CheckNamespace

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// Unity <see cref="Object"/>와 C# <see cref="object"/> 타입 모두를 위한 유틸리티 메서드입니다.
	/// </summary>
	/// <author>Bruno Coimbra, https://github.com/coimbrastudios/framework/blob/master/Coimbra/Utilities/</author>
	public static class ObjectExtensions
	{
		private static readonly Dictionary<Type, string> Cache = new();
		private const string PersistentSceneName = "DontDestroyOnLoad";

		/// <summary>
		/// Defines the possible results when calling <see cref="ObjectUtility.Dispose"/>, <see cref="GameObjectUtility.Dispose"/> or <see cref="Actor.Dispose"/>.
		/// </summary>
		public enum ObjectDisposeResult
		{
			None = 0,
			Pooled = 1,
			Destroyed = 2,
		}

		/// <summary>
		/// <see cref="Actor"/>인지 먼저 확인하여 <see cref="GameObject"/>를 올바르게 파괴합니다.
		/// </summary>
		/// <seealso cref="Actor.Dispose"/>
		public static ObjectDisposeResult Dispose(this GameObject? gameObject, bool forceDestroy)
		{
			if (!gameObject.TryGetValid(out gameObject))
			{
				return ObjectDisposeResult.None;
			}

			if (Application.isPlaying)
			{
				Object.Destroy(gameObject);
			}
			else
			{
				Object.DestroyImmediate(gameObject);
			}

			return ObjectDisposeResult.Destroyed;
		}

		/// <summary>
		/// 주어진 컴포넌트 타입을 가져오거나 추가합니다.
		/// </summary>
		public static T GetOrAddComponent<T>(this GameObject gameObject)
			where T : Component
		{
			return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
		}

		/// <summary>
		/// 주어진 컴포넌트 타입을 가져오거나 추가합니다.
		/// </summary>
		public static Component GetOrAddComponent(this GameObject gameObject, SerializableType<Component> type)
		{
			return gameObject.TryGetComponent(type.Value, out Component component) ? component : gameObject.AddComponent(type.Value);
		}

		/// <summary>
		/// 게임 오브젝트가 주어진 컴포넌트 타입을 가지고 있는지 확인합니다.
		/// </summary>
		public static bool HasComponent<T>(this GameObject gameObject)
		{
#pragma warning disable UNT0014
			return gameObject.TryGetComponent<T>(out _);
#pragma warning restore UNT0014
		}

		/// <summary>
		/// 게임 오브젝트가 주어진 컴포넌트 타입을 가지고 있는지 확인합니다.
		/// </summary>
		public static bool HasComponent(this GameObject gameObject, Type type)
		{
			return gameObject.TryGetComponent(type, out _);
		}

		/// <summary>
		/// 객체가 현재 영구적인지 확인합니다.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPersistent(this GameObject gameObject)
		{
			Scene scene = gameObject.scene;

			return scene is { buildIndex: -1, name: PersistentSceneName };
		}

		/// <summary>
		/// <see cref="Actor"/>인지 먼저 확인하여 <see cref="Object"/>를 올바르게 파괴합니다.
		/// </summary>
		/// <seealso cref="Actor.Dispose"/>
		public static ObjectDisposeResult Dispose(this Object o, bool forceDestroy)
		{
			if (!o.TryGetValid(out o))
			{
				return ObjectDisposeResult.None;
			}

			if (o is GameObject gameObject)
			{
				return gameObject.Dispose(forceDestroy);
			}

			if (o is IDisposable disposable)
			{
				disposable.Dispose();
			}

			if (Application.isPlaying)
			{
				Object.Destroy(o);
			}
			else
			{
				Object.DestroyImmediate(o);
			}

			return ObjectDisposeResult.Destroyed;
		}

		/// <summary>
		/// ?. 및 ?? 연산자와 함께 사용할 유효한 객체를 가져옵니다.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetValid<T>(this T o)
		{
			if (o is Object obj)
			{
#pragma warning disable CS8603 // null 참조 반환 가능성.
				return obj != null ? o : default;
#pragma warning restore CS8603 // null 참조 반환 가능성.
			}

			return o;
		}

		/// <summary>
		/// Unity <see cref="Object"/>이고 이미 파괴된 경우에도 객체가 유효한지 안전하게 확인하는 방법입니다.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsValid(this object o)
		{
			if (o is Object obj)
			{
				return obj != null;
			}

			return o != null;
		}

		/// <summary>
		/// Safe way to check if an object is valid even if the object is an Unity <see cref="Object"/> and got destroyed already, getting a valid object to be used with ?. and ?? operators too.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryGetValid<T>(this T o, [NotNullWhen(true)] out T valid)
		{
			valid = GetValid(o);

			return valid != null;
		}

		/// <summary>
		/// 지정된 <see cref="Type"/>에 대한 <see cref="string"/>을 가져옵니다.
		/// </summary>
		public static string GetDisplayString(this Type type)
		{
			if (Cache.TryGetValue(type, out string value))
			{
				return value;
			}

			static void appendGenericParameters(StringBuilder stringBuilder, Type type)
			{
				Type[] types = type.GenericTypeArguments;

				if (!type.IsGenericType || types.Length == 0)
				{
					return;
				}

				stringBuilder.Append("<");
				stringBuilder.Append(types[0].Name);
				appendGenericParameters(stringBuilder, types[0]);

				for (int i = 1; i < type.GenericTypeArguments.Length; i++)
				{
					stringBuilder.Append(", ");
					stringBuilder.Append(types[i].Name);
					appendGenericParameters(stringBuilder, types[i]);
				}

				stringBuilder.Append(">");
			}

			var stringBuilder = new StringBuilder();

			stringBuilder.Append(type.Name);
			appendGenericParameters(stringBuilder, type);

			if (!string.IsNullOrWhiteSpace(type.Namespace))
			{
				stringBuilder.Append(" (").Append(type.Namespace).Append(")");
			}

			value = stringBuilder.ToString();

			Cache.Add(type, value);

			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Type GetTypeFromString(in string fullTypeName)
		{
			int index = fullTypeName.IndexOf(' ');
			string assemblyName = fullTypeName.Substring(0, index);
			string typeName = fullTypeName.Substring(index + 1);

			return Assembly.Load(assemblyName).GetType(typeName);
		}
	}
}
