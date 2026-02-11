using Newtonsoft.Json.Linq;

namespace GeunedaEditor.GameData
{
	/// <summary>
	/// 설정 마이그레이션을 위한 인터페이스입니다.
	/// 버전 간 설정 스키마가 변경될 때 에디터에서 마이그레이션이 실행됩니다.
	/// </summary>
	public interface IConfigMigration
	{
		/// <summary>
		/// 이 마이그레이션의 원본 버전입니다.
		/// </summary>
		ulong FromVersion { get; }
		
		/// <summary>
		/// 이 마이그레이션의 대상 버전입니다.
		/// </summary>
		ulong ToVersion { get; }

		/// <summary>
		/// 주어진 JSON 객체를 마이그레이션합니다.
		/// </summary>
		void Migrate(JObject configJson);
	}
}
