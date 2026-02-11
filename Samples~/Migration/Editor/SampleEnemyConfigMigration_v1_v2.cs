using GeunedaEditor.GameData;
using Newtonsoft.Json.Linq;

namespace Geuneda.DataExtensions.Samples.Migration
{
	/// <summary>
	/// <see cref="SampleEnemyConfig"/>의 버전 1에서 버전 2로의 마이그레이션입니다.
	/// 
	/// 시연 내용:
	/// - 필드 이름 변경 (Damage -> AttackDamage)
	/// - 기존 데이터 기반 조건부 기본값 (Health에서 파생된 ArmorType)
	/// </summary>
	[ConfigMigration(typeof(SampleEnemyConfig))]
	public sealed class SampleEnemyConfigMigration_v1_v2 : IConfigMigration
	{
		public ulong FromVersion => 1;
		public ulong ToVersion => 2;

		public void Migrate(JObject configJson)
		{
			// ─────────────────────────────────────────────────────────────────
			// 패턴 1: 필드 이름 변경 (Damage -> AttackDamage)
			// ─────────────────────────────────────────────────────────────────
			if (configJson["Damage"] != null)
			{
				configJson["AttackDamage"] = configJson["Damage"];
				configJson.Remove("Damage");
			}

			// ─────────────────────────────────────────────────────────────────
			// 패턴 2: 조건부 기본값 (Health 기반 ArmorType)
			// ─────────────────────────────────────────────────────────────────
			if (configJson["ArmorType"] == null)
			{
				var health = configJson["Health"]?.Value<int>() ?? 0;

				// 체력 값에서 방어구 타입을 파생합니다
				string armorType;
				if (health >= 100)
				{
					armorType = "Heavy";
				}
				else if (health >= 50)
				{
					armorType = "Medium";
				}
				else
				{
					armorType = "Light";
				}

				configJson["ArmorType"] = armorType;
			}
		}
	}
}
