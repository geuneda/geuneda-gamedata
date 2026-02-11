using System;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// Observable 필드 리더에서 계산 필드를 생성하기 위한 확장 메서드입니다.
	/// </summary>
	public static class ObservableExtensions
	{
		/// <summary>
		/// 주어진 셀렉터를 사용하여 이 필드의 값을 변환하는 계산 필드를 생성합니다.
		/// 소스 필드가 변경되면 계산 필드가 자동으로 업데이트됩니다.
		/// </summary>
		public static ComputedField<TResult> Select<T, TResult>(
			this IObservableFieldReader<T> source,
			Func<T, TResult> selector)
		{
			return new ComputedField<TResult>(() => selector(source.Value));
		}

		/// <summary>
		/// 이 Observable 필드를 다른 필드와 결합하여 계산 결과를 생성합니다.
		/// 어느 쪽 소스 필드가 변경되어도 계산 필드가 자동으로 업데이트됩니다.
		/// </summary>
		public static ComputedField<TResult> CombineWith<T1, T2, TResult>(
			this IObservableFieldReader<T1> first,
			IObservableFieldReader<T2> second,
			Func<T1, T2, TResult> combiner)
		{
			return new ComputedField<TResult>(() => combiner(first.Value, second.Value));
		}

		/// <summary>
		/// 이 Observable 필드를 다른 두 필드와 결합하여 계산 결과를 생성합니다.
		/// 소스 필드 중 하나라도 변경되면 계산 필드가 자동으로 업데이트됩니다.
		/// </summary>
		public static ComputedField<TResult> CombineWith<T1, T2, T3, TResult>(
			this IObservableFieldReader<T1> first,
			IObservableFieldReader<T2> second,
			IObservableFieldReader<T3> third,
			Func<T1, T2, T3, TResult> combiner)
		{
			return new ComputedField<TResult>(() => combiner(first.Value, second.Value, third.Value));
		}

		/// <summary>
		/// 이 Observable 필드를 다른 세 필드와 결합하여 계산 결과를 생성합니다.
		/// 소스 필드 중 하나라도 변경되면 계산 필드가 자동으로 업데이트됩니다.
		/// </summary>
		public static ComputedField<TResult> CombineWith<T1, T2, T3, T4, TResult>(
			this IObservableFieldReader<T1> first,
			IObservableFieldReader<T2> second,
			IObservableFieldReader<T3> third,
			IObservableFieldReader<T4> fourth,
			Func<T1, T2, T3, T4, TResult> combiner)
		{
			return new ComputedField<TResult>(() => combiner(first.Value, second.Value, third.Value, fourth.Value));
		}
	}
}
