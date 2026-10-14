using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Geuneda.DataExtensions;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geuneda.DataExtensions.Tests
{
	[TestFixture]
	public class SecureValueMonitorTest
	{
		private struct RawView
		{
			public ulong Data;
			public uint Check;
			public uint Nonce;
		}

		private readonly List<Action<Type>> _subscribed = new List<Action<Type>>();

		[TearDown]
		public void TearDown()
		{
			foreach (var handler in _subscribed)
			{
				SecureValueMonitor.TamperDetected -= handler;
			}

			_subscribed.Clear();
		}

		private void Subscribe(Action<Type> handler)
		{
			_subscribed.Add(handler);
			SecureValueMonitor.TamperDetected += handler;
		}

		private static SecureInt Tampered(int value)
		{
			SecureInt secure = value;
			UnsafeUtility.As<SecureInt, RawView>(ref secure).Data ^= 1;
			return secure;
		}

		[Test]
		public void NoSubscriber_ReadStillReturnsValue()
		{
			Assert.That(Tampered(8).Value, Is.EqualTo(9));
		}

		[Test]
		public void HandlerException_IsLoggedAndOtherHandlersRun()
		{
			var calls = 0;
			Subscribe(_ => throw new InvalidOperationException("handler failed"));
			Subscribe(_ => calls++);
			LogAssert.Expect(LogType.Exception, new Regex("handler failed"));

			var value = Tampered(8).Value;

			Assert.That(value, Is.EqualTo(9));
			Assert.That(calls, Is.EqualTo(1));
		}

		[Test]
		public void TamperedReadInsideHandler_IsNotReportedAgain()
		{
			var calls = 0;
			var tampered = Tampered(20);
			Subscribe(type =>
			{
				calls++;
				var nested = tampered.Value;
			});

			_ = tampered.Value;
			_ = tampered.Value;

			Assert.That(calls, Is.EqualTo(2));
		}

		[Test]
		public void HandlerCanUnsubscribeItselfOnFirstDetection()
		{
			var calls = 0;
			Action<Type> once = null;
			once = _ =>
			{
				calls++;
				SecureValueMonitor.TamperDetected -= once;
			};
			Subscribe(once);
			var tampered = Tampered(3);

			_ = tampered.Value;
			_ = tampered.Value;

			Assert.That(calls, Is.EqualTo(1));
		}

		[Test]
		public void Handler_RunsOnReadingThread()
		{
			var handlerThread = -1;
			Subscribe(_ => handlerThread = Thread.CurrentThread.ManagedThreadId);
			var tampered = Tampered(5);

			var readerThread = Task.Run(() =>
			{
				_ = tampered.Value;
				return Thread.CurrentThread.ManagedThreadId;
			}).Result;

			Assert.That(readerThread, Is.Not.EqualTo(Thread.CurrentThread.ManagedThreadId));
			Assert.That(handlerThread, Is.EqualTo(readerThread));
		}

		[Test]
		public void ResetState_RunsAtSubsystemRegistrationAndClearsHandlers()
		{
			var method = typeof(SecureValueMonitor).GetMethod("ResetState", BindingFlags.Static | BindingFlags.NonPublic);
			var attribute = method?.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
			var calls = 0;
			SecureValueMonitor.TamperDetected += _ => calls++;

			method?.Invoke(null, null);
			_ = Tampered(1).Value;

			Assert.That(attribute, Is.Not.Null);
			Assert.That(attribute.loadType, Is.EqualTo(RuntimeInitializeLoadType.SubsystemRegistration));
			Assert.That(calls, Is.EqualTo(0));
		}
	}
}
