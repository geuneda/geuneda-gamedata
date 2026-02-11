using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 역직렬화를 허용된 타입의 화이트리스트로 제한하는 커스텀 직렬화 바인더입니다.
	/// Newtonsoft.Json에서 TypeNameHandling.Auto 사용 시 타입 주입 공격을 방지합니다.
	/// 
	/// 바인더는 직렬화/역직렬화되는 설정에서 허용된 타입을 자동 검색하고,
	/// 내부 구조에 필요한 일반 .NET 컬렉션 타입도 허용합니다.
	/// </summary>
	public class ConfigTypesBinder : ISerializationBinder
	{
		private readonly HashSet<Type> _allowedTypes;
		private readonly HashSet<string> _allowedTypeNames;
		
		// 내부 구조에 안전하고 필수적인 내장 컬렉션 타입
		private static readonly HashSet<Type> _builtInAllowedTypes = new HashSet<Type>
		{
			typeof(Dictionary<,>),
			typeof(List<>),
			typeof(HashSet<>),
			typeof(int),
			typeof(string),
			typeof(float),
			typeof(double),
			typeof(bool),
			typeof(long),
			typeof(ulong),
			typeof(short),
			typeof(ushort),
			typeof(byte),
			typeof(sbyte),
			typeof(decimal),
			typeof(char),
			typeof(DateTime),
			typeof(TimeSpan),
			typeof(Guid),
			typeof(object[]),
			typeof(int[]),
			typeof(string[]),
			typeof(float[]),
			typeof(double[]),
		};

		/// <summary>
		/// 지정된 허용 타입으로 새 ConfigTypesBinder를 생성합니다.
		/// </summary>
		/// <param name="allowedConfigTypes">역직렬화 중 허용할 설정 타입입니다.</param>
		public ConfigTypesBinder(IEnumerable<Type> allowedConfigTypes)
		{
			_allowedTypes = new HashSet<Type>(_builtInAllowedTypes);
			_allowedTypeNames = new HashSet<string>();
			
			if (allowedConfigTypes != null)
			{
				foreach (var type in allowedConfigTypes)
				{
					AddAllowedType(type);
				}
			}
		}

		/// <summary>
		/// ConfigsProvider에서 타입을 자동 검색하는 ConfigTypesBinder를 생성합니다.
		/// </summary>
		/// <param name="provider">타입을 검색할 프로바이더입니다.</param>
		/// <returns>검색된 타입을 가진 새 ConfigTypesBinder입니다.</returns>
		public static ConfigTypesBinder FromProvider(IConfigsProvider provider)
		{
			if (provider == null)
			{
				return new ConfigTypesBinder(null);
			}

			var configs = provider.GetAllConfigs();
			var types = new List<Type>();
			
			foreach (var kvp in configs)
			{
				types.Add(kvp.Key);
				
				// 이 설정에 대해 Dictionary<int, T> 구체적 타입도 허용
				var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), kvp.Key);
				types.Add(dictType);
			}
			
			return new ConfigTypesBinder(types);
		}

		/// <summary>
		/// 허용된 타입 목록에 타입을 추가합니다.
		/// </summary>
		public void AddAllowedType(Type type)
		{
			if (type == null) return;
			
			_allowedTypes.Add(type);
			_allowedTypeNames.Add(type.FullName);
			_allowedTypeNames.Add(type.AssemblyQualifiedName);
			
			// 제네릭 타입의 경우, 제네릭 타입 정의도 허용
			if (type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				_allowedTypes.Add(type.GetGenericTypeDefinition());
				
				// 제네릭 인수도 허용
				foreach (var arg in type.GetGenericArguments())
				{
					AddAllowedType(arg);
				}
			}
		}

		/// <inheritdoc />
		public Type BindToType(string assemblyName, string typeName)
		{
			// 전체 타입 이름 구성
			var fullTypeName = string.IsNullOrEmpty(assemblyName) 
				? typeName 
				: $"{typeName}, {assemblyName}";
			
			// 타입 해석 시도
			var type = Type.GetType(fullTypeName);
			
			if (type == null)
			{
				// 일반 타입에 대해 어셈블리 없이 시도
				type = Type.GetType(typeName);
			}
			
			if (type == null)
			{
				throw new Newtonsoft.Json.JsonSerializationException(
					$"Type '{fullTypeName}' could not be resolved. Ensure the type exists and is accessible.");
			}
			
			// 타입이 허용되는지 확인
			if (IsTypeAllowed(type))
			{
				return type;
			}
			
			throw new Newtonsoft.Json.JsonSerializationException(
				$"Type '{type.FullName}' is not allowed for deserialization. " +
				"Only whitelisted config types are permitted for security reasons.");
		}

		/// <inheritdoc />
		public void BindToName(Type serializedType, out string assemblyName, out string typeName)
		{
			// 직렬화를 위해 표준 명명 사용
			assemblyName = serializedType.Assembly.FullName;
			typeName = serializedType.FullName;
		}

		private bool IsTypeAllowed(Type type)
		{
			// 직접 일치
			if (_allowedTypes.Contains(type))
			{
				return true;
			}
			
			// 이름으로 확인 (교차 어셈블리 시나리오 처리)
			if (_allowedTypeNames.Contains(type.FullName) || 
			    _allowedTypeNames.Contains(type.AssemblyQualifiedName))
			{
				return true;
			}
			
			// 제네릭 타입의 경우, 제네릭 정의가 허용되는지 확인
			// 그리고 모든 타입 인수가 허용되는지 확인
			if (type.IsGenericType)
			{
				var genericDef = type.GetGenericTypeDefinition();
				if (_allowedTypes.Contains(genericDef) || _builtInAllowedTypes.Contains(genericDef))
				{
					// 모든 제네릭 인수 확인
					foreach (var arg in type.GetGenericArguments())
					{
						if (!IsTypeAllowed(arg))
						{
							return false;
						}
					}
					return true;
				}
			}
			
			// 내장 허용 타입 확인
			if (_builtInAllowedTypes.Contains(type))
			{
				return true;
			}
			
			// 허용된 타입의 배열 허용
			if (type.IsArray)
			{
				return IsTypeAllowed(type.GetElementType());
			}
			
			return false;
		}
	}
}
