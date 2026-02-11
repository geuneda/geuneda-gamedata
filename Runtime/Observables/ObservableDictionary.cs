using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

		/// <inheritdoc cref="Observe(TKey,System.Action{TKey,TValue,TValue,Geuneda.Observables.ObservableUpdateType})" />
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
		private readonly IDictionary<TKey, IList<Action<TKey, TValue, TValue, ObservableUpdateType>>> _keyUpdateActions =
			new Dictionary<TKey, IList<Action<TKey, TValue, TValue, ObservableUpdateType>>>();
		private readonly IList<Action<TKey, TValue, TValue, ObservableUpdateType>> _updateActions =
			new List<Action<TKey, TValue, TValue, ObservableUpdateType>>();
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
				foreach (var pair in Dictionary)
				{
					InvokeUpdate(pair.Key, default);
				}
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
		public TValue this[TKey key]
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

			if (ObservableUpdateFlag != ObservableUpdateFlag.UpdateOnly && _keyUpdateActions.TryGetValue(key, out var actions))
			{
				for (var i = 0; i < actions.Count; i++)
				{
					actions[i](key, default, value, ObservableUpdateType.Added);
				}
			}

			if (ObservableUpdateFlag != ObservableUpdateFlag.KeyUpdateOnly)
			{
				for (var i = 0; i < _updateActions.Count; i++)
				{
					_updateActions[i](key, default, value, ObservableUpdateType.Added);
				}
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}
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

			if (ObservableUpdateFlag != ObservableUpdateFlag.UpdateOnly && _keyUpdateActions.TryGetValue(key, out var actions))
			{
				for (var i = actions.Count - 1; i > -1; i--)
				{
					var action = actions[i];

					action(key, value, default, ObservableUpdateType.Removed);

					// 액션이 구독 해제된 경우 인덱스를 이동합니다
					i = AdjustIndex(i, action, actions);
				}
			}
			if (ObservableUpdateFlag != ObservableUpdateFlag.KeyUpdateOnly)
			{
				for (var i = _updateActions.Count - 1; i > -1; i--)
				{
					var action = _updateActions[i];

					action(key, value, default, ObservableUpdateType.Removed);

					// 액션이 구독 해제된 경우 인덱스를 이동합니다
					i = AdjustIndex(i, action, _updateActions);
				}
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}

			return true;
		}

		/// <inheritdoc />
		public virtual void Clear()
		{
			if (!_isBatching)
			{
				if (ObservableUpdateFlag != ObservableUpdateFlag.UpdateOnly)
				{
					// 콜백 중 하나가 목록을 수정하는 경우에 대비하여 복사본을 생성합니다(예: 구독자 제거)
					var copy = new Dictionary<TKey, IList<Action<TKey, TValue, TValue, ObservableUpdateType>>>(_keyUpdateActions);

					foreach (var data in copy)
					{
						var listCopy = data.Value.ToList();
						for (var i = 0; i < listCopy.Count; i++)
						{
							listCopy[i](data.Key, Dictionary[data.Key], default, ObservableUpdateType.Removed);
						}
					}
				}

				if (ObservableUpdateFlag != ObservableUpdateFlag.KeyUpdateOnly)
				{
					foreach (var data in Dictionary)
					{
						var listCopy = _updateActions.ToList();
						for (var i = 0; i < listCopy.Count; i++)
						{
							listCopy[i](data.Key, data.Value, default, ObservableUpdateType.Removed);
						}
					}
				}

				for (var i = 0; i < _dependencyActions.Count; i++)
				{
					_dependencyActions[i].Invoke();
				}
			}

			Dictionary.Clear();
		}

		/// <inheritdoc />
		public void InvokeUpdate(TKey key)
		{
			InvokeUpdate(key, Dictionary[key]);
		}

		/// <inheritdoc />
		public void StopObserving(TKey key)
		{
			_keyUpdateActions.Remove(key);
		}

		/// <inheritdoc />
		public void Observe(Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate)
		{
			_updateActions.Add(onUpdate);
		}

		/// <inheritdoc />
		public void Observe(TKey key, Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate)
		{
			var list = new List<Action<TKey, TValue, TValue, ObservableUpdateType>> { onUpdate };

			if (_keyUpdateActions.TryGetValue(key, out var listeners))
			{
				listeners.Add(onUpdate);
			}
			else
			{
				_keyUpdateActions.Add(key, new List<Action<TKey, TValue, TValue, ObservableUpdateType>> { onUpdate });
			}
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
				for (var i = actions.Value.Count - 1; i > -1; i--)
				{
					if (actions.Value[i] == onUpdate)
					{
						actions.Value.RemoveAt(i);
						break;
					}
				}
			}

			for (var i = _updateActions.Count - 1; i > -1; i--)
			{
				if (_updateActions[i] == onUpdate)
				{
					_updateActions.RemoveAt(i);
					break;
				}
			}
		}

		/// <inheritdoc />
		public void StopObservingAll(object subscriber = null)
		{
			if (subscriber == null)
			{
				_keyUpdateActions.Clear();
				_updateActions.Clear();
				return;
			}

			foreach (var actions in _keyUpdateActions)
			{
				for (var i = actions.Value.Count - 1; i > -1; i--)
				{
					if (actions.Value[i].Target == subscriber)
					{
						actions.Value.RemoveAt(i);
					}
				}
			}

			for (var i = _updateActions.Count - 1; i > -1; i--)
			{
				if (_updateActions[i].Target == subscriber)
				{
					_updateActions.RemoveAt(i);
				}
			}
		}

		protected void InvokeUpdate(TKey key, TValue previousValue)
		{
			if (_isBatching)
			{
				return;
			}

			var value = Dictionary[key];

			if (ObservableUpdateFlag != ObservableUpdateFlag.UpdateOnly && _keyUpdateActions.TryGetValue(key, out var actions))
			{
				for (var i = 0; i < actions.Count; i++)
				{
					actions[i](key, previousValue, value, ObservableUpdateType.Updated);
				}
			}

			if (ObservableUpdateFlag != ObservableUpdateFlag.KeyUpdateOnly)
			{
				for (var i = 0; i < _updateActions.Count; i++)
				{
					_updateActions[i](key, previousValue, value, ObservableUpdateType.Updated);
				}
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}
		}

		private int AdjustIndex(int index, Action<TKey, TValue, TValue, ObservableUpdateType> action,
			IList<Action<TKey, TValue, TValue, ObservableUpdateType>> list)
		{
			if (index < list.Count && list[index] == action)
			{
				return index;
			}

			for (var i = index - 1; i > -1; i--)
			{
				if (list[i] == action)
				{
					return i;
				}
			}

			return index + 1;
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
