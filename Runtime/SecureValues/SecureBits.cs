using System;
using System.Security.Cryptography;
using System.Threading;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 보안 값의 저장 형식입니다. 평문 64비트를 nonce 마다 다른 패드로 XOR 하고, 평문의 32비트 검사값을 함께 저장합니다.
	/// 패드와 검사값은 실행마다 새로 만드는 세션 키에서 나옵니다.
	/// </summary>
	/// <remarks>
	/// 가벼운 난독화이고 검사값은 MAC 이 아닙니다. 검사값의 비밀 입력이 패드뿐이라, 알고리즘을 아는 공격자는 알려진 값 하나와
	/// 그 메모리로 패드(<c>_data ^ 평문</c>)를 얻어 위조할 수 있고, <c>Mix</c> 가 전단사라 세션 키도 복원됩니다.
	/// nonce 는 32비트 카운터라 프로세스 전체 쓰기 2^32-1 회마다 순환합니다.
	/// 모든 필드가 0 인 상태(default)는 값 0 입니다. 인코딩은 nonce 0 을 만들지 않으므로 정상 인코딩과 겹치지 않습니다.
	/// </remarks>
	internal readonly struct SecureBits
	{
		private const ulong Golden = 0x9E3779B97F4A7C15UL;
		private const ulong CheckSalt = 0xD6E8FEB86659FD93UL;

		// 실행 중에는 바꾸지 않습니다. 바꾸면 그전에 만든 값이 모두 변조로 읽힙니다.
		private static readonly ulong SessionKey = CreateSessionKey();

		// D2 예외(core AGENTS.md). 세션 시작 때 되돌리지 않습니다. 같은 세션 키로 nonce 를 다시 쓰면 같은 값의 인코딩이 되풀이됩니다.
		private static int _nonceCounter;

		private readonly ulong _data;
		private readonly uint _check;
		private readonly uint _nonce;

		private SecureBits(ulong data, uint check, uint nonce)
		{
			_data = data;
			_check = check;
			_nonce = nonce;
		}

		/// <summary>
		/// 평문을 새 nonce 로 인코딩합니다
		/// </summary>
		public static SecureBits Encode(ulong plain)
		{
			var nonce = NextNonce();
			var pad = Pad(nonce);

			return new SecureBits(plain ^ pad, Check(plain, pad), nonce);
		}

		/// <summary>
		/// 평문을 복원합니다. 검사값이 맞지 않으면 <paramref name="valueType"/> 으로 변조를 알리고, 복원한 값을 그대로 돌려줍니다
		/// </summary>
		public ulong Decode(Type valueType)
		{
			if (_nonce == 0 && _data == 0 && _check == 0)
			{
				return 0;
			}

			var pad = Pad(_nonce);
			var plain = _data ^ pad;

			if (Check(plain, pad) != _check)
			{
				SecureValueMonitor.Report(valueType);
			}

			return plain;
		}

		private static uint NextNonce()
		{
			uint nonce;

			do
			{
				nonce = unchecked((uint)Interlocked.Increment(ref _nonceCounter));
			}
			while (nonce == 0);

			return nonce;
		}

		private static ulong Pad(uint nonce)
		{
			return Mix(SessionKey ^ unchecked(nonce * Golden));
		}

		private static uint Check(ulong plain, ulong pad)
		{
			return (uint)(Mix(plain ^ Mix(pad ^ CheckSalt)) >> 32);
		}

		// SplitMix64 의 마무리 함수입니다. 전단사라서 서로 다른 nonce 는 서로 다른 패드가 됩니다
		private static ulong Mix(ulong z)
		{
			unchecked
			{
				z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
				z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
				return z ^ (z >> 31);
			}
		}

		private static ulong CreateSessionKey()
		{
			var bytes = new byte[sizeof(ulong)];

			using (var random = RandomNumberGenerator.Create())
			{
				random.GetBytes(bytes);
			}

			return BitConverter.ToUInt64(bytes, 0);
		}
	}
}
