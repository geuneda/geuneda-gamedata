using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// Config Browser 창이 <see cref="IConfigsProvider"/> 인스턴스를 검색하고 검사하는 데 사용하는 정적 레지스트리입니다.
	/// 프로바이더는 생성자에 구현된 자체 등록 패턴을 통해 자동으로 등록됩니다.
	/// </summary>
	/// <remarks>
	/// <para>추적된 프로바이더가 가비지 컬렉션을 방해하지 않도록 약한 참조를 사용합니다.</para>
	/// <para>모든 멤버는 <c>#if UNITY_EDITOR</c>를 통해 플레이어 빌드에서 컴파일 제외됩니다.</para>
	/// </remarks>
	public static class ConfigsProviderDebugRegistry
	{
#if UNITY_EDITOR
		internal readonly struct ProviderDebugInfo
		{
			public readonly int Id;
			public readonly string Name;
			public readonly DateTime CreatedAt;
			public readonly string FilePath;
			public readonly int LineNumber;

			public ProviderDebugInfo(int id, string name, DateTime createdAt, string filePath, int lineNumber)
			{
				Id = id;
				Name = name;
				CreatedAt = createdAt;
				FilePath = filePath;
				LineNumber = lineNumber;
			}

			public string FileName => string.IsNullOrEmpty(FilePath) ? null : Path.GetFileName(FilePath);
			public string SourceLocation => string.IsNullOrEmpty(FilePath) ? null : $"{FileName}:{LineNumber}";
		}

		private static int _nextId = 1;
		private static readonly ConditionalWeakTable<IConfigsProvider, ProviderEntry> _entries = new ConditionalWeakTable<IConfigsProvider, ProviderEntry>();
		private static readonly List<WeakReference<IConfigsProvider>> _refs = new List<WeakReference<IConfigsProvider>>();

		/// <summary>
		/// 디버그 레지스트리에 프로바이더 인스턴스를 등록합니다.
		/// 에디터 빌드에서 <see cref="ConfigsProvider"/> 생성자에 의해 자동으로 호출됩니다.
		/// </summary>
		internal static void Register(IConfigsProvider instance)
		{
			if (instance == null) return;

			if (_entries.TryGetValue(instance, out _))
			{
				return;
			}

			var (autoName, filePath, lineNumber) = GetAutoName();
			var info = new ProviderDebugInfo(_nextId++, autoName, DateTime.UtcNow, filePath, lineNumber);
			
			_entries.Add(instance, new ProviderEntry(info));
			_refs.Add(new WeakReference<IConfigsProvider>(instance));
		}

		/// <summary>
		/// 현재 추적 중인 모든 프로바이더 인스턴스의 스냅샷을 열거합니다.
		/// 가비지 컬렉션된 인스턴스의 항목을 자동으로 정리합니다.
		/// </summary>
		public static IEnumerable<ProviderSnapshot> EnumerateSnapshots()
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

		private static (string name, string filePath, int lineNumber) GetAutoName()
		{
			string filePath = null;
			int lineNumber = 0;
			string typeName = "Unknown";

			try
			{
				// 프레임 0: GetAutoName
				// 프레임 1: Register
				// 프레임 2: ConfigsProvider 생성자
				// 프레임 3: 생성자 호출자
				var stack = new StackTrace(3, true);
				var frame = stack.GetFrame(0);
				var method = frame?.GetMethod();
				typeName = method?.DeclaringType?.Name ?? "Unknown";
				filePath = frame?.GetFileName();
				lineNumber = frame?.GetFileLineNumber() ?? 0;
			}
			catch
			{
				// 대체
			}

			var name = string.IsNullOrEmpty(filePath) ? $"ConfigsProvider#{_nextId}" : $"{typeName}";
			return (name, filePath, lineNumber);
		}

		/// <summary>
		/// 프로바이더 인스턴스의 디버그 정보에 대한 시점 스냅샷입니다.
		/// </summary>
		public readonly struct ProviderSnapshot
		{
			private readonly ProviderDebugInfo _info;

			public int Id => _info.Id;
			public string Name => _info.Name;
			public DateTime CreatedAt => _info.CreatedAt;
			public string FilePath => _info.FilePath;
			public int LineNumber => _info.LineNumber;
			public string FileName => _info.FileName;
			public string SourceLocation => _info.SourceLocation;

			public readonly int ConfigTypeCount;
			public readonly WeakReference<IConfigsProvider> ProviderRef;

			internal ProviderSnapshot(ProviderDebugInfo info, int configTypeCount, IConfigsProvider instance)
			{
				_info = info;
				ConfigTypeCount = configTypeCount;
				ProviderRef = new WeakReference<IConfigsProvider>(instance);
			}
		}

		private sealed class ProviderEntry
		{
			private readonly ProviderDebugInfo _info;

			public ProviderEntry(ProviderDebugInfo info)
			{
				_info = info;
			}

			public ProviderSnapshot ToSnapshot(IConfigsProvider instance)
			{
				var count = 0;
				try { count = instance.GetAllConfigs().Count; } catch { }
				return new ProviderSnapshot(_info, count, instance);
			}
		}
#endif
	}
}
