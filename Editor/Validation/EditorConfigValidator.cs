using System;
using System.Collections.Generic;
using System.Reflection;
using Geuneda.DataExtensions;

namespace GeunedaEditor.GameData
{
	/// <summary>
	/// 설정 데이터를 위한 에디터 전용 유효성 검사 유틸리티를 제공합니다.
	/// <see cref="ValidationAttribute"/>로 장식된 필드와 프로퍼티를 리플렉션을 사용하여 유효성 검사합니다.
	/// </summary>
	public static class EditorConfigValidator
	{
		/// <summary>
		/// 주어진 프로바이더의 모든 설정을 유효성 검사합니다.
		/// 등록된 모든 설정 타입을 순회하며 각 설정 인스턴스를 유효성 검사합니다.
		/// </summary>
		public static ValidationResult ValidateAll(IConfigsProvider provider)
		{
			var result = new ValidationResult();
			var allConfigs = provider.GetAllConfigs();
			var validateMethod = typeof(EditorConfigValidator)
				.GetMethod(nameof(Validate), BindingFlags.Public | BindingFlags.Static)
				?? throw new InvalidOperationException($"Method {nameof(Validate)} not found");

			foreach (var pair in allConfigs)
			{
				var genericMethod = validateMethod.MakeGenericMethod(pair.Key);
				var typeResult = (ValidationResult)genericMethod.Invoke(null, new object[] { provider });
				result.Errors.AddRange(typeResult.Errors);
				result.ValidConfigs.AddRange(typeResult.ValidConfigs);
			}

			return result;
		}

		/// <summary>
		/// 주어진 프로바이더에서 특정 타입의 모든 설정을 유효성 검사합니다.
		/// </summary>
		public static ValidationResult Validate<T>(IConfigsProvider provider)
		{
			var result = new ValidationResult();
			var configs = provider.GetConfigsDictionary<T>();
			var typeName = typeof(T).Name;

			foreach (var pair in configs)
			{
				var errorCountBefore = result.Errors.Count;
				ValidateObject(pair.Value, typeof(T), pair.Key, result);

				if (result.Errors.Count == errorCountBefore)
				{
					result.ValidConfigs.Add(new ValidConfig
					{
						ConfigType = typeName,
						ConfigId = pair.Key
					});
				}
			}
			return result;
		}

		private static void ValidateObject(object obj, Type type, int? id, ValidationResult result)
		{
			if (obj == null) return;

			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var field in fields)
			{
				ValidateMember(type, id, field.Name, field.GetCustomAttributes<ValidationAttribute>(), 
					field.GetValue(obj), result);
			}

			var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var prop in props)
			{
				ValidateMember(type, id, prop.Name, prop.GetCustomAttributes<ValidationAttribute>(), 
					prop.GetValue(obj), result);
			}
		}

		private static void ValidateMember(
			Type type,
			int? id,
			string memberName,
			IEnumerable<ValidationAttribute> attributes,
			object value,
			ValidationResult result)
		{
			foreach (var attr in attributes)
			{
				if (!attr.IsValid(value, out var message))
				{
					result.Errors.Add(new ValidationError
					{
						ConfigType = type.Name,
						ConfigId = id,
						FieldName = memberName,
						Message = message
					});
				}
			}
		}
	}
}
