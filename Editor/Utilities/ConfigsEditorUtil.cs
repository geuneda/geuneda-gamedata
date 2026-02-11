using System;
using System.Collections;
using System.Collections.Generic;
using Geuneda.DataExtensions;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// 설정 ID와 박싱된 값을 보유하는 경량 쌍입니다.
	/// 리플렉션을 통해 프로바이더 컨테이너를 순회하는 에디터 유틸리티에서 사용됩니다.
	/// </summary>
	internal readonly struct ConfigEntry
	{
		public readonly int Id;
		public readonly object Value;

		public ConfigEntry(int id, object value)
		{
			Id = id;
			Value = value;
		}
	}

	/// <summary>
	/// 컴파일 타임에 설정 타입을 알지 못한 채
	/// <see cref="IConfigsProvider"/> 컨테이너에서 설정 데이터를 읽고 요약하기 위한 에디터 전용 유틸리티 메서드입니다.
	/// </summary>
	internal static class ConfigsEditorUtil
	{
		/// <summary>
		/// 리플렉션을 사용하여 <c>Dictionary&lt;int, T&gt;</c> <paramref name="container"/>에서
		/// 모든 항목을 읽으려고 시도합니다. 컨테이너가 유효한 int 키 딕셔너리인 경우 true를 반환하며,
		/// <paramref name="entries"/>를 ID 순으로 정렬하여 채웁니다.
		/// </summary>
		public static bool TryReadConfigs(IEnumerable container, out List<ConfigEntry> entries)
		{
			entries = new List<ConfigEntry>();
			if (container == null) return false;

			// ConfigsProvider는 싱글톤과 컬렉션 모두에 Dictionary<int, T>를 저장합니다.
			// T에 관계없이 리플렉션을 사용하여 항목을 순회합니다.
			var type = container.GetType();
			if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Dictionary<,>))
			{
				return false;
			}

			var keyType = type.GetGenericArguments()[0];
			if (keyType != typeof(int))
			{
				return false;
			}

			foreach (var item in container)
			{
				var itemType = item.GetType();
				var keyProp = itemType.GetProperty("Key");
				var valueProp = itemType.GetProperty("Value");
				if (keyProp == null || valueProp == null) continue;

				var id = (int)keyProp.GetValue(item);
				var value = valueProp.GetValue(item);
				entries.Add(new ConfigEntry(id, value));
			}

			entries.Sort((a, b) => a.Id.CompareTo(b.Id));
			return true;
		}

		/// <summary>
		/// 주어진 <paramref name="provider"/>의 요약 수를 계산합니다: 등록된
		/// 설정 타입 수와 모든 타입에 걸친 개별 설정 항목의 총 수입니다.
		/// </summary>
		public static (int typeCount, int totalCount) ComputeConfigCounts(IConfigsProvider provider)
		{
			var allConfigs = provider.GetAllConfigs();
			var typeCount = allConfigs.Count;
			var totalCount = 0;

			foreach (var kv in allConfigs)
			{
				if (TryReadConfigs(kv.Value, out var entries))
				{
					totalCount += entries.Count;
				}
			}

			return (typeCount, totalCount);
		}
	}
}
