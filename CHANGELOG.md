# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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
- **BREAKING**: 패키지가 `com.gamelovers.dataextensions`에서 `com.gamelovers.gamedata`로 이름 변경 (geuneda 포크에서는 `com.geuneda.dataextensions` 유지)
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
