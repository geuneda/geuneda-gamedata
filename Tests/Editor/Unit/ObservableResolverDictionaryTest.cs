using System;
using System.Collections.Generic;
using Geuneda.DataExtensions;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests
{
	[TestFixture]
	public class ObservableResolverDictionaryTest
	{
		private int _key = 0;
		private string _value = "1";
		private ObservableResolverDictionary<int, int, int, string> _dictionary;
		private IDictionary<int, string> _originDictionary;

		[SetUp]
		public void Init()
		{
			// 생성자에서 반복 가능한 실제 딕셔너리를 사용합니다
			_originDictionary = new Dictionary<int, string> { { _key, _value } };
			_dictionary = new ObservableResolverDictionary<int, int, int, string>(
				_originDictionary,
				origin => new KeyValuePair<int, int>(origin.Key, int.Parse(origin.Value)),
				(key, value) => new KeyValuePair<int, string>(key, value.ToString()));
		}

		[Test]
		public void TryGetOriginValue_KeyExists_ReturnsTrueAndOutValue()
		{
			Assert.IsTrue(_dictionary.TryGetOriginValue(_key, out var value));
		}

		[Test]
		public void TryGetOriginValue_KeyDoesNotExist_ReturnsFalseAndOutDefault()
		{
			var result = _dictionary.TryGetOriginValue(999, out var value);

			Assert.IsFalse(result);
			Assert.IsNull(value);
		}

		[Test]
		public void AddOrigin_AddsValueToOriginDictionary()
		{
			var newKey = 99;
			var newValue = "99";
			_dictionary.AddOrigin(newKey, newValue);

			Assert.IsTrue(_originDictionary.ContainsKey(newKey));
			Assert.AreEqual(newValue, _originDictionary[newKey]);
		}

		[Test]
		public void UpdateOrigin_UpdatesValueInOriginDictionary()
		{
			var updatedValue = "42";
			_dictionary.UpdateOrigin(_key, updatedValue);

			Assert.AreEqual(updatedValue, _originDictionary[_key]);
		}

		[Test]
		public void RemoveOrigin_RemovesValueFromOriginDictionary()
		{
			Assert.IsTrue(_dictionary.RemoveOrigin(_key));
			Assert.IsFalse(_originDictionary.ContainsKey(_key));
		}

		[Test]
		public void ClearOrigin_ClearsOriginDictionary()
		{
			_dictionary.ClearOrigin();

			Assert.AreEqual(0, _originDictionary.Count);
		}

		[Test]
		public void Rebind_ChangesOriginDictionary()
		{
			// 참고: _key는 Init()에서 이미 딕셔너리에 존재하므로 다시 추가하지 않습니다

			// 새 딕셔너리 생성 및 리바인딩
			var newDictionary = new Dictionary<int, string> { { 100, "100" }, { 200, "200" } };
			_dictionary.Rebind(
				newDictionary,
				origin => new KeyValuePair<int, int>(origin.Key, int.Parse(origin.Value)),
				(key, value) => new KeyValuePair<int, string>(key, value.ToString()));

			// 새 딕셔너리가 사용되는지 확인
			Assert.AreEqual(2, _dictionary.Count);
			Assert.IsTrue(_dictionary.ContainsKey(100));
			Assert.IsTrue(_dictionary.ContainsKey(200));
			Assert.AreEqual(100, _dictionary[100]);
			Assert.AreEqual(200, _dictionary[200]);

			// 이전 딕셔너리가 더 이상 사용되지 않는지 확인
			Assert.IsFalse(_dictionary.ContainsKey(_key));
		}

		[Test]
		public void Rebind_KeepsObservers()
		{
			// 옵저버 설정
			var observerCalls = 0;
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.UpdateOnly;
			_dictionary.Observe((key, prev, curr, type) => observerCalls++);

			// 새 딕셔너리 생성 및 리바인딩
			var newDictionary = new Dictionary<int, string> { { 100, "100" } };
			_dictionary.Rebind(
				newDictionary,
				origin => new KeyValuePair<int, int>(origin.Key, int.Parse(origin.Value)),
				(key, value) => new KeyValuePair<int, string>(key, value.ToString()));

			// 업데이트 트리거 및 옵저버 활성 상태 확인
			_dictionary.Add(300, 300);
			Assert.AreEqual(1, observerCalls);
		}
		[Test]
		public void ContainsKey_ReturnsTrue_WhenKeyExists()
		{
			// 키는 Init()에서 원본 딕셔너리를 통해 추가되었습니다
			Assert.IsTrue(_dictionary.ContainsKey(_key));
		}

		[Test]
		public void TryGetValue_ReturnsTrue_WhenKeyExists()
		{
			// 키는 Init()에서 값 "1"을 가진 원본 딕셔너리를 통해 추가되었습니다
			Assert.IsTrue(_dictionary.TryGetValue(_key, out var value));
			Assert.AreEqual(1, value); // "1" 파싱됨
		}

		[Test]
		public void Add_InvalidFormat_ThrowsException()
		{
			// 유효하지 않은 형식 값을 가진 딕셔너리 생성
			var invalidDictionary = new Dictionary<int, string> { { 99, "invalid" } };
			
			// FormatException은 "invalid" 파싱 시 생성자에서 발생해야 합니다
			Assert.Throws<FormatException>(() =>
			{
				_ = new ObservableResolverDictionary<int, int, int, string>(
					invalidDictionary,
					origin => new KeyValuePair<int, int>(origin.Key, int.Parse(origin.Value)),
					(key, value) => new KeyValuePair<int, string>(key, value.ToString()));
			});
		}
	}
}