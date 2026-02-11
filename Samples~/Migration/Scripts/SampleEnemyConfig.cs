using System;

namespace Geuneda.DataExtensions.Samples.Migration
{
	/// <summary>
	/// <see cref="GeunedaEditor.GameData.MigrationRunner"/>를 사용한 복잡한 스키마 마이그레이션을 시연하는 설정입니다.
	/// 
	/// 스키마 진화:
	/// - v1: Id, Name, Health, Damage (원본 스키마)
	/// - v2: Damage를 AttackDamage로 이름 변경, 조건부 기본값으로 ArmorType 추가
	/// - v3: Health를 BaseHealth + BonusHealth로 분할, 파생 값을 가진 중첩 Stats 객체 추가,
	///       Abilities 배열 추가
	/// </summary>
	[Serializable]
	public struct SampleEnemyConfig
	{
		// ═══════════════════════════════════════════════════════════════════
		// v1부터 존재하는 필드
		// ═══════════════════════════════════════════════════════════════════
		
		public int Id;
		public string Name;

		// ═══════════════════════════════════════════════════════════════════
		// v2에서 이름 변경된 필드 (Damage -> AttackDamage)
		// ═══════════════════════════════════════════════════════════════════
		
		/// <summary>
		/// 기본 공격 피해입니다. v2 마이그레이션에서 "Damage"에서 이름이 변경되었습니다.
		/// </summary>
		public int AttackDamage;

		// ═══════════════════════════════════════════════════════════════════
		// v2에서 추가된 필드
		// ═══════════════════════════════════════════════════════════════════
		
		/// <summary>
		/// 방어구 타입 분류입니다. Health 기반 조건부 기본값으로 v2에서 추가되었습니다:
		/// - Health >= 100: "Heavy"
		/// - Health >= 50: "Medium"  
		/// - 그 외: "Light"
		/// </summary>
		public string ArmorType;

		// ═══════════════════════════════════════════════════════════════════
		// v3에서 추가/수정된 필드
		// ═══════════════════════════════════════════════════════════════════
		
		/// <summary>
		/// 기본 체력 풀(마이그레이션 중 원본 Health 값의 80%)입니다.
		/// v3에서 "Health"에서 분할되었습니다.
		/// </summary>
		public int BaseHealth;

		/// <summary>
		/// 장비/버프에서 오는 추가 체력(마이그레이션 중 원본 Health 값의 20%)입니다.
		/// v3에서 "Health"에서 분할되었습니다.
		/// </summary>
		public int BonusHealth;

		/// <summary>
		/// v3 마이그레이션 중 다른 필드에서 파생된 계산 스탯입니다.
		/// </summary>
		public EnemyStats Stats;

		/// <summary>
		/// 능력 식별자 목록입니다. v3에서 빈 배열로 초기화됩니다.
		/// </summary>
		public string[] Abilities;
	}
}
