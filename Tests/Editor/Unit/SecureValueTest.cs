using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geuneda.DataExtensions;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Geuneda.DataExtensions.Tests
{
	[TestFixture]
	public class SecureValueTest
	{
		// 보안 값의 메모리 배치. 메모리 편집 도구처럼 바이트를 직접 바꿀 때 씁니다
		private struct RawView
		{
			public ulong Data;
			public uint Check;
			public uint Nonce;
		}

		private readonly List<Type> _detected = new List<Type>();

		[SetUp]
		public void SetUp()
		{
			_detected.Clear();
			SecureValueMonitor.TamperDetected += OnTamperDetected;
		}

		[TearDown]
		public void TearDown()
		{
			SecureValueMonitor.TamperDetected -= OnTamperDetected;
		}

		private void OnTamperDetected(Type type)
		{
			_detected.Add(type);
		}

		private static ref RawView Raw<T>(ref T value) where T : struct
		{
			return ref UnsafeUtility.As<T, RawView>(ref value);
		}

		[Test]
		public void Layout_Is16Bytes()
		{
			Assert.That(UnsafeUtility.SizeOf<SecureInt>(), Is.EqualTo(16));
			Assert.That(UnsafeUtility.SizeOf<SecureLong>(), Is.EqualTo(16));
			Assert.That(UnsafeUtility.SizeOf<SecureFloat>(), Is.EqualTo(16));
			Assert.That(UnsafeUtility.SizeOf<SecureDouble>(), Is.EqualTo(16));
			Assert.That(UnsafeUtility.SizeOf<SecureFloatP>(), Is.EqualTo(16));
		}

		[Test]
		public void Int_RoundTrip()
		{
			var random = new Random(1);
			var values = new List<int> { 0, 1, -1, int.MaxValue, int.MinValue };

			for (var i = 0; i < 10000; i++)
			{
				values.Add(random.Next(int.MinValue, int.MaxValue));
			}

			foreach (var value in values)
			{
				SecureInt secure = value;
				Assert.That(secure.Value, Is.EqualTo(value));
			}

			Assert.That(_detected, Is.Empty);
		}

		[Test]
		public void Long_RoundTrip()
		{
			var random = new Random(2);
			var bytes = new byte[8];
			var values = new List<long> { 0, 1, -1, long.MaxValue, long.MinValue };

			for (var i = 0; i < 10000; i++)
			{
				random.NextBytes(bytes);
				values.Add(BitConverter.ToInt64(bytes, 0));
			}

			foreach (var value in values)
			{
				SecureLong secure = value;
				Assert.That(secure.Value, Is.EqualTo(value));
			}

			Assert.That(_detected, Is.Empty);
		}

		[Test]
		public void Float_RoundTripKeepsBits()
		{
			var random = new Random(3);
			var bytes = new byte[4];
			var values = new List<float> { 0f, -0f, float.Epsilon, float.MaxValue, float.MinValue, float.PositiveInfinity, float.NegativeInfinity, float.NaN };

			for (var i = 0; i < 10000; i++)
			{
				random.NextBytes(bytes);
				values.Add(BitConverter.ToSingle(bytes, 0));
			}

			foreach (var value in values)
			{
				SecureFloat secure = value;
				Assert.That(BitConverter.SingleToInt32Bits(secure.Value), Is.EqualTo(BitConverter.SingleToInt32Bits(value)));
			}

			Assert.That(_detected, Is.Empty);
		}

		[Test]
		public void Double_RoundTripKeepsBits()
		{
			var random = new Random(4);
			var bytes = new byte[8];
			var values = new List<double> { 0d, -0d, double.Epsilon, double.MaxValue, double.MinValue, double.PositiveInfinity, double.NaN };

			for (var i = 0; i < 10000; i++)
			{
				random.NextBytes(bytes);
				values.Add(BitConverter.ToDouble(bytes, 0));
			}

			foreach (var value in values)
			{
				SecureDouble secure = value;
				Assert.That(BitConverter.DoubleToInt64Bits(secure.Value), Is.EqualTo(BitConverter.DoubleToInt64Bits(value)));
			}

			Assert.That(_detected, Is.Empty);
		}

		[Test]
		public void FloatP_RoundTripKeepsRawValue()
		{
			var random = new Random(5);
			var bytes = new byte[4];
			var raws = new List<uint> { 0, 0x80000000, 1, 0x7F800000, 0xFF800000, 0x7FC00000 };

			for (var i = 0; i < 10000; i++)
			{
				random.NextBytes(bytes);
				raws.Add(BitConverter.ToUInt32(bytes, 0));
			}

			foreach (var raw in raws)
			{
				SecureFloatP secure = floatP.FromRaw(raw);
				Assert.That(secure.Value.RawValue, Is.EqualTo(raw));
			}

			Assert.That(_detected, Is.Empty);
		}

		[Test]
		public void Default_ReadsZeroWithoutDetection()
		{
			Assert.That(default(SecureInt).Value, Is.EqualTo(0));
			Assert.That(default(SecureLong).Value, Is.EqualTo(0L));
			Assert.That(default(SecureFloat).Value, Is.EqualTo(0f));
			Assert.That(default(SecureDouble).Value, Is.EqualTo(0d));
			Assert.That(default(SecureFloatP).Value.RawValue, Is.EqualTo(0u));
			Assert.That(_detected, Is.Empty);
		}

		[Test]
		public void Memory_DoesNotHoldPlainValue()
		{
			const int plain = 0x5A5A1234;
			SecureInt secure = plain;
			var raw = Raw(ref secure);

			Assert.That(unchecked((uint)raw.Data), Is.Not.EqualTo((uint)plain));
			Assert.That((uint)(raw.Data >> 32), Is.Not.EqualTo((uint)plain));
			Assert.That(raw.Check, Is.Not.EqualTo((uint)plain));
		}

		[Test]
		public void Write_ReencodesSameValueDifferently()
		{
			SecureInt first = 7;
			SecureInt second = 7;
			var firstRaw = Raw(ref first);
			var secondRaw = Raw(ref second);

			Assert.That(firstRaw.Nonce, Is.Not.EqualTo(secondRaw.Nonce));
			Assert.That(firstRaw.Data, Is.Not.EqualTo(secondRaw.Data));
			Assert.That(first.Value, Is.EqualTo(second.Value));
		}

		[Test]
		public void TamperedData_ReportsAndReturnsDecodedValue()
		{
			SecureInt secure = 12345;
			Raw(ref secure).Data ^= 1;

			var value = secure.Value;

			Assert.That(value, Is.EqualTo(12344));
			Assert.That(_detected, Is.EqualTo(new[] { typeof(SecureInt) }));
		}

		[Test]
		public void TamperedCheck_Reports()
		{
			SecureLong secure = 99L;
			Raw(ref secure).Check ^= 0x100;

			Assert.That(secure.Value, Is.EqualTo(99L));
			Assert.That(_detected, Is.EqualTo(new[] { typeof(SecureLong) }));
		}

		[Test]
		public void TamperedNonce_Reports()
		{
			SecureFloat secure = 1.5f;
			Raw(ref secure).Nonce += 1;

			_ = secure.Value;

			Assert.That(_detected, Is.EqualTo(new[] { typeof(SecureFloat) }));
		}

		[Test]
		public void ZeroedNonceWithData_Reports()
		{
			SecureDouble secure = 2.5;
			Raw(ref secure).Nonce = 0;

			_ = secure.Value;

			Assert.That(_detected, Is.EqualTo(new[] { typeof(SecureDouble) }));
		}

		[Test]
		public void TamperedUpperBitsOfInt_Reports()
		{
			SecureInt secure = 5;
			Raw(ref secure).Data ^= 1UL << 40;

			_ = secure.Value;

			Assert.That(_detected, Is.EqualTo(new[] { typeof(SecureInt) }));
		}

		[Test]
		public void TamperedFloatP_ReportsItsType()
		{
			SecureFloatP secure = (floatP)3f;
			Raw(ref secure).Data ^= 0x10;

			_ = secure.Value;

			Assert.That(_detected, Is.EqualTo(new[] { typeof(SecureFloatP) }));
		}

		[Test]
		public void FullyZeroedMemory_ReadsZeroWithoutDetection()
		{
			// 문서화한 한계: 모든 필드를 0 으로 지우면 default 와 구별할 수 없습니다
			SecureInt secure = 777;
			Raw(ref secure) = default;

			Assert.That(secure.Value, Is.EqualTo(0));
			Assert.That(_detected, Is.Empty);
		}

		[Test]
		public void EveryTamperedRead_Reports()
		{
			SecureInt secure = 10;
			Raw(ref secure).Data ^= 2;

			_ = secure.Value;
			_ = secure.Value;
			int implicitRead = secure;

			Assert.That(_detected.Count, Is.EqualTo(3));
		}

		[Test]
		public void Rewrite_ClearsTamper()
		{
			SecureInt secure = 10;
			Raw(ref secure).Data ^= 2;
			secure = 10;

			Assert.That(secure.Value, Is.EqualTo(10));
			Assert.That(_detected, Is.Empty);
		}

		[Test]
		public void Operators_WorkThroughImplicitConversion()
		{
			SecureInt gold = 5;
			gold += 3;
			gold++;
			gold--;
			gold++;
			SecureInt other = 9;
			SecureLong big = 1L << 40;
			big += gold;

			Assert.That(gold.Value, Is.EqualTo(9));
			Assert.That(gold == other, Is.True);
			Assert.That(gold < 10, Is.True);
			Assert.That(gold * 2, Is.EqualTo(18));
			Assert.That(big.Value, Is.EqualTo((1L << 40) + 9));

			SecureFloat speed = 1.5f;
			speed *= 2;
			Assert.That(speed.Value, Is.EqualTo(3f));

			SecureDouble ratio = 0.25;
			ratio++;
			Assert.That(ratio.Value, Is.EqualTo(1.25));

			SecureFloatP hp = (floatP)10;
			hp -= (floatP)2.5f;
			hp++;
			floatP doubled = hp.Value * 2;
			Assert.That((float)hp.Value, Is.EqualTo(8.5f));
			Assert.That((float)doubled, Is.EqualTo(17f));
			Assert.That(hp < (floatP)9, Is.True);
			Assert.That(_detected, Is.Empty);
		}

		[Test]
		public void IncrementOverflow_WrapsLikeInt()
		{
			SecureInt value = int.MaxValue;
			value++;

			Assert.That(value.Value, Is.EqualTo(int.MinValue));
		}

		[Test]
		public void Equality_ComparesValuesNotEncodings()
		{
			SecureInt first = 42;
			SecureInt second = 42;
			var set = new HashSet<SecureInt> { first };
			var lookup = new Dictionary<SecureLong, string> { { 7L, "seven" } };

			Assert.That(first.Equals(second), Is.True);
			Assert.That(first.Equals((object)second), Is.True);
			Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
			Assert.That(set.Contains(second), Is.True);
			Assert.That(lookup[7L], Is.EqualTo("seven"));
			Assert.That(EqualityComparer<SecureFloatP>.Default.Equals((floatP)1.5f, (floatP)1.5f), Is.True);
			Assert.That(new SecureFloat(float.NaN).Equals(new SecureFloat(float.NaN)), Is.True);
		}

		[Test]
		public void Sort_UsesValues()
		{
			var list = new List<SecureInt> { 3, -1, 2 };
			list.Sort();

			Assert.That(list.Select(v => v.Value), Is.EqualTo(new[] { -1, 2, 3 }));
		}

		[Test]
		public void FloatPNaN_EqualityAndOrderAreConsistent()
		{
			SecureFloatP negativeNaN = floatP.FromRaw(0xFFC00000);
			SecureFloatP positiveNaN = floatP.FromRaw(0x7FC00000);
			SecureFloatP finite = (floatP)1.5f;

			Assert.That(negativeNaN.Equals(finite), Is.False);
			Assert.That(finite.Equals(negativeNaN), Is.False);
			Assert.That(negativeNaN.Equals(positiveNaN), Is.True);
			Assert.That(negativeNaN.GetHashCode(), Is.EqualTo(positiveNaN.GetHashCode()));

			var list = new List<SecureFloatP> { finite, positiveNaN, (floatP)(-2), negativeNaN };
			list.Sort();

			Assert.That(list[0].Value.IsNaN() && list[1].Value.IsNaN(), Is.True);
			Assert.That((float)list[2].Value, Is.EqualTo(-2f));
			Assert.That((float)list[3].Value, Is.EqualTo(1.5f));
		}

		[Test]
		public void NonceCounterWrap_SkipsZeroAndKeepsDecoding()
		{
			// nonce 는 32비트라 프로세스 전체 쓰기 2^32-1 회마다 순환합니다. 0 은 default 전용이라 건너뜁니다
			var counter = typeof(SecureInt).Assembly.GetType("Geuneda.DataExtensions.SecureBits")
				.GetField("_nonceCounter", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			var original = counter.GetValue(null);

			try
			{
				counter.SetValue(null, -2);
				SecureInt last = 1;
				SecureInt wrapped = 2;

				Assert.That(Raw(ref last).Nonce, Is.EqualTo(uint.MaxValue));
				Assert.That(Raw(ref wrapped).Nonce, Is.EqualTo(1u));
				Assert.That(last.Value, Is.EqualTo(1));
				Assert.That(wrapped.Value, Is.EqualTo(2));
				Assert.That(_detected, Is.Empty);
			}
			finally
			{
				counter.SetValue(null, original);
			}
		}

		[Test]
		public void ToString_FormatsValue()
		{
			SecureInt gold = 1234567;
			SecureFloat rate = 0.5f;

			Assert.That(gold.ToString(), Is.EqualTo(1234567.ToString()));
			Assert.That(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:N0}", gold), Is.EqualTo("1,234,567"));
			Assert.That(rate.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("0.50"));
		}

		[Test]
		public void ConcurrentWrites_UseUniqueNoncesAndDecode()
		{
			const int perThread = 20000;
			var results = new SecureInt[4][];

			Parallel.For(0, results.Length, thread =>
			{
				var values = new SecureInt[perThread];

				for (var i = 0; i < perThread; i++)
				{
					values[i] = thread * perThread + i;
				}

				results[thread] = values;
			});

			var nonces = new HashSet<uint>();

			for (var thread = 0; thread < results.Length; thread++)
			{
				for (var i = 0; i < perThread; i++)
				{
					Assert.That(results[thread][i].Value, Is.EqualTo(thread * perThread + i));
					Assert.That(nonces.Add(Raw(ref results[thread][i]).Nonce), Is.True);
				}
			}

			Assert.That(_detected, Is.Empty);
		}

		[Test]
		public void WriteAndRead_DoNotAllocate()
		{
			SecureInt gold = 0;
			SecureFloatP hp = floatP.One;

			// 측정기가 할당을 잡는지 먼저 확인합니다(GC.GetAllocatedBytesForCurrentThread 는 Unity Mono 에서 늘 0)
			Assert.That(() => { _ = new object[8]; }, Is.AllocatingGCMemory());
			Assert.That(() =>
			{
				for (var i = 0; i < 1000; i++)
				{
					gold += i;
					hp = hp.Value + floatP.One;
				}
			}, Is.Not.AllocatingGCMemory());
			Assert.That(gold.Value, Is.EqualTo(499500));
		}
	}
}
