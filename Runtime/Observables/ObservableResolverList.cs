using System;
using System.Collections.Generic;

namespace Geuneda.DataExtensions
{
	/// <inheritdoc />
	/// <remarks>
	/// 이 인터페이스는 다른 타입의 값을 가진 2개의 리스트 사이를 해석합니다
	/// </remarks>
	public interface IObservableResolverListReader<T, out TOrigin> : IObservableListReader<T>
	{
		/// <summary>
		/// 전체 인터페이스에서 해석되는 원본 리스트입니다
		/// </summary>
		IReadOnlyList<TOrigin> OriginList { get; }
	}

	/// <inheritdoc />
	/// <remarks>
	/// 이 인터페이스는 다른 타입의 값을 가진 2개의 리스트 사이를 해석합니다
	/// </remarks>
	public interface IObservableResolverList<T, TOrigin> :
		IObservableResolverListReader<T, TOrigin>,
		IObservableList<T>
	{
		/// <summary>
		/// 지정된 인덱스에 해당하는 원본 리스트의 값을 업데이트합니다.
		/// </summary>
		/// <param name="index">원본 리스트에서 업데이트할 값의 인덱스입니다.</param>
		/// <param name="value">원본 리스트에 설정할 새 값입니다.</param>
		void UpdateOrigin(TOrigin value, int index);

		/// <inheritdoc cref="List{T}.Add"/>
		/// <remarks>
		/// 원본 리스트에 값을 추가합니다
		/// </remarks>
		void AddOrigin(TOrigin value);

		/// <inheritdoc cref="List{T}.Remove"/>
		/// <remarks>
		/// 원본 리스트에서 값을 제거합니다
		/// </remarks>
		bool RemoveOrigin(TOrigin value);

		/// <inheritdoc cref="List{T}.Clear"/>
		/// <remarks>
		/// 원본 리스트를 비웁니다
		/// </remarks>
		void ClearOrigin();

		/// <summary>
		/// 기존 옵저버를 잃지 않고 이 리스트를 새 원본 리스트와 리졸버 함수에 리바인딩합니다.
		/// 내부 리스트는 새 리졸버를 사용하여 새 원본 리스트에서 다시 빌드됩니다.
		/// </summary>
		/// <param name="originList">바인딩할 새 원본 리스트입니다</param>
		/// <param name="fromOrignResolver">원본 타입에서 이 리스트 타입으로 변환하는 새 함수입니다</param>
		/// <param name="toOrignResolver">이 리스트 타입에서 원본 타입으로 변환하는 새 함수입니다</param>
		void Rebind(IList<TOrigin> originList, Func<TOrigin, T> fromOrignResolver, Func<T, TOrigin> toOrignResolver);
	}

	/// <inheritdoc cref="IObservableResolverList{T, TOrigin}"/>
	/// <remarks>
	/// 이 클래스는 다른 타입의 값을 가진 2개의 리스트 사이를 해석합니다
	/// </remarks>
	public class ObservableResolverList<T, TOrigin> : ObservableList<T>, IObservableResolverList<T, TOrigin>
	{
		private IList<TOrigin> _originList;
		private Func<TOrigin, T> _fromOrignResolver;
		private Func<T, TOrigin> _toOrignResolver;

		/// <inheritdoc />
		public IReadOnlyList<TOrigin> OriginList => new List<TOrigin>(_originList);

		public ObservableResolverList(IList<TOrigin> originList,
			Func<TOrigin, T> fromOrignResolver,
			Func<T, TOrigin> toOrignResolver) :
			base(new List<T>(originList.Count))
		{
			_originList = originList;
			_fromOrignResolver = fromOrignResolver;
			_toOrignResolver = toOrignResolver;

			for (var i = 0; i < originList.Count; i++)
			{
				List.Add(fromOrignResolver(originList[i]));
			}
		}

		/// <inheritdoc />
		public void Rebind(IList<TOrigin> originList,
			Func<TOrigin, T> fromOrignResolver,
			Func<T, TOrigin> toOrignResolver)
		{
			_originList = originList;
			_fromOrignResolver = fromOrignResolver;
			_toOrignResolver = toOrignResolver;

			// 새 원본 리스트에서 내부 리스트를 다시 빌드합니다
			List.Clear();
			for (var i = 0; i < originList.Count; i++)
			{
				List.Add(fromOrignResolver(originList[i]));
			}
		}

		/// <inheritdoc />
		public override void Add(T data)
		{
			_originList.Add(_toOrignResolver(data));
			base.Add(data);
		}

		/// <inheritdoc />
		public override void RemoveAt(int index)
		{
			_originList.RemoveAt(index);
			base.RemoveAt(index);
		}

		/// <inheritdoc />
		public override void Clear()
		{
			_originList.Clear();
			base.Clear();
		}

		/// <inheritdoc />
		public void UpdateOrigin(TOrigin value, int index)
		{
			_originList[index] = value;
			List[index] = _fromOrignResolver(value);
		}

		/// <inheritdoc />
		public void AddOrigin(TOrigin value)
		{
			_originList.Add(value);
			List.Add(_fromOrignResolver(value));
		}

		/// <inheritdoc />
		public bool RemoveOrigin(TOrigin value)
		{
			_originList.Remove(value);

			return base.Remove(_fromOrignResolver(value));
		}

		/// <inheritdoc />
		public void ClearOrigin()
		{
			_originList.Clear();
			base.Clear();
		}
	}
}

