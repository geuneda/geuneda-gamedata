using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// <see cref="ObservableUpdateType"/> 규칙에 정의된 요소 변경을 관찰할 수 있는 간단한 딕셔너리입니다
	/// </summary>
	public interface IObservableDictionary : IEnumerable
	{
		/// <summary>
		/// 이 딕셔너리의 요소 수를 요청합니다
		/// </summary>
		int Count { get; }

		/// <summary>
		/// 이 딕셔너리에서 요소를 업데이트할 때 수행되는 Observable 업데이트 구성을 정의합니다
		/// </summary>
		ObservableUpdateFlag ObservableUpdateFlag { get; set; }

		/// <summary>
		/// 이 딕셔너리의 배치 업데이트를 시작합니다.
		/// 반환된 객체가 해제될 때까지 알림이 억제됩니다.
		/// </summary>
		IDisposable BeginBatch();
	}

	/// <inheritdoc cref="IObservableDictionary"/>
	/// <remarks>
	/// 이 딕셔너리는 요소 읽기만 허용하고 수정은 허용하지 않습니다
	/// </remarks>
	public interface IObservableDictionaryReader<TKey, TValue> : IObservableDictionary, IEnumerable<KeyValuePair<TKey, TValue>>
	{
		/// <summary>
		/// 주어진 <paramref name="key"/>에 연결된 데이터를 조회하고 반환합니다
		/// </summary>
		TValue this[TKey key] { get; }

		/// <summary>
		/// 이 딕셔너리를 <see cref="IReadOnlyDictionary{TKey,TValue}"/>로 요청합니다
		/// </summary>
		ReadOnlyDictionary<TKey, TValue> ReadOnlyDictionary { get; }

		/// <inheritdoc cref="Dictionary{TKey,TValue}.TryGetValue" />
		bool TryGetValue(TKey key, out TValue value);

		/// <inheritdoc cref="Dictionary{TKey,TValue}.ContainsKey" />
		bool ContainsKey(TKey key);

		/// <summary>
		/// 주어진 <paramref name="onUpdate"/>로 이 딕셔너리의 변경을 관찰합니다
		/// </summary>
		/// <remarks>
		/// <see cref="this.ObservableUpdateFlag"/>가 <see cref="ObservableUpdateFlag.KeyUpdateOnly"/>로 설정되지 않아야 합니다
		/// </remarks>
		void Observe(Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate);

		/// <summary>
		/// 주어진 <paramref name="key"/>의 데이터가 변경될 때
		/// 주어진 <paramref name="onUpdate"/>로 이 딕셔너리의 변경을 관찰합니다
		/// </summary>
		/// <remarks>
		/// <see cref="this.ObservableUpdateFlag"/>가 <see cref="ObservableUpdateFlag.UpdateOnly"/>로 설정되지 않아야 합니다
		/// </remarks>
		void Observe(TKey key, Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate);

		/// <inheritdoc cref="Observe(TKey,System.Action{TKey,TValue,TValue,Geuneda.DataExtensions.ObservableUpdateType})" />
		/// <remarks>
		/// 이 딕셔너리를 관찰하기 시작하기 전에 주어진 <paramref name="onUpdate"/> 메서드를 호출합니다
		/// </remarks>
		/// <remarks>
		/// <see cref="this.ObservableUpdateFlag"/>가 <see cref="ObservableUpdateFlag.UpdateOnly"/>로 설정되지 않아야 합니다
		/// </remarks>
		void InvokeObserve(TKey key, Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate);

		/// <summary>
		/// 주어진 <paramref name="onUpdate"/>로 이 딕셔너리의 모든 데이터 변경 관찰을 중지합니다
		/// </summary>
		/// <remarks>
		/// <see cref="this.ObservableUpdateFlag"/>가 <see cref="ObservableUpdateFlag.KeyUpdateOnly"/>로 설정되지 않아야 합니다
		/// </remarks>
		void StopObserving(Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate);

		/// <summary>
		/// 주어진 <paramref name="key"/>에 대한 이 딕셔너리 업데이트 관찰을 중지합니다
		/// </summary>
		void StopObserving(TKey key);

		/// <summary>
		/// 주어진 <paramref name="subscriber"/> 호출의 모든 딕셔너리 변경 관찰을 중지합니다.
		/// 주어진 <paramref name="subscriber"/>가 null이면 모든 관찰을 중지합니다.
		/// </summary>
		void StopObservingAll(object subscriber = null);
	}

	/// <inheritdoc cref="IObservableDictionary"/>
	public interface IObservableDictionary<TKey, TValue> : IObservableDictionaryReader<TKey, TValue>
	{
		/// <summary>
		/// 딕셔너리에서 주어진 <paramref name="key"/>를 변경합니다.
		/// 데이터를 수신 중인 모든 옵저버에게 알립니다
		/// </summary>
		new TValue this[TKey key] { get; set; }

		/// <inheritdoc cref="Dictionary{TKey,TValue}.Add" />
		void Add(TKey key, TValue value);

		/// <inheritdoc cref="Dictionary{TKey,TValue}.Remove" />
		bool Remove(TKey key);

		/// <inheritdoc cref="Dictionary{TKey,TValue}.Clear"/>
		void Clear();

		/// <remarks>
		/// 이 딕셔너리에서 주어진 <paramref name="key"/>를 관찰 중인 모든 업데이트 메서드를 호출합니다
		/// </remarks>
		void InvokeUpdate(TKey key);
	}

	/// <inheritdoc />
	public partial class ObservableDictionary<TKey, TValue> : IObservableDictionary<TKey, TValue>, IBatchable, IComputedDependency
	{
		private readonly IDictionary<TKey, ObserverList<Action<TKey, TValue, TValue, ObservableUpdateType>>> _keyUpdateActions =
			new Dictionary<TKey, ObserverList<Action<TKey, TValue, TValue, ObservableUpdateType>>>();
		private readonly ObserverList<Action<TKey, TValue, TValue, ObservableUpdateType>> _updateActions =
			new ObserverList<Action<TKey, TValue, TValue, ObservableUpdateType>>();
		private readonly ObserverList<Action> _dependencyActions = new ObserverList<Action>();
		private bool _isBatching;

		// 플레이어 빌드에서 호출이 컴파일 제외되도록 partial 메서드로 선언됩니다.
		partial void EditorDebug_Register();

		/// <inheritdoc />
		public int Count
		{
			get
			{
				ComputedTracker.OnRead(this);
				return Dictionary.Count;
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
				var keyUpdates = TakeKeySnapshots();

				try
				{
					using var updates = ObservableUpdateFlag != ObservableUpdateFlag.KeyUpdateOnly
						? _updateActions.TakeSnapshot()
						: default;

					// 콜백이 새 배치를 열었으면 남은 항목은 그 배치가 끝날 때 알립니다
					foreach (var entry in keyUpdates)
					{
						if (_isBatching)
						{
							break;
						}

						// 앞선 콜백이 값을 바꾸거나 키를 지웠을 수 있으므로 알리는 시점의 값을 읽습니다
						if (!Dictionary.TryGetValue(entry.Key, out var value))
						{
							continue;
						}

						entry.Snapshot.Notify(entry.Key, default(TValue), value, ObservableUpdateType.Updated, ref errors);
					}

					if (ObservableUpdateFlag != ObservableUpdateFlag.KeyUpdateOnly && !_isBatching)
					{
						foreach (var pair in Dictionary)
						{
							updates.Notify(pair.Key, default(TValue), pair.Value, ObservableUpdateType.Updated, ref errors);

							// 새 배치에서 사전이 바뀌었을 수 있으므로 다음 항목으로 넘어가기(열거자 검사) 전에 멈춥니다
							if (_isBatching)
							{
								break;
							}
						}
					}
				}
				finally
				{
					DisposeKeySnapshots(keyUpdates);
				}

				// 배치 안에서 비워 항목이 없어도 의존 값은 갱신합니다. 새 배치가 열렸으면 그 배치가 끝날 때 갱신합니다
				if (!_isBatching)
				{
					_dependencyActions.NotifyAll(ref errors);
				}

				errors.ThrowIfAny();
			}
		}

		/// <inheritdoc />
		public ObservableUpdateFlag ObservableUpdateFlag { get; set; }
		/// <inheritdoc />
		public ReadOnlyDictionary<TKey, TValue> ReadOnlyDictionary => new ReadOnlyDictionary<TKey, TValue>(Dictionary);

		protected virtual IDictionary<TKey, TValue> Dictionary { get; set; }

		private ObservableDictionary()
		{
			EditorDebug_Register();
		}

		public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
		{
			Dictionary = dictionary;
			ObservableUpdateFlag = ObservableUpdateFlag.KeyUpdateOnly;
			EditorDebug_Register();
		}

		/// <summary>
		/// 기존 옵저버를 잃지 않고 이 딕셔너리를 새 딕셔너리에 리바인딩합니다.
		/// </summary>
		/// <param name="dictionary">바인딩할 새 딕셔너리입니다</param>
		public void Rebind(IDictionary<TKey, TValue> dictionary)
		{
			Dictionary = dictionary;
		}

		/// <inheritdoc cref="Dictionary{TKey,TValue}.this" />
		public virtual TValue this[TKey key]
		{
			get
			{
				ComputedTracker.OnRead(this);
				return Dictionary[key];
			}
			set
			{
				var previousValue = Dictionary[key];

				Dictionary[key] = value;

				InvokeUpdate(key, previousValue);
			}
		}

		/// <inheritdoc />
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return Dictionary.GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <inheritdoc />
		public bool TryGetValue(TKey key, out TValue value)
		{
			ComputedTracker.OnRead(this);
			return Dictionary.TryGetValue(key, out value);
		}

		/// <inheritdoc />
		public bool ContainsKey(TKey key)
		{
			ComputedTracker.OnRead(this);
			return Dictionary.ContainsKey(key);
		}

		/// <inheritdoc />
		public virtual void Add(TKey key, TValue value)
		{
			Dictionary.Add(key, value);

			if (_isBatching)
			{
				return;
			}

			var errors = new NotifyErrors();
			Notify(key, default, value, ObservableUpdateType.Added, ref errors);
			errors.ThrowIfAny();
		}

		/// <inheritdoc />
		public virtual bool Remove(TKey key)
		{
			if (!Dictionary.TryGetValue(key, out var value) || !Dictionary.Remove(key))
			{
				return false;
			}

			if (_isBatching)
			{
				return true;
			}

			var errors = new NotifyErrors();
			Notify(key, value, default, ObservableUpdateType.Removed, ref errors);
			errors.ThrowIfAny();

			return true;
		}

		/// <inheritdoc />
		public virtual void Clear()
		{
			if (_isBatching)
			{
				Dictionary.Clear();
				return;
			}

			var errors = new NotifyErrors();

			// 연산 시작 시점의 구독자로 스냅샷을 고정합니다. 콜백 안에서 새로 구독해도 이번 Clear 는 받지 않습니다
			var keyUpdates = TakeKeySnapshots();

			try
			{
				using var updates = ObservableUpdateFlag != ObservableUpdateFlag.KeyUpdateOnly
					? _updateActions.TakeSnapshot()
					: default;

				foreach (var entry in keyUpdates)
				{
					entry.Snapshot.Notify(entry.Key, entry.Value, default(TValue), ObservableUpdateType.Removed, ref errors);
				}

				if (ObservableUpdateFlag != ObservableUpdateFlag.KeyUpdateOnly)
				{
					foreach (var pair in Dictionary)
					{
						updates.Notify(pair.Key, pair.Value, default(TValue), ObservableUpdateType.Removed, ref errors);
					}
				}
			}
			finally
			{
				DisposeKeySnapshots(keyUpdates);
			}

			Dictionary.Clear();

			// 의존 값(ComputedField)이 비워진 뒤의 상태로 다시 계산되도록, 비운 다음 그 시점의 의존 관계 전체에 알립니다.
			// 콜백이 새 배치를 열었으면 그 배치가 끝날 때 알립니다
			if (!_isBatching)
			{
				_dependencyActions.NotifyAll(ref errors);
			}

			errors.ThrowIfAny();
		}

		/// <summary>
		/// 키 구독이 있고 사전에 있는 키마다 (키, 현재 값, 키 구독 스냅샷)을 모읍니다.
		/// 미리 구독했지만 사전에 없는 키는 알릴 값이 없으므로 넣지 않습니다
		/// </summary>
		private List<(TKey Key, TValue Value, ObserverList<Action<TKey, TValue, TValue, ObservableUpdateType>>.Snapshot Snapshot)> TakeKeySnapshots()
		{
			var keyUpdates = new List<(TKey Key, TValue Value, ObserverList<Action<TKey, TValue, TValue, ObservableUpdateType>>.Snapshot Snapshot)>();

			if (ObservableUpdateFlag == ObservableUpdateFlag.UpdateOnly)
			{
				return keyUpdates;
			}

			foreach (var pair in _keyUpdateActions)
			{
				if (Dictionary.TryGetValue(pair.Key, out var value))
				{
					keyUpdates.Add((pair.Key, value, pair.Value.TakeSnapshot()));
				}
			}

			return keyUpdates;
		}

		private static void DisposeKeySnapshots(
			List<(TKey Key, TValue Value, ObserverList<Action<TKey, TValue, TValue, ObservableUpdateType>>.Snapshot Snapshot)> keyUpdates)
		{
			foreach (var entry in keyUpdates)
			{
				entry.Snapshot.Dispose();
			}
		}

		/// <inheritdoc />
		public void InvokeUpdate(TKey key)
		{
			InvokeUpdate(key, Dictionary[key]);
		}

		/// <inheritdoc />
		public void StopObserving(TKey key)
		{
			if (_keyUpdateActions.TryGetValue(key, out var actions))
			{
				// 진행 중인 통지에서도 이 키의 구독자가 빠지도록 등록을 먼저 비활성화합니다
				actions.Clear();
				_keyUpdateActions.Remove(key);
			}
		}

		/// <inheritdoc />
		public void Observe(Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate)
		{
			_updateActions.Add(onUpdate);
		}

		/// <inheritdoc />
		public void Observe(TKey key, Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate)
		{
			if (!_keyUpdateActions.TryGetValue(key, out var listeners))
			{
				listeners = new ObserverList<Action<TKey, TValue, TValue, ObservableUpdateType>>();
				_keyUpdateActions.Add(key, listeners);
			}

			listeners.Add(onUpdate);
		}

		/// <inheritdoc />
		public void InvokeObserve(TKey key, Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate)
		{
			Observe(key, onUpdate);
			InvokeUpdate(key);
		}

		/// <inheritdoc />
		public void StopObserving(Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate)
		{
			foreach (var actions in _keyUpdateActions)
			{
				actions.Value.Remove(onUpdate);
			}

			_updateActions.Remove(onUpdate);
		}

		/// <inheritdoc />
		public void StopObservingAll(object subscriber = null)
		{
			if (subscriber == null)
			{
				// 진행 중인 통지에서도 빠지도록 키별 등록을 먼저 비활성화합니다
				foreach (var actions in _keyUpdateActions)
				{
					actions.Value.Clear();
				}

				_keyUpdateActions.Clear();
				_updateActions.Clear();
				return;
			}

			foreach (var actions in _keyUpdateActions)
			{
				actions.Value.RemoveByTarget(subscriber);
			}

			_updateActions.RemoveByTarget(subscriber);
		}

		protected void InvokeUpdate(TKey key, TValue previousValue)
		{
			if (_isBatching)
			{
				return;
			}

			var errors = new NotifyErrors();
			Notify(key, previousValue, Dictionary[key], ObservableUpdateType.Updated, ref errors);
			errors.ThrowIfAny();
		}

		/// <summary>
		/// 키 하나의 변경을 키 구독자, 전체 구독자, 의존 순서로 알립니다.
		/// 키·전체 구독 스냅샷은 시작 시점에 고정하고, 의존 갱신은 알리는 시점의 의존 관계 전체에 보냅니다
		/// </summary>
		private void Notify(TKey key, TValue previousValue, TValue value, ObservableUpdateType updateType, ref NotifyErrors errors)
		{
			using (var keyUpdates = ObservableUpdateFlag != ObservableUpdateFlag.UpdateOnly && _keyUpdateActions.TryGetValue(key, out var actions)
				       ? actions.TakeSnapshot()
				       : default)
			using (var updates = ObservableUpdateFlag != ObservableUpdateFlag.KeyUpdateOnly
				       ? _updateActions.TakeSnapshot()
				       : default)
			{
				keyUpdates.Notify(key, previousValue, value, updateType, ref errors);
				updates.Notify(key, previousValue, value, updateType, ref errors);
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
				kind: "Dictionary",
				valueGetter: () => $"Count: {Count}",
				subscriberCountGetter: () => _updateActions.Count);
		}
#endif
	}
}
