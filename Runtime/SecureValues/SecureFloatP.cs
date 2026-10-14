using System;
using Newtonsoft.Json;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 메모리 변조를 감지하는 <see cref="floatP"/> 입니다. 원시 비트(<see cref="floatP.RawValue"/>)를 섞어 저장하고 쓸 때마다 새로 인코딩합니다.
	/// 읽을 때 변조를 감지하면 <see cref="SecureValueMonitor.TamperDetected"/> 로 알리고 복원한 값을 그대로 돌려줍니다.
	/// </summary>
	/// <remarks>
	/// <see cref="floatP"/> 와 암시적으로 변환되므로 <see cref="floatP"/> 와 섞은 산술·비교와 <c>+=</c> 는 그대로 쓸 수 있습니다.
	/// 연산자는 <see cref="floatP"/> 에 있으므로 <c>SecureFloatP</c> 끼리의 연산(<c>a + b</c>, <c>a == b</c>)과
	/// 정수 리터럴과의 연산(<c>a * 2</c>)은 컴파일되지 않습니다. 이때는 <see cref="Value"/> 로 꺼내 계산합니다.
	/// JSON 에서는 평범한 실수(float)로 읽고 씁니다. Unity 직렬화(<c>[SerializeField]</c>)는 지원하지 않습니다.
	/// </remarks>
	[JsonConverter(typeof(SecureFloatPJsonConverter))]
	public readonly struct SecureFloatP : IEquatable<SecureFloatP>, IComparable<SecureFloatP>, IFormattable
	{
		private readonly SecureBits _bits;

		public SecureFloatP(floatP value)
		{
			_bits = SecureBits.Encode(value.RawValue);
		}

		/// <summary>
		/// 복원한 값입니다. 변조를 감지하면 <see cref="SecureValueMonitor.TamperDetected"/> 가 발생합니다
		/// </summary>
		public floatP Value => floatP.FromRaw(unchecked((uint)_bits.Decode(typeof(SecureFloatP))));

		public static implicit operator SecureFloatP(floatP value) => new SecureFloatP(value);

		public static implicit operator floatP(SecureFloatP value) => value.Value;

		public static SecureFloatP operator ++(SecureFloatP value) => new SecureFloatP(value.Value + floatP.One);

		public static SecureFloatP operator --(SecureFloatP value) => new SecureFloatP(value.Value - floatP.One);

		public bool Equals(SecureFloatP other) => Value.Equals(other.Value);

		public override bool Equals(object obj) => obj is SecureFloatP other && Equals(other);

		public override int GetHashCode() => Value.GetHashCode();

		public int CompareTo(SecureFloatP other) => Value.CompareTo(other.Value);

		public override string ToString() => Value.ToString();

		public string ToString(string format, IFormatProvider formatProvider) => Value.ToString(format, formatProvider);
	}
}
