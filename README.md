# Geuneda Data Extensions

Unity용 데이터 타입 확장 패키지입니다. Observable 컬렉션, 고정소수점 연산, 직렬화 가능한 타입 등 다양한 유틸리티를 제공합니다.

## 설치 방법

### Unity Package Manager (Git URL)

1. Unity Editor에서 **Window → Package Manager** 열기
2. **+** 버튼 클릭 → **Add package from git URL...**
3. 다음 URL 입력:
```
https://github.com/geuneda/geuneda-dataextensions.git
```

또는 `Packages/manifest.json`에 직접 추가:
```json
{
  "dependencies": {
    "com.geuneda.dataextensions": "https://github.com/geuneda/geuneda-dataextensions.git#v1.0.0"
  }
}
```

## 주요 기능

### Observable 컬렉션
데이터 변경을 감지하고 이벤트를 발생시키는 반응형 컬렉션입니다.

- **ObservableField<T>**: 단일 값의 변경 감지
- **ObservableList<T>**: 리스트의 추가/삭제/변경 감지
- **ObservableDictionary<TKey, TValue>**: 딕셔너리의 변경 감지

```csharp
using Geuneda;

var health = new ObservableField<int>(100);
health.OnChanged += (oldValue, newValue) => {
    Debug.Log($"체력 변경: {oldValue} → {newValue}");
};
health.Value = 80; // 이벤트 발생
```

### 고정소수점 연산 (floatP)
결정론적 연산이 필요한 멀티플레이어 게임을 위한 고정소수점 타입입니다.

```csharp
using Geuneda;

floatP a = (floatP)1.5f;
floatP b = (floatP)2.5f;
floatP result = a + b; // 정확히 4.0
```

### EnumSelector
인스펙터에서 Enum 값을 쉽게 선택할 수 있는 어트리뷰트입니다.

```csharp
using Geuneda;

public class Example : MonoBehaviour
{
    [EnumSelector]
    public MyEnum selectedValue;
}
```

### SerializableType
System.Type을 직렬화할 수 있게 해주는 래퍼 클래스입니다.

```csharp
using Geuneda;

[Serializable]
public class TypeReference
{
    public SerializableType myType;
}
```

### UnitySerializedDictionary
Unity 인스펙터에서 편집 가능한 직렬화 딕셔너리입니다.

```csharp
using Geuneda;

[Serializable]
public class StringIntDictionary : UnitySerializedDictionary<string, int> { }

public class Example : MonoBehaviour
{
    public StringIntDictionary scores;
}
```

### ReadOnly 어트리뷰트
인스펙터에서 읽기 전용으로 표시할 필드를 지정합니다.

```csharp
using Geuneda;

public class Example : MonoBehaviour
{
    [ReadOnly]
    public string readOnlyField = "수정 불가";
}
```

## API 레퍼런스

| 클래스 | 설명 |
|--------|------|
| `ObservableField<T>` | 변경 감지 가능한 단일 값 |
| `ObservableList<T>` | 변경 감지 가능한 리스트 |
| `ObservableDictionary<K,V>` | 변경 감지 가능한 딕셔너리 |
| `floatP` | 고정소수점 타입 |
| `EnumSelector` | Enum 선택 어트리뷰트 |
| `SerializableType` | 직렬화 가능한 Type 래퍼 |
| `UnitySerializedDictionary<K,V>` | Unity 직렬화 딕셔너리 |
| `ReadOnlyAttribute` | 읽기 전용 어트리뷰트 |

## 요구 사항

- Unity 6000.0 이상

## 라이센스

MIT License

원본 저작권: Miguel Tomas (GameLovers)
