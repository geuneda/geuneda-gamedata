using System;
using System.Collections.Generic;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 배치 업데이트를 지원하는 Observable의 인터페이스입니다.
	/// </summary>
	public interface IBatchable
	{
		/// <summary>
		/// 개별 업데이트 억제를 시작합니다.
		/// </summary>
		void SuppressNotifications();

		/// <summary>
		/// 업데이트 억제를 중지하고 통합 알림을 트리거합니다.
		/// </summary>
		void ResumeNotifications();
	}

	/// <summary>
	/// 수명 동안 Observable 그룹에 대한 알림을 억제하는 일회용 객체입니다.
	/// 객체가 해제될 때 알림이 통합되어 한 번 발생합니다.
	/// </summary>
	public class ObservableBatch : IDisposable
	{
		private readonly List<IBatchable> _observables = new List<IBatchable>();
		private bool _disposed;

		/// <summary>
		/// 이 배치에 Observable을 추가합니다.
		/// </summary>
		public void Add(IBatchable observable)
		{
			if (_disposed) throw new ObjectDisposedException(nameof(ObservableBatch));
			
			_observables.Add(observable);
			observable.SuppressNotifications();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (_disposed) return;

			foreach (var observable in _observables)
			{
				observable.ResumeNotifications();
			}

			_observables.Clear();
			_disposed = true;
		}
	}
}
