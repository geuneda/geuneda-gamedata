# Geuneda Data Extensions

Unity 게임을 위한 핵심 데이터 유틸리티 및 설정 관리 패키지입니다.

## 이 패키지를 사용하는 이유

Unity에서 게임 데이터를 관리하면 흩어진 설정 파일, 데이터와 로직 간의 강한 결합, 크로스 플랫폼 불일치 등의 문제가 발생합니다. 이 패키지는 이러한 문제를 해결합니다:

| 문제 | 해결 방법 |
|------|-----------|
| 흩어진 설정 관리 | O(1) 조회와 버저닝을 지원하는 타입 안전 `ConfigsProvider` |
| 데이터 변경에 대한 강한 결합 | 반응형 프로그래밍을 위한 Observable 타입 |
| 수동 파생 상태 업데이트 | 의존성 추적 기반 자동 업데이트 `ComputedField` |
| 크로스 플랫폼 부동소수점 불일치 | 결정론적 `floatP` 타입 |
| 백엔드 동기화 복잡성 | `ConfigsSerializer`를 통한 JSON 직렬화 |
| Dictionary 인스펙터 편집 | 인스펙터 지원 `UnitySerializedDictionary` |
| 불안정한 enum 직렬화 | enum 이름 기반 저장 `EnumSelector` |

### 주요 기능

- **설정 관리** - 버저닝을 지원하는 타입 안전, 고성능 설정 저장소
- **Observable 데이터 타입** - `ObservableField`, `ObservableList`, `ObservableDictionary`로 반응형 업데이트
- **계산된 값** - `ComputedField`로 의존성 추적 기반 자동 파생 상태 업데이트
- **결정론적 수학** - `floatP` 타입으로 크로스 플랫폼 재현 가능한 부동소수점 연산
- **백엔드 동기화** - 원자적 설정 업데이트를 위한 JSON 직렬화/역직렬화
- **ScriptableObject 컨테이너** - 인스펙터에서 설정 편집을 위한 디자이너 친화적 워크플로우
- **Unity 직렬화** - `UnitySerializedDictionary`와 `EnumSelector`로 인스펙터 지원
- **O(1) 조회** - 사전 빌드된 인메모리 딕셔너리로 최대 런타임 성능

## 시스템 요구사항

- **Unity** 6000.0+ (Unity 6)
- **Newtonsoft.Json** (com.unity.nuget.newtonsoft-json v3.2.1) - 자동 설치
- **UniTask** (com.cysharp.unitask v2.5.10) - 비동기 백엔드 인터페이스에 사용
- **TextMeshPro** (com.unity.textmeshpro v3.0.6) - 샘플 UI 스크립트에 사용

## 설치 방법

### Unity Package Manager (Git URL)

1. Unity Editor에서 **Window > Package Manager** 열기
2. **+** 버튼 클릭 > **Add package from git URL...**
3. 다음 URL 입력:
```
https://github.com/geuneda/geuneda-dataextensions.git
```

또는 `Packages/manifest.json`에 직접 추가:
```json
{
  "dependencies": {
    "com.geuneda.dataextensions": "https://github.com/geuneda/geuneda-dataextensions.git#v1.0.1"
  }
}
```

## 주요 컴포넌트

| 컴포넌트 | 역할 |
|----------|------|
| **ConfigsProvider** | O(1) 조회와 버저닝을 지원하는 타입 안전 설정 저장소 |
| **ConfigsSerializer** | 클라이언트/서버 설정 동기화를 위한 JSON 직렬화 |
| **ConfigTypesBinder** | 안전한 역직렬화를 위한 화이트리스트 기반 타입 바인더 |
| **ObservableField** | 변경 콜백이 있는 단일 값 반응형 래퍼 |
| **ObservableList** | 추가/삭제/업데이트 콜백이 있는 리스트 반응형 래퍼 |
| **ObservableDictionary** | 키 기반 콜백이 있는 딕셔너리 반응형 래퍼 |
| **ComputedField** | 의존성을 추적하는 자동 업데이트 파생 값 |
| **floatP** | 크로스 플랫폼 수학을 위한 결정론적 부동소수점 타입 |
| **MathfloatP** | floatP 타입용 수학 함수 (Sin, Cos, Sqrt 등) |
| **EnumSelector** | enum 값 변경에도 안전한 enum 드롭다운 |
| **UnitySerializedDictionary** | Unity 인스펙터에서 표시되는 딕셔너리 타입 |

## 에디터 도구

### Config Browser
- **메뉴**: `Tools > Game Data > Config Browser`
- 설정 탐색, 유효성 검사, JSON 내보내기, 마이그레이션 미리보기

### Observable Debugger
- **메뉴**: `Tools > Game Data > Observable Debugger`
- 실시간 Observable 인스턴스 검사, 이름/종류/활성 상태 필터링

### ConfigsScriptableObject 인스펙터
- `ConfigsScriptableObject<,>` 파생 에셋의 인스펙터 UI
- 인라인 항목 상태(중복 키/어트리뷰트 기반 유효성 검사)

## 사용 예제

### ConfigsProvider

```csharp
using Geuneda.DataExtensions;

var provider = new ConfigsProvider();

// ID 리졸버로 컬렉션 추가
provider.AddConfigs(item => item.Id, itemConfigs);

// 싱글톤 설정 추가
provider.AddSingletonConfig(new GameSettings { Difficulty = 2 });

// 설정 접근
var item = provider.GetConfig<ItemConfig>(42);              // ID로 접근
var settings = provider.GetConfig<GameSettings>();          // 싱글톤
var allItems = provider.GetConfigsList<ItemConfig>();       // 리스트로 (할당 발생)

// 제로 할당 열거
foreach (var enemy in provider.EnumerateConfigs<EnemyConfig>())
{
    ProcessEnemy(enemy);
}
```

### Observable 데이터 타입

```csharp
using Geuneda.DataExtensions;

// ObservableField
var score = new ObservableField<int>(0);
score.Observe((prev, curr) => Debug.Log($"점수: {prev} -> {curr}"));
score.Value = 100;

// ObservableList
var inventory = new ObservableList<string>(new List<string>());
inventory.Observe((index, prev, curr, updateType) => { /* ... */ });
inventory.Add("검");

// ObservableDictionary
var stats = new ObservableDictionary<string, int>(new Dictionary<string, int>());
stats.Observe("health", (key, prev, curr, type) => { /* ... */ });
stats.Add("health", 100);
```

### ComputedField

```csharp
using Geuneda.DataExtensions;

var baseHealth = new ObservableField<int>(100);
var bonusHealth = new ObservableField<int>(25);

var totalHealth = new ComputedField<int>(() => baseHealth.Value + bonusHealth.Value);
totalHealth.Observe((prev, curr) => Debug.Log($"총 HP: {prev} -> {curr}"));

baseHealth.Value = 120;  // 트리거: "총 HP: 125 -> 145"
```

### 결정론적 floatP

```csharp
floatP a = 3.14f;              // float에서 암시적 변환
floatP sum = a + 2.0f;         // 산술 연산
float result = (float)sum;     // float로 변환
```

### 일괄 업데이트

```csharp
var health = new ObservableField<int>(100);
var mana = new ObservableField<int>(50);

using (health.BeginBatch())
{
    health.Value = 80;
    mana.Value = 40;
}
// 배치 완료 후 모든 옵저버가 한 번에 호출됨
```

## 샘플

Package Manager > **Geuneda 게임 데이터 확장** > **Samples**에서 가져올 수 있습니다.

- **반응형 UI 데모 (uGUI)** - ObservableField, ObservableList, ComputedField와 uGUI 바인딩
- **반응형 UI 데모 (UI Toolkit)** - ObservableField, ObservableList, ComputedField와 UI Toolkit 바인딩
- **디자이너 워크플로우** - ConfigsScriptableObject, UnitySerializedDictionary, EnumSelector를 사용한 편집 워크플로우
- **마이그레이션** - MigrationRunner를 사용한 에디터 전용 스키마 마이그레이션 데모

## 라이센스

MIT License

원본 저작권: Miguel Tomas (GameLovers)
