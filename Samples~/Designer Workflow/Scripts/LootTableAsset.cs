using UnityEngine;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// 디자이너가 인스펙터에서 드롭률을 편집할 수 있도록 <see cref="LootTable"/>을 감싸는 ScriptableObject입니다.
	/// </summary>
	[CreateAssetMenu(fileName = "LootTable", menuName = "GameData Samples/Designer Workflow/Loot Table")]
	public sealed class LootTableAsset : ScriptableObject
	{
		[SerializeField] private LootTable _dropRates = new LootTable();

		public LootTable DropRates => _dropRates;
	}
}

