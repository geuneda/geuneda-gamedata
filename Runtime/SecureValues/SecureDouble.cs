using System;
using Newtonsoft.Json;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 메모리 변조를 감지하는 <see cref="double"/> 입니다. 비트 그대로 섞어 저장하고 쓸 때마다 새로 인코딩합니다.
	/// 읽을 때 변조를 감지하면 <see cref="SecureValueMonitor.TamperDetected"/> 로 알리고 복원한 값을 그대로 돌려줍니다.
	/// </summary>
	/// <remarks>
	/// <see cref="double"/> 와 암시적으로 변환되므로 산술, 비교, <c>+=</c> 를 그대로 쓸 수 있습니다.
	/// JSON 에서는 평범한 실수로 읽고 씁니다. Unity 직렬화(<c>[SerializeField]</c>)는 지원하지 않습니다.
	/// </remarks>
	[JsonConverter(typeof(SecureDoubleJsonConverter))]
	public readonly struct SecureDouble : IEquatable<SecureDouble>, IComparable<SecureDouble>, IFormattable
	{
		private readonly SecureBits _bits;

		public SecureDouble(double value)
		{
			_bits = SecureBits.Encode(unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
		}

		/// <summary>
		/// 복원한 값입니다. 변조를 감지하면 <see cref="SecureValueMonitor.TamperDetected"/> 가 발생합니다
		/// </summary>
		public double Value => BitConverter.Int64BitsToDouble(unchecked((long)_bits.Decode(typeof(SecureDouble))));

		public static implicit operator SecureDouble(double value) => new SecureDouble(value);

		public static implicit operator double(SecureDouble value) => value.Value;

		public static SecureDouble operator ++(SecureDouble value) => new SecureDouble(value.Value + 1);

		public static SecureDouble operator --(SecureDouble value) => new SecureDouble(value.Value - 1);

		public bool Equals(SecureDouble other) => Value.Equals(other.Value);

		public override bool Equals(object obj) => obj is SecureDouble other && Equals(other);

		public override int GetHashCode() => Value.GetHashCode();

		public int CompareTo(SecureDouble other) => Value.CompareTo(other.Value);

		public override string ToString() => Value.ToString();

		public string ToString(string format, IFormatProvider formatProvider) => Value.ToString(format, formatProvider);
	}
}
