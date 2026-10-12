# Reactive UI 데모 (uGUI)

`Geuneda.DataExtensions` 의 Observable 타입을 uGUI 에 바인딩해 데이터가 바뀌면 UI 가 스스로 갱신되는 구조를 보여줍니다. 데이터 모델은 뷰를 모르고, 뷰가 데이터 변경을 구독합니다.

## 샘플 내용

- 체력 바: `ObservableField<int>` 에 Slider 와 레이블을 바인딩
- 스탯 패널: `BaseDamage`, `WeaponBonus` 와 둘의 합인 `ComputedField<int>` `TotalDamage` 표시
- 인벤토리 목록: `ObservableList<string>` 의 추가/삭제에 반응
- 일괄 업데이트: `ObservableBatch` 로 여러 변경을 묶어 배치가 끝날 때 한 번에 통지

## 사용 방법

1. 샘플을 임포트하고 `ReactiveUGuiDemo.unity` 씬을 엽니다.
2. 플레이 모드에 들어가 버튼을 눌러 봅니다.
   - **Damage / Heal**: 체력 변경 → 체력 바 갱신
   - **+Base Damage / +Weapon Bonus**: `TotalDamage` 자동 재계산
   - **Add / Remove Item**: 인벤토리 목록 갱신
   - **Batch Update**: 여러 값을 한 배치로 변경

## 구현 세부사항

`Scripts/` 폴더:

- `PlayerData.cs` (`ReactiveUGuiPlayerData`): `ObservableField<int>` (Health, BaseDamage, WeaponBonus), `ObservableList<string>` (Inventory), `ComputedField<int>` (TotalDamage = BaseDamage + WeaponBonus). `Dispose` 에서 `TotalDamage.Dispose()` 를 호출합니다.
- `ReactiveUGuiDemoController.cs`: 데이터 생성, 뷰 바인딩, 버튼 연결, `ObservableBatch` 사용 예
- `ReactiveHealthBar.cs`: Slider 와 TMP 레이블을 체력에 바인딩
- `ReactiveUGuiStatsPanel.cs`: 레이블을 공격력 스탯에 바인딩
- `ReactiveInventoryList.cs`: `_itemsRoot` 아래에 항목 프리팹을 인벤토리 목록에 맞춰 생성/삭제
