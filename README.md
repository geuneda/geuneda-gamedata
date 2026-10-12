# Geuneda Game Data

Unity 게임용 데이터 기반 패키지입니다. Observable 반응형 데이터 타입, 타입 안전 설정 저장소(`ConfigsProvider`), 결정론적 부동소수점(`floatP`), JSON/Unity 직렬화 헬퍼를 제공합니다.

- 패키지명: `com.geuneda.gamedata`
- 네임스페이스: `Geuneda.DataExtensions` (에디터 API는 `GeunedaEditor.GameData`)
- 어셈블리: `Geuneda.GameData`, `Geuneda.GameData.Editor`

## 설치

이 패키지는 `com.cysharp.unitask` 에 의존하는데, UniTask 는 Unity 기본 레지스트리에 없으므로 먼저 설치해야 합니다. `Packages/manifest.json` 에 다음을 추가합니다.

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
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
