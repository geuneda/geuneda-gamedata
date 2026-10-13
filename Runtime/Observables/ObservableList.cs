using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// <see cref="ObservableUpdateType"/> 규칙에 정의된 요소 변경을 관찰할 수 있는 리스트입니다
	/// </summary>
	public interface IObservableListReader : IEnumerable
	{
		/// <summary>
		/// 리스트의 요소 수를 요청합니다
		/// </summary>
		int Count { get; }
	}

	/// <inheritdoc cref="IObservableListReader"/>
	/// <remarks>
	/// 읽기 전용 Observable 리스트 인터페이스
	/// </remarks>
	public interface IObservableListReader<T> : IObservableListReader, IEnumerable<T>
	{
		/// <summary>
		/// 주어진 <paramref name="index"/>에 연결된 데이터를 조회하고 반환합니다
		/// </summary>
		T this[int index] { get; }

		/// <summary>
		/// 이 리스트의 배치 업데이트를 시작합니다.
		/// 반환된 객체가 해제될 때까지 알림이 억제됩니다.
		/// </summary>
		IDisposable BeginBatch();

		/// <summary>
		/// 이 리스트를 <see cref="IReadOnlyList{T}"/>로 요청합니다
		/// </summary>
		IReadOnlyList<T> ReadOnlyList { get; }

		/// <inheritdoc cref="List{T}.Contains"/>
		bool Contains(T value);

		/// <inheritdoc cref="List{T}.IndexOf(T)"/>
		int IndexOf(T value);

		/// <summary>
		/// 주어진 <paramref name="onUpdate"/>로 이 리스트의 변경을 관찰합니다
		/// </summary>
		void Observe(Action<int, T, T, ObservableUpdateType> onUpdate);

		/// <summary>
		/// 데이터가 변경될 때 주어진 <paramref name="onUpdate"/>로 이 리스트를 관찰하고 주어진 <paramref name="index"/>로 호출합니다
		/// </summary>
		void InvokeObserve(int index, Action<int, T, T, ObservableUpdateType> onUpdate);

		/// <summary>
		/// 주어진 <paramref name="onUpdate"/>로 이 딕셔너리의 모든 데이터 변경 관찰을 중지합니다
		/// </summary>
		void StopObserving(Action<int, T, T, ObservableUpdateType> onUpdate);

		/// <summary>
		/// 주어진 <paramref name="subscriber"/> 호출의 모든 딕셔너리 변경 관찰을 중지합니다.
		/// 주어진 <paramref name="subscriber"/>가 null이면 모든 관찰을 중지합니다.
		/// </summary>
		void StopObservingAll(object subscriber = null);
	}

	/// <inheritdoc />
	public interface IObservableList<T> : IObservableListReader<T>
	{
		/// <summary>
		/// 리스트에서 주어진 <paramref name="index"/>를 변경합니다. 데이터가 존재하지 않으면 추가됩니다.
		/// 데이터를 수신 중인 모든 옵저버에게 알립니다
		/// </summary>
		new T this[int index] { get; set; }

		/// <inheritdoc cref="List{T}.Add"/>
		void Add(T data);

		/// <inheritdoc cref="List{T}.Remove"/>
		bool Remove(T data);

		/// <inheritdoc cref="List{T}.RemoveAt"/>
		void RemoveAt(int index);

		/// <inheritdoc cref="List{T}.Clear"/>
		void Clear();

		/// <remarks>
		/// 이 리스트에서 주어진 <paramref name="index"/>를 관찰 중인 모든 업데이트 메서드를 호출합니다
		/// </remarks>
		void InvokeUpdate(int index);
	}

	/// <inheritdoc />
	public partial class ObservableList<T> : IObservableList<T>, IBatchable, IComputedDependency
	{
		private readonly ObserverList<Action<int, T, T, ObservableUpdateType>> _updateActions = new ObserverList<Action<int, T, T, ObservableUpdateType>>();
		private readonly ObserverList<Action> _dependencyActions = new ObserverList<Action>();
		private bool _isBatching;

		// 플레이어 빌드에서 호출이 컴파일 제외되도록 partial 메서드로 선언됩니다.
		partial void EditorDebug_Register();

		/// <inheritdoc cref="IObservableList{T}.this" />
		public virtual T this[int index]
		{
			get
			{
				ComputedTracker.OnRead(this);
				return List[index];
			}
			set
			{
				var previousValue = List[index];

				List[index] = value;

				InvokeUpdate(index, previousValue);
			}
		}

		/// <inheritdoc />
		public int Count
		{
			get
			{
				ComputedTracker.OnRead(this);
				return List.Count;
			}
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
		public IReadOnlyList<T> ReadOnlyList => new List<T>(List);

		protected virtual List<T> List { get; set; }

		protected ObservableList()
		{
			EditorDebug_Register();
		}

		public ObservableList(IList<T> list)
		{
			List = list as List<T> ?? list.ToList();
			EditorDebug_Register();
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

				// 한 항목의 구독자 예외로 나머지 항목 통지가 멈추지 않도록 끝까지 알린 뒤 다시 던집니다
				var errors = new NotifyErrors();

				using (var updates = _updateActions.TakeSnapshot())
				{
					for (var i = 0; i < List.Count; i++)
					{
						// 콜백이 새 배치를 열었으면 남은 항목은 그 배치가 끝날 때 알립니다
						if (_isBatching)
						{
							break;
						}

						updates.Notify(i, default(T), List[i], ObservableUpdateType.Updated, ref errors);
					}
				}

				// 배치 안에서 비워 항목이 없어도 의존 값은 갱신합니다. 새 배치가 열렸으면 그 배치가 끝날 때 갱신합니다
				if (!_isBatching)
				{
					_dependencyActions.NotifyAll(ref errors);
				}

				errors.ThrowIfAny();
			}
		}

		/// <summary>
		/// 기존 옵저버를 잃지 않고 이 리스트를 새 리스트에 리바인딩합니다.
		/// </summary>
		/// <param name="list">바인딩할 새 리스트입니다</param>
		public void Rebind(IList<T> list)
		{
			List = list as List<T> ?? list.ToList();
		}

		/// <inheritdoc cref="List{T}.GetEnumerator"/>
		public List<T>.Enumerator GetEnumerator()
		{
			return List.GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return List.GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return List.GetEnumerator();
		}

		/// <inheritdoc />
		public bool Contains(T value)
		{
			ComputedTracker.OnRead(this);
			return List.Contains(value);
		}

		/// <inheritdoc />
		public int IndexOf(T value)
		{
			ComputedTracker.OnRead(this);
			return List.IndexOf(value);
		}

		/// <inheritdoc />
		public virtual void Add(T data)
		{
			List.Add(data);

			if (_isBatching)
			{
				return;
			}

			var errors = new NotifyErrors();
			Notify(List.Count - 1, default, data, ObservableUpdateType.Added, ref errors);
			errors.ThrowIfAny();
		}

		/// <inheritdoc />
		public bool Remove(T data)
		{
			var idx = List.IndexOf(data);

			if (idx >= 0)
			{
				RemoveAt(idx);

				return true;
			}

			return false;
		}

		/// <inheritdoc />
		public virtual void RemoveAt(int index)
		{
			var data = List[index];

			List.RemoveAt(index);

			if (_isBatching)
			{
				return;
			}

			var errors = new NotifyErrors();
			Notify(index, data, default, ObservableUpdateType.Removed, ref errors);
			errors.ThrowIfAny();
		}

		/// <inheritdoc />
		public virtual void Clear()
		{
			if (_isBatching)
			{
				List.Clear();
				return;
			}

			var errors = new NotifyErrors();

			// 연산 시작 시점의 구독자에게만 알립니다. 콜백 안에서 새로 구독해도 이번 Clear 는 받지 않습니다
			using (var updates = _updateActions.TakeSnapshot())
			{
				for (var j = 0; j < List.Count; j++)
				{
					updates.Notify(j, List[j], default(T), ObservableUpdateType.Removed, ref errors);
				}
			}

			List.Clear();

			// 의존 값(ComputedField)이 비워진 뒤의 상태로 다시 계산되도록, 비운 다음 그 시점의 의존 관계 전체에 알립니다.
			// 콜백이 새 배치를 열었으면 그 배치가 끝날 때 알립니다
			if (!_isBatching)
			{
				_dependencyActions.NotifyAll(ref errors);
			}

			errors.ThrowIfAny();
		}

		/// <inheritdoc />
		public void InvokeUpdate(int index)
		{
			InvokeUpdate(index, List[index]);
		}

		/// <inheritdoc />
		public void Observe(Action<int, T, T, ObservableUpdateType> onUpdate)
		{
			_updateActions.Add(onUpdate);
		}

		/// <inheritdoc />
		public void InvokeObserve(int index, Action<int, T, T, ObservableUpdateType> onUpdate)
		{
			Observe(onUpdate);
			InvokeUpdate(index);
		}

		/// <inheritdoc />
		public void StopObserving(Action<int, T, T, ObservableUpdateType> onUpdate)
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

			_updateActions.RemoveByTarget(subscriber);
		}

		protected void InvokeUpdate(int index, T previousValue)
		{
			if (_isBatching)
			{
				return;
			}

			var errors = new NotifyErrors();
			Notify(index, previousValue, List[index], ObservableUpdateType.Updated, ref errors);
			errors.ThrowIfAny();
		}

		private void Notify(int index, T previousValue, T value, ObservableUpdateType updateType, ref NotifyErrors errors)
		{
			using (var updates = _updateActions.TakeSnapshot())
			{
				updates.Notify(index, previousValue, value, updateType, ref errors);
			}

			_dependencyActions.NotifyAll(ref errors);
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
				kind: "List",
				valueGetter: () => $"Count: {Count}",
				subscriberCountGetter: () => _updateActions.Count);
		}
#endif
	}
}
