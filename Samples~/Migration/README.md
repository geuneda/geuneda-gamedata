# 스키마 마이그레이션 샘플

이 샘플은 **Config Browser**와 `MigrationRunner` API를 사용하여 설정 스키마를 마이그레이션하는 실제 개발 워크플로우를 보여줍니다.

## 설계 철학

새로운 기능이 추가됨에 따라 설정 스키마는 시간이 지나면서 변화합니다. 이 패키지는 정보 손실이나 수동 파일 편집 없이 레거시 데이터를 현재 클래스 정의에 맞게 변환하는 강력한 방법을 제공합니다.

이 샘플은 실제 도구를 탐색하며 학습할 수 있도록 구성되어 있습니다:
1. **탐색**: 마이그레이션이 등록되고 검색되는 방법을 확인합니다.
2. **미리보기**: 적용 전에 변환 결과를 시각적으로 확인합니다.
3. **적용**: 마이그레이션을 개별적으로 또는 체인으로 실행합니다.

## 마이그레이션 체이닝 동작 원리

`MigrationRunner`는 버전 번호에 기반하여 **마이그레이션을 자동으로 체이닝**합니다. "미리보기"를 클릭하면 프로바이더의 현재 버전에서 대상 버전까지 모든 마이그레이션이 순차적으로 적용됩니다.

예를 들어, 프로바이더가 버전 1인 경우:
- **v1→v2** 클릭: v1→v2만 적용
- **v2→v3** 클릭: v1→v2 **와** v2→v3 **모두** 적용 (체이닝)

즉, 명시적인 "v1→v3" 마이그레이션을 작성할 필요가 없습니다 - 러너가 자동으로 체인을 처리합니다.

## 스키마 변화 (SampleEnemyConfig)

이 샘플은 전투 유닛 설정이 세 가지 버전에 걸쳐 변화하는 과정을 따릅니다:

### 버전 1 (원본)
- `Id`: int
- `Name`: string
- `Health`: int
- `Damage`: int

### 버전 2 (리팩토링)
- **이름 변경**: `Damage` → `AttackDamage` (더 명확한 이름)
- **새 필드**: `ArmorType` (string)
- **파생 로직**: `Health` ≥ 100이면 `ArmorType`이 자동으로 "Heavy"로 설정되고, 그렇지 않으면 "Medium" 또는 "Light"로 설정됩니다.

### 버전 3 (복잡도 증가)
- **분할**: `Health`가 `BaseHealth` (80%)와 `BonusHealth` (20%)로 분할됩니다.
- **새 객체**: `Stats` (중첩 객체) 포함:
  - `DamageReduction`: `ArmorType`에서 파생.
  - `CritChance`: `AttackDamage`에서 파생.
  - `MoveSpeedMultiplier`: `ArmorType`에서 파생.
- **새 배열**: `Abilities` (빈 배열로 초기화).

## 사용 방법

1. **샘플을 임포트**하고 `Migration.unity` 씬을 엽니다.
2. **플레이 모드에 진입**합니다. `ConfigsProvider`가 초기화되고 내부 버전이 1로 설정됩니다.
3. 씬의 버튼 또는 `Tools > Game Data > Config Browser`를 통해 **Config Browser를 엽니다**.
4. 브라우저에서 활성 프로바이더를 선택합니다.
5. **Migrations** 탭으로 이동합니다.
6. 두 개의 대기 중인 마이그레이션을 확인할 수 있습니다:
   - `SampleEnemyConfigMigration_v1_v2` (상태: **Current**)
   - `SampleEnemyConfigMigration_v2_v3` (상태: **Pending**)

### 미리보기 워크플로우 (커스텀 JSON으로 테스트)

7. 씬의 출력 패널에서 (런타임에 표시되는) **v1 샘플 JSON을 복사**합니다.
8. Config Browser의 Migrations 탭에 있는 **"Custom Input JSON" 필드에 붙여넣기**합니다.
9. 드롭다운에서 **대상 버전을 선택**하고 **Preview**를 클릭합니다:
   - **Input** 패널에 붙여넣은 v1 JSON이 표시됩니다
   - **Output** 패널에 선택한 대상 버전까지 마이그레이션이 적용된 결과가 표시됩니다

### 적용 워크플로우 (프로바이더 데이터 업데이트)

10. 드롭다운에서 **대상 버전을 선택**하고 **Apply Migration**을 클릭합니다:
    - 실제 프로바이더 데이터에 마이그레이션이 적용됩니다
    - 프로바이더 버전이 업데이트됩니다 (예: 1 → 2)
    - **State** 열이 새 버전을 반영하도록 업데이트됩니다
11. v1→v2 적용 후 확인 사항:
    - v1→v2가 **Applied** 상태로 변경됨
    - v2→v3가 **Current** 상태로 변경됨 (다음 적용 준비 완료)

### 샘플 v1 JSON

다음 JSON을 직접 사용할 수도 있습니다:

```json
{
  "Id": 1,
  "Name": "Orc Warlord",
  "Health": 150,
  "Damage": 25
}
```

이것은 v1 스키마 설정을 나타냅니다. Custom Input JSON 필드에 붙여넣어 마이그레이션이 데이터를 어떻게 변환하는지 확인하세요.

## 구현 세부사항

### 마이그레이션 클래스
마이그레이션은 `IConfigMigration`을 상속하고 `[ConfigMigration]` 어트리뷰트가 표시된 클래스로 구현됩니다. `Editor/` 폴더의 예제를 참조하세요:
- `SampleEnemyConfigMigration_v1_v2.cs`: 이름 변경 및 조건부 기본값 설정을 보여줍니다.
- `SampleEnemyConfigMigration_v2_v3.cs`: 필드 분할, 중첩 객체, 배열을 보여줍니다.

### API 참조
- `MigrationRunner.GetAvailableMigrations<T>()`: 특정 타입에 대해 등록된 마이그레이션을 검색합니다.
- `MigrationRunner.Migrate()`: `JObject`에 변환을 적용합니다.
- `MigrationRunner.MigrateScriptableObject()`: `ScriptableObject` 에셋을 위한 고수준 헬퍼입니다.
