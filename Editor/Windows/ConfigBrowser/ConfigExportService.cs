using System.Collections.Generic;
using Geuneda.DataExtensions;
using Newtonsoft.Json;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// <see cref="IConfigsProvider"/>에서 설정 데이터를 JSON으로 직렬화하는 상태 비저장 서비스입니다.
	/// Config Browser에서 전체 프로바이더 및 항목별 내보내기 작업에 사용됩니다.
	/// </summary>
	internal static class ConfigExportService
	{
		/// <summary>
		/// 주어진 <paramref name="provider"/>의 모든 설정을 단일 들여쓰기 JSON 문자열로 직렬화합니다.
		/// 각 설정 타입은 전체 이름으로 키가 지정되며, ID-값 쌍의 딕셔너리를 포함합니다.
		/// </summary>
		public static string ExportProviderToJson(IConfigsProvider provider)
		{
			var result = new Dictionary<string, object>();
			foreach (var kv in provider.GetAllConfigs())
			{
				if (ConfigsEditorUtil.TryReadConfigs(kv.Value, out var entries))
				{
					var dict = new Dictionary<int, object>();
					for (int i = 0; i < entries.Count; i++)
					{
						dict[entries[i].Id] = entries[i].Value;
					}
					result[kv.Key.FullName ?? kv.Key.Name] = dict;
				}
			}

			return JsonConvert.SerializeObject(result, Formatting.Indented);
		}

		/// <summary>
		/// 단일 객체를 들여쓰기 JSON 문자열로 직렬화합니다.
		/// null이면 빈 문자열을, 실패 시 오류 메시지가 포함된 주석을 반환합니다.
		/// </summary>
		public static string ToJson(object obj)
		{
			if (obj == null) return string.Empty;
			try
			{
				return JsonConvert.SerializeObject(obj, Formatting.Indented);
			}
			catch (System.Exception ex)
			{
				return $"// Failed to serialize {obj.GetType().Name}: {ex.Message}";
			}
		}
	}
}
