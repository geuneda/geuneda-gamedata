using System;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// <see cref="ConfigsProvider.AddConfigs{T}"/>를 시연하기 위한 샘플 ID 키 설정입니다.
	/// </summary>
	[Serializable]
	public struct EnemyConfig
	{
		public int Id;
		public string Name;
		public int Health;
		public int Damage;
	}
}

