# Designer Workflow 샘플

이 샘플은 게임 디자이너가 Unity 에디터에서 **ScriptableObject**를 사용하여 설정 데이터를 작성하고, 이를 런타임 `ConfigsProvider` 시스템에 로드하는 방법을 보여줍니다.

## 설계 철학

게임 디자이너는 코드를 건드리지 않고도 게임 데이터를 편집할 수 있는 시각적이고 인스펙터 친화적인 도구가 필요합니다. 이 샘플은 다음과 같은 방식으로 이를 해결합니다:
1. **ScriptableObject 에셋**: 프로젝트에 저장되는 디자이너 편집 가능한 에셋.
2. **ConfigsProvider 통합**: 타입 기반 설정 시스템으로의 런타임 로딩.
3. **커스텀 Property Drawer**: 드롭다운이 포함된 향상된 인스펙터 UX.

## 샘플 내용

이 샘플에는 세 가지 유형의 설정 패턴이 포함되어 있습니다:

### 싱글톤 설정 (GameSettings)
난이도 및 마스터 볼륨과 같은 단일 값을 가진 전역 설정.

### ID 키 컬렉션 (EnemyConfigs)
체력, 공격력 등의 적 스탯을 포함하며 ID로 인덱싱되는 다중 설정.

### 딕셔너리 설정 (LootTable)
아이템 타입별 드롭 확률을 위해 `UnitySerializedDictionary`를 사용하는 키-값 쌍. 안정적인 이름 기반 enum 직렬화를 위해 `EnumSelector`를 사용합니다.

## 사용 방법

1. **샘플을 임포트**하고 `DesignerWorkflow.unity` 씬을 엽니다.
2. **플레이 모드에 진입**하면 설정이 로드되어 표시되는 것을 확인할 수 있습니다.
3. 인스펙터를 통해 `Assets/Resources/`의 **에셋을 편집**합니다:
   - `SampleEnemyConfigs`에서 적 스탯 수정
   - `SampleGameSettings`에서 난이도/볼륨 조정
   - `SampleLootTable`에서 드롭 확률 편집
4. 씬에서 **Reload를 클릭**하면 런타임에 변경 사항이 반영되는 것을 확인할 수 있습니다.

## 구현 세부사항

### 설정 에셋
설정 데이터를 저장하는 ScriptableObject 래퍼입니다. `Scripts/` 폴더를 참조하세요:
- `GameSettingsAsset.cs`: `AddSingletonConfig<T>()`를 사용하는 싱글톤 설정.
- `EnemyConfigsAsset.cs`: `AddConfigs<T>()`를 사용하는 ID 키 설정.
- `LootTableAsset.cs`: `UnitySerializedDictionary`를 사용하는 딕셔너리 설정.

### EnumSelector
`ItemTypeSelector`는 `EnumSelector<ItemType>`를 확장하여 enum 값을 숫자가 아닌 **이름**으로 저장하므로, enum 값이 재정렬될 때 데이터 손상을 방지합니다.

### 커스텀 Property Drawer
`Editor/` 폴더의 `ItemTypeSelectorPropertyDrawer.cs`는 `EnumSelector` 하위 클래스를 위한 드롭다운 UI 생성 방법을 보여줍니다.
