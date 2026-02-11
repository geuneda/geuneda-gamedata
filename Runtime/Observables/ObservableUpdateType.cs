namespace Geuneda.DataExtensions
{
	public enum ObservableUpdateType
	{
		Added,
		Updated,
		Removed
	}

	public enum ObservableUpdateFlag
	{
		// 키 인덱스를 지정하지 않은 모든 구독자를 업데이트합니다
		UpdateOnly,
		// 키 인덱스를 추가한 구독자만 업데이트합니다
		KeyUpdateOnly,
		// 모든 유형의 구독자를 업데이트합니다 [높은 성능 비용이 있습니다]
		Both
	}
}
