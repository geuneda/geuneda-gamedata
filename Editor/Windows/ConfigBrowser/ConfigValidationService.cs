using System;
using System.Collections.Generic;
using System.Reflection;
using Geuneda.DataExtensions;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// <see cref="ValidationAttribute"/> 하위 클래스로 장식된 필드와 프로퍼티를 검사하여
	/// 설정 데이터를 유효성 검사하는 상태 비저장 서비스입니다. 전체 프로바이더 및
	/// 단일 항목 유효성 검사 범위를 모두 지원합니다.
	/// </summary>
	internal static class ConfigValidationService
	{
		private const int SingleConfigId = 0;

		/// <summary>
		/// 주어진 <paramref name="provider"/>의 모든 설정 항목을 유효성 검사하고
		/// 수집된 <see cref="ValidationErrorInfo"/> 오류 목록을 반환합니다.
		/// </summary>
		public static List<ValidationErrorInfo> ValidateAll(IConfigsProvider provider)
		{
			if (provider == null) return new List<ValidationErrorInfo>();

			var errors = new List<ValidationErrorInfo>();
			foreach (var kv in provider.GetAllConfigs())
			{
				if (!ConfigsEditorUtil.TryReadConfigs(kv.Value, out var entries))
				{
					continue;
				}

				for (int i = 0; i < entries.Count; i++)
				{
					ValidateObject(kv.Key, entries[i].Id, entries[i].Value, errors);
				}
			}

			return errors;
		}

		/// <summary>
		/// <paramref name="selection"/>으로 설명된 단일 설정 항목을 유효성 검사하고
		/// 수집된 <see cref="ValidationErrorInfo"/> 오류 목록을 반환합니다.
		/// </summary>
		public static List<ValidationErrorInfo> ValidateSingle(ConfigSelection selection)
		{
			var errors = new List<ValidationErrorInfo>();
			if (!selection.IsValid) return errors;
			ValidateObject(selection.ConfigType, selection.ConfigId, selection.Value, errors);
			return errors;
		}

		private static void ValidateObject(Type configType, int configId, object instance, List<ValidationErrorInfo> errors)
		{
			if (instance == null) return;

			foreach (var field in configType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				var attrs = field.GetCustomAttributes(typeof(ValidationAttribute), inherit: true);
				AddValidationErrors(configType, configId, field.Name, attrs, field.GetValue(instance), errors);
			}

			foreach (var prop in configType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (!prop.CanRead) continue;
				var attrs = prop.GetCustomAttributes(typeof(ValidationAttribute), inherit: true);
				AddValidationErrors(configType, configId, prop.Name, attrs, prop.GetValue(instance), errors);
			}
		}

		private static void AddValidationErrors(Type configType, int configId, string memberName, object[] attrs, object value, List<ValidationErrorInfo> errors)
		{
			for (int i = 0; i < attrs.Length; i++)
			{
				if (attrs[i] is ValidationAttribute validationAttribute)
				{
					if (!validationAttribute.IsValid(value, out var message))
					{
						errors.Add(new ValidationErrorInfo(configType.Name, configId == SingleConfigId ? null : configId, memberName, message));
					}
				}
			}
		}
	}
}
