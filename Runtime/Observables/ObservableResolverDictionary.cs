using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Geuneda.DataExtensions
{
	/// <inheritdoc />
	/// <remarks>
	/// 이 인터페이스는 다른 타입의 키와 값을 가진 2개의 딕셔너리 사이를 해석합니다
	/// </remarks>
	public interface IObservableResolverDictionaryReader<TKey, TValue, TKeyOrigin, TValueOrigin> :
		IObservableDictionaryReader<TKey, TValue>
	{
		/// <summary>
		/// 전체 인터페이스에서 해석되는 원본 딕셔너리입니다
		/// </summary>
		ReadOnlyDictionary<TKeyOrigin, TValueOrigin> OriginDictionary { get; }

		/// <summary>
		/// 지정된 키에 해당하는 원본 딕셔너리의 값을 가져옵니다.
		/// </summary>
		/// <param name="key">원본 딕셔너리에서 찾을 키입니다.</param>
		/// <returns>지정된 키에 해당하는 원본 딕셔너리의 값입니다.</returns>
		TValueOrigin GetOriginValue(TKey key);

		/// <summary>
		/// 지정된 키에 해당하는 원본 딕셔너리의 값을 가져오려고 시도합니다.
		/// </summary>
		/// <param name="key">원본 딕셔너리에서 찾을 키입니다.</param>
		/// <param name="value">When this method returns, contains the value from the origin dictionary corresponding to the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns>원본 딕셔너리에 지정된 키의 요소가 포함되어 있으면 true, 그렇지 않으면 false입니다.</returns>
		bool TryGetOriginValue(TKey key, out TValueOrigin value);
	}

	/// <inheritdoc cref="IObservableDictionary{TKey,TValue}"/>
	/// <remarks>
	/// 이 인터페이스는 다른 타입의 키와 값을 가진 2개의 딕셔너리 사이를 해석합니다
	/// </remarks>
	public interface IObservableResolverDictionary<TKey, TValue, TKeyOrigin, TValueOrigin> :
		IObservableResolverDictionaryReader<TKey, TValue, TKeyOrigin, TValueOrigin>,
		IObservableDictionary<TKey, TValue>
	{
		/// <summary>
		/// 지정된 원본 키에 해당하는 원본 딕셔너리의 값을 업데이트합니다.
		/// </summary>
		/// <param name="key">원본 딕셔너리에서 업데이트할 값의 키입니다.</param>
		/// <param name="value">원본 딕셔너리에 설정할 새 값입니다.</param>
		void UpdateOrigin(TKeyOrigin key, TValueOrigin value);

		/// <inheritdoc cref="Dictionary{TKey,TValue}.Add" />
		/// <remarks>
		/// 원본 딕셔너리에 추가합니다
		/// </remarks>
		void AddOrigin(TKeyOrigin key, TValueOrigin value);

		/// <inheritdoc cref="Dictionary{TKey,TValue}.Remove" />
		/// <remarks>
		/// 원본 딕셔너리에서 제거합니다
		/// </remarks>
		bool RemoveOrigin(TKeyOrigin key);

		/// <inheritdoc cref="Dictionary{TKey,TValue}.Clear" />
		/// <remarks>
		/// 원본 딕셔너리를 비웁니다
		/// </remarks>
		void ClearOrigin();

		/// <summary>
		/// 기존 옵저버를 잃지 않고 이 딕셔너리를 새 원본 딕셔너리와 리졸버 함수에 리바인딩합니다.
		/// 내부 딕셔너리는 새 리졸버를 사용하여 새 원본 딕셔너리에서 다시 빌드됩니다.
		/// </summary>
		/// <param name="dictionary">바인딩할 새 원본 딕셔너리입니다</param>
		/// <param name="fromOrignResolver">원본 타입에서 이 딕셔너리 타입으로 변환하는 새 함수입니다</param>
		/// <param name="toOrignResolver">이 딕셔너리 타입에서 원본 타입으로 변환하는 새 함수입니다</param>
		void Rebind(IDictionary<TKeyOrigin, TValueOrigin> dictionary,
			Func<KeyValuePair<TKeyOrigin, TValueOrigin>, KeyValuePair<TKey, TValue>> fromOrignResolver,
			Func<TKey, TValue, KeyValuePair<TKeyOrigin, TValueOrigin>> toOrignResolver);
	}

	/// <inheritdoc cref="IObservableResolverDictionary{TKey, TValue, TKeyOrigin, TValueOrigin}"/>
	public class ObservableResolverDictionary<TKey, TValue, TKeyOrigin, TValueOrigin> :
		ObservableDictionary<TKey, TValue>,
		IObservableResolverDictionary<TKey, TValue, TKeyOrigin, TValueOrigin>
	{
		private IDictionary<TKeyOrigin, TValueOrigin> _dictionary;
		private Func<TKey, TValue, KeyValuePair<TKeyOrigin, TValueOrigin>> _toOrignResolver;
		private Func<KeyValuePair<TKeyOrigin, TValueOrigin>, KeyValuePair<TKey, TValue>> _fromOrignResolver;

		/// <inheritdoc />
		public ReadOnlyDictionary<TKeyOrigin, TValueOrigin> OriginDictionary => new ReadOnlyDictionary<TKeyOrigin, TValueOrigin>(_dictionary);

		public ObservableResolverDictionary(IDictionary<TKeyOrigin, TValueOrigin> dictionary,
			Func<KeyValuePair<TKeyOrigin, TValueOrigin>, KeyValuePair<TKey, TValue>> fromOrignResolver,
			Func<TKey, TValue, KeyValuePair<TKeyOrigin, TValueOrigin>> toOrignResolver)
			: base(new Dictionary<TKey, TValue>(dictionary.Count))
		{
			_dictionary = dictionary;
			_toOrignResolver = toOrignResolver;
			_fromOrignResolver = fromOrignResolver;

			foreach (var pair in dictionary)
			{
				Dictionary.Add(fromOrignResolver(pair));
			}
		}

		/// <inheritdoc />
		public void Rebind(IDictionary<TKeyOrigin, TValueOrigin> dictionary,
			Func<KeyValuePair<TKeyOrigin, TValueOrigin>, KeyValuePair<TKey, TValue>> fromOrignResolver,
			Func<TKey, TValue, KeyValuePair<TKeyOrigin, TValueOrigin>> toOrignResolver)
		{
			_dictionary = dictionary;
			_toOrignResolver = toOrignResolver;
			_fromOrignResolver = fromOrignResolver;

			// 새 원본 딕셔너리에서 내부 딕셔너리를 다시 빌드합니다
			Dictionary.Clear();
			foreach (var pair in dictionary)
			{
				Dictionary.Add(fromOrignResolver(pair));
			}
		}

		/// <inheritdoc />
		public TValueOrigin GetOriginValue(TKey key)
		{
			return _dictionary[_toOrignResolver(key, default).Key];
		}

		/// <inheritdoc />
		public bool TryGetOriginValue(TKey key, out TValueOrigin value)
		{
			return _dictionary.TryGetValue(_toOrignResolver(key, default).Key, out value);
		}

		/// <inheritdoc />
		public void UpdateOrigin(TKeyOrigin key, TValueOrigin value)
		{
			var convertPair = _fromOrignResolver(new KeyValuePair<TKeyOrigin, TValueOrigin>(key, value));

			_dictionary[key] = value;
			this[convertPair.Key] = convertPair.Value;
		}

		/// <inheritdoc />
		public override void Add(TKey key, TValue value)
		{
			_dictionary.Add(_toOrignResolver(key, value));
			base.Add(key, value);
		}

		/// <inheritdoc />
		public override bool Remove(TKey key)
		{
			if (!Dictionary.TryGetValue(key, out var value)) return false;

			var pair = _toOrignResolver(key, value);

			_dictionary.Remove(pair.Key);

			return base.Remove(key);
		}

		/// <inheritdoc />
		public override void Clear()
		{
			_dictionary.Clear();
			base.Clear();
		}

		/// <inheritdoc />
		public void AddOrigin(TKeyOrigin key, TValueOrigin value)
		{
			var convertPair = _fromOrignResolver(new KeyValuePair<TKeyOrigin, TValueOrigin>(key, value));

			_dictionary.Add(key, value);
			base.Add(convertPair.Key, convertPair.Value);
		}

		/// <inheritdoc />
		public bool RemoveOrigin(TKeyOrigin key)
		{
			if (!_dictionary.TryGetValue(key, out var value)) return false;

			var convertPair = _fromOrignResolver(new KeyValuePair<TKeyOrigin, TValueOrigin>(key, value));

			_dictionary.Remove(key);
			return base.Remove(convertPair.Key);
		}

		/// <inheritdoc />
		public void ClearOrigin()
		{
			_dictionary.Clear();
			base.Clear();
		}
	}
}

