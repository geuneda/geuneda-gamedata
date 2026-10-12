# 스키마 마이그레이션 샘플

Config Browser 와 `MigrationRunner` API 로 설정 스키마를 마이그레이션하는 에디터 전용 워크플로우를 보여줍니다. 마이그레이션은 레거시 JSON 을 현재 클래스 정의에 맞게 변환합니다.

## 마이그레이션 체이닝

`MigrationRunner` 는 버전 번호를 기준으로 마이그레이션을 순서대로 이어서 적용합니다. 현재 버전에서 대상 버전까지 해당하는 마이그레이션이 모두 실행되므로 "v1→v3" 같은 마이그레이션을 따로 작성할 필요가 없습니다.

프로바이더가 버전 1일 때:

- 대상 v2: v1→v2 만 적용
- 대상 v3: v1→v2, v2→v3 순서로 적용

## 스키마 변화 (SampleEnemyConfig)

### 버전 1

- `Id` (int), `Name` (string), `Health` (int), `Damage` (int)

### 버전 2

- 이름 변경: `Damage` → `AttackDamage`
- 새 필드: `ArmorType` (string). `Health` 가 100 이상이면 "Heavy", 50 이상이면 "Medium", 그 밖에는 "Light"

### 버전 3

- 분할: `Health` → `BaseHealth` (80%) + `BonusHealth` (나머지)
- 새 중첩 객체 `Stats`:
  - `DamageReduction`: `ArmorType` 에서 파생
  - `CritChance`: `AttackDamage` 에서 파생
  - `MoveSpeedMultiplier`: `ArmorType` 에서 파생
- 새 배열: `Abilities` (빈 배열로 초기화)

## 사용 방법

1. 샘플을 임포트하고 `Migration.unity` 씬을 엽니다.
2. 플레이 모드에 들어갑니다. `ConfigsProvider` 가 만들어지고 버전이 1로 설정됩니다.
3. 씬의 버튼 또는 `Tools > Game Data > Config Browser` 로 Config Browser 를 엽니다.
4. 툴바에서 프로바이더를 선택하고 **Migrations** 탭으로 이동합니다.
5. 마이그레이션 두 개가 보입니다.
   - `SampleEnemyConfigMigration_v1_v2` (State: **Current**)
   - `SampleEnemyConfigMigration_v2_v3` (State: **Pending**)

### 미리보기 (커스텀 JSON)

1. 씬 출력 패널의 v1 샘플 JSON(아래와 같음)을 복사합니다.
2. Migrations 탭의 **Custom Input JSON** 필드에 붙여넣습니다.
3. **Target Version** 드롭다운에서 대상 버전을 고르고 **Preview** 를 누릅니다. **Input** 패널에 입력 JSON 이, **Output** 패널에 마이그레이션 결과가 표시됩니다.

```json
{
  "Id": 1,
  "Name": "Orc Warlord",
  "Health": 150,
  "Damage": 25
}
```

같은 JSON 이 `Data/sample_enemy_v1.json` 에도 있습니다.

### 적용 (프로바이더 데이터 갱신)

1. 대상 버전을 고르고 **Apply Migration** 을 누르면 실제 프로바이더 데이터에 마이그레이션이 적용되고 프로바이더 버전이 올라갑니다(예: 1 → 2).
2. v1→v2 를 적용한 뒤에는 v1→v2 가 **Applied**, v2→v3 가 **Current** 로 바뀝니다.

## 구현 세부사항

마이그레이션은 `IConfigMigration` 을 구현하고 `[ConfigMigration(typeof(SampleEnemyConfig))]` 를 붙인 클래스입니다. 버전은 어트리뷰트가 아니라 `FromVersion`, `ToVersion` 프로퍼티로 지정합니다. `Editor/` 폴더:

- `SampleEnemyConfigMigration_v1_v2.cs`: 필드 이름 변경, 조건부 기본값
- `SampleEnemyConfigMigration_v2_v3.cs`: 필드 분할, 중첩 객체, 배열

`MigrationRunner` 주요 API (`GeunedaEditor.GameData`, 에디터 전용):

- `GetAvailableMigrations<T>()` / `GetAvailableMigrations(Type)`: 설정 타입에 등록된 마이그레이션 목록(`MigrationInfo`)
- `GetLatestVersion(Type)`: 등록된 마이그레이션의 최대 `ToVersion`
- `Migrate(Type configType, JObject json, ulong currentVersion, ulong targetVersion)`: `JObject` 에 마이그레이션을 적용하고 적용 개수를 반환
- `MigrateScriptableObject(scriptableObject, configType, fromVersion, toVersion = 0)`: ScriptableObject 를 JSON 으로 바꿔 마이그레이션한 뒤 다시 채움. `toVersion` 이 0이면 최신 버전까지 적용하고 `MigrationResult` 를 반환
