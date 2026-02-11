using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// <see cref="ObservableUpdateType"/> 규칙에 정의된 요소 변경을 관찰할 수 있는 해시 셋입니다.
	/// </summary>
	public interface IObservableHashSetReader<T> : IEnumerable<T>
	{
		/// <summary>
		/// 해시 셋의 요소 수를 요청합니다.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// 이 해시 셋의 배치 업데이트를 시작합니다.
		/// 반환된 객체가 해제될 때까지 알림이 억제됩니다.
		/// </summary>
		IDisposable BeginBatch();

		/// <summary>
		/// 해시 셋에 주어진 <paramref name="item"/>이 포함되어 있는지 확인합니다.
		/// </summary>
		bool Contains(T item);

		/// <summary>
		/// 주어진 <paramref name="onUpdate"/>로 이 해시 셋의 변경을 관찰합니다.
		/// </summary>
		void Observe(Action<T, ObservableUpdateType> onUpdate);

		/// <summary>
		/// 주어진 <paramref name="onUpdate"/>로 이 해시 셋의 변경 관찰을 중지합니다.
		/// </summary>
		void StopObserving(Action<T, ObservableUpdateType> onUpdate);

		/// <summary>
		/// 주어진 <paramref name="subscriber"/> 호출의 모든 해시 셋 변경 관찰을 중지합니다.
		/// 주어진 <paramref name="subscriber"/>가 null이면 모든 관찰을 중지합니다.
		/// </summary>
		void StopObservingAll(object subscriber = null);
	}

	/// <inheritdoc />
	public interface IObservableHashSet<T> : IObservableHashSetReader<T>
	{
		/// <summary>
		/// 주어진 <paramref name="item"/>을 해시 셋에 추가합니다.
		/// 항목이 추가되면 true, 이미 존재하면 false를 반환합니다.
		/// </summary>
		bool Add(T item);

		/// <summary>
		/// 해시 셋에서 주어진 <paramref name="item"/>을 제거합니다.
		/// 항목이 제거되면 true, 존재하지 않으면 false를 반환합니다.
		/// </summary>
		bool Remove(T item);

		/// <summary>
		/// 해시 셋을 비웁니다.
		/// </summary>
		void Clear();
	}

	/// <inheritdoc />
	public partial class ObservableHashSet<T> : IObservableHashSet<T>, IBatchable, IComputedDependency
	{
		private readonly HashSet<T> _hashSet;
		private readonly IList<Action<T, ObservableUpdateType>> _updateActions = new List<Action<T, ObservableUpdateType>>();
		private readonly List<Action> _dependencyActions = new List<Action>();
		private bool _isBatching;

		// 플레이어 빌드에서 호출이 컴파일 제외되도록 partial 메서드로 선언됩니다.
		partial void EditorDebug_Register();

		/// <inheritdoc />
		public int Count
		{
			get
			{
				ComputedTracker.OnRead(this);
				return _hashSet.Count;
			}
		}

		public ObservableHashSet()
		{
			_hashSet = new HashSet<T>();
			EditorDebug_Register();
		}

		public ObservableHashSet(IEnumerable<T> collection)
		{
			_hashSet = new HashSet<T>(collection);
			EditorDebug_Register();
		}

		public ObservableHashSet(IEqualityComparer<T> comparer)
		{
			_hashSet = new HashSet<T>(comparer);
			EditorDebug_Register();
		}

		/// <inheritdoc />
		void IComputedDependency.Subscribe(Action onDependencyChanged)
		{
			_dependencyActions.Add(onDependencyChanged);
		}

		/// <inheritdoc />
		void IComputedDependency.Unsubscribe(Action onDependencyChanged)
		{
			_dependencyActions.Remove(onDependencyChanged);
		}

		/// <inheritdoc />
		public IDisposable BeginBatch()
		{
			var batch = new ObservableBatch();
			batch.Add(this);
			return batch;
		}

		/// <inheritdoc />
		void IBatchable.SuppressNotifications()
		{
			_isBatching = true;
		}

		/// <inheritdoc />
		void IBatchable.ResumeNotifications()
		{
			if (_isBatching)
			{
				_isBatching = false;
				foreach (var item in _hashSet)
				{
					InvokeUpdate(item, ObservableUpdateType.Added);
				}
			}
		}

		/// <inheritdoc />
		public bool Contains(T item)
		{
			ComputedTracker.OnRead(this);
			return _hashSet.Contains(item);
		}

		/// <inheritdoc />
		public void Observe(Action<T, ObservableUpdateType> onUpdate)
		{
			_updateActions.Add(onUpdate);
		}

		/// <inheritdoc />
		public void StopObserving(Action<T, ObservableUpdateType> onUpdate)
		{
			_updateActions.Remove(onUpdate);
		}

		/// <inheritdoc />
		public void StopObservingAll(object subscriber = null)
		{
			if (subscriber == null)
			{
				_updateActions.Clear();
				return;
			}

			for (var i = _updateActions.Count - 1; i > -1; i--)
			{
				if (_updateActions[i].Target == subscriber)
				{
					_updateActions.RemoveAt(i);
				}
			}
		}

		/// <inheritdoc />
		public bool Add(T item)
		{
			if (_hashSet.Add(item))
			{
				InvokeUpdate(item, ObservableUpdateType.Added);
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		public bool Remove(T item)
		{
			if (_hashSet.Remove(item))
			{
				InvokeUpdate(item, ObservableUpdateType.Removed);
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		public void Clear()
		{
			if (!_isBatching)
			{
				var copy = _updateActions.ToList();
				foreach (var item in _hashSet)
				{
					foreach (var action in copy)
					{
						action(item, ObservableUpdateType.Removed);
					}
				}

				for (var i = 0; i < _dependencyActions.Count; i++)
				{
					_dependencyActions[i].Invoke();
				}
			}
			_hashSet.Clear();
		}

		private void InvokeUpdate(T item, ObservableUpdateType updateType)
		{
			if (_isBatching)
			{
				return;
			}

			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i](item, updateType);
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _hashSet.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}


		// ═══════════════════════════════════════════════════════════════════════════
		// 에디터 전용: Observable 디버그 창 지원
		// ═══════════════════════════════════════════════════════════════════════════
		// 이 섹션은 Observable 디버그 창(Tools > Game Data > Observable Debugger)을 위한
		// Observable 인스턴스의 자동 등록을 제공합니다.
		//
		// 기능:
		// - 사용자 설정 불필요
		// - 약한 참조를 사용한 자동 추적 (메모리 누수 없음)
		// - 캡처된 게터를 통한 실시간 값/구독자 검사
		//
		// 이 코드는 #if UNITY_EDITOR를 통해 빌드에서 컴파일 제외됩니다.
		// ═══════════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR
		partial void EditorDebug_Register()
		{
			ObservableDebugRegistry.Register(
				instance: this,
				kind: "HashSet",
				valueGetter: () => $"Count: {Count}",
				subscriberCountGetter: () => _updateActions.Count);
		}
#endif
	}
}
