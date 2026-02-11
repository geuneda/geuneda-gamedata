using System;
using System.Collections.Generic;
using Geuneda.DataExtensions;
using NSubstitute;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests
{
	[TestFixture]
	public class ObservableBatchTest
	{
		private IBatchable _mockBatchable1;
		private IBatchable _mockBatchable2;

		[SetUp]
		public void Setup()
		{
			_mockBatchable1 = Substitute.For<IBatchable>();
			_mockBatchable2 = Substitute.For<IBatchable>();
		}

		[Test]
		public void Add_SuppressesNotificationsImmediately()
		{
			var batch = new ObservableBatch();
			batch.Add(_mockBatchable1);

			_mockBatchable1.Received(1).SuppressNotifications();
		}

		[Test]
		public void Add_MultipleObservables_AllSuppressed()
		{
			var batch = new ObservableBatch();
			batch.Add(_mockBatchable1);
			batch.Add(_mockBatchable2);

			_mockBatchable1.Received(1).SuppressNotifications();
			_mockBatchable2.Received(1).SuppressNotifications();
		}

		[Test]
		public void Dispose_ResumesAllNotifications()
		{
			var batch = new ObservableBatch();
			batch.Add(_mockBatchable1);
			batch.Add(_mockBatchable2);

			batch.Dispose();

			_mockBatchable1.Received(1).ResumeNotifications();
			_mockBatchable2.Received(1).ResumeNotifications();
		}

		[Test]
		public void Dispose_NotificationsInAddOrder()
		{
			// 수신된 호출은 NSubstitute에서 순서대로 추적되지만, 목록도 사용할 수 있습니다
			var callOrder = new List<int>();
			_mockBatchable1.When(x => x.ResumeNotifications()).Do(_ => callOrder.Add(1));
			_mockBatchable2.When(x => x.ResumeNotifications()).Do(_ => callOrder.Add(2));

			var batch = new ObservableBatch();
			batch.Add(_mockBatchable1);
			batch.Add(_mockBatchable2);

			batch.Dispose();

			Assert.AreEqual(1, callOrder[0]);
			Assert.AreEqual(2, callOrder[1]);
		}

		[Test]
		public void DoubleDispose_NoError_NoDoubleNotification()
		{
			var batch = new ObservableBatch();
			batch.Add(_mockBatchable1);

			batch.Dispose();
			batch.Dispose();

			_mockBatchable1.Received(1).ResumeNotifications();
		}

		[Test]
		public void AddAfterDispose_ThrowsObjectDisposedException()
		{
			var batch = new ObservableBatch();
			batch.Dispose();

			Assert.Throws<ObjectDisposedException>(() => batch.Add(_mockBatchable1));
		}
	}
}
