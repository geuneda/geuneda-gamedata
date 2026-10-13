using System;
using System.Collections.Generic;
using Geuneda.DataExtensions;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests.Regression
{
	[TestFixture]
	public class RegressionTests
	{
		// 이 파일은 회귀 버그를 문서화하고 방지하기 위한 곳입니다.
		// 버그가 발견되고 수정될 때 여기에 테스트를 추가하세요.

		// ── 통지 중 구독 해제 (Remove 통지에서 범위 예외 또는 중복 호출) ──

		[Test]
		public void ObservableListRemoveAt_LastSubscriberUnsubscribesItself_NotifiesEachOnce()
		{
			var list = new ObservableList<int>(new List<int> { 1 });
			var calls = new List<string>();
			Action<int, int, int, ObservableUpdateType> first = (_, _, _, _) => calls.Add("first");
			Action<int, int, int, ObservableUpdateType> last = null;
			last = (_, _, _, _) =>
			{
				calls.Add("last");
				list.StopObserving(last);
			};
			list.Observe(first);
			list.Observe(last);

			Assert.DoesNotThrow(() => list.RemoveAt(0));
			CollectionAssert.AreEqual(new[] { "first", "last" }, calls);
		}

		[Test]
		public void ObservableListRemoveAt_MiddleSubscriberUnsubscribesItself_NotifiesEachOnce()
		{
			var list = new ObservableList<int>(new List<int> { 1 });
			var calls = new List<string>();
			Action<int, int, int, ObservableUpdateType> middle = null;
			middle = (_, _, _, _) =>
			{
				calls.Add("middle");
				list.StopObserving(middle);
			};
			list.Observe((_, _, _, _) => calls.Add("first"));
			list.Observe(middle);
			list.Observe((_, _, _, _) => calls.Add("last"));

			list.RemoveAt(0);

			CollectionAssert.AreEqual(new[] { "first", "middle", "last" }, calls);
		}

		[Test]
		public void ObservableDictionaryRemove_SubscribersUnsubscribeThemselves_NotifiesEachOnce()
		{
			var dictionary = new ObservableDictionary<int, int>(new Dictionary<int, int> { { 1, 10 } })
			{
				ObservableUpdateFlag = ObservableUpdateFlag.Both
			};
			var calls = new List<string>();
			Action<int, int, int, ObservableUpdateType> keyLast = null;
			Action<int, int, int, ObservableUpdateType> globalLast = null;
			keyLast = (_, _, _, _) =>
			{
				calls.Add("key-last");
				dictionary.StopObserving(keyLast);
			};
			globalLast = (_, _, _, _) =>
			{
				calls.Add("global-last");
				dictionary.StopObserving(globalLast);
			};
			dictionary.Observe(1, (_, _, _, _) => calls.Add("key-first"));
			dictionary.Observe(1, keyLast);
			dictionary.Observe((_, _, _, _) => calls.Add("global-first"));
			dictionary.Observe(globalLast);

			Assert.DoesNotThrow(() => dictionary.Remove(1));
			CollectionAssert.AreEqual(new[] { "key-first", "key-last", "global-first", "global-last" }, calls);
		}

		[Test]
		public void ObservableField_SubscriberUnsubscribesLaterSubscriber_LaterSubscriberIsNotCalled()
		{
			var field = new ObservableField<int>(0);
			var laterCalls = 0;
			Action<int, int> later = (_, _) => laterCalls++;
			field.Observe((_, _) => field.StopObserving(later));
			field.Observe(later);

			field.Value = 1;

			Assert.AreEqual(0, laterCalls);
		}

		[Test]
		public void ObservableListAdd_DuplicateSubscriptionOneRemovedDuringNotify_RemainingOneCalledOnce()
		{
			var list = new ObservableList<int>(new List<int>());
			var calls = 0;
			Action<int, int, int, ObservableUpdateType> duplicate = (_, _, _, _) => calls++;
			list.Observe((_, _, _, _) => list.StopObserving(duplicate));
			list.Observe(duplicate);
			list.Observe(duplicate);

			list.Add(1);

			Assert.AreEqual(1, calls);
		}

		[Test]
		public void ObservableFieldSet_SubscriberRemovedAndReaddedDuringNotify_ReaddedOneWaitsForNextNotify()
		{
			var field = new ObservableField<int>(0);
			var calls = 0;
			var readded = false;
			Action<int, int> target = (_, _) => calls++;
			field.Observe((_, _) =>
			{
				if (readded) return;
				readded = true;
				field.StopObserving(target);
				field.Observe(target);
			});
			field.Observe(target);

			field.Value = 1;
			Assert.AreEqual(0, calls);

			field.Value = 2;
			Assert.AreEqual(1, calls);
		}

		[Test]
		public void ObservableDictionaryRemove_StopObservingKeyDuringNotify_LaterKeySubscriberNotCalled()
		{
			var dictionary = new ObservableDictionary<int, int>(new Dictionary<int, int> { { 1, 10 } });
			var laterCalls = 0;
			dictionary.Observe(1, (_, _, _, _) => dictionary.StopObserving(1));
			dictionary.Observe(1, (_, _, _, _) => laterCalls++);

			dictionary.Remove(1);

			Assert.AreEqual(0, laterCalls);
		}

		[Test]
		public void ObservableDictionaryAdd_StopObservingAllDuringNotify_LaterSubscribersNotCalled()
		{
			var dictionary = new ObservableDictionary<int, int>(new Dictionary<int, int>())
			{
				ObservableUpdateFlag = ObservableUpdateFlag.Both
			};
			var laterCalls = 0;
			dictionary.Observe(1, (_, _, _, _) => dictionary.StopObservingAll());
			dictionary.Observe(1, (_, _, _, _) => laterCalls++);
			dictionary.Observe((_, _, _, _) => laterCalls++);

			dictionary.Add(1, 10);

			Assert.AreEqual(0, laterCalls);
		}

		[Test]
		public void ObservableDictionaryAdd_GlobalSubscribedFromKeyCallback_WaitsForNextOperation()
		{
			var dictionary = new ObservableDictionary<int, int>(new Dictionary<int, int>())
			{
				ObservableUpdateFlag = ObservableUpdateFlag.Both
			};
			var globalCalls = 0;
			Action<int, int, int, ObservableUpdateType> global = (_, _, _, _) => globalCalls++;
			dictionary.Observe(1, (_, _, _, _) => dictionary.Observe(global));

			dictionary.Add(1, 10);
			Assert.AreEqual(0, globalCalls);

			dictionary.Add(2, 20);
			Assert.AreEqual(1, globalCalls);
		}

		[Test]
		public void ObservableListClear_SubscribedFromFirstRemovedCallback_ReceivesNothingFromThisClear()
		{
			var list = new ObservableList<int>(new List<int> { 1, 2, 3 });
			var lateCalls = 0;
			var subscribed = false;
			list.Observe((_, _, _, _) =>
			{
				if (subscribed) return;
				subscribed = true;
				list.Observe((_, _, _, _) => lateCalls++);
			});

			list.Clear();

			Assert.AreEqual(0, lateCalls);
		}

		// ── 구독 중인데 사전에 없는 키가 있을 때 Clear 에서 KeyNotFoundException ──

		[Test]
		public void ObservableDictionaryClear_ObservedKeyMissing_NotifiesOnlyPresentKeys()
		{
			var dictionary = new ObservableDictionary<int, int>(new Dictionary<int, int> { { 1, 10 } });
			var removed = new List<int>();
			dictionary.Observe(1, (key, previous, _, type) => removed.Add(previous));
			dictionary.Observe(2, (key, previous, _, type) => removed.Add(previous));

			Assert.DoesNotThrow(() => dictionary.Clear());
			CollectionAssert.AreEqual(new[] { 10 }, removed);
			Assert.AreEqual(0, dictionary.Count);
		}

		[Test]
		public void ObservableResolverDictionaryClear_ObservedKeyMissing_DoesNotThrow()
		{
			var dictionary = new ObservableResolverDictionary<int, int, int, int>(
				new Dictionary<int, int> { { 1, 10 } },
				pair => pair,
				(key, value) => new KeyValuePair<int, int>(key, value));
			dictionary.Observe(2, (_, _, _, _) => { });

			Assert.DoesNotThrow(() => dictionary.Clear());
			Assert.AreEqual(0, dictionary.Count);
		}

		// ── 구독자 예외가 나머지 통지와 의존 갱신을 막음 ──

		[Test]
		public void ObservableFieldSet_SubscriberThrows_OtherSubscribersAndComputedStillUpdate()
		{
			var field = new ObservableField<int>(1);
			var computed = new ComputedField<int>(() => field.Value * 2);
			Assert.AreEqual(2, computed.Value);
			var laterCalls = 0;
			field.Observe((_, _) => throw new InvalidOperationException("boom"));
			field.Observe((_, _) => laterCalls++);

			var exception = Assert.Throws<InvalidOperationException>(() => field.Value = 5);

			Assert.AreEqual("boom", exception.Message);
			Assert.AreEqual(1, laterCalls);
			Assert.AreEqual(10, computed.Value);
		}

		[Test]
		public void ObservableListAdd_TwoSubscribersThrow_ThrowsAggregateAfterNotifyingAll()
		{
			var list = new ObservableList<int>(new List<int>());
			var calls = 0;
			list.Observe((_, _, _, _) => throw new InvalidOperationException("first"));
			list.Observe((_, _, _, _) => calls++);
			list.Observe((_, _, _, _) => throw new ArgumentException("second"));

			var exception = Assert.Throws<AggregateException>(() => list.Add(1));

			Assert.AreEqual(1, calls);
			Assert.AreEqual(2, exception.InnerExceptions.Count);
			Assert.AreEqual(1, list.Count);
		}

		[Test]
		public void ObservableBatchDispose_SubscriberThrows_ResumesAllObservables()
		{
			var first = new ObservableField<int>(0);
			var second = new ObservableField<int>(0);
			var secondCalls = 0;
			first.Observe((_, _) => throw new InvalidOperationException("boom"));
			second.Observe((_, _) => secondCalls++);

			var batch = new ObservableBatch();
			batch.Add(first);
			batch.Add(second);
			first.Value = 1;
			second.Value = 1;

			Assert.Throws<InvalidOperationException>(() => batch.Dispose());
			Assert.AreEqual(1, secondCalls);

			// 배치 상태에 남지 않았으면 이후 변경도 바로 통지됩니다
			second.Value = 2;
			Assert.AreEqual(2, secondCalls);
			Assert.DoesNotThrow(() => batch.Dispose());
			Assert.AreEqual(2, secondCalls);
		}

		[Test]
		public void ObservableListResume_SubscriberThrowsOnFirstItem_NotifiesRemainingItems()
		{
			var list = new ObservableList<int>(new List<int>());
			var notifiedIndexes = new List<int>();
			list.Observe((index, _, _, _) =>
			{
				notifiedIndexes.Add(index);
				if (index == 0) throw new InvalidOperationException("boom");
			});

			var batch = list.BeginBatch();
			list.Add(1);
			list.Add(2);

			Assert.Throws<InvalidOperationException>(() => batch.Dispose());
			CollectionAssert.AreEqual(new[] { 0, 1 }, notifiedIndexes);
		}

		// ── Clear 가 비우기 전에 의존 통지를 보내 ComputedField 가 낡은 값에 머묾 ──

		[Test]
		public void ObservableDictionaryClear_ComputedCount_SeesClearedState()
		{
			var dictionary = new ObservableDictionary<int, int>(new Dictionary<int, int> { { 1, 10 }, { 2, 20 } });
			var computed = new ComputedField<int>(() => dictionary.Count);
			Assert.AreEqual(2, computed.Value);

			dictionary.Clear();

			Assert.AreEqual(0, computed.Value);
		}

		[Test]
		public void ObservableHashSetClear_ComputedCount_SeesClearedState()
		{
			var set = new ObservableHashSet<int>(new[] { 1, 2, 3 });
			var computed = new ComputedField<int>(() => set.Count);
			Assert.AreEqual(3, computed.Value);

			set.Clear();

			Assert.AreEqual(0, computed.Value);
		}

		// ── Clear 의 Removed 콜백에서 처음 계산된 ComputedField 가 비우기 전 값에 머묾 ──

		[Test]
		public void ObservableListClear_ComputedFirstReadInRemovedCallback_SeesClearedState()
		{
			var list = new ObservableList<int>(new List<int> { 1 });
			var computed = new ComputedField<int>(() => list.Count);
			list.Observe((_, _, _, _) => _ = computed.Value);

			list.Clear();

			Assert.AreEqual(0, computed.Value);
		}

		[Test]
		public void ObservableDictionaryClear_ComputedFirstReadInRemovedCallback_SeesClearedState()
		{
			var dictionary = new ObservableDictionary<int, int>(new Dictionary<int, int> { { 1, 10 } });
			var computed = new ComputedField<int>(() => dictionary.Count);
			dictionary.Observe(1, (_, _, _, _) => _ = computed.Value);

			dictionary.Clear();

			Assert.AreEqual(0, computed.Value);
		}

		[Test]
		public void ObservableHashSetClear_ComputedFirstReadInRemovedCallback_SeesClearedState()
		{
			var set = new ObservableHashSet<int>(new[] { 1 });
			var computed = new ComputedField<int>(() => set.Count);
			set.Observe((_, _) => _ = computed.Value);

			set.Clear();

			Assert.AreEqual(0, computed.Value);
		}

		// ── 배치 안에서 비우면 재개할 항목이 없어 ComputedField 가 갱신되지 않음 ──

		[Test]
		public void ObservableListBatch_ClearedInsideBatch_ComputedSeesClearedState()
		{
			var list = new ObservableList<int>(new List<int> { 1 });
			var computed = new ComputedField<int>(() => list.Count);
			Assert.AreEqual(1, computed.Value);

			using (list.BeginBatch())
			{
				list.Clear();
			}

			Assert.AreEqual(0, computed.Value);
		}

		[Test]
		public void ObservableDictionaryBatch_LastKeyRemovedInsideBatch_ComputedSeesRemovedState()
		{
			var dictionary = new ObservableDictionary<int, int>(new Dictionary<int, int> { { 1, 10 } });
			var computed = new ComputedField<int>(() => dictionary.Count);
			Assert.AreEqual(1, computed.Value);

			using (dictionary.BeginBatch())
			{
				dictionary.Remove(1);
			}

			Assert.AreEqual(0, computed.Value);
		}

		[Test]
		public void ObservableDictionaryResume_EarlierKeyCallbackChangesLaterKey_LaterKeyGetsCurrentValue()
		{
			var dictionary = new ObservableDictionary<int, int>(new Dictionary<int, int> { { 1, 10 }, { 2, 20 } });
			var secondKeyValues = new List<int>();
			var changed = false;

			dictionary.Observe(1, (_, _, _, _) =>
			{
				if (!changed)
				{
					changed = true;
					dictionary[2] = 22;
				}
			});
			dictionary.Observe(2, (_, _, current, _) => secondKeyValues.Add(current));

			using (dictionary.BeginBatch())
			{
				dictionary[1] = 11;
				dictionary[2] = 21;
			}

			Assert.AreEqual(new[] { 22, 22 }, secondKeyValues);
		}

		[Test]
		public void ObservableHashSetBatch_ClearedInsideBatch_ComputedSeesClearedState()
		{
			var set = new ObservableHashSet<int>(new[] { 1 });
			var computed = new ComputedField<int>(() => set.Count);
			Assert.AreEqual(1, computed.Value);

			using (set.BeginBatch())
			{
				set.Clear();
			}

			Assert.AreEqual(0, computed.Value);
		}

		// ── 재개 콜백에서 새 배치를 열어도 남은 항목이 통지됨 ──

		[Test]
		public void ObservableListResume_CallbackOpensNewBatch_RemainingItemsWaitForThatBatch()
		{
			var list = new ObservableList<int>(new List<int> { 1, 2 });
			var notified = new List<(int Index, int Value)>();
			IDisposable nextBatch = null;
			list.Observe((index, _, value, _) =>
			{
				notified.Add((index, value));

				if (index == 0 && nextBatch == null)
				{
					nextBatch = list.BeginBatch();
					list[1] = 20;
				}
			});

			using (list.BeginBatch())
			{
				list[0] = 10;
			}

			CollectionAssert.AreEqual(new[] { (0, 10) }, notified);

			nextBatch.Dispose();

			CollectionAssert.AreEqual(new[] { (0, 10), (0, 10), (1, 20) }, notified);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void ObservableHashSetResume_CallbackOpensNewBatchAndAdds_StopsWithoutEnumeratorError(bool throwAfterAdd)
		{
			var set = new ObservableHashSet<int>(new[] { 1, 2 });
			var notified = new List<int>();
			IDisposable nextBatch = null;
			set.Observe((item, _) =>
			{
				notified.Add(item);

				if (nextBatch == null)
				{
					nextBatch = set.BeginBatch();
					set.Add(3);

					if (throwAfterAdd) throw new ArgumentException("boom");
				}
			});

			var batch = set.BeginBatch();

			if (throwAfterAdd)
			{
				Assert.Throws<ArgumentException>(() => batch.Dispose());
			}
			else
			{
				batch.Dispose();
			}

			Assert.AreEqual(1, notified.Count);

			nextBatch.Dispose();

			Assert.AreEqual(4, notified.Count);
			CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, notified.GetRange(1, 3));
		}

		[TestCase(false)]
		[TestCase(true)]
		public void ObservableDictionaryResume_GlobalCallbackOpensNewBatchAndAdds_StopsWithoutEnumeratorError(bool throwAfterAdd)
		{
			var dictionary = new ObservableDictionary<int, int>(new Dictionary<int, int> { { 1, 10 }, { 2, 20 } })
			{
				ObservableUpdateFlag = ObservableUpdateFlag.UpdateOnly
			};
			var notified = new List<int>();
			IDisposable nextBatch = null;
			dictionary.Observe((key, _, _, _) =>
			{
				notified.Add(key);

				if (nextBatch == null)
				{
					nextBatch = dictionary.BeginBatch();
					dictionary.Add(3, 30);

					if (throwAfterAdd) throw new ArgumentException("boom");
				}
			});

			var batch = dictionary.BeginBatch();

			if (throwAfterAdd)
			{
				Assert.Throws<ArgumentException>(() => batch.Dispose());
			}
			else
			{
				batch.Dispose();
			}

			Assert.AreEqual(1, notified.Count);

			nextBatch.Dispose();

			Assert.AreEqual(4, notified.Count);
			CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, notified.GetRange(1, 3));
		}

		// ── Clear 콜백이 새 배치를 열면 의존 갱신도 그 배치가 끝날 때 ──

		[Test]
		public void ObservableListClear_RemovedCallbackOpensNewBatch_DependencyWaitsForThatBatch()
		{
			var list = new ObservableList<int>(new List<int> { 1 });
			var computed = new ComputedField<int>(() => list.Count);
			var computedValues = new List<int>();
			computed.Observe((_, current) => computedValues.Add(current));
			IDisposable nextBatch = null;
			list.Observe((_, _, _, _) =>
			{
				if (nextBatch == null) nextBatch = list.BeginBatch();
			});

			list.Clear();

			CollectionAssert.IsEmpty(computedValues);

			nextBatch.Dispose();

			CollectionAssert.AreEqual(new[] { 0 }, computedValues);
		}

		[Test]
		public void ObservableDictionaryClear_RemovedCallbackOpensNewBatch_DependencyWaitsForThatBatch()
		{
			var dictionary = new ObservableDictionary<int, int>(new Dictionary<int, int> { { 1, 10 } });
			var computed = new ComputedField<int>(() => dictionary.Count);
			var computedValues = new List<int>();
			computed.Observe((_, current) => computedValues.Add(current));
			IDisposable nextBatch = null;
			dictionary.Observe(1, (_, _, _, _) =>
			{
				if (nextBatch == null) nextBatch = dictionary.BeginBatch();
			});

			dictionary.Clear();

			CollectionAssert.IsEmpty(computedValues);

			nextBatch.Dispose();

			CollectionAssert.AreEqual(new[] { 0 }, computedValues);
		}

		[Test]
		public void ObservableHashSetClear_RemovedCallbackOpensNewBatch_DependencyWaitsForThatBatch()
		{
			var set = new ObservableHashSet<int>(new[] { 1 });
			var computed = new ComputedField<int>(() => set.Count);
			var computedValues = new List<int>();
			computed.Observe((_, current) => computedValues.Add(current));
			IDisposable nextBatch = null;
			set.Observe((_, _) =>
			{
				if (nextBatch == null) nextBatch = set.BeginBatch();
			});

			set.Clear();

			CollectionAssert.IsEmpty(computedValues);

			nextBatch.Dispose();

			CollectionAssert.AreEqual(new[] { 0 }, computedValues);
		}

		// ── null 구독이 정상 구독의 해제를 막음 ──

		[Test]
		public void ObservableField_NullSubscriptionPresent_StopObservingOtherCallbackSucceeds()
		{
			var field = new ObservableField<int>(0);
			var calls = 0;
			Action<int, int> handler = (_, _) => calls++;
			field.Observe(null);
			field.Observe(handler);

			Assert.DoesNotThrow(() => field.StopObserving(handler));
			Assert.DoesNotThrow(() => field.StopObservingAll(this));

			// 남은 null 등록은 통지 때 예외가 되지만 해제한 handler 는 호출되지 않습니다
			Assert.Throws<NullReferenceException>(() => field.Value = 1);
			Assert.AreEqual(0, calls);
		}
	}
}
