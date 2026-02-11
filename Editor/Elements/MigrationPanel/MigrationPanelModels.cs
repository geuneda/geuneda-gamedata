using System;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// 마이그레이션 패널에 표시되는 단일 설정 마이그레이션 단계를 나타내는 불변 데이터 행입니다.
	/// 각 행은 <see cref="MigrationType"/>에 정의된 마이그레이션 로직을 사용하여
	/// <see cref="ConfigType"/>을 <see cref="FromVersion"/>에서 <see cref="ToVersion"/>으로 매핑합니다.
	/// </summary>
	internal readonly struct MigrationRow
	{
		public readonly Type ConfigType;
		public readonly ulong FromVersion;
		public readonly ulong ToVersion;
		public readonly Type MigrationType;
		public readonly MigrationState State;

		public MigrationRow(Type configType, ulong fromVersion, ulong toVersion, Type migrationType, MigrationState state)
		{
			ConfigType = configType;
			FromVersion = fromVersion;
			ToVersion = toVersion;
			MigrationType = migrationType;
			State = state;
		}
	}

	/// <summary>
	/// 현재 프로바이더 버전에 상대적인 마이그레이션의 생명주기 상태를 설명합니다.
	/// </summary>
	internal enum MigrationState
	{
		Applied, // 마이그레이션이 이미 적용됨 (현재 버전이 ToVersion 이상)
		Current, // 다음으로 적용할 마이그레이션 (현재 버전이 FromVersion과 동일)
		Pending // 마이그레이션이 대기 중 (현재 버전이 FromVersion 미만)
	}
}
