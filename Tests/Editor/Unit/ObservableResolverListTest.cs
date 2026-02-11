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

			Assert.IsTrue(_list.RemoveOrigin(value));
			_mockList.Received().Remove(value);
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
	}
}