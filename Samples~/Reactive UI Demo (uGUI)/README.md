# Reactive UI 데모 (uGUI)

이 샘플은 Geuneda.DataExtensions의 Observable 데이터 타입과 Unity의 레거시 UI 시스템(uGUI)을 사용하여 **반응형 사용자 인터페이스**를 구축하는 방법을 보여줍니다.

## 설계 철학

전통적인 UI 코드는 수동 업데이트 호출로 인해 복잡해지기 쉽습니다. 반응형 패턴은 다음과 같은 방식으로 이를 해결합니다:
1. **데이터가 UI를 구동**: 뷰가 데이터 변경을 구독하고 자동으로 업데이트됩니다.
2. **분리된 아키텍처**: 데이터 모델이 뷰에 대해 아무것도 알지 못합니다.
3. **일괄 업데이트**: 여러 변경 사항을 그룹화하여 UI 갱신 횟수를 줄일 수 있습니다.

## 샘플 내용

이 샘플은 반응형 바인딩이 적용된 플레이어 스탯 패널을 보여줍니다:

### 체력 바
슬라이더와 레이블을 자동으로 업데이트하는 `ObservableField<int>`에 바인딩됩니다.

### 스탯 패널
`BaseDamage`, `WeaponBonus`, 그리고 두 값 중 하나가 변경되면 자동으로 업데이트되는 계산된 `TotalDamage`를 표시합니다.

### 인벤토리 목록
추가/삭제 작업에 반응하는 `ObservableList<string>`에 바인딩됩니다.

### 일괄 업데이트
여러 변경 사항을 단일 UI 갱신으로 그룹화하는 `ObservableBatch`를 보여줍니다.

## 사용 방법

1. **샘플을 임포트**하고 `ReactiveUGuiDemo.unity` 씬을 엽니다.
2. **플레이 모드에 진입**하면 반응형 UI가 작동하는 것을 확인할 수 있습니다.
3. **버튼과 상호작용**합니다:
   - **Damage/Heal**: 체력을 수정하고 체력 바가 업데이트되는 것을 확인합니다.
   - **+Base Damage / +Weapon Bonus**: 계산된 TotalDamage가 자동으로 업데이트되는 것을 확인합니다.
   - **Add/Remove Item**: 인벤토리 목록이 컬렉션 변경에 반응하는 것을 확인합니다.
   - **Batch Update**: 단일 UI 갱신으로 여러 변경 사항을 적용합니다.

## 구현 세부사항

### 데이터 모델
`PlayerData.cs`에서 Observable 프로퍼티를 정의합니다:
- `ObservableField<int>`: 단일 값 (Health, BaseDamage, WeaponBonus).
- `ObservableList<string>`: 컬렉션 (Inventory).
- `ComputedField<int>`: 파생 값 (TotalDamage = BaseDamage + WeaponBonus).

### 뷰 컴포넌트
Observable에 바인딩하는 MonoBehaviour 컴포넌트입니다. `Scripts/` 폴더를 참조하세요:
- `ReactiveHealthBar.cs`: Slider와 Label을 체력에 바인딩합니다.
- `ReactiveUGuiStatsPanel.cs`: Label을 공격력 스탯에 바인딩합니다.
- `ReactiveInventoryList.cs`: 세로 레이아웃을 인벤토리 목록에 바인딩합니다.
