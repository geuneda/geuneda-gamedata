using System;
using Newtonsoft.Json;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 메모리 변조를 감지하는 <see cref="int"/> 입니다. 값을 섞어 저장하고 쓸 때마다 새로 인코딩합니다.
	/// 읽을 때 변조를 감지하면 <see cref="SecureValueMonitor.TamperDetected"/> 로 알리고 복원한 값을 그대로 돌려줍니다.
	/// </summary>
	/// <remarks>
	/// <see cref="int"/> 와 암시적으로 변환되므로 산술, 비교, <c>+=</c> 를 그대로 쓸 수 있습니다.
	/// JSON 에서는 평범한 정수로 읽고 씁니다. Unity 직렬화(<c>[SerializeField]</c>)는 지원하지 않습니다.
	/// </remarks>
	[JsonConverter(typeof(SecureIntJsonConverter))]
	public readonly struct SecureInt : IEquatable<SecureInt>, IComparable<SecureInt>, IFormattable
	{
		private readonly SecureBits _bits;

		public SecureInt(int value)
		{
			_bits = SecureBits.Encode(unchecked((uint)value));
		}

		/// <summary>
		/// 복원한 값입니다. 변조를 감지하면 <see cref="SecureValueMonitor.TamperDetected"/> 가 발생합니다
		/// </summary>
		public int Value => unchecked((int)_bits.Decode(typeof(SecureInt)));

		public static implicit operator SecureInt(int value) => new SecureInt(value);

		public static implicit operator int(SecureInt value) => value.Value;

		public static SecureInt operator ++(SecureInt value) => new SecureInt(unchecked(value.Value + 1));

		public static SecureInt operator --(SecureInt value) => new SecureInt(unchecked(value.Value - 1));

		public bool Equals(SecureInt other) => Value == other.Value;

		public override bool Equals(object obj) => obj is SecureInt other && Equals(other);

		public override int GetHashCode() => Value.GetHashCode();

		public int CompareTo(SecureInt other) => Value.CompareTo(other.Value);

		public override string ToString() => Value.ToString();

		public string ToString(string format, IFormatProvider formatProvider) => Value.ToString(format, formatProvider);
	}
}
