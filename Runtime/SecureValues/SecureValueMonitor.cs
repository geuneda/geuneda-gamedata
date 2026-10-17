using System;
using UnityEngine;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 보안 값(<see cref="SecureInt"/> 등)의 변조 감지를 알립니다. 감지해도 예외를 던지지 않으며, 대응은 게임이 정합니다.
	/// </summary>
	/// <remarks>
	/// 보안 값은 읽는 순간 감지하므로 인스턴스 서비스로 알릴 방법이 없어 static 이벤트로 둡니다(D2 예외, core AGENTS.md).
	/// 세션 시작(<see cref="RuntimeInitializeLoadType.SubsystemRegistration"/>) 때 구독을 모두 지웁니다.
	/// </remarks>
	public static class SecureValueMonitor
	{
		/// <summary>
		/// 변조를 감지한 읽기마다 발생합니다. 인자는 보안 값 타입(<c>typeof(SecureInt)</c> 등)입니다.
		/// </summary>
		/// <remarks>
		/// 값을 읽은 스레드에서 바로 부릅니다. 같은 값을 매 프레임 읽으면 매 프레임 발생하므로 처음 한 번만 처리하려면 핸들러에서 구독을 해제합니다.
		/// 핸들러 예외는 로그로 남기고 다음 핸들러로 넘어갑니다. 핸들러 안에서 다시 감지되는 읽기는 알리지 않습니다.
		/// </remarks>
		public static event Action<Type> TamperDetected;

		[ThreadStatic]
		private static bool _reporting;

		internal static void Report(Type valueType)
		{
			var handlers = TamperDetected;

			if (handlers == null || _reporting)
			{
				return;
			}

			_reporting = true;

			try
			{
				foreach (var handler in handlers.GetInvocationList())
				{
					try
					{
						((Action<Type>)handler)(valueType);
					}
					catch (Exception e)
					{
						// 값을 읽는 코드 흐름을 끊지 않도록 핸들러 예외는 로그만 남깁니다
						GameDataLog.Logger.Error(e, $"A {nameof(TamperDetected)} handler threw");
					}
				}
			}
			finally
			{
				_reporting = false;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetState()
		{
			TamperDetected = null;
		}
	}
}
