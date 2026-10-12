# Designer Workflow 샘플

디자이너가 인스펙터에서 편집한 ScriptableObject 에셋을 런타임 `ConfigsProvider` 에 로드하는 방법을 보여줍니다.

## 샘플 내용

| 패턴 | 에셋 / 타입 | 사용 API |
|------|-------------|----------|
| 싱글톤 설정 (난이도, 마스터 볼륨) | `GameSettingsAsset` (`ConfigsScriptableObject<int, GameSettingsConfig>`, 키 0 항목 하나) | `AddSingletonConfig` |
| ID 키 컬렉션 (적 스탯) | `EnemyConfigsAsset` (`ConfigsScriptableObject<int, EnemyConfig>`) | `AddConfigs` |
| 딕셔너리 설정 (아이템 타입별 드롭 확률) | `LootTableAsset` 의 `LootTable` (`UnitySerializedDictionary<ItemTypeSelector, float>`) | `UnitySerializedDictionary`, `EnumSelector` |

## 사용 방법

1. 샘플을 임포트하고 `DesignerWorkflow.unity` 씬을 엽니다.
2. 플레이 모드에 들어가면 로드된 설정이 화면에 표시됩니다.
3. 샘플 폴더의 `Assets/Resources/` 에 있는 에셋을 인스펙터에서 편집합니다.
   - `SampleEnemyConfigs`: 적 스탯
   - `SampleGameSettings`: 난이도, 볼륨
   - `SampleLootTable`: 드롭 확률
4. 씬의 Reload 버튼을 누르면 변경 사항이 다시 로드됩니다.

## 구현 세부사항

`Scripts/` 폴더:

- `ConfigLoader.cs`: `Resources.Load` 로 세 에셋을 읽어 `ConfigsProvider` 를 만듭니다. 게임 설정은 `ConfigsDictionary` 의 키 0 값을 `AddSingletonConfig` 로, 적 설정은 `Configs` 목록의 값을 `AddConfigs(e => e.Id, ...)` 로 등록합니다.
- `GameSettingsAsset.cs`, `EnemyConfigsAsset.cs`: `ConfigsScriptableObject<TId, TAsset>` 를 상속한 에셋입니다.
- `LootTableAsset.cs`, `LootTable.cs`: `UnitySerializedDictionary` 를 상속한 구체 타입을 필드로 가진 ScriptableObject 입니다.
- `ItemTypeSelector.cs`: `EnumSelector<ItemType>` 의 `[Serializable]` 하위 클래스입니다. enum 값을 숫자가 아닌 이름으로 저장하므로 enum 항목 순서가 바뀌어도 데이터가 유지됩니다.

`Editor/` 폴더:

- `ItemTypeSelectorPropertyDrawer.cs`: `[CustomPropertyDrawer(typeof(ItemTypeSelector))]` 와 `EnumSelectorPropertyDrawer<ItemType>` 상속으로 드롭다운 UI 를 제공합니다. `EnumSelector` 의 저장 필드는 `HideInInspector` 이므로, 하위 클래스마다 이런 드로어가 있어야 인스펙터에 표시됩니다.
