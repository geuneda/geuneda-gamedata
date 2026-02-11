using System;
using Geuneda.DataExtensions;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests.Boundary
{
	[TestFixture]
	public class floatPBoundaryTest
	{
		[Test]
		public void MaxValue_Operations()
		{
			var max = floatP.MaxValue;
			// float 정밀도 한계로 인해 MaxValue에 1을 더해도 무한대로 오버플로되지 않습니다
			// (가수부가 차이를 표현할 수 없음). 2를 곱하면 오버플로가 발생합니다.
			Assert.IsTrue((max * (floatP)2f).IsInfinity());
			Assert.AreEqual(max, max * floatP.One);
		}

		[Test]
		public void MinValue_Operations()
		{
			var min = floatP.MinValue;
			// float 정밀도 한계로 인해 MinValue에서 1을 빼도 무한대로 오버플로되지 않습니다.
			// 2를 곱하면 오버플로가 발생합니다.
			Assert.IsTrue((min * (floatP)2f).IsInfinity());
			Assert.AreEqual(min, min * floatP.One);
		}

		[Test]
		public void NaN_Propagation_AllOperations()
		{
			var nan = floatP.NaN;
			Assert.IsTrue((nan + floatP.One).IsNaN());
			Assert.IsTrue((nan * floatP.Zero).IsNaN());
			Assert.IsTrue(MathfloatP.Sin(nan).IsNaN());
		}

		[Test]
		public void Zero_NegativeZero_Equality()
		{
			var zero = floatP.Zero;
			var negZero = -floatP.Zero;
			Assert.IsTrue(zero == negZero);
			Assert.IsFalse(zero.RawValue == negZero.RawValue); // 부호 비트에서 차이가 있습니다
		}
	}
}
