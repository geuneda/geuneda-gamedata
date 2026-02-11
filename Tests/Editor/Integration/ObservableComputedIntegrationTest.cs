using System;
using System.Collections.Generic;
using Geuneda.DataExtensions;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests.Integration
{
	[TestFixture]
	public class ObservableComputedIntegrationTest
	{
		[Test]
		public void ComputedField_WithMultipleObservables_TracksAll()
		{
			var field1 = new ObservableField<int>(10);
			var field2 = new ObservableField<int>(20);
			var list = new ObservableList<int>(new List<int> { 1, 2 });
			
			var computed = new ComputedField<int>(() => field1.Value + field2.Value + list.Count);
			
			Assert.AreEqual(32, computed.Value);

			field1.Value = 100;
			Assert.AreEqual(122, computed.Value);

			list.Add(3);
			Assert.AreEqual(123, computed.Value);
		}

		[Test]
		public void BatchUpdates_WithComputedField_SingleRecalculation()
		{
			var field1 = new ObservableField<int>(10);
			var field2 = new ObservableField<int>(20);
			var callCount = 0;
			
			var computed = new ComputedField<int>(() =>
			{
				callCount++;
				return field1.Value + field2.Value;
			});

			computed.Observe((p, c) => { }); // 의존성 변경 시 재계산을 트리거하기 위해 관찰합니다
			var val = computed.Value; // 초기 계산
			callCount = 0;

			// 계산 필드가 배치에 포함될 때, 한 번만 재계산해야 합니다
			// 배치가 끝날 때(필드당 한 번이 아님)
			using (var batch = new ObservableBatch())
			{
				batch.Add(field1);
				batch.Add(field2);
				batch.Add(computed);
				
				field1.Value = 100;
				field2.Value = 200;
			}
			
			Assert.AreEqual(1, callCount);
		}

		[Test]
		public void ChainedComputedFields_PropagateDirtyFlag()
		{
			var field = new ObservableField<int>(10);
			var c1 = field.Select(x => x + 1);
			var c2 = c1.Select(x => x + 1);
			
			Assert.AreEqual(12, c2.Value);
			
			field.Value = 20;
			Assert.AreEqual(22, c2.Value);
		}
	}
}
