using System;
using System.Collections.Generic;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// <see cref="ObservableUpdateType"/> 규칙에 정의된 요소 변경을 관찰할 수 있는 필드입니다
	/// </summary>
	public interface IObservableFieldReader<out T>
	{
		/// <summary>
		/// 필드 값
		/// </summary>
		T Value { get; }

		/// <summary>
		/// 이 필드의 배치 업데이트를 시작합니다.
		/// 반환된 객체가 해제될 때까지 알림이 억제됩니다.
		/// </summary>
		IDisposable BeginBatch();

		/// <summary>
		/// 데이터가 변경될 때 주어진 <paramref name="onUpdate"/>로 이 필드를 관찰합니다
		/// </summary>
		void Observe(Action<T, T> onUpdate);

		/// <inheritdoc cref="Observe" />
		/// <remarks>
		/// 이 필드를 관찰하기 시작하기 전에 주어진 <paramref name="onUpdate"/> 메서드를 호출합니다
		/// </remarks>
		void InvokeObserve(Action<T, T> onUpdate);

		/// <summary>
		/// 주어진 <paramref name="onUpdate"/>로 이 필드의 모든 데이터 변경 관찰을 중지합니다
		/// </summary>
		void StopObserving(Action<T, T> onUpdate);

		/// <summary>
		/// 주어진 <paramref name="subscriber"/> 호출의 모든 필드 관찰을 중지합니다.
		/// 주어진 <paramref name="subscriber"/>가 null이면 모든 관찰을 중지합니다.
		/// </summary>
		void StopObservingAll(object subscriber = null);

		/// <remarks>
		/// 이 필드를 관찰 중인 모든 업데이트 메서드를 호출합니다
		/// </remarks>
		void InvokeUpdate();
	}

	/// <inheritdoc />
	public interface IObservableField<T> : IObservableFieldReader<T>
	{
		/// <summary>
		/// 변경될 수 있는 필드 값
		/// </summary>
		new T Value { get; set; }

		/// <summary>
		/// 기존 옵저버를 잃지 않고 이 필드를 새 값에 리바인딩합니다.
		/// </summary>
		/// <param name="initialValue">필드의 새 초기 값입니다</param>
		void Rebind(T initialValue);
	}

	/// <inheritdoc />
	public partial class ObservableField<T> : IObservableField<T>, IBatchable, IComputedDependency
	{
		private readonly IList<Action<T, T>> _updateActions = new List<Action<T, T>>();
		private readonly List<Action> _dependencyActions = new List<Action>();

		private T _value;
		private bool _isBatching;
		private T _batchPreviousValue;

		/// <inheritdoc cref="IObservableField{T}.Value" />
		public virtual T Value
		{
			get
			{
				ComputedTracker.OnRead(this);
				return _value;
			}
			set
			{
				var previousValue = _value;

				_value = value;
				InvokeUpdate(previousValue);
			}
		}

		public ObservableField()
		{
			_value = default;
			EditorDebug_Register();
		}

		public ObservableField(T initialValue)
		{
			_value = initialValue;
			EditorDebug_Register();
		}

		public static implicit operator T(ObservableField<T> value) => value.Value;

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
			if (!_isBatching)
			{
				_isBatching = true;
				_batchPreviousValue = _value;
			}
		}

		/// <inheritdoc />
		void IBatchable.ResumeNotifications()
		{
			if (_isBatching)
			{
				_isBatching = false;
				InvokeUpdate(_batchPreviousValue);
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
		public void Rebind(T initialValue)
		{
			_value = initialValue;
		}

		/// <inheritdoc />
		public void Observe(Action<T, T> onUpdate)
		{
			_updateActions.Add(onUpdate);
		}

		/// <inheritdoc />
		public void InvokeObserve(Action<T, T> onUpdate)
		{
			onUpdate(Value, Value);

			Observe(onUpdate);
		}

		/// <inheritdoc />
		public void StopObserving(Action<T, T> onUpdate)
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
		public void InvokeUpdate()
		{
			InvokeUpdate(Value);
		}

		/// <summary>
		/// 의존성 추적을 트리거하지 않고 현재 값을 가져옵니다.
		/// 값을 다르게 저장하는 파생 클래스에서 재정의합니다.
		/// </summary>
		protected virtual T GetCurrentValue() => _value;

		protected void InvokeUpdate(T previousValue)
		{
			if (_isBatching)
			{
				return;
			}

			// 반복적인 Value 게터 호출을 피하기 위해 값을 캐싱합니다(ComputedTracker.OnRead를 트리거하므로)
			var currentValue = GetCurrentValue();

			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i].Invoke(previousValue, currentValue);
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}
		}

		// 플레이어 빌드에서 호출이 컴파일 제외되도록 partial 메서드로 선언됩니다.
		partial void EditorDebug_Register();

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
				kind: "Field",
				valueGetter: () =>
				{
					object v = Value;
					return v?.ToString() ?? string.Empty;
				},
				subscriberCountGetter: () => _updateActions.Count);
		}
#endif
	}
}
