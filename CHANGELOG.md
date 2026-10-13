# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

**Fixes**:
- `ObservableList.RemoveAt`/`Remove`와 `ObservableDictionary.Remove` 통지 중 구독자가 자기 구독을 해제하면, 마지막 구독자에서는 `ArgumentOutOfRangeException`이 나고 중간 구독자에서는 다음 구독자가 두 번 호출되던 문제 수정
- 미리 `Observe(key)` 한 키가 사전에 없을 때 `ObservableDictionary.Clear`(와 이를 쓰는 `ObservableResolverDictionary.Clear`/`ClearOrigin`)가 `KeyNotFoundException`을 던지던 문제 수정. 사전에 있는 키의 구독자에게만 `Removed`를 보냅니다
- `ObservableDictionary.Clear`와 `ObservableHashSet.Clear`가 비우기 전에 의존 통지를 보내 `ComputedField`가 비우기 전 값으로 남던 문제 수정. `ObservableList.Clear`처럼 비운 뒤에 의존 통지를 보냅니다(`Removed` 콜백은 지금처럼 비우기 전에 호출)
- 구독자 하나가 예외를 던지면 나머지 구독자와 의존 갱신이 실행되지 않아 `ComputedField`가 낡은 값에 머물던 문제 수정. 모든 구독자와 의존 통지를 마친 뒤 예외를 다시 던집니다. 예외가 하나면 원래 예외(스택 정보 유지), 여럿이면 `AggregateException`입니다. 이미 적용된 데이터 변경은 되돌리지 않습니다
- `ObservableBatch.Dispose`에서 한 Observable의 구독자 예외로 뒤쪽 Observable이 재개되지 않아 배치 상태에 남던 문제 수정. 모두 재개하고 배치를 닫은 뒤 예외를 다시 던집니다. List/Dictionary/HashSet의 재개도 한 항목의 예외로 나머지 항목 통지가 멈추지 않습니다
- `ObservableDictionary.StopObserving(key)`와 `StopObservingAll()`이 진행 중인 통지에는 반영되지 않던 문제 수정
- 배치 안에서 컬렉션을 모두 비우면 배치가 끝날 때 의존 통지를 보내지 않아 `ComputedField`가 낡은 값에 머물던 문제 수정. 재개 시 항목 통지 뒤 의존 통지를 항상 한 번 보냅니다
- 배치 재개 통지 중 콜백이 같은 Observable에 새 배치를 열어도 나머지 항목을 계속 통지하던 문제 수정. 남은 항목 통지와 의존 통지를 멈추고, 새 배치가 끝날 때 전체 항목을 다시 통지합니다. HashSet/Dictionary는 새 배치에서 항목을 추가해도 열거자 예외 없이 멈춥니다. `Clear`의 `Removed` 콜백이 새 배치를 열면 비운 뒤의 의존 통지도 그 배치가 끝날 때 보냅니다
- `null` 콜백이 구독돼 있으면 다른 콜백의 `StopObserving`/`StopObservingAll(subscriber)`가 `NullReferenceException`을 던지던 문제 수정

**Changes**:
- 모든 Observable과 `ComputedField`의 구독 목록을 공통 `ObserverList<TAction>`(internal)으로 바꿨습니다. 구독 한 건마다 등록 객체를 두고, 해제하면 그 등록만 비활성으로 표시합니다
  - 변경 연산 하나가 시작될 때 관련 구독 목록의 스냅샷(ArrayPool)을 고정하고 구독 순서대로 호출합니다. `Remove`/`Clear`의 통지 순서가 역순에서 구독 순서로 바뀌었습니다
  - 통지 중 해제된 등록은 아직 호출 전이면 건너뜁니다. 같은 콜백을 여러 번 구독했으면 해제한 건만 빠집니다
  - 통지 중 새로 구독한 콜백은 그 연산의 통지를 받지 않고 다음 연산부터 받습니다. 이전에는 `Add`/값 설정에서 같은 통지 루프의 뒤쪽에 붙어 바로 호출되기도 했습니다
  - 의존 통지(`ComputedField` 무효화)는 스냅샷 규칙과 달리 알리는 시점의 의존 관계 전체에 보냅니다. 콜백 안에서 처음 계산된 `ComputedField`도 그 연산에서 갱신됩니다
  - `ObservableDictionary`의 배치 재개는 항목마다 키 구독자와 전체 구독자를 번갈아 부르던 순서에서, 모든 키 구독자에게 먼저, 그다음 전체 구독자에게 알리는 순서로 바뀌었습니다(`Clear`와 같은 순서). 키 구독자에게는 알리는 시점의 값을 보내고, 앞선 콜백이 지운 키는 건너뜁니다
  - `Clear`가 구독자 목록을 매번 복사하던 할당(`ToList`)이 없어졌습니다. 구독할 때 등록 객체 하나를 할당합니다
- `ObservableList.Clear`의 통지 순서가 "구독자별로 모든 항목"에서 "항목별로 모든 구독자"로 바뀌었습니다. 각 구독자가 받는 항목과 인덱스는 같습니다
- `ObservableDictionary.StopObserving(Action)`은 각 목록에서 같은 콜백의 마지막 등록 대신 첫 등록을 해제합니다(같은 콜백이 여러 번 구독돼 있으면 다른 구독자와의 상대 호출 순서가 달라질 수 있음)

## [1.0.4] - 2026-10-12

**Fixes**:
- `ObservableResolverList`/`ObservableResolverDictionary`의 인덱서 setter(`list[i] = v`, `dict[k] = v`)가 원본 컬렉션에 반영되지 않던 문제 수정. `ObservableList`/`ObservableDictionary` 인덱서를 `virtual`로 바꾸고 리졸버에서 재정의해 원본도 함께 갱신합니다. 없는 키에 쓰면 원본은 바꾸지 않고 기존처럼 `KeyNotFoundException`을 던집니다

## [1.0.3] - 2026-10-12

**Changes**:
- `ObservableResolverList.AddOrigin`/`UpdateOrigin`이 `ObservableResolverDictionary`와 같이 옵저버에게 `Added`/`Updated`를 통지하도록 변경
- `com.unity.textmeshpro` 3.0.6 의존성을 `com.unity.ugui` 2.0.0으로 교체 (Unity 6에서는 TextMeshPro가 uGUI에 포함됨)

**Fixes**:
- `ObservableResolverList.RemoveOrigin`이 재정의된 `RemoveAt`을 거쳐 원본 리스트에서 요소를 한 번 더 제거하던 문제 수정 (원본에 요소가 하나뿐이면 `ArgumentOutOfRangeException` 발생). 이제 원본에서 값의 위치를 찾아 원본과 내부 리스트의 같은 위치를 지우며, 원본에 값이 없으면 아무것도 바꾸지 않고 `false`를 반환
- `ConfigValidationServiceTests.ValidateAll_ReportsFieldsAndConfigIds`가 같은 타입을 싱글톤과 컬렉션으로 중복 등록해 실패하던 테스트 수정
- `MigrationRunner` XML 예제를 실제 API(`ConfigMigration(Type)`, `MigrateScriptableObject`의 `configType` 인자)에 맞게 수정
- 존재하지 않는 대상을 가리키는 cref와 잘못된 주석 정정
- `Samples~`에 누락된 `.meta` 파일 추가

---

## [1.0.2] - 2026-02-26

**Changes**:
- `package.json`의 `displayName`과 `description`을 영어로 변경
- 영어 문서를 한국어로 번역
- 모든 .cs 파일의 영어 주석을 한국어로 번역

---

## [1.0.1] - 2026-02-10

**New**:
- CoderGamester/Unity-GameData 최신 변경사항 동기화 (upstream v1.0.1)
- `ConfigTreeBuilder`, `ConfigValidationService`, `ConfigsEditorUtil` 단위 테스트 추가

**Changes**:
- 모놀리식 `ConfigBrowserWindow`과 `MigrationPanelElement`을 MVC 아키텍처로 분리

---

## [1.0.0] - 2026-02-10

**New**:
- CoderGamester/Unity-GameData v1.0.0 동기화
- `com.gamelovers.configsprovider`를 이 패키지에 병합
- **보안**: 역직렬화 시 허용된 타입만 화이트리스트로 관리하는 `ConfigTypesBinder` 추가
- 타입 안전 설정 저장소 및 버저닝을 위한 `ConfigsProvider` 추가
- 백엔드 동기화 지원 JSON 직렬화를 위한 `ConfigsSerializer` 추가
- ScriptableObject 기반 설정 컨테이너 `ConfigsScriptableObject` 추가
- `IConfigsProvider`, `IConfigsAdder`, `IConfigsContainer` 인터페이스 추가
- Newtonsoft.Json 의존성 추가
- Unity 6 UI Toolkit 지원 (`ReadOnlyPropertyDrawer`, `EnumSelectorPropertyDrawer`)
- `Runtime/link.xml` 추가 (코드 스트리핑 방지)
- `Color`, `Vector2`, `Vector3`, `Vector4`, `Quaternion`용 Newtonsoft JSON 컨버터 추가
- 의존성 추적 기반 자동 파생 Observable `ComputedField<T>` 추가 (Select, CombineWith 확장 메서드 포함)
- `ObservableHashSet<T>` 컬렉션 타입 추가
- Observable 업데이트 배치 처리 `ObservableBatch` 추가
- `[Required]`, `[Range]`, `[MinLength]` 어트리뷰트 유효성 검사 프레임워크 추가
- 에디터 전용 설정 유효성 검사 도구 추가 (`EditorConfigValidator`, `ValidationResult`)
- `Secure` 직렬화 모드 추가
- 설정 스키마 마이그레이션 프레임워크 추가 (에디터 전용)
- 통합 Config Browser 에디터 도구 추가
- ScriptableObject 인스펙터 도구 추가
- Observable Debugger 에디터 도구 추가
- Observable 리졸버 타입 추가 (`ObservableResolverField`, `ObservableResolverList`, `ObservableResolverDictionary`)

**Changes**:
- **BREAKING**: 패키지가 `com.gamelovers.dataextensions`에서 `com.gamelovers.gamedata`로 이름 변경 (geuneda 포크의 패키지명은 `com.geuneda.gamedata`이며 런타임 네임스페이스는 `Geuneda.DataExtensions`를 유지)
- `SerializableType<T>`의 IL2CPP/AOT 안전성 개선
- `EnumSelector`의 정적 딕셔너리 캐싱 및 O(1) 조회 최적화
- `IConfigBackendService`가 UniTask 사용으로 변경
- `com.cysharp.unitask` (2.5.10) 패키지 의존성 추가

**Fixes**:
- `EnumSelector.SetSelection`에서 명시적/비연속 값을 가진 enum 처리 수정

---

## [0.7.0] - 2025-11-03

**New**:
- 모든 Observable 클래스에 *Rebind* 기능 추가
- 모든 Observable Resolver 클래스에 *Rebind* 메서드 추가
- *IObservableResolverField* 인터페이스에 *Rebind* 메서드 추가

## [0.6.7] - 2025-04-07

**New**:
- Unity의 *GameObject* 타입에 추가 로직을 위한 *UnityObjectExtensions* 추가

## [0.6.6] - 2024-11-30

**Fix**:
- *ObservableDictionary.Remove(T)*에서 요소를 찾지 못했을 때 불필요한 업데이트를 보내지 않도록 수정

## [0.6.5] - 2024-11-20

**Fix**:
- *ObservableDictionary*에서 요소 추가/삭제 중 구독/해제 시 발생하는 문제 수정
- *ObservableList*에서 요소 추가/삭제 중 구독/해제 시 발생하는 문제 수정

## [0.6.4] - 2024-11-13

**Fix**:
- *ObservableDictionary* 단위 테스트에서 빌드를 방해하는 문제 수정

## [0.6.3] - 2024-11-02

**Fix**:
- *ObservableDictionary* 컴파일 문제 수정

## [0.6.2] - 2024-11-02

**New**:
- *ObservableDictionary* 구독자 업데이트 성능 향상을 위한 *ObservableUpdateFlag* 추가

**Fix**:
- *ObservableDictionary*에서 *Clear* 호출 시 Remove 업데이트 액션이 설정되지 않는 문제 수정

## [0.6.1] - 2024-11-01

**Fix**:
- *ObservableResolverDictionary*에서 *Remove()* 및 *RemoveOrigin* 호출 시 크래시 수정

## [0.6.0] - 2023-08-05

**Changed**:
- *ObservableResolverList*와 *ObservableResolverDictionary*의 다른 데이터 타입 리졸브 개선

## [0.5.1] - 2023-09-04

**New**:
- 객체와 구조체 타입 컨테이너 모두 지원하는 StructPair 데이터 타입 추가

**Fix**:
- GameObject와 Object의 dispose 확장 메서드에서 null 참조 체크 추가

## [0.5.0] - 2023-08-05

**New**:
- 결정론적 부동소수점 타입 **floatP** 추가

## [0.4.0] - 2023-07-30

**New**:
- Unity Object 및 GameObject 타입의 유틸리티 메서드 및 확장 추가
- 인스펙터에서 타입 확인/수정/저장을 위한 SerializableType 구조체 추가

## [0.3.0] - 2023-07-28

**New**:
- ObservableField에 이전/현재 값으로 업데이트 관찰 지원 추가
- Unity에서 딕셔너리 직렬화를 위한 UnitySerializedDictionary 클래스 추가

## [0.2.0] - 2020-09-28

**New**:
- *ObservableResolverList*, *ObservableResolverDictionary*, *ObservableResolverField* 추가
- 모든 데이터 타입에 단위 테스트 추가

**Changed**:
- *ObservableIdList* 제거 (동일 결과를 *ObservableList* 또는 *ObservableDictionary*로 구현 가능)
- 모든 Pair Data를 새로운 직렬화 가능한 *Pair<Key,Value>* 타입으로 이동
- 모든 Vector2, Vector3, Vector4 확장을 ValueData 파일로 이동

## [0.1.1] - 2020-08-31

**Changed**:
- 어셈블리 정의를 패키지에 맞게 이름 변경
- 불필요한 파일 제거

## [0.1.0] - 2020-08-31

- 패키지 배포를 위한 초기 제출
