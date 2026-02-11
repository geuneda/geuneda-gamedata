using System;

namespace Geuneda.DataExtensions
{
	/// <inheritdoc />
	/// <remarks>
	/// 새 리졸버 함수에 리바인딩할 수 있는 리졸버 필드입니다
	/// </remarks>
	public interface IObservableResolverField<T> : IObservableField<T>
	{
		/// <summary>
		/// 기존 옵저버를 잃지 않고 이 필드를 새 리졸버 함수에 리바인딩합니다
		/// </summary>
		/// <param name="fieldResolver">필드의 새 게터 함수입니다</param>
		/// <param name="fieldSetter">필드의 새 세터 함수입니다</param>
		void Rebind(Func<T> fieldResolver, Action<T> fieldSetter);
	}

	/// <inheritdoc cref="IObservableResolverField{T}"/>
	public class ObservableResolverField<T> : ObservableField<T>, IObservableResolverField<T>
	{
		private Func<T> _fieldResolver;
		private Action<T> _fieldSetter;

		/// <inheritdoc cref="IObservableField{T}.Value" />
		public override T Value
		{
			get
			{
				ComputedTracker.OnRead(this);
				return _fieldResolver();
			}
			set
			{
				var previousValue = _fieldResolver();

				_fieldSetter(value);

				InvokeUpdate(previousValue);
			}
		}

		private ObservableResolverField() { }

		public ObservableResolverField(Func<T> fieldResolver, Action<T> fieldSetter)
		{
			_fieldResolver = fieldResolver;
			_fieldSetter = fieldSetter;
		}

		/// <summary>
		/// 필드를 값으로 변환하는 암시적 연산자입니다
		/// </summary>
		public static implicit operator T(ObservableResolverField<T> value) => value.Value;

		/// <summary>
		/// 기존 옵저버를 잃지 않고 이 필드를 새 리졸버 함수에 리바인딩합니다
		/// </summary>
		public void Rebind(Func<T> fieldResolver, Action<T> fieldSetter)
		{
			_fieldResolver = fieldResolver;
			_fieldSetter = fieldSetter;
		}

		/// <inheritdoc />
		protected override T GetCurrentValue() => _fieldResolver();
	}
}

