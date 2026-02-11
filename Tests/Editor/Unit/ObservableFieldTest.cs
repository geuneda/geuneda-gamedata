using Geuneda.DataExtensions;
using NSubstitute;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests
{
	[TestFixture]
	public class ObservableFieldTest
	{
		/// <summary>
		/// 메서드 호출 수신을 확인하기 위한 모킹 인터페이스
		/// </summary>
		public interface IMockCaller<in T>
		{
			void UpdateCall(T previous, T value);
		}

		private ObservableField<int> _observableField;
		private ObservableResolverField<int> _observableResolverField;
		private int _mockInt;
		private IMockCaller<int> _caller;

		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller<int>>();
			_observableField = new ObservableField<int>(_mockInt);
			_observableResolverField = new ObservableResolverField<int>(() => _mockInt, i => _mockInt = i);
		}

		[Test]
		public void ValueCheck()
		{
			Assert.AreEqual(_mockInt, _observableField.Value);
			Assert.AreEqual(_mockInt, _observableResolverField.Value);
		}

		[Test]
		public void ValueSetCheck()
		{
			const int valueCheck = 6;

			_mockInt = 5;

			Assert.AreNotEqual(_mockInt, _observableField.Value);
			Assert.AreEqual(_mockInt, _observableResolverField.Value);

			_observableField.Value = _mockInt;

			Assert.AreEqual(_mockInt, _observableField.Value);

			_observableResolverField.Value = valueCheck;

			Assert.AreEqual(valueCheck, _mockInt);
			Assert.AreNotEqual(_mockInt, _observableField.Value);
			Assert.AreEqual(_mockInt, _observableResolverField.Value);
		}

		[Test]
		public void ObserveCheck()
		{
			const int valueCheck = 6;

			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());

			_observableField.Value = valueCheck;
			_observableResolverField.Value = valueCheck;

			_caller.Received(2).UpdateCall(0, valueCheck);
		}

		[Test]
		public void InvokeObserveCheck()
		{
			_observableField.InvokeObserve(_caller.UpdateCall);
			_observableResolverField.InvokeObserve(_caller.UpdateCall);

			_caller.Received(2).UpdateCall(0, 0);
		}

		[Test]
		public void InvokeCheck()
		{
			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.Received(2).UpdateCall(0, 0);
		}

		[Test]
		public void InvokeCheck_NotObserving_DoesNothing()
		{
			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(0, 0);
		}

		[Test]
		public void StopObserveCheck()
		{
			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableField.StopObserving(_caller.UpdateCall);
			_observableResolverField.StopObserving(_caller.UpdateCall);

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		public void StopObserve_NotObserving_DoesNothing()
		{
			_observableField.StopObserving(_caller.UpdateCall);
			_observableResolverField.StopObserving(_caller.UpdateCall);

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		public void StopObservingAllCheck()
		{
			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableField.StopObservingAll(_caller);
			_observableResolverField.StopObservingAll(_caller);

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		public void StopObservingAll_MultipleCalls_Check()
		{
			_observableField.Observe(_caller.UpdateCall);
			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableField.StopObservingAll(_caller);
			_observableResolverField.StopObservingAll(_caller);

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		public void StopObservingAll_Everything_Check()
		{
			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableField.StopObservingAll();
			_observableResolverField.StopObservingAll();

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		public void StopObservingAll_NotObserving_DoesNothing()
		{
			_observableField.StopObservingAll();
			_observableResolverField.StopObservingAll();

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		public void RebindCheck()
		{
			const int valueCheck = 10;
			var newMockInt = 5;

			// 옵저버 설정
			_observableResolverField.Observe(_caller.UpdateCall);
			_caller.ClearReceivedCalls();

			// 새 필드에 리바인딩
			_observableResolverField.Rebind(() => newMockInt, i => newMockInt = i);

			// 리바인딩이 작동하는지 확인
			Assert.AreEqual(newMockInt, _observableResolverField.Value);

			// 리바인딩된 필드를 통해 값 설정
			_observableResolverField.Value = valueCheck;

			// 새 필드가 업데이트되었는지 확인
			Assert.AreEqual(valueCheck, newMockInt);
			Assert.AreEqual(valueCheck, _observableResolverField.Value);

			// 이전 필드가 업데이트되지 않았는지 확인
			Assert.AreNotEqual(valueCheck, _mockInt);

			// 리바인딩 후 옵저버가 여전히 작동하는지 확인
			_caller.Received(1).UpdateCall(5, valueCheck);
		}

		[Test]
		public void RebindCheck_KeepsObservers()
		{
			const int valueCheck = 15;
			var newMockInt = 0;

			// 리바인딩 전 여러 옵저버 설정
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);

			// 새 필드에 리바인딩
			_observableResolverField.Rebind(() => newMockInt, i => newMockInt = i);
			_caller.ClearReceivedCalls();

			// 업데이트 트리거
			_observableResolverField.Value = valueCheck;

			// 두 옵저버 모두 알림을 받았는지 확인
			_caller.Received(2).UpdateCall(0, valueCheck);
		}

		[Test]
		public void RebindCheck_BaseClass()
		{
			const int initialValue = 5;
			const int newValue = 10;

			// 기본 클래스에 옵저버 설정
			_observableField.Observe(_caller.UpdateCall);

			// 새 값에 리바인딩
			_observableField.Rebind(initialValue);

			// 새 값이 설정되었는지 확인
			Assert.AreEqual(initialValue, _observableField.Value);

			// 업데이트 트리거 및 옵저버 작동 확인
			_observableField.Value = newValue;
			_caller.Received(1).UpdateCall(initialValue, newValue);
		}

		[Test]
		public void BeginBatch_SuppressesNotifications()
		{
			_observableField.Observe(_caller.UpdateCall);

			using (_observableField.BeginBatch())
			{
				_observableField.Value = 10;
				_observableField.Value = 20;
			}

			_caller.Received(1).UpdateCall(0, 20);
		}

		[Test]
		public void ComputedDependency_ReadTriggersTracking()
		{
			var dependencyCalled = false;
			var computed = new ComputedField<int>(() =>
			{
				dependencyCalled = true;
				return _observableField.Value;
			});

			var val = computed.Value;

			Assert.IsTrue(dependencyCalled);
			Assert.AreEqual(_observableField.Value, val);

			_observableField.Value = 10;
			// ComputedField는 이제 더티 상태이며 다음 접근 시 재계산해야 합니다
			Assert.AreEqual(10, computed.Value);
		}
	}
}