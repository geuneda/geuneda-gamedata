using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Geuneda.DataExtensions;
using NSubstitute;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests
{
	[TestFixture]
	public class ObservableResolverListTest
	{
		private int _index = 0;
		private IObservableResolverList<int, string> _list;
		private IList<string> _mockList;

		[SetUp]
		public void SetUp()
		{
			_mockList = Substitute.For<IList<string>>();
			_list = new ObservableResolverList<int, string>(_mockList,
				origin => int.Parse(origin),
				value => value.ToString());
		}

		[Test]
		public void AddOrigin_AddsValueToOriginList()
		{
			var value = "1";

			_list.AddOrigin(value);

			_mockList.Received().Add(value);
		}

		[Test]
		public void UpdateOrigin_UpdatesOriginList()
		{
			var value = "1";

			_list.AddOrigin(value);
			_list.UpdateOrigin(value, _index);

			_mockList.Received()[_index] = value;
		}

		[Test]
		public void RemoveOrigin_RemovesValueFromOriginList()
		{
			var value = "1";

			_list.AddOrigin(value);
			_mockList.IndexOf(value).Returns(0);

			Assert.IsTrue(_list.RemoveOrigin(value));
			_mockList.Received().RemoveAt(0);
		}

		[Test]
		public void RemoveOrigin_ValueNotInOrigin_ReturnsFalseAndKeepsBothLists()
		{
			var originList = new List<string> { "1" };
			var list = new ObservableResolverList<int, string>(originList,
				origin => int.Parse(origin),
				value => value.ToString());

			// "01" 은 원본에 없지만 변환하면 1 이 되어 내부 리스트의 값과 같다
			Assert.IsFalse(list.RemoveOrigin("01"));

			CollectionAssert.AreEqual(new[] { "1" }, originList);
			CollectionAssert.AreEqual(new[] { 1 }, list.ReadOnlyList);
		}

		[Test]
		public void RemoveOrigin_ResolverCreatesNewReferences_RemovesSameIndexInBothLists()
		{
			var originList = new List<string> { "a", "b" };
			var list = new ObservableResolverList<ReferenceItem, string>(originList,
				origin => new ReferenceItem(origin),
				item => item.Value);
			var calls = new List<(int index, ObservableUpdateType type)>();
			list.Observe((index, prev, curr, type) => calls.Add((index, type)));

			Assert.IsTrue(list.RemoveOrigin("a"));

			CollectionAssert.AreEqual(new[] { "b" }, originList);
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("b", list[0].Value);
			CollectionAssert.AreEqual(new[] { (0, ObservableUpdateType.Removed) }, calls);
		}

		[Test]
		public void RemoveOrigin_RealList_RemovesOnlyOneOriginElement()
		{
			var originList = new List<string> { "1", "2", "3" };
			var list = new ObservableResolverList<int, string>(originList,
				origin => int.Parse(origin),
				value => value.ToString());

			Assert.IsTrue(list.RemoveOrigin("1"));

			CollectionAssert.AreEqual(new[] { "2", "3" }, originList);
			CollectionAssert.AreEqual(new[] { 2, 3 }, list.ReadOnlyList);
		}

		[Test]
		public void RemoveOrigin_RealList_LastElement_DoesNotThrow()
		{
			var originList = new List<string> { "1" };
			var list = new ObservableResolverList<int, string>(originList,
				origin => int.Parse(origin),
				value => value.ToString());

			Assert.DoesNotThrow(() => list.RemoveOrigin("1"));
			Assert.AreEqual(0, originList.Count);
			Assert.AreEqual(0, list.Count);
		}

		[Test]
		public void AddOrigin_NotifiesObservers()
		{
			var originList = new List<string> { "1" };
			var list = new ObservableResolverList<int, string>(originList,
				origin => int.Parse(origin),
				value => value.ToString());
			var calls = new List<(int index, int prev, int curr, ObservableUpdateType type)>();
			list.Observe((index, prev, curr, type) => calls.Add((index, prev, curr, type)));

			list.AddOrigin("2");

			CollectionAssert.AreEqual(new[] { "1", "2" }, originList);
			Assert.AreEqual(1, calls.Count);
			Assert.AreEqual((1, 0, 2, ObservableUpdateType.Added), calls[0]);
		}

		[Test]
		public void UpdateOrigin_NotifiesObservers()
		{
			var originList = new List<string> { "1", "2" };
			var list = new ObservableResolverList<int, string>(originList,
				origin => int.Parse(origin),
				value => value.ToString());
			var calls = new List<(int index, int prev, int curr, ObservableUpdateType type)>();
			list.Observe((index, prev, curr, type) => calls.Add((index, prev, curr, type)));

			list.UpdateOrigin("5", 1);

			CollectionAssert.AreEqual(new[] { "1", "5" }, originList);
			Assert.AreEqual(5, list[1]);
			Assert.AreEqual(1, calls.Count);
			Assert.AreEqual((1, 2, 5, ObservableUpdateType.Updated), calls[0]);
		}

		[Test]
		public void ClearOrigin_ClearsOriginList()
		{
			_list.ClearOrigin();

			_mockList.Received().Clear();
		}

		[Test]
		public void Rebind_ChangesOriginList()
		{
			// 초기 데이터 추가
			_list.AddOrigin("1");
			_list.AddOrigin("2");

			// 새 원본 리스트 생성 및 리바인딩
			var newOriginList = new List<string> { "10", "20", "30", "40" };
			_list.Rebind(
				newOriginList,
				origin => int.Parse(origin),
				value => value.ToString());

			// 새 리스트가 사용되는지 확인
			Assert.AreEqual(4, _list.Count);
			Assert.AreEqual(10, _list[0]);
			Assert.AreEqual(20, _list[1]);
			Assert.AreEqual(30, _list[2]);
			Assert.AreEqual(40, _list[3]);

			// 추가 작업이 새 원본 리스트를 사용하는지 확인
			_list.Add(50);
			Assert.AreEqual("50", newOriginList[4]);
		}

		[Test]
		public void Rebind_KeepsObservers()
		{
			// 옵저버 설정
			var observerCalls = 0;
			_list.Observe((index, prev, curr, type) => observerCalls++);

			// 새 원본 리스트 생성 및 리바인딩
			var newOriginList = new List<string> { "100", "200" };
			_list.Rebind(
				newOriginList,
				origin => int.Parse(origin),
				value => value.ToString());

			// 업데이트 트리거 및 옵저버 활성 상태 확인
			_list.Add(300);
			Assert.AreEqual(1, observerCalls);
		}

		[Test]
		public void StopObserving_StopsNotifications()
		{
			var observerCalls = 0;
			Action<int, int, int, ObservableUpdateType> observer = (index, prev, curr, type) => observerCalls++;
			
			_list.Observe(observer);
			_list.StopObserving(observer);
			
			_list.Add(300);
			Assert.AreEqual(0, observerCalls);
		}

		[Test]
		public void Add_InvalidFormat_ThrowsException()
		{
			// 리졸버 'origin => int.Parse(origin)'은 origin이 유효한 숫자가 아니면 예외를 발생시킵니다
			// AddOrigin은 원본 값을 즉시 파싱하므로, 추가 시 예외가 발생합니다
			Assert.Throws<FormatException>(() => _list.AddOrigin("invalid"));
		}

		/// <summary>
		/// Equals 를 재정의하지 않아 변환할 때마다 다른 참조가 되는 테스트용 항목
		/// </summary>
		private sealed class ReferenceItem
		{
			public string Value { get; }

			public ReferenceItem(string value)
			{
				Value = value;
			}
		}
	}
}