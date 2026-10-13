using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 통지 중 구독자가 던진 예외를 모아 두었다가 통지가 끝난 뒤 다시 던집니다
	/// </summary>
	internal struct NotifyErrors
	{
		private Exception _first;
		private List<Exception> _all;

		public void Add(Exception exception)
		{
			if (_first == null)
			{
				_first = exception;
				return;
			}

			_all ??= new List<Exception> { _first };
			_all.Add(exception);
		}

		/// <summary>
		/// 예외가 하나면 원래 스택 정보를 유지한 채 다시 던지고, 여럿이면 <see cref="AggregateException"/> 으로 던집니다
		/// </summary>
		public void ThrowIfAny()
		{
			if (_all != null)
			{
				throw new AggregateException(_all);
			}

			if (_first != null)
			{
				ExceptionDispatchInfo.Capture(_first).Throw();
			}
		}
	}

	/// <summary>
	/// Observable 의 구독 목록입니다. 구독 한 건마다 등록 객체를 두고 해제하면 그 등록만 비활성으로 표시합니다.
	/// </summary>
	/// <remarks>
	/// 통지 규칙(모든 Observable 과 ComputedField 공통):
	/// - 변경 연산은 시작할 때 Observe 구독 목록의 스냅샷을 만들고, 스냅샷을 구독 순서대로 호출합니다.
	/// - 연산 도중 해제된 등록은 아직 호출 전이면 건너뜁니다. 같은 대리자를 여러 번 구독했으면 해제한 건만 빠집니다.
	/// - 연산 도중 새로 구독한 Observe 콜백은 그 연산의 통지를 받지 않고 다음 연산부터 받습니다.
	/// - 의존 갱신(ComputedField 무효화)은 내부 동작이라 알리는 시점의 의존 관계 전체에 보냅니다
	///   (<see cref="ObserverSnapshotExtensions.NotifyAll"/>). 콜백 안에서 처음 계산된 ComputedField 도 갱신됩니다.
	/// - 구독자가 예외를 던져도 나머지 구독자와 의존 통지는 계속 실행하고, 예외는 <see cref="NotifyErrors"/> 에 모읍니다.
	/// </remarks>
	internal sealed class ObserverList<TAction> where TAction : Delegate
	{
		internal sealed class Registration
		{
			public readonly TAction Action;
			public bool Active = true;

			public Registration(TAction action)
			{
				Action = action;
			}
		}

		/// <summary>
		/// 연산 시작 시점의 등록 스냅샷입니다. 다 쓰면 <see cref="Dispose"/> 로 풀에 돌려줍니다
		/// </summary>
		internal readonly struct Snapshot : IDisposable
		{
			public readonly Registration[] Items;
			public readonly int Count;

			public Snapshot(Registration[] items, int count)
			{
				Items = items;
				Count = count;
			}

			public void Dispose()
			{
				if (Items != null)
				{
					ArrayPool<Registration>.Shared.Return(Items, true);
				}
			}
		}

		private readonly List<Registration> _registrations = new List<Registration>();

		public int Count => _registrations.Count;

		public void Add(TAction action)
		{
			_registrations.Add(new Registration(action));
		}

		/// <summary>
		/// 같은 대리자의 첫 등록 한 건을 해제합니다(<see cref="List{T}.Remove"/> 와 같은 규칙)
		/// </summary>
		public bool Remove(TAction action)
		{
			for (var i = 0; i < _registrations.Count; i++)
			{
				// null 로 구독한 등록이 있어도 비교할 수 있도록 정적 Equals 를 씁니다
				if (Equals(_registrations[i].Action, action))
				{
					Deactivate(i);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// 대리자 대상이 <paramref name="subscriber"/> 인 등록을 모두 해제합니다
		/// </summary>
		public void RemoveByTarget(object subscriber)
		{
			for (var i = _registrations.Count - 1; i > -1; i--)
			{
				if (_registrations[i].Action?.Target == subscriber)
				{
					Deactivate(i);
				}
			}
		}

		public void Clear()
		{
			for (var i = 0; i < _registrations.Count; i++)
			{
				_registrations[i].Active = false;
			}

			_registrations.Clear();
		}

		public Snapshot TakeSnapshot()
		{
			var count = _registrations.Count;

			if (count == 0)
			{
				return default;
			}

			var items = ArrayPool<Registration>.Shared.Rent(count);
			_registrations.CopyTo(items, 0);

			return new Snapshot(items, count);
		}

		private void Deactivate(int index)
		{
			_registrations[index].Active = false;
			_registrations.RemoveAt(index);
		}
	}

	/// <summary>
	/// 대리자 인자 수별 스냅샷 호출입니다. 클로저 할당이 없도록 인자를 직접 받습니다
	/// </summary>
	internal static class ObserverSnapshotExtensions
	{
		/// <summary>
		/// 지금 등록된 의존 구독 전체에 알립니다. 의존 갱신처럼 알리는 시점의 구독자가 모두 받아야 할 때 씁니다
		/// </summary>
		public static void NotifyAll(this ObserverList<Action> list, ref NotifyErrors errors)
		{
			using var snapshot = list.TakeSnapshot();
			snapshot.Notify(ref errors);
		}

		public static void Notify(this in ObserverList<Action>.Snapshot snapshot, ref NotifyErrors errors)
		{
			for (var i = 0; i < snapshot.Count; i++)
			{
				var registration = snapshot.Items[i];

				if (!registration.Active)
				{
					continue;
				}

				try
				{
					registration.Action();
				}
				catch (Exception exception)
				{
					errors.Add(exception);
				}
			}
		}

		public static void Notify<T1, T2>(this in ObserverList<Action<T1, T2>>.Snapshot snapshot,
			T1 arg1, T2 arg2, ref NotifyErrors errors)
		{
			for (var i = 0; i < snapshot.Count; i++)
			{
				var registration = snapshot.Items[i];

				if (!registration.Active)
				{
					continue;
				}

				try
				{
					registration.Action(arg1, arg2);
				}
				catch (Exception exception)
				{
					errors.Add(exception);
				}
			}
		}

		public static void Notify<T1, T2, T3, T4>(this in ObserverList<Action<T1, T2, T3, T4>>.Snapshot snapshot,
			T1 arg1, T2 arg2, T3 arg3, T4 arg4, ref NotifyErrors errors)
		{
			for (var i = 0; i < snapshot.Count; i++)
			{
				var registration = snapshot.Items[i];

				if (!registration.Active)
				{
					continue;
				}

				try
				{
					registration.Action(arg1, arg2, arg3, arg4);
				}
				catch (Exception exception)
				{
					errors.Add(exception);
				}
			}
		}
	}
}
