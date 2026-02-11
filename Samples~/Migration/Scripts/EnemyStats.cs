using System;

namespace Geuneda.DataExtensions.Samples.Migration
{
	/// <summary>
	/// <see cref="SampleEnemyConfig"/>에서 사용하며 중첩 객체를 도입하는
	/// 복잡한 스키마 마이그레이션을 시연하기 위한 중첩 스탯 구조입니다.
	/// 마이그레이션을 통해 v3에서 추가되었습니다.
	/// </summary>
	[Serializable]
	public struct EnemyStats
	{
		/// <summary>
		/// 피해 감소 백분율(0-100)입니다. 마이그레이션 중 ArmorType에서 파생됩니다.
		/// </summary>
		public int DamageReduction;

		/// <summary>
		/// 치명타 확률 백분율(0-100)입니다.
		/// </summary>
		public int CritChance;

		/// <summary>
		/// 이동 속도 배율(1.0 = 일반 속도)입니다.
		/// </summary>
		public float MoveSpeedMultiplier;
	}
}
