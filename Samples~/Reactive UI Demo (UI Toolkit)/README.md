# Reactive UI 데모 (UI Toolkit)

[uGUI 데모](<../Reactive UI Demo (uGUI)/README.md>)와 같은 플레이어 스탯 패널을 UI Toolkit 으로 구현합니다. 이름으로 요소를 찾아 Observable 에 바인딩하고, 구독 로직은 `IDisposable` 뷰 헬퍼 클래스에 모아 뷰가 사라질 때 정리합니다.

## 샘플 내용

- 체력 바: `ObservableField<int>` 에 ProgressBar 와 Label 을 바인딩
- 스탯 패널: `BaseDamage`, `WeaponBonus` 와 둘의 합인 `ComputedField<int>` `TotalDamage` 표시
- 인벤토리 목록: `ObservableList<string>` 의 추가/삭제에 ScrollView 가 반응
- 일괄 업데이트: `ObservableBatch` 로 여러 변경을 묶어 배치가 끝날 때 한 번에 통지

## 사용 방법

1. 샘플을 임포트하고 `ReactiveToolkitDemo.unity` 씬을 엽니다.
2. 플레이 모드에 들어가 버튼을 눌러 봅니다.
   - **Damage / Heal**: 체력 변경 → ProgressBar 갱신
   - **+Base Damage / +Weapon Bonus**: `TotalDamage` 자동 재계산
   - **Add / Remove Item**: ScrollView 갱신
   - **Batch Update**: 여러 값을 한 배치로 변경

## 구현 세부사항

`Scripts/` 폴더:

- `PlayerData.cs` (`ReactiveToolkitPlayerData`): uGUI 데모와 같은 구성의 데이터 모델 (`ObservableField<int>`, `ObservableList<string>`, `ComputedField<int>`)
- `ReactiveToolkitDemoController.cs`: UXML 요소 조회(`Q<T>(name)`), 뷰 헬퍼 생성, 버튼 연결, `ObservableBatch` 사용 예
- `ReactiveToolkitHealthBar.cs`: ProgressBar 와 Label 을 체력에 바인딩
- `ReactiveToolkitStatsPanel.cs`: Label 을 공격력 스탯에 바인딩
- `ReactiveToolkitInventoryList.cs`: ScrollView 를 인벤토리 목록에 바인딩

`UI/` 폴더:

- `ReactiveToolkitDemo.uxml`: 레이아웃
- `ReactiveToolkitDemo.uss`: 스타일시트
- `ReactiveToolkitDemo Panel Settings.asset`, `UnityDefaultRuntimeTheme.tss`: 런타임 패널 설정과 테마
