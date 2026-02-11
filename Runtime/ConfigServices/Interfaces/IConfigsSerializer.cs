namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 게임별 직렬화 가능한 IConfigsProvider 인스턴스입니다.
	/// </summary>
	public interface IConfigsSerializer
	{
		/// <summary>
		/// 설정에서 지정된 설정 키를 문자열로 직렬화합니다.
		/// </summary>
		string Serialize(IConfigsProvider cfg, string version);

		/// <summary>
		/// 주어진 문자열을 역직렬화합니다. 역직렬화된 설정을 포함하는 새 IConfigsProvider를
		/// 인스턴스화하고 반환합니다.
		/// </summary>
		T Deserialize<T>(string serialized) where T : IConfigsAdder;
	}
}
