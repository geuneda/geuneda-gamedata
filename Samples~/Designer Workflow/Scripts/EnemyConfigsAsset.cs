using UnityEngine;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// 키/값 쌍(Id -> EnemyConfig)으로 저장된 디자이너 편집 가능한 적 설정입니다.
	/// </summary>
	[CreateAssetMenu(fileName = "EnemyConfigs", menuName = "GameData Samples/Designer Workflow/Enemy Configs")]
	public sealed class EnemyConfigsAsset : ConfigsScriptableObject<int, EnemyConfig>
	{
	}
}

