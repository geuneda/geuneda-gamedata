using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Geuneda.DataExtensions
{
	// ═══════════════════════════════════════════════════════════════════════════
	// 에디터 전용: Observable 디버그 창 지원
	// ═══════════════════════════════════════════════════════════════════════════
	// 이 파일은 Observable Debugger 창에서 사용하는 내부 레지스트리를 구현하며,
	// 사용자 코드에서 수동 등록 호출 없이 Observable 인스턴스를
	// 검색하고 검사할 수 있습니다.
	//
	// 설계 참고:
	// - 추적된 Observable이 메모리를 누수하지 않도록 약한 참조를 사용합니다.
	// - 디버그 창이 리플렉션 없이 라이브 데이터를 폴링할 수 있도록
	//   값/구독자 게터를 대리자로 저장합니다.
	// - 플레이어 빌드에서 컴파일 제외됩니다.
	// ═══════════════════════════════════════════════════════════════════════════

	/// <summary>
	/// Observable Debugger 창이 Observable 인스턴스를 검색하고 검사하는 데 사용하는 정적 레지스트리입니다.
	/// Observable은 각 Observable 클래스의 에디터 전용 코드 블록에 구현된
	/// 자체 등록 패턴을 통해 자동으로 등록됩니다.
	/// </summary>
	/// <remarks>
	/// <para>추적된 Observable이 가비지 컬렉션을 방해하지 않도록 약한 참조를 사용합니다.</para>
	/// <para>모든 멤버는 <c>#if UNITY_EDITOR</c>를 통해 플레이어 빌드에서 컴파일 제외됩니다.</para>
	/// </remarks>
	public static class ObservableDebugRegistry
	{
#if UNITY_EDITOR
		internal readonly struct ObservableDebugInfo
		{
			public readonly int Id;
			public readonly string Name;
			public readonly string Kind;
			public readonly DateTime CreatedAt;
			public readonly string FilePath;
			public readonly int LineNumber;

			public ObservableDebugInfo(int id, string name, string kind, DateTime createdAt, string filePath, int lineNumber)
			{
				Id = id;
				Name = name;
				Kind = kind;
				CreatedAt = createdAt;
				FilePath = filePath;
				LineNumber = lineNumber;
			}

			public string FileName => string.IsNullOrEmpty(FilePath) ? null : Path.GetFileName(FilePath);

			public string SourceLocation => string.IsNullOrEmpty(FilePath) ? null : $"{FileName}:{LineNumber}";
		}

		private static int _nextId = 1;
		private static readonly ConditionalWeakTable<object, Entry> _entries = new ConditionalWeakTable<object, Entry>();
		private static readonly List<WeakReference<object>> _refs = new List<WeakReference<object>>();

		/// <summary>
		/// 디버그 레지스트리에 Observable 인스턴스를 등록합니다.
		/// 에디터 빌드에서 Observable 생성자에 의해 자동으로 호출됩니다.
		/// </summary>
		/// <param name="instance">등록할 Observable 인스턴스입니다.</param>
		/// <param name="kind">Observable 타입 카테고리입니다(Field, Computed, List, Dictionary, HashSet).</param>
		/// <param name="valueGetter">현재 값을 문자열로 가져오는 대리자입니다.</param>
		/// <param name="subscriberCountGetter">현재 구독자 수를 가져오는 대리자입니다.</param>
		internal static void Register(
			object instance,
			string kind,
			Func<string> valueGetter,
			Func<int> subscriberCountGetter)
		{
			if (instance == null) return;

			if (_entries.TryGetValue(instance, out _))
			{
				return;
			}

			var (name, filePath, lineNumber) = GetAutoName(kind, instance.GetType());
			var info = new ObservableDebugInfo(_nextId++, name, kind, DateTime.UtcNow, filePath, lineNumber);
			_entries.Add(instance, new Entry(info, valueGetter, subscriberCountGetter));
			_refs.Add(new WeakReference<object>(instance));
		}

		/// <summary>
		/// 현재 추적 중인 모든 Observable 인스턴스의 스냅샷을 열거합니다.
		/// 가비지 컬렉션된 인스턴스의 항목을 자동으로 정리합니다.
		/// </summary>
		public static IEnumerable<EntrySnapshot> EnumerateSnapshots()
		{
			for (int i = _refs.Count - 1; i >= 0; i--)
			{
				if (_refs[i].TryGetTarget(out var instance) && _entries.TryGetValue(instance, out var entry))
				{
					yield return entry.ToSnapshot(instance);
				}
				else
				{
					_refs.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// 현재 추적 중인 특정 Observable 인스턴스의 스냅샷을 찾습니다.
		/// </summary>
		public static EntrySnapshot? FindByInstance(object instance)
		{
			if (instance == null) return null;
			if (_entries.TryGetValue(instance, out var entry))
			{
				return entry.ToSnapshot(instance);
			}
			return null;
		}

		private static (string name, string filePath, int lineNumber) GetAutoName(string kind, Type instanceType)
		{
			string filePath = null;
			int lineNumber = 0;
			string typeName = "Unknown";
			string memberName = kind;

			try
			{
				// 소스 위치를 캡처하기 위해 파일 정보를 활성화합니다
				var stack = new StackTrace(4, true);
				var frame = stack.GetFrame(0);
				var method = frame?.GetMethod();
				typeName = method?.DeclaringType?.Name ?? "Unknown";
				filePath = frame?.GetFileName();
				lineNumber = frame?.GetFileLineNumber() ?? 0;

				// 소스 라인에서 필드/프로퍼티 이름을 추출하려고 시도합니다
				if (!string.IsNullOrEmpty(filePath) && lineNumber > 0)
				{
					var extractedName = TryExtractMemberName(filePath, lineNumber);
					if (!string.IsNullOrEmpty(extractedName))
					{
						memberName = extractedName;
					}
				}
			}
			catch
			{
				// 오류 발생 시 기본 이름으로 대체합니다
			}

			var name = $"{typeName}.{memberName}<{instanceType.Name}>";
			return (name, filePath, lineNumber);
		}

		/// <summary>
		/// 주어진 위치의 소스 코드에서 필드 또는 프로퍼티 이름을 추출하려고 시도합니다.
		/// </summary>
		private static string TryExtractMemberName(string filePath, int lineNumber)
		{
			try
			{
				if (!File.Exists(filePath)) return null;

				var lines = File.ReadAllLines(filePath);
				if (lineNumber <= 0 || lineNumber > lines.Length) return null;

				var line = lines[lineNumber - 1];

				// 프로퍼티 패턴: "public ObservableField<int> Health { get; }"
				// 필드 패턴: "private ObservableField<int> _health ="
				// 할당 패턴: "Health = new ObservableField<int>()"

				// 프로퍼티 선언 패턴 매칭 시도
				var propertyMatch = Regex.Match(line, @"\b(\w+)\s*\{\s*get\s*;");
				if (propertyMatch.Success)
				{
					return propertyMatch.Groups[1].Value;
				}

				// 필드 할당 패턴 매칭 시도 (= new 앞의 이름)
				var assignmentMatch = Regex.Match(line, @"\b(\w+)\s*=\s*new\s+(?:Observable|Computed)");
				if (assignmentMatch.Success)
				{
					return assignmentMatch.Groups[1].Value;
				}

				// 필드 선언 패턴 매칭 시도 (타입 뒤에 이름)
				var fieldMatch = Regex.Match(line, @"(?:Observable|Computed)\w*<[^>]+>\s+(\w+)\s*[;=]");
				if (fieldMatch.Success)
				{
					return fieldMatch.Groups[1].Value;
				}
			}
			catch
			{
				// 조용히 실패 - 이것은 더 나은 명명을 위한 것일 뿐, 치명적이지 않음
			}

			return null;
		}

		/// <summary>
		/// Observable 인스턴스의 디버그 정보에 대한 시점 스냅샷입니다.
		/// </summary>
		public readonly struct EntrySnapshot
		{
			private readonly ObservableDebugInfo _info;

			public int Id => _info.Id;
			public string Name => _info.Name;
			public string Kind => _info.Kind;
			public DateTime CreatedAt => _info.CreatedAt;
			public string FilePath => _info.FilePath;
			public int LineNumber => _info.LineNumber;
			public string FileName => _info.FileName;
			public string SourceLocation => _info.SourceLocation;

			public readonly string Value;
			public readonly int Subscribers;
			public readonly Type RuntimeType;
			public readonly WeakReference<object> InstanceRef;

			internal EntrySnapshot(ObservableDebugInfo info, string value, int subscribers, Type runtimeType, WeakReference<object> instanceRef)
			{
				_info = info;
				Value = value;
				Subscribers = subscribers;
				RuntimeType = runtimeType;
				InstanceRef = instanceRef;
			}
		}

		private sealed class Entry
		{
			private readonly ObservableDebugInfo _info;
			private readonly Func<string> _valueGetter;
			private readonly Func<int> _subscriberCountGetter;

			public Entry(ObservableDebugInfo info, Func<string> valueGetter, Func<int> subscriberCountGetter)
			{
				_info = info;
				_valueGetter = valueGetter;
				_subscriberCountGetter = subscriberCountGetter;
			}

			/// <summary>
			/// 현재 라이브 데이터로 이 항목에서 스냅샷을 생성합니다.
			/// </summary>
			public EntrySnapshot ToSnapshot(object instance)
			{
				string value;
				int subs;

				try { value = _valueGetter?.Invoke() ?? string.Empty; }
				catch (Exception ex) { value = $"<error: {ex.Message}>"; }

				try { subs = _subscriberCountGetter?.Invoke() ?? 0; }
				catch { subs = 0; }

				return new EntrySnapshot(_info, value, subs, instance.GetType(), new WeakReference<object>(instance));
			}
		}
#endif
	}
}

