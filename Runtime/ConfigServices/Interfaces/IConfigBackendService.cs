using Cysharp.Threading.Tasks;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 설정을 보유하고 버전을 관리하는 서비스를 나타내는 인터페이스입니다
	/// </summary>
	public interface IConfigBackendService
	{
		/// <summary>
		/// 백엔드에 있는 설정의 현재 버전을 가져옵니다.
		/// 매 요청마다 수행되므로 빠른 작업이어야 합니다.
		/// </summary>
		public UniTask<ulong> GetRemoteVersion();

		/// <summary>
		/// 백엔드에서 주어진 설정을 가져옵니다.
		/// </summary>
		public UniTask<IConfigsProvider> FetchRemoteConfiguration(ulong version);
	}
}
