using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GeunedaEditor.GameData
{
	/// <summary>
	/// 사용 가능한 마이그레이션에 대한 정보입니다.
	/// </summary>
	public readonly struct MigrationInfo
	{
		public readonly Type ConfigType;
		public readonly ulong FromVersion;
		public readonly ulong ToVersion;
		public readonly Type MigrationType;

		public MigrationInfo(Type configType, ulong fromVersion, ulong toVersion, Type migrationType)
		{
			ConfigType = configType;
			FromVersion = fromVersion;
			ToVersion = toVersion;
			MigrationType = migrationType;
		}

		public override string ToString() => $"{ConfigType.Name}: v{FromVersion} → v{ToVersion}";
	}

	/// <summary>
	/// 마이그레이션 작업의 결과입니다.
	/// </summary>
	public readonly struct MigrationResult
	{
		public readonly bool Success;
		public readonly string Message;
		public readonly int MigrationsApplied;

		public MigrationResult(bool success, string message, int migrationsApplied = 0)
		{
			Success = success;
			Message = message;
			MigrationsApplied = migrationsApplied;
		}

		public static MigrationResult Ok(int count) => new MigrationResult(true, $"Applied {count} migration(s)", count);
		public static MigrationResult NoMigrations() => new MigrationResult(true, "No migrations needed", 0);
		public static MigrationResult Error(string message) => new MigrationResult(false, message, 0);
	}

	/// <summary>
	/// 에디터에서 설정 마이그레이션을 검색하고 실행합니다.
	/// 마이그레이션은 버전 간 설정 스키마가 변경될 때 사용되며,
	/// 기존 설정 데이터를 새 스키마에 맞게 변환할 수 있게 합니다.
	/// 
	/// <example>
	/// 마이그레이션 생성:
	/// <code>
	/// [ConfigMigration(typeof(EnemyConfig), fromVersion: 1, toVersion: 2)]
	/// public class EnemyConfigMigration_v1_v2 : IConfigMigration
	/// {
	///     public ulong FromVersion => 1;
	///     public ulong ToVersion => 2;
	///     
	///     public void Migrate(JObject configJson)
	///     {
	///         // 기본값으로 새 필드 추가
	///         configJson["Armor"] = 10;
	///     }
	/// }
	/// </code>
	/// 
	/// 마이그레이션 실행:
	/// <code>
	/// // 사용 가능한 마이그레이션 조회
	/// var migrations = MigrationRunner.GetAvailableMigrations&lt;EnemyConfig&gt;();
	/// 
	/// // JSON 데이터 마이그레이션
	/// var json = JObject.Parse(jsonString);
	/// MigrationRunner.Migrate(typeof(EnemyConfig), json, currentVersion: 1, targetVersion: 2);
	/// 
	/// // 또는 ScriptableObject 마이그레이션
	/// var result = MigrationRunner.MigrateScriptableObject(myConfigSO, fromVersion: 1, toVersion: 2);
	/// </code>
	/// </example>
	/// </summary>
	public static class MigrationRunner
	{
		private static readonly Dictionary<Type, List<(IConfigMigration Migration, Type MigrationType)>> _migrations = 
			new Dictionary<Type, List<(IConfigMigration, Type)>>();
		
		private static bool _initialized;

		/// <summary>
		/// 마이그레이션이 검색되었는지 확인합니다. 첫 사용 시 자동으로 호출됩니다.
		/// 강제 재검색을 위해 명시적으로 호출할 수 있습니다(예: 도메인 리로드 후).
		/// </summary>
		public static void Initialize(bool force = false)
		{
			if (_initialized && !force) return;
			
			_migrations.Clear();

			var types = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(s =>
				{
					try { return s.GetTypes(); }
					catch (ReflectionTypeLoadException) { return Array.Empty<Type>(); }
				})
				.Where(p => typeof(IConfigMigration).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

			foreach (var type in types)
			{
				var attrs = type.GetCustomAttributes<ConfigMigrationAttribute>();
				foreach (var attr in attrs)
				{
					if (!_migrations.TryGetValue(attr.ConfigType, out var list))
					{
						list = new List<(IConfigMigration, Type)>();
						_migrations.Add(attr.ConfigType, list);
					}
					
					var instance = (IConfigMigration)Activator.CreateInstance(type);
					list.Add((instance, type));
				}
			}

			_initialized = true;
		}

		/// <summary>
		/// 등록된 마이그레이션이 있는 모든 설정 타입을 가져옵니다.
		/// </summary>
		public static IReadOnlyCollection<Type> GetConfigTypesWithMigrations()
		{
			Initialize();
			return _migrations.Keys;
		}

		/// <summary>
		/// 설정 타입에 대해 사용 가능한 모든 마이그레이션 정보를 가져옵니다.
		/// </summary>
		public static IReadOnlyList<MigrationInfo> GetAvailableMigrations<T>()
		{
			return GetAvailableMigrations(typeof(T));
		}

		/// <summary>
		/// 설정 타입에 대해 사용 가능한 모든 마이그레이션 정보를 가져옵니다.
		/// </summary>
		public static IReadOnlyList<MigrationInfo> GetAvailableMigrations(Type configType)
		{
			Initialize();
			
			if (!_migrations.TryGetValue(configType, out var list))
			{
				return Array.Empty<MigrationInfo>();
			}

			return list
				.Select(m => new MigrationInfo(configType, m.Migration.FromVersion, m.Migration.ToVersion, m.MigrationType))
				.OrderBy(m => m.FromVersion)
				.ToList();
		}

		/// <summary>
		/// 설정 타입에 대해 사용 가능한 최신 대상 버전을 가져옵니다.
		/// </summary>
		public static ulong GetLatestVersion(Type configType)
		{
			Initialize();
			
			if (!_migrations.TryGetValue(configType, out var list) || list.Count == 0)
			{
				return 0;
			}

			return list.Max(m => m.Migration.ToVersion);
		}

		/// <summary>
		/// 설정 JSON 객체를 한 버전에서 다른 버전으로 마이그레이션합니다.
		/// 적용 가능한 모든 마이그레이션을 순서대로 적용합니다.
		/// </summary>
		public static int Migrate(Type configType, JObject configJson, ulong currentVersion, ulong targetVersion)
		{
			Initialize();
			
			if (!_migrations.TryGetValue(configType, out var migrations))
			{
				return 0;
			}

			var applicableMigrations = migrations
				.Where(m => m.Migration.FromVersion >= currentVersion && m.Migration.ToVersion <= targetVersion)
				.OrderBy(m => m.Migration.FromVersion)
				.ToList();

			foreach (var (migration, _) in applicableMigrations)
			{
				migration.Migrate(configJson);
			}

			return applicableMigrations.Count;
		}

		/// <summary>
		/// ScriptableObject의 직렬화된 데이터를 마이그레이션합니다.
		/// JSON으로 변환하고, 마이그레이션을 적용하고, 객체를 업데이트합니다.
		/// </summary>
		public static MigrationResult MigrateScriptableObject<T>(
			T scriptableObject, 
			Type configType, 
			ulong fromVersion, 
			ulong toVersion = 0) where T : ScriptableObject
		{
			Initialize();

			if (toVersion == 0)
			{
				toVersion = GetLatestVersion(configType);
			}

			if (fromVersion >= toVersion)
			{
				return MigrationResult.NoMigrations();
			}

			try
			{
				// JSON으로 직렬화
				var json = JsonConvert.SerializeObject(scriptableObject);
				var jObject = JObject.Parse(json);

				// 마이그레이션 적용
				var count = Migrate(configType, jObject, fromVersion, toVersion);

				if (count == 0)
				{
					return MigrationResult.NoMigrations();
				}

				// 다시 역직렬화
				JsonConvert.PopulateObject(jObject.ToString(), scriptableObject);

				return MigrationResult.Ok(count);
			}
			catch (Exception ex)
			{
				return MigrationResult.Error($"Migration failed: {ex.Message}");
			}
		}
	}
}
