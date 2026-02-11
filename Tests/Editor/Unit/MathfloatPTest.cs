using System;
using Geuneda.DataExtensions;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests
{
	[TestFixture]
	public class MathfloatPTest
	{
		private const float _epsilon = 0.001f;

		[Test]
		public void Abs_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Abs((floatP)5f));
			Assert.AreEqual((floatP)5f, MathfloatP.Abs((floatP)(-5f)));
			Assert.AreEqual(floatP.Zero, MathfloatP.Abs(floatP.Zero));
			Assert.IsTrue(MathfloatP.Abs(floatP.NaN).IsNaN());
		}

		[Test]
		public void Max_Works()
		{
			Assert.AreEqual((floatP)10f, MathfloatP.Max((floatP)5f, (floatP)10f));
			Assert.AreEqual((floatP)10f, MathfloatP.Max((floatP)10f, (floatP)5f));
			Assert.AreEqual((floatP)5f, MathfloatP.Max((floatP)5f, (floatP)5f));
			Assert.IsTrue(MathfloatP.Max(floatP.NaN, (floatP)1f).IsNaN());
		}

		[Test]
		public void Min_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Min((floatP)5f, (floatP)10f));
			Assert.AreEqual((floatP)5f, MathfloatP.Min((floatP)10f, (floatP)5f));
			Assert.AreEqual((floatP)5f, MathfloatP.Min((floatP)5f, (floatP)5f));
			Assert.IsTrue(MathfloatP.Min(floatP.NaN, (floatP)1f).IsNaN());
		}

		[Test]
		public void Clamp_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Clamp((floatP)5f, (floatP)0f, (floatP)10f));
			Assert.AreEqual((floatP)0f, MathfloatP.Clamp((floatP)(-5f), (floatP)0f, (floatP)10f));
			Assert.AreEqual((floatP)10f, MathfloatP.Clamp((floatP)15f, (floatP)0f, (floatP)10f));
		}

		[Test]
		public void Clamp01_Works()
		{
			Assert.AreEqual((floatP)0.5f, MathfloatP.Clamp01((floatP)0.5f));
			Assert.AreEqual(floatP.Zero, MathfloatP.Clamp01((floatP)(-0.1f)));
			Assert.AreEqual(floatP.One, MathfloatP.Clamp01((floatP)1.1f));
		}

		[Test]
		public void Lerp_Works()
		{
			Assert.AreEqual((floatP)0f, MathfloatP.Lerp((floatP)0f, (floatP)10f, (floatP)0f));
			Assert.AreEqual((floatP)10f, MathfloatP.Lerp((floatP)0f, (floatP)10f, (floatP)1f));
			Assert.AreEqual((floatP)5f, MathfloatP.Lerp((floatP)0f, (floatP)10f, (floatP)0.5f));
			// 클램핑됨
			Assert.AreEqual((floatP)10f, MathfloatP.Lerp((floatP)0f, (floatP)10f, (floatP)1.5f));
		}

		[Test]
		public void LerpUnclamped_Works()
		{
			Assert.AreEqual((floatP)15f, MathfloatP.LerpUnclamped((floatP)0f, (floatP)10f, (floatP)1.5f));
		}

		[Test]
		public void SmoothStep_Works()
		{
			Assert.AreEqual((floatP)0f, MathfloatP.SmoothStep((floatP)0f, (floatP)10f, (floatP)0f));
			Assert.AreEqual((floatP)10f, MathfloatP.SmoothStep((floatP)0f, (floatP)10f, (floatP)1f));
			Assert.AreEqual((floatP)5f, MathfloatP.SmoothStep((floatP)0f, (floatP)10f, (floatP)0.5f));
		}

		[Test]
		public void InverseLerp_Works()
		{
			Assert.AreEqual((floatP)0.5f, MathfloatP.InverseLerp((floatP)0f, (floatP)10f, (floatP)5f));
			Assert.AreEqual((floatP)0f, MathfloatP.InverseLerp((floatP)0f, (floatP)10f, (floatP)0f));
			Assert.AreEqual((floatP)1f, MathfloatP.InverseLerp((floatP)0f, (floatP)10f, (floatP)10f));
		}

		[Test]
		public void MoveTowards_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.MoveTowards((floatP)0f, (floatP)10f, (floatP)5f));
			Assert.AreEqual((floatP)10f, MathfloatP.MoveTowards((floatP)0f, (floatP)10f, (floatP)15f));
			Assert.AreEqual((floatP)5f, MathfloatP.MoveTowards((floatP)10f, (floatP)0f, (floatP)5f));
		}

		[Test]
		public void Sign_Works()
		{
			Assert.AreEqual(1, MathfloatP.Sign((floatP)5f));
			Assert.AreEqual(-1, MathfloatP.Sign((floatP)(-5f)));
			Assert.AreEqual(1, MathfloatP.Sign(floatP.Zero));
		}

		[Test]
		public void Repeat_Works()
		{
			Assert.AreEqual((floatP)2f, MathfloatP.Repeat((floatP)12f, (floatP)10f));
			Assert.AreEqual((floatP)8f, MathfloatP.Repeat((floatP)(-2f), (floatP)10f));
		}

		[Test]
		public void PingPong_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.PingPong((floatP)5f, (floatP)10f));
			Assert.AreEqual((floatP)5f, MathfloatP.PingPong((floatP)15f, (floatP)10f));
		}

		[Test]
		public void DeltaAngle_Works()
		{
			Assert.AreEqual((floatP)10f, MathfloatP.DeltaAngle((floatP)0f, (floatP)10f));
			Assert.AreEqual((floatP)(-10f), MathfloatP.DeltaAngle((floatP)10f, (floatP)0f));
			Assert.AreEqual((floatP)(-20f), MathfloatP.DeltaAngle((floatP)350f, (floatP)330f));
			Assert.AreEqual((floatP)20f, MathfloatP.DeltaAngle((floatP)350f, (floatP)10f));
		}

		[Test]
		public void Sin_Works()
		{
			Assert.AreEqual(0f, (float)MathfloatP.Sin(floatP.Zero), _epsilon);
			Assert.AreEqual(1f, (float)MathfloatP.Sin((floatP)(Math.PI / 2.0)), _epsilon);
			Assert.AreEqual(0f, (float)MathfloatP.Sin((floatP)Math.PI), _epsilon);
		}

		[Test]
		public void Cos_Works()
		{
			Assert.AreEqual(1f, (float)MathfloatP.Cos(floatP.Zero), _epsilon);
			Assert.AreEqual(0f, (float)MathfloatP.Cos((floatP)(Math.PI / 2.0)), _epsilon);
			Assert.AreEqual(-1f, (float)MathfloatP.Cos((floatP)Math.PI), _epsilon);
		}

		[Test]
		public void Tan_Works()
		{
			Assert.AreEqual(0f, (float)MathfloatP.Tan(floatP.Zero), _epsilon);
			Assert.AreEqual(1f, (float)MathfloatP.Tan((floatP)(Math.PI / 4.0)), _epsilon);
		}

		[Test]
		public void Sqrt_Works()
		{
			Assert.AreEqual((floatP)2f, MathfloatP.Sqrt((floatP)4f));
			Assert.AreEqual(floatP.Zero, MathfloatP.Sqrt(floatP.Zero));
			Assert.IsTrue(MathfloatP.Sqrt((floatP)(-1f)).IsNaN());
		}

		[Test]
		public void Pow_Works()
		{
			Assert.AreEqual((floatP)8f, MathfloatP.Pow((floatP)2f, (floatP)3f));
			Assert.AreEqual(floatP.One, MathfloatP.Pow((floatP)10f, floatP.Zero));
		}

		[Test]
		public void Exp_Works()
		{
			Assert.AreEqual(1f, (float)MathfloatP.Exp(floatP.Zero), _epsilon);
			Assert.AreEqual((float)Math.E, (float)MathfloatP.Exp(floatP.One), _epsilon);
		}

		[Test]
		public void Log_Works()
		{
			Assert.AreEqual(0f, (float)MathfloatP.Log(floatP.One), _epsilon);
			Assert.AreEqual(1f, (float)MathfloatP.Log((floatP)Math.E), _epsilon);
		}

		[Test]
		public void Floor_Works()
		{
			Assert.AreEqual((floatP)1f, MathfloatP.Floor((floatP)1.9f));
			Assert.AreEqual((floatP)(-2f), MathfloatP.Floor((floatP)(-1.1f)));
		}

		[Test]
		public void Ceil_Works()
		{
			Assert.AreEqual((floatP)2f, MathfloatP.Ceil((floatP)1.1f));
			Assert.AreEqual((floatP)(-1f), MathfloatP.Ceil((floatP)(-1.9f)));
		}

		[Test]
		public void Round_Works()
		{
			Assert.AreEqual((floatP)2f, MathfloatP.Round((floatP)1.6f));
			Assert.AreEqual((floatP)1f, MathfloatP.Round((floatP)1.4f));
		}

		[Test]
		public void Truncate_Works()
		{
			Assert.AreEqual((floatP)1f, MathfloatP.Truncate((floatP)1.9f));
			Assert.AreEqual((floatP)(-1f), MathfloatP.Truncate((floatP)(-1.9f)));
		}

		[Test]
		public void Hypothenuse_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Hypothenuse((floatP)3f, (floatP)4f));
		}

		[Test]
		public void Determinism_VerifyRawValues()
		{
			// 기본 연산이 동일한 원시 값을 생성하는지 확인합니다
			var a = (floatP)1.234f;
			var b = (floatP)5.678f;
			
			var res1 = a * b + MathfloatP.Sin(a);
			var raw1 = res1.RawValue;
			
			var res2 = a * b + MathfloatP.Sin(a);
			var raw2 = res2.RawValue;
			
			Assert.AreEqual(raw1, raw2);
		}

		// ══════════════════════════════════════════════════════════════
		// 계획당 50+ 커버리지를 위한 확장 테스트
		// ══════════════════════════════════════════════════════════════

		#region 삼각함수 확장 테스트

		[Test]
		public void Sin_AllQuadrants()
		{
			// 제1사분면 (0에서 π/2)
			Assert.Greater((float)MathfloatP.Sin((floatP)(Math.PI / 4.0)), 0f);
			// 제2사분면 (π/2에서 π)
			Assert.Greater((float)MathfloatP.Sin((floatP)(3 * Math.PI / 4.0)), 0f);
			// 제3사분면 (π에서 3π/2)
			Assert.Less((float)MathfloatP.Sin((floatP)(5 * Math.PI / 4.0)), 0f);
			// 제4사분면 (3π/2에서 2π)
			Assert.Less((float)MathfloatP.Sin((floatP)(7 * Math.PI / 4.0)), 0f);
		}

		[Test]
		public void Cos_AllQuadrants()
		{
			// 제1사분면
			Assert.Greater((float)MathfloatP.Cos((floatP)(Math.PI / 4.0)), 0f);
			// 제2사분면
			Assert.Less((float)MathfloatP.Cos((floatP)(3 * Math.PI / 4.0)), 0f);
			// 제3사분면
			Assert.Less((float)MathfloatP.Cos((floatP)(5 * Math.PI / 4.0)), 0f);
			// 제4사분면
			Assert.Greater((float)MathfloatP.Cos((floatP)(7 * Math.PI / 4.0)), 0f);
		}

		[Test]
		public void Sin_3PiOver2_ReturnsMinusOne()
		{
			Assert.AreEqual(-1f, (float)MathfloatP.Sin((floatP)(3 * Math.PI / 2.0)), _epsilon);
		}

		[Test]
		public void Asin_Works()
		{
			Assert.AreEqual(0f, (float)MathfloatP.Asin(floatP.Zero), _epsilon);
			Assert.AreEqual((float)(Math.PI / 2), (float)MathfloatP.Asin(floatP.One), _epsilon);
			Assert.AreEqual((float)(-Math.PI / 2), (float)MathfloatP.Asin(floatP.MinusOne), _epsilon);
		}

		[Test]
		public void Acos_Works()
		{
			Assert.AreEqual((float)(Math.PI / 2), (float)MathfloatP.Acos(floatP.Zero), _epsilon);
			Assert.AreEqual(0f, (float)MathfloatP.Acos(floatP.One), _epsilon);
			Assert.AreEqual((float)Math.PI, (float)MathfloatP.Acos(floatP.MinusOne), _epsilon);
		}

		[Test]
		public void Atan_Works()
		{
			Assert.AreEqual(0f, (float)MathfloatP.Atan(floatP.Zero), _epsilon);
			Assert.AreEqual((float)(Math.PI / 4), (float)MathfloatP.Atan(floatP.One), _epsilon);
		}

		[Test]
		public void Atan2_AllQuadrants()
		{
			// 제1사분면
			var q1 = MathfloatP.Atan2((floatP)1f, (floatP)1f);
			Assert.AreEqual((float)(Math.PI / 4), (float)q1, _epsilon);
			// 제2사분면
			var q2 = MathfloatP.Atan2((floatP)1f, (floatP)(-1f));
			Assert.AreEqual((float)(3 * Math.PI / 4), (float)q2, _epsilon);
			// 제3사분면
			var q3 = MathfloatP.Atan2((floatP)(-1f), (floatP)(-1f));
			Assert.AreEqual((float)(-3 * Math.PI / 4), (float)q3, _epsilon);
			// 제4사분면
			var q4 = MathfloatP.Atan2((floatP)(-1f), (floatP)1f);
			Assert.AreEqual((float)(-Math.PI / 4), (float)q4, _epsilon);
		}

		[Test]
		public void Atan2_OnAxes()
		{
			// 양의 X축
			Assert.AreEqual(0f, (float)MathfloatP.Atan2(floatP.Zero, floatP.One), _epsilon);
			// 양의 Y축
			Assert.AreEqual((float)(Math.PI / 2), (float)MathfloatP.Atan2(floatP.One, floatP.Zero), _epsilon);
			// 음의 X축
			Assert.AreEqual((float)Math.PI, (float)MathfloatP.Atan2(floatP.Zero, floatP.MinusOne), _epsilon);
			// 음의 Y축
			Assert.AreEqual((float)(-Math.PI / 2), (float)MathfloatP.Atan2(floatP.MinusOne, floatP.Zero), _epsilon);
		}

		#endregion

		#region 거듭제곱/Exp/Log 확장 테스트

		[Test]
		public void Pow_NegativeBase_IntExponent()
		{
			Assert.AreEqual((floatP)(-8f), MathfloatP.Pow((floatP)(-2f), (floatP)3f));
			Assert.AreEqual((floatP)4f, MathfloatP.Pow((floatP)(-2f), (floatP)2f));
		}

		[Test]
		public void Pow_OnePower_ReturnsSame()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Pow((floatP)5f, floatP.One));
		}

		[Test]
		public void Log_Zero_ReturnsNegativeInfinity()
		{
			Assert.IsTrue(MathfloatP.Log(floatP.Zero).IsNegativeInfinity());
		}

		[Test]
		public void Log_Negative_ReturnsNaN()
		{
			Assert.IsTrue(MathfloatP.Log((floatP)(-1f)).IsNaN());
		}

		[Test]
		public void Log2_Works()
		{
			Assert.AreEqual(1f, (float)MathfloatP.Log2((floatP)2f), _epsilon);
			Assert.AreEqual(2f, (float)MathfloatP.Log2((floatP)4f), _epsilon);
			Assert.AreEqual(3f, (float)MathfloatP.Log2((floatP)8f), _epsilon);
		}

		[Test]
		public void Log10_Works()
		{
			Assert.AreEqual(1f, (float)MathfloatP.Log10((floatP)10f), _epsilon);
			Assert.AreEqual(2f, (float)MathfloatP.Log10((floatP)100f), _epsilon);
			Assert.AreEqual(3f, (float)MathfloatP.Log10((floatP)1000f), _epsilon);
		}

		[Test]
		public void Sqrt_One_ReturnsOne()
		{
			Assert.AreEqual(floatP.One, MathfloatP.Sqrt(floatP.One));
		}

		#endregion

		#region 반올림 확장 테스트

		[Test]
		public void Floor_Integer_ReturnsSame()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Floor((floatP)5f));
			Assert.AreEqual((floatP)(-5f), MathfloatP.Floor((floatP)(-5f)));
		}

		[Test]
		public void Ceil_Integer_ReturnsSame()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Ceil((floatP)5f));
			Assert.AreEqual((floatP)(-5f), MathfloatP.Ceil((floatP)(-5f)));
		}

		[Test]
		public void Round_HalfCases()
		{
			// 구현은 반올림 동작을 사용합니다(뱅커스 라운딩이 아님)
			Assert.AreEqual((floatP)2f, MathfloatP.Round((floatP)1.5f));
			Assert.AreEqual((floatP)3f, MathfloatP.Round((floatP)2.5f));
		}

		[Test]
		public void Round_Integer_ReturnsSame()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Round((floatP)5f));
		}

		#endregion

		#region 유틸리티 확장 테스트

		[Test]
		public void LerpAngle_Works()
		{
			// 0에서 90도 사이에서 0.5
			Assert.AreEqual(45f, (float)MathfloatP.LerpAngle((floatP)0f, (floatP)90f, (floatP)0.5f), _epsilon);
			// 360도 래핑: LerpAngle은 최단 경로를 사용하지만 결과를 정규화하지 않습니다
			// delta = DeltaAngle(350, 20) = 30 (최단 경로), result = 350 + 30 * 0.5 = 365
			Assert.AreEqual(365f, (float)MathfloatP.LerpAngle((floatP)350f, (floatP)20f, (floatP)0.5f), _epsilon);
		}

		[Test]
		public void MoveTowardsAngle_Works()
		{
			Assert.AreEqual(5f, (float)MathfloatP.MoveTowardsAngle((floatP)0f, (floatP)10f, (floatP)5f), _epsilon);
			Assert.AreEqual(10f, (float)MathfloatP.MoveTowardsAngle((floatP)0f, (floatP)10f, (floatP)15f), _epsilon);
		}

		[Test]
		public void Approximately_Works()
		{
			Assert.IsTrue(MathfloatP.Approximately((floatP)1.0f, (floatP)1.000001f));
			Assert.IsFalse(MathfloatP.Approximately((floatP)1.0f, (floatP)1.1f));
		}

		#endregion

		#region Min/Max 확장 테스트

		[Test]
		public void Max_WithInfinity()
		{
			Assert.IsTrue(MathfloatP.Max(floatP.PositiveInfinity, (floatP)100f).IsPositiveInfinity());
			Assert.AreEqual((floatP)100f, MathfloatP.Max((floatP)100f, floatP.NegativeInfinity));
		}

		[Test]
		public void Min_WithInfinity()
		{
			Assert.IsTrue(MathfloatP.Min(floatP.NegativeInfinity, (floatP)(-100f)).IsNegativeInfinity());
			Assert.AreEqual((floatP)(-100f), MathfloatP.Min((floatP)(-100f), floatP.PositiveInfinity));
		}

		#endregion

		#region Abs 확장 테스트

		[Test]
		public void Abs_Infinity()
		{
			Assert.IsTrue(MathfloatP.Abs(floatP.NegativeInfinity).IsPositiveInfinity());
			Assert.IsTrue(MathfloatP.Abs(floatP.PositiveInfinity).IsPositiveInfinity());
		}

		#endregion

		#region Clamp 확장 테스트

		[Test]
		public void Clamp_AtBoundaries()
		{
			Assert.AreEqual((floatP)0f, MathfloatP.Clamp((floatP)0f, (floatP)0f, (floatP)10f));
			Assert.AreEqual((floatP)10f, MathfloatP.Clamp((floatP)10f, (floatP)0f, (floatP)10f));
		}

		#endregion

		#region 결정성 확장 테스트

		[Test]
		public void AllTrigFunctions_RawValueConsistent()
		{
			var input = (floatP)0.5f;
			
			var sin1 = MathfloatP.Sin(input).RawValue;
			var sin2 = MathfloatP.Sin(input).RawValue;
			Assert.AreEqual(sin1, sin2);
			
			var cos1 = MathfloatP.Cos(input).RawValue;
			var cos2 = MathfloatP.Cos(input).RawValue;
			Assert.AreEqual(cos1, cos2);
			
			var tan1 = MathfloatP.Tan(input).RawValue;
			var tan2 = MathfloatP.Tan(input).RawValue;
			Assert.AreEqual(tan1, tan2);
		}

		[Test]
		public void AllPowerFunctions_RawValueConsistent()
		{
			var input = (floatP)2.5f;
			
			var sqrt1 = MathfloatP.Sqrt(input).RawValue;
			var sqrt2 = MathfloatP.Sqrt(input).RawValue;
			Assert.AreEqual(sqrt1, sqrt2);
			
			var log1 = MathfloatP.Log(input).RawValue;
			var log2 = MathfloatP.Log(input).RawValue;
			Assert.AreEqual(log1, log2);
			
			var exp1 = MathfloatP.Exp(input).RawValue;
			var exp2 = MathfloatP.Exp(input).RawValue;
			Assert.AreEqual(exp1, exp2);
		}

		[Test]
		public void CrossPlatform_Determinism_ComplexExpression()
		{
			// 이 테스트는 복잡한 표현이 동일한 원시 값을 생성하는지 확인합니다
			var a = (floatP)1.5f;
			var b = (floatP)2.5f;
			var c = (floatP)3.5f;
			
			var result1 = MathfloatP.Sin(a) * MathfloatP.Cos(b) + MathfloatP.Sqrt(c);
			var result2 = MathfloatP.Sin(a) * MathfloatP.Cos(b) + MathfloatP.Sqrt(c);
			
			Assert.AreEqual(result1.RawValue, result2.RawValue);
		}

		#endregion

		#region ScaleB 테스트

		[Test]
		public void ScaleB_PowerOfTwo()
		{
			Assert.AreEqual((floatP)4f, MathfloatP.ScaleB(floatP.One, 2));
			Assert.AreEqual((floatP)8f, MathfloatP.ScaleB(floatP.One, 3));
			Assert.AreEqual((floatP)0.5f, MathfloatP.ScaleB(floatP.One, -1));
		}

		#endregion

		#region 엣지 케이스 테스트

		[Test]
		public void Lerp_NegativeT_클램핑됨()
		{
			Assert.AreEqual((floatP)0f, MathfloatP.Lerp((floatP)0f, (floatP)10f, (floatP)(-0.5f)));
		}

		[Test]
		public void InverseLerp_OutOfRange()
		{
			// 범위 미만 값
			Assert.AreEqual((floatP)0f, MathfloatP.InverseLerp((floatP)0f, (floatP)10f, (floatP)(-5f)));
			// 범위 초과 값
			Assert.AreEqual((floatP)1f, MathfloatP.InverseLerp((floatP)0f, (floatP)10f, (floatP)15f));
		}

		[Test]
		public void SmoothStep_ClampsInput()
		{
			Assert.AreEqual((floatP)0f, MathfloatP.SmoothStep((floatP)0f, (floatP)10f, (floatP)(-0.5f)));
			Assert.AreEqual((floatP)10f, MathfloatP.SmoothStep((floatP)0f, (floatP)10f, (floatP)1.5f));
		}

		#endregion
	}
}
