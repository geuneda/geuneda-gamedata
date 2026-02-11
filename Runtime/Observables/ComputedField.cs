using System;
using System.Collections.Generic;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 의존성 추적을 위해 읽히고 있을 때 알릴 수 있는 객체의 인터페이스입니다.
	/// </summary>
	internal interface IComputedDependency
	{
		/// <summary>
		/// 이 의존성의 값이 변경될 때 알림을 받도록 구독합니다.
		/// </summary>
		void Subscribe(Action onDependencyChanged);

		/// <summary>
		/// 이 의존성의 변경 알림 구독을 해제합니다.
		/// </summary>
		void Unsubscribe(Action onDependencyChanged);
	}

	/// <summary>
	/// 리플렉션 없이 의존성 추적을 받기 위한 계산 필드의 내부 인터페이스입니다.
	/// </summary>
	internal interface IComputedFieldInternal
	{
		/// <summary>
		/// 이 계산 필드가 의존하는 의존성을 등록합니다.
		/// 의존성이 변경되면, 이 계산 필드는 더티로 표시되고 재계산됩니다.
		/// </summary>
		void AddDependency(IComputedDependency dependency);
	}

	/// <summary>
	/// 다른 Observable 필드에서 계산되는 필드입니다.
	/// 의존성 중 하나라도 변경되면 자동으로 업데이트됩니다.
	/// </summary>
	public partial class ComputedField<T> : IObservableFieldReader<T>, IDisposable, IBatchable, IComputedDependency, IComputedFieldInternal
	{
		private readonly Func<T> _computation;
		private readonly List<Action<T, T>> _updateActions = new List<Action<T, T>>();
		private readonly List<Action> _dependencyActions = new List<Action>();
		private readonly HashSet<IComputedDependency> _dependencies = new HashSet<IComputedDependency>();
		private T _value;
		private bool _isDirty = true;
		private bool _isBatching;

		// 플레이어 빌드에서 호출이 컴파일 제외되도록 partial 메서드로 선언됩니다.
		partial void EditorDebug_Register();

		/// <inheritdoc />
		public T Value
		{
			get
			{
				if (_isDirty)
				{
					Recompute();
				}
				ComputedTracker.OnRead(this);
				return _value;
			}
		}

		public ComputedField(Func<T> computation)
		{
			_computation = computation;
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
			if (!_isBatching)
			{
				_isBatching = true;
			}
		}

		/// <inheritdoc />
		void IBatchable.ResumeNotifications()
		{
			if (_isBatching)
			{
				_isBatching = false;
				InvokeUpdate();
			}
		}

		/// <inheritdoc />
		public void Observe(Action<T, T> onUpdate)
		{
			// 옵저버 추가 전에 의존성이 추적되는지 확인
			if (_isDirty)
			{
				Recompute();
			}
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
			if (_isBatching)
			{
				return;
			}

			_isDirty = true;
			var previousValue = _value;
			Recompute();
			
			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i].Invoke(previousValue, _value);
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}
		}

		private void Recompute()
		{
			// 계산 중 의존성 추적
			ComputedTracker.BeginTracking(this);
			try
			{
				_value = _computation();
			}
			finally
			{
				ComputedTracker.EndTracking();
			}

			_isDirty = false;
		}

		/// <inheritdoc />
		void IComputedFieldInternal.AddDependency(IComputedDependency dependency)
		{
			if (ReferenceEquals(dependency, this)) return;

			if (_dependencies.Add(dependency))
			{
				dependency.Subscribe(OnDependencyChanged);
			}
		}

		private void OnDependencyChanged()
		{
			if (!_isDirty)
			{
				_isDirty = true;
				InvokeUpdate();
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

		public void Dispose()
		{
			foreach (var dependency in _dependencies)
			{
				dependency.Unsubscribe(OnDependencyChanged);
			}
			_dependencies.Clear();
			_updateActions.Clear();
			_dependencyActions.Clear();
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
				kind: "Computed",
				valueGetter: () =>
				{
					object v = Value;
					return v?.ToString() ?? string.Empty;
				},
				subscriberCountGetter: () => _updateActions.Count);
		}
#endif
	}

	/// <summary>
	/// 계산 필드를 위한 정적 팩토리입니다.
	/// </summary>
	public static class ObservableField
	{
		/// <summary>
		/// 주어진 계산 함수에서 새 계산 필드를 생성합니다.
		/// </summary>
		public static ComputedField<T> Computed<T>(Func<T> computation)
		{
			return new ComputedField<T>(computation);
		}

		/// <summary>
		/// 두 Observable 필드를 결합하여 계산 결과를 생성합니다.
		/// 소스 필드 중 하나라도 변경되면 계산 필드가 자동으로 업데이트됩니다.
		/// </summary>
		public static ComputedField<TResult> Combine<T1, T2, TResult>(
			IObservableFieldReader<T1> first,
			IObservableFieldReader<T2> second,
			Func<T1, T2, TResult> combiner)
		{
			return new ComputedField<TResult>(() => combiner(first.Value, second.Value));
		}

		/// <summary>
		/// 세 Observable 필드를 결합하여 계산 결과를 생성합니다.
		/// 소스 필드 중 하나라도 변경되면 계산 필드가 자동으로 업데이트됩니다.
		/// </summary>
		public static ComputedField<TResult> Combine<T1, T2, T3, TResult>(
			IObservableFieldReader<T1> first,
			IObservableFieldReader<T2> second,
			IObservableFieldReader<T3> third,
			Func<T1, T2, T3, TResult> combiner)
		{
			return new ComputedField<TResult>(() => combiner(first.Value, second.Value, third.Value));
		}

		/// <summary>
		/// 네 Observable 필드를 결합하여 계산 결과를 생성합니다.
		/// 소스 필드 중 하나라도 변경되면 계산 필드가 자동으로 업데이트됩니다.
		/// </summary>
		public static ComputedField<TResult> Combine<T1, T2, T3, T4, TResult>(
			IObservableFieldReader<T1> first,
			IObservableFieldReader<T2> second,
			IObservableFieldReader<T3> third,
			IObservableFieldReader<T4> fourth,
			Func<T1, T2, T3, T4, TResult> combiner)
		{
			return new ComputedField<TResult>(() => combiner(first.Value, second.Value, third.Value, fourth.Value));
		}
	}

	internal static class ComputedTracker
	{
		[ThreadStatic]
		private static Stack<IComputedFieldInternal> _activeComputations;

		public static void BeginTracking(IComputedFieldInternal computation)
		{
			_activeComputations ??= new Stack<IComputedFieldInternal>();
			_activeComputations.Push(computation);
		}

		public static void EndTracking()
		{
			_activeComputations?.Pop();
		}

		public static void OnRead(IComputedDependency dependency)
		{
			// 빠른 경로: 추적 진행 중 아님 (가장 일반적인 경우)
			if (_activeComputations == null || _activeComputations.Count == 0)
			{
				return;
			}

			_activeComputations.Peek().AddDependency(dependency);
		}
	}
}
