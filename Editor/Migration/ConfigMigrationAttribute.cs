using System;

namespace GeunedaEditor.GameData
{
	/// <summary>
	/// 특정 설정 타입의 설정 마이그레이션 핸들러로 클래스를 표시하는 어트리뷰트입니다.
	/// 설정 스키마가 변경될 때 에디터에서 마이그레이션이 검색되고 실행됩니다.
	/// 버전 정보는 <see cref="IConfigMigration"/> 인터페이스 구현에서 얻습니다.
	/// 
	/// <example>
	/// <code>
	/// [ConfigMigration(typeof(EnemyConfig))]
	/// public class EnemyConfigMigration_v1_v2 : IConfigMigration
	/// {
	///     public ulong FromVersion => 1;
	///     public ulong ToVersion => 2;
	///     
	///     public void Migrate(JObject configJson)
	///     {
	///         configJson["Armor"] = 10; // 새 필드 추가
	///     }
	/// }
	/// </code>
	/// </example>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class ConfigMigrationAttribute : Attribute
	{
		/// <summary>
		/// 이 마이그레이션이 적용되는 설정 타입입니다.
		/// </summary>
		public Type ConfigType { get; }

		/// <summary>
		/// 지정된 설정 타입에 대한 새 마이그레이션 어트리뷰트를 생성합니다.
		/// </summary>
		/// <param name="configType">이 마이그레이션이 처리하는 설정 타입입니다.</param>
		public ConfigMigrationAttribute(Type configType)
		{
			ConfigType = configType;
		}
	}
}
