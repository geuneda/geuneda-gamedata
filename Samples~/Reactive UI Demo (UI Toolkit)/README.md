# Reactive UI 데모 (UI Toolkit)

이 샘플은 Geuneda.DataExtensions의 Observable 데이터 타입과 Unity의 **UI Toolkit**을 사용하여 **반응형 사용자 인터페이스**를 구축하는 방법을 보여줍니다.

## 설계 철학

UI Toolkit은 CSS와 유사한 현대적인 UI 개발 방식을 제공합니다. 이 샘플은 반응형 데이터 바인딩을 통합하는 방법을 보여줍니다:
1. **요소 쿼리**: 이름으로 요소를 찾아 데이터를 바인딩합니다.
2. **뷰 헬퍼**: 구독 로직을 재사용 가능한 클래스로 캡슐화합니다.
3. **깔끔한 해제**: 뷰가 파괴될 때 구독을 올바르게 정리합니다.

## 샘플 내용

이 샘플은 반응형 바인딩이 적용된 플레이어 스탯 패널을 보여줍니다 (uGUI 데모와 동일하지만 UI Toolkit 사용):

### 체력 바
ProgressBar와 Label을 자동으로 업데이트하는 `ObservableField<int>`에 바인딩됩니다.

### 스탯 패널
`BaseDamage`, `WeaponBonus`, 그리고 두 값 중 하나가 변경되면 자동으로 업데이트되는 계산된 `TotalDamage`를 표시합니다.

### 인벤토리 목록
ScrollView에서 추가/삭제 작업에 반응하는 `ObservableList<string>`에 바인딩됩니다.

### 일괄 업데이트
여러 변경 사항을 단일 UI 갱신으로 그룹화하는 `ObservableBatch`를 보여줍니다.

## 사용 방법

1. **샘플을 임포트**하고 `ReactiveToolkitDemo.unity` 씬을 엽니다.
2. **플레이 모드에 진입**하면 반응형 UI가 작동하는 것을 확인할 수 있습니다.
3. **버튼과 상호작용**합니다:
   - **Damage/Heal**: 체력을 수정하고 프로그레스 바가 업데이트되는 것을 확인합니다.
   - **+Base Damage / +Weapon Bonus**: 계산된 TotalDamage가 자동으로 업데이트되는 것을 확인합니다.
   - **Add/Remove Item**: 인벤토리 ScrollView가 컬렉션 변경에 반응하는 것을 확인합니다.
   - **Batch Update**: 단일 UI 갱신으로 여러 변경 사항을 적용합니다.

## 구현 세부사항

### 데이터 모델
`PlayerData.cs`에서 Observable 프로퍼티를 정의합니다:
- `ObservableField<int>`: 단일 값 (Health, BaseDamage, WeaponBonus).
- `ObservableList<string>`: 컬렉션 (Inventory).
- `ComputedField<int>`: 파생 값 (TotalDamage = BaseDamage + WeaponBonus).

### 뷰 헬퍼
깔끔한 구독 관리를 위해 `IDisposable`을 구현하는 순수 C# 클래스입니다. `Scripts/` 폴더를 참조하세요:
- `ReactiveToolkitHealthBar.cs`: ProgressBar와 Label을 체력에 바인딩합니다.
- `ReactiveToolkitStatsPanel.cs`: Label을 공격력 스탯에 바인딩합니다.
- `ReactiveToolkitInventoryList.cs`: ScrollView를 인벤토리 목록에 바인딩합니다.

### UI 에셋
`UI/` 폴더에 포함되어 있습니다:
- `ReactiveToolkitDemo.uxml`: UI 레이아웃 정의.
- `ReactiveToolkitDemo.uss`: 시각적 스타일링을 위한 스타일시트.
