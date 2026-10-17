# Geuneda Game Data

Unity 게임용 데이터 기반 패키지입니다. Observable 반응형 데이터 타입, 타입 안전 설정 저장소(`ConfigsProvider`), 결정론적 부동소수점(`floatP`), 메모리 변조를 감지하는 보안 값(`SecureInt` 등), JSON/Unity 직렬화 헬퍼를 제공합니다.

- 패키지명: `com.geuneda.gamedata`
- 네임스페이스: `Geuneda.DataExtensions` (에디터 API는 `GeunedaEditor.GameData`)
- 어셈블리: `Geuneda.GameData`, `Geuneda.GameData.Editor`

## 설치

이 패키지는 `com.geuneda.core` 와 `com.cysharp.unitask` 에 의존합니다. 둘 다 Unity 기본 레지스트리에 없어 자동으로 설치되지 않으므로 `Packages/manifest.json` 에 함께 추가합니다.

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.geuneda.core": "https://github.com/geuneda/geuneda-core.git",
    "com.geuneda.gamedata": "https://github.com/geuneda/geuneda-gamedata.git"
  }
}
```

Package Manager 의 **+ > Add package from git URL...** 에 `https://github.com/geuneda/geuneda-gamedata.git` 을 입력해도 됩니다. 특정 버전에 고정하려면 URL 끝에 `#<태그>` 를 붙입니다.

나머지 의존성(Newtonsoft.Json, uGUI)은 Unity 레지스트리 패키지입니다. Unity 6 에서는 TextMeshPro 가 `com.unity.ugui` 2.0 에 포함되어 있습니다.

## 빠른 시작

```csharp
using System;
using System.Collections.Generic;
using Geuneda.DataExtensions;
using UnityEngine;

[Serializable]
public struct ItemConfig
{
    public int Id;
    public string Name;
}

public class GameDataQuickStart : MonoBehaviour
{
    private readonly ObservableField<int> _score = new ObservableField<int>(0);

    private void Start()
    {
        // 반응형 필드: 값이 바뀌면 (이전 값, 현재 값)으로 콜백이 호출된다
        _score.Observe((previous, current) => Debug.Log($"점수: {previous} -> {current}"));
        _score.Value = 100; // "점수: 0 -> 100"

        // 설정 저장소: ID 리졸버로 컬렉션을 등록하고 ID로 조회한다
        var configs = new ConfigsProvider();
        configs.AddConfigs(item => item.Id, new List<ItemConfig>
        {
            new ItemConfig { Id = 1, Name = "Sword" },
            new ItemConfig { Id = 2, Name = "Shield" }
        });

        Debug.Log(configs.GetConfig<ItemConfig>(1).Name); // "Sword"
    }

    private void OnDestroy()
    {
        _score.StopObservingAll();
    }
}
```

## 핵심 기능

### Observable 데이터 타입

| 타입 | 콜백 시그니처 |
|------|---------------|
| `ObservableField<T>` | `Action<T, T>` (이전 값, 현재 값) |
| `ObservableList<T>` | `Action<int, T, T, ObservableUpdateType>` (인덱스, 이전 값, 현재 값, 종류) |
| `ObservableDictionary<TKey, TValue>` | `Action<TKey, TValue, TValue, ObservableUpdateType>` |
| `ObservableHashSet<T>` | `Action<T, ObservableUpdateType>` |
| `ObservableResolverField/List/Dictionary` | 다른 저장소의 값을 getter/setter 또는 변환 함수로 감싸는 버전 |

```csharp
var inventory = new ObservableList<string>(new List<string>());
inventory.Observe((index, previous, current, type) => Debug.Log($"{type} [{index}] {current}"));
inventory.Add("Sword"); // Added

var stats = new ObservableDictionary<string, int>(new Dictionary<string, int>());
stats.Observe("Health", (key, previous, current, type) => Debug.Log($"{key}: {current}"));
stats.Add("Health", 100); // 키 구독자에게 Added 통지
```

- `ObservableField.Value` setter 는 값이 같아도 항상 통지합니다.
- `ObservableDictionary` 의 `ObservableUpdateFlag` 기본값은 `KeyUpdateOnly` 입니다. 이 상태에서는 키를 지정한 `Observe(key, ...)` 구독자만 통지를 받습니다. 키 없이 `Observe(...)` 로 구독했다면 `UpdateOnly` 또는 `Both` 로 바꿔야 합니다.
- `StopObservingAll(subscriber)` 에 구독 객체를 넘기면 그 객체의 콜백만 해제하고, 인자를 생략하면 모두 해제합니다.

**통지 규칙** (모든 Observable 과 `ComputedField` 공통)

- 변경 연산(값 설정, `Add`, `Remove`, `Clear` 등) 하나가 시작될 때 그 시점의 구독자 목록을 고정하고, 구독한 순서대로 호출합니다. `Remove`, `Clear` 도 같은 순서입니다.
- 콜백 안에서 구독을 해제해도 다른 구독자를 건너뛰거나 두 번 부르지 않습니다. 아직 호출되지 않은 구독자를 해제하면 그 구독자는 이번 통지에서 빠집니다. 같은 콜백을 여러 번 구독했다면 해제한 한 건만 빠집니다. `StopObserving(key)` 와 `StopObservingAll()` 도 진행 중인 통지에 바로 반영됩니다.
- 콜백 안에서 새로 구독한 콜백은 그 연산의 통지를 받지 않고 다음 연산부터 받습니다. 의존 갱신(`ComputedField` 무효화)은 이 규칙과 달리 알리는 시점의 의존 관계 전체에 보내므로, 콜백 안에서 처음 계산된 `ComputedField` 도 갱신됩니다.
- 구독자가 예외를 던져도 나머지 구독자와 의존 갱신(`ComputedField`)은 끝까지 실행한 뒤 예외를 다시 던집니다. 예외가 하나면 원래 예외 그대로, 여럿이면 `AggregateException` 입니다. 이미 적용된 데이터 변경은 되돌리지 않습니다.
- `Clear` 는 구독자에게 `Removed` 를 보낸 뒤 실제로 비우고, 그다음 의존 갱신을 알립니다. 미리 구독했지만 아직 없는 키에는 `Removed` 를 보내지 않습니다.

### ComputedField

계산 함수 안에서 읽은 Observable 을 자동으로 추적해, 그중 하나가 바뀌면 다시 계산하고 통지합니다.

```csharp
var baseHealth = new ObservableField<int>(100);
var bonusHealth = new ObservableField<int>(25);

var totalHealth = new ComputedField<int>(() => baseHealth.Value + bonusHealth.Value);
totalHealth.Observe((previous, current) => Debug.Log($"총 HP: {previous} -> {current}"));

baseHealth.Value = 120; // "총 HP: 125 -> 145"
totalHealth.Dispose();  // 다 쓰면 의존성 구독을 해제한다
```

확장 메서드 `Select`, `CombineWith` (2~4개)와 정적 팩토리 `ObservableField.Computed`, `ObservableField.Combine` 으로도 만들 수 있습니다.

### 일괄 업데이트

`ObservableBatch` 에 넣은 Observable 은 배치가 해제될 때까지 통지를 보류합니다. `BeginBatch()` 는 자기 자신만 담긴 배치를 반환합니다.

```csharp
var health = new ObservableField<int>(100);
var mana = new ObservableField<int>(50);

using (var batch = new ObservableBatch())
{
    batch.Add(health);
    batch.Add(mana);

    health.Value = 80;
    mana.Value = 40;
}
// 여기서 health, mana 옵저버가 각각 한 번씩 호출된다
```

- 배치가 끝나면 컬렉션은 남은 항목마다 통지한 뒤 의존 갱신(`ComputedField`)을 한 번 보냅니다. 배치 안에서 모두 지워 남은 항목이 없어도 의존 갱신은 보냅니다.
- 재개 통지 중 콜백이 같은 Observable 에 새 배치를 열면 남은 항목 통지와 의존 갱신을 멈추고, 그 배치가 끝날 때 전체 항목을 다시 통지합니다.
- `Clear` 의 `Removed` 콜백이 새 배치를 열면 `Removed` 는 끝까지 보내고, 비운 뒤의 의존 갱신은 그 배치가 끝날 때 보냅니다.
- 구독자가 예외를 던져도 배치에 든 Observable 을 모두 재개하고 배치를 닫은 뒤 예외를 다시 던집니다.

### 설정 관리

- `ConfigsProvider`: 타입별로 `Dictionary<int, T>` 를 미리 만들어 두는 설정 저장소입니다. `AddConfigs` (ID 컬렉션), `AddSingletonConfig` (타입당 하나), `GetConfig`, `TryGetConfig`, `GetConfigsList`, `GetConfigsDictionary`, `EnumerateConfigs` 를 제공합니다. 버전은 `ulong Version` 이며 `UpdateTo(version, configs)` 로만 바뀝니다.
- `ConfigsSerializer`: `ConfigsProvider` 내용을 Newtonsoft.Json 으로 직렬화/역직렬화합니다. 기본 모드 `TrustedOnly` 는 `ConfigTypesBinder` 화이트리스트로 역직렬화 타입을 제한합니다. `Secure` 모드는 타입 메타데이터를 쓰지 않으므로 직렬화 전용입니다.
- `ConfigsScriptableObject<TId, TAsset>`: 인스펙터에서 `Pair<TId, TAsset>` 목록을 편집하고 런타임에 `ConfigsDictionary` 로 조회하는 ScriptableObject 기본 클래스입니다.
- `IConfigBackendService`: 원격 설정 버전 조회와 다운로드를 위한 UniTask 기반 인터페이스입니다(구현은 프로젝트에서 제공).

```csharp
var provider = new ConfigsProvider();
provider.AddConfigs(item => item.Id, new List<ItemConfig> { new ItemConfig { Id = 1, Name = "Sword" } });

var serializer = new ConfigsSerializer();
string json = serializer.Serialize(provider, "1"); // 설정 타입에 [Serializable] 이 없으면 예외

var loaded = new ConfigsProvider();
serializer.Deserialize(json, loaded); // loaded.Version == 1
```

`TrustedOnly` 모드는 `Serialize` 한 타입을 화이트리스트에 자동 등록합니다. 다른 `ConfigsSerializer` 인스턴스로 역직렬화할 때는 먼저 `RegisterAllowedTypes` 또는 `RegisterAllowedTypesFromProvider` 로 허용 타입을 등록해야 합니다.

### Unity 직렬화 헬퍼

- `UnitySerializedDictionary<TKey, TValue>`: 인스펙터에서 편집되는 Dictionary 입니다. 추상 클래스이므로 `[Serializable]` 구체 클래스를 선언해서 씁니다.
- `EnumSelector<T>`: enum 을 숫자 대신 이름으로 저장해 enum 항목 순서가 바뀌어도 데이터가 깨지지 않게 합니다. `[Serializable]` 하위 클래스와 `EnumSelectorPropertyDrawer<T>` 하위 클래스를 함께 선언해야 인스펙터에 드롭다운이 나옵니다(Designer Workflow 샘플 참고).
- `SerializableType<T>`, `Pair<TKey, TValue>`, `StructPair<TKey, TValue>`, `Vector2/3/4Serializable`, `Vector2Int/3IntSerializable`, `Vector4Int`
- `ConfigsSerializer` 에 등록된 JSON 컨버터: `Color`, `Vector2`, `Vector3`, `Vector4`, `Quaternion`

### 결정론적 수학

`floatP` 는 정수 연산으로 IEEE binary32 를 구현한 구조체로, 플랫폼과 관계없이 같은 결과를 냅니다. 수학 함수는 `MathfloatP` (`Sin`, `Cos`, `Sqrt`, `Pow`, `Lerp`, `Clamp` 등)에 있습니다.

```csharp
floatP a = 3.14f;                        // float 에서 암시적 변환
floatP sum = a + 2;                      // int 에서 암시적 변환 후 연산
floatP root = MathfloatP.Sqrt(sum);
float result = root;                     // float 로 암시적 변환
```

### 보안 값

`SecureInt`, `SecureLong`, `SecureFloat`, `SecureDouble`, `SecureFloatP` 는 메모리 편집 도구(GameGuardian, Cheat Engine 등)가 값을 찾아 바꾸는 것을 어렵게 하고, 바꾸면 감지합니다. 오프라인 재화나 전투 수치처럼 클라이언트가 들고 있는 값에 씁니다.

```csharp
public class Wallet
{
    public SecureInt Gold;                  // JSON 세이브에서는 {"Gold":100} 처럼 int 필드와 같은 형식
}

var wallet = new Wallet();
wallet.Gold = 100;                          // int 에서 암시적 변환
wallet.Gold += 50;                          // 산술, 비교, ++ 도 int 처럼
int gold = wallet.Gold;                     // 읽을 때 변조 검사

SecureFloatP hp = (floatP)10;
hp -= (floatP)2.5f;                         // floatP 와 섞은 연산은 그대로
floatP half = hp.Value * 2;                 // SecureFloatP 끼리, 정수 리터럴과의 연산은 Value 로 꺼내서

SecureValueMonitor.TamperDetected += OnTamper;   // 감지 알림 (static 이벤트)

void OnTamper(Type valueType)
{
    SecureValueMonitor.TamperDetected -= OnTamper;   // 감지된 읽기마다 발생하므로 처음 한 번만 처리
    // 로그를 보내고, 감지 시점을 숨기려면 몇 초 무작위로 기다린 뒤 대응(종료, 서버 신고 등)
}
```

- **저장 방식:** 값을 세션 키와 nonce 로 섞어 저장하고 32비트 검사값을 함께 둡니다. 쓸 때마다 새 nonce 로 다시 인코딩하므로 같은 값이어도 메모리 모양이 매번 바뀝니다. 세션 키는 실행마다 암호학 난수로 한 번 만듭니다. 크기는 타입마다 16바이트이고, 세션 키를 만든 뒤의 정상 읽기·쓰기에는 할당이 없습니다(변조를 알릴 때는 핸들러 목록을 복사합니다).
- **감지:** 검사값이 맞지 않으면 `SecureValueMonitor.TamperDetected` 를 부르고, 예외 없이 복원한 값을 그대로 돌려줍니다. 대응은 게임이 정합니다. 이벤트는 값을 읽은 스레드에서 바로 불리며, 핸들러 예외는 로그로 남기고 다음 핸들러로 넘어갑니다. 핸들러 안에서 다시 감지되는 읽기는 알리지 않습니다. 세션 시작 때 구독을 모두 지웁니다(도메인 리로드를 끈 Play Mode 포함).
- **비교:** `Equals`, `GetHashCode`, `CompareTo` 는 인코딩이 아니라 값을 비교하므로 `Dictionary` 키, `HashSet`, 정렬, `ObservableField<SecureInt>` 에 그대로 쓸 수 있습니다.
- **JSON:** Newtonsoft 로 같은 이름의 `int`/`long`/`float`/`double` 필드와 같은 형식(평문 숫자)으로 읽고 씁니다. 기존 세이브의 필드 타입을 바꿔도 호환됩니다. `SecureInt?` 같은 nullable 은 `null` 도 그대로 왕복하고, nullable 이 아닌 타입에 `null` 이 오면 원래 타입처럼 `JsonSerializationException` 입니다. `SecureFloatP` 는 float 숫자로 씁니다. float 필드처럼 NaN 의 payload 는 남지 않고, Unity(Mono)에서는 -0 이 0 으로 읽힙니다. 그 밖의 값은 비트 그대로 돌아옵니다.

한계도 알고 써야 합니다.

- 가벼운 난독화이고 검사값은 MAC 이 아닙니다. 막는 것은 메모리 검색 도구로 평문 값을 찾아 바꾸는 단순 변조와 그 감지까지입니다. 알고리즘을 아는 공격자는 화면에 보이는 값 하나와 그 값의 메모리(16바이트)만 있으면 세션 키를 찾지 않고도 검사값을 맞춘 위조 값을 만들 수 있고, 같은 정보로 세션 키도 복원됩니다. IL2CPP 메타데이터를 덤프하면 세션 키가 있는 정적 필드 위치도 알 수 있습니다. 재화 지급처럼 중요한 판정은 서버가 검증해야 합니다.
- 모든 필드를 0 으로 지우는 변조는 `default`(값 0)와 구별되지 않아 감지하지 못합니다.
- nonce 는 32비트 카운터 하나를 모든 보안 값이 함께 씁니다. 프로세스 전체 쓰기가 2^32-1 회를 넘으면 nonce 가 처음부터 다시 쓰여, 같은 값을 같은 nonce 로 쓰면 메모리 모양도 같아집니다(60fps 로 매 프레임 값 1,000개를 쓰면 약 20시간).
- 한 필드를 여러 스레드가 동시에 쓰고 읽으면 16바이트 복사가 원자적이지 않아 정상 값이 변조로 읽힐 수 있습니다. 스레드 사이에 공유하려면 잠금으로 보호합니다.
- Unity 직렬화(`[SerializeField]`, 인스펙터, `JsonUtility`)는 지원하지 않습니다. 이 경로로 저장한 값은 사라집니다.
- 세이브 파일과 네트워크 메시지는 평문입니다. 저장 데이터 변조 방지는 이 기능의 범위가 아닙니다.

### 유효성 검사

설정 필드에 `[Required]`, `[Range(min, max)]`, `[MinLength(n)]` 을 붙이면 에디터에서 `EditorConfigValidator` 와 Config Browser, `ConfigsScriptableObject` 인스펙터가 검사합니다. `[ReadOnly]` 는 인스펙터에서 필드를 읽기 전용으로 표시합니다. 이 `RangeAttribute` 는 `UnityEngine.RangeAttribute` 와 이름이 같으므로 두 네임스페이스를 함께 쓰면 별칭이나 전체 이름으로 구분해야 합니다.

### 에디터 도구

| 도구 | 위치 | 기능 |
|------|------|------|
| Config Browser | `Tools > Game Data > Config Browser` | 에디터에서 생성된 `ConfigsProvider` 인스턴스 탐색, 유효성 검사, JSON 내보내기, Migrations 탭 |
| Observable Debugger | `Tools > Game Data > Observable Debugger` | 살아 있는 Observable 인스턴스 값과 구독자 수 확인, 이름/종류/활성 여부 필터 |
| ConfigsScriptableObject 인스펙터 | 에셋 선택 시 자동 | 항목별 중복 키/유효성 오류 표시, Validate All |
| `MigrationRunner` | 에디터 API | `IConfigMigration` + `[ConfigMigration(typeof(T))]` 로 정의한 스키마 마이그레이션 실행 |

## 샘플

Package Manager 에서 이 패키지를 선택하고 **Samples** 에서 가져옵니다.

| 샘플 | 내용 |
|------|------|
| [Reactive UI Demo (uGUI)](<Samples~/Reactive UI Demo (uGUI)/README.md>) | `ObservableField`, `ObservableList`, `ComputedField`, `ObservableBatch` 를 uGUI 에 바인딩 |
| [Reactive UI Demo (UI Toolkit)](<Samples~/Reactive UI Demo (UI Toolkit)/README.md>) | 같은 데모를 UI Toolkit 으로 구현 |
| [Designer Workflow](<Samples~/Designer Workflow/README.md>) | `ConfigsScriptableObject`, `UnitySerializedDictionary`, `EnumSelector` 로 만든 에셋을 `ConfigsProvider` 에 로드 |
| [Migration](<Samples~/Migration/README.md>) | Config Browser 와 `MigrationRunner` 를 사용한 에디터 전용 스키마 마이그레이션 |

## 요구 사항

- Unity 6000.0 이상
- `com.unity.nuget.newtonsoft-json` 3.2.1
- `com.cysharp.unitask` 2.5.10 (`IConfigBackendService` 에서 사용)
- `com.unity.ugui` 2.0.0 (TextMeshPro 포함, 샘플 UI 에서 사용)

## 라이선스

MIT License. 원본 저작권: Miguel Tomas (GameLovers). 자세한 내용은 [LICENSE.md](LICENSE.md) 를 참고하세요.
