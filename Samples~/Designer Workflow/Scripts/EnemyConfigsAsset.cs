using UnityEngine;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// Designer-editable enemy configs stored as key/value pairs (Id -> EnemyConfig).
	/// </summary>
	[CreateAssetMenu(fileName = "EnemyConfigs", menuName = "GameData Samples/Designer Workflow/Enemy Configs")]
	public sealed class EnemyConfigsAsset : ConfigsScriptableObject<int, EnemyConfig>
	{
	}
}

