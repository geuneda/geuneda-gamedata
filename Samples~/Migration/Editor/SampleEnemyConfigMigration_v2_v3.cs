using GeunedaEditor.GameData;
using Newtonsoft.Json.Linq;

namespace Geuneda.DataExtensions.Samples.Migration
{
	/// <summary>
	/// <see cref="SampleEnemyConfig"/>의 버전 2에서 버전 3으로의 마이그레이션입니다.
	/// 
	/// 시연 내용:
	/// - 필드 분할 (Health -> BaseHealth + BonusHealth)
	/// - 중첩 객체 생성 (파생된 DamageReduction을 가진 Stats)
	/// - 배열 초기화 (Abilities)
	/// - 데이터 파생 (기존 필드에서 값 계산)
	/// </summary>
	[ConfigMigration(typeof(SampleEnemyConfig))]
	public sealed class SampleEnemyConfigMigration_v2_v3 : IConfigMigration
	{
		public ulong FromVersion => 2;
		public ulong ToVersion => 3;

		public void Migrate(JObject configJson)
		{
			// ─────────────────────────────────────────────────────────────────
			// 패턴 1: 필드 분할 (Health -> BaseHealth + BonusHealth)
			// 원본 체력 분할: 80% 기본, 20% 보너스
			// ─────────────────────────────────────────────────────────────────
			if (configJson["Health"] != null)
			{
				var totalHealth = configJson["Health"].Value<int>();
				
				// 분할 계산 (80% 기본, 20% 보너스, 반올림 손실 없음 보장)
				var baseHealth = (int)(totalHealth * 0.8f);
				var bonusHealth = totalHealth - baseHealth;

				configJson["BaseHealth"] = baseHealth;
				configJson["BonusHealth"] = bonusHealth;
				
				// 이전 필드를 제거합니다
				configJson.Remove("Health");
			}
			else
			{
				// Health가 누락된 경우의 대체 처리
				if (configJson["BaseHealth"] == null) configJson["BaseHealth"] = 0;
				if (configJson["BonusHealth"] == null) configJson["BonusHealth"] = 0;
			}

			// ─────────────────────────────────────────────────────────────────
			// 패턴 2: 파생 값을 가진 중첩 객체 생성
			// ArmorType에서 파생된 DamageReduction을 가진 Stats 객체 생성
			// ─────────────────────────────────────────────────────────────────
			if (configJson["Stats"] == null)
			{
				var armorType = configJson["ArmorType"]?.Value<string>() ?? "Light";
				var attackDamage = configJson["AttackDamage"]?.Value<int>() ?? 0;

				// ArmorType에서 DamageReduction을 파생합니다
				int damageReduction;
				switch (armorType)
				{
					case "Heavy":
						damageReduction = 40;
						break;
					case "Medium":
						damageReduction = 20;
						break;
					case "Light":
					default:
						damageReduction = 5;
						break;
				}

				// AttackDamage에서 CritChance를 파생합니다 (높은 피해 = 낮은 치명타, 25% 상한)
				var critChance = attackDamage > 0 
					? System.Math.Min(25, 500 / attackDamage) 
					: 10;

				// ArmorType에서 MoveSpeedMultiplier를 파생합니다
				float moveSpeed;
				switch (armorType)
				{
					case "Heavy":
						moveSpeed = 0.7f;
						break;
					case "Medium":
						moveSpeed = 1.0f;
						break;
					case "Light":
					default:
						moveSpeed = 1.3f;
						break;
				}

				configJson["Stats"] = new JObject
				{
					["DamageReduction"] = damageReduction,
					["CritChance"] = critChance,
					["MoveSpeedMultiplier"] = moveSpeed
				};
			}

			// ─────────────────────────────────────────────────────────────────
			// 패턴 3: 배열 초기화
			// 향후 사용을 위해 빈 Abilities 배열을 초기화합니다
			// ─────────────────────────────────────────────────────────────────
			if (configJson["Abilities"] == null)
			{
				configJson["Abilities"] = new JArray();
			}
		}
	}
}
