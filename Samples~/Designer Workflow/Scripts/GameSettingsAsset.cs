using UnityEngine;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// 단일 항목(Id 0 -> GameSettingsConfig)으로 저장된 디자이너 편집 가능한 게임 설정입니다.
	/// 워크플로우를 시연하기 위해 <see cref="ConfigsScriptableObject{TId,TAsset}"/>를 사용합니다.
	/// </summary>
	[CreateAssetMenu(fileName = "GameSettings", menuName = "GameData Samples/Designer Workflow/Game Settings")]
	public sealed class GameSettingsAsset : ConfigsScriptableObject<int, GameSettingsConfig>
	{
		public const int SingletonKey = 0;
	}
}

