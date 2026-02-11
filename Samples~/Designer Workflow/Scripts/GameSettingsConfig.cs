using System;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// 싱글톤 스타일 설정 샘플 (<see cref="ConfigsProvider.AddSingletonConfig{T}"/>를 통해 로드됨).
	/// </summary>
	[Serializable]
	public struct GameSettingsConfig
	{
		public int Difficulty;
		public float MasterVolume;
	}
}

