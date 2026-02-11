using System;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// Unity가 인스펙터에서 전리품 테이블을 직렬화할 수 있도록 하는 구체적인 딕셔너리 타입입니다.
	/// </summary>
	[Serializable]
	public sealed class LootTable : UnitySerializedDictionary<ItemTypeSelector, float>
	{
	}
}

