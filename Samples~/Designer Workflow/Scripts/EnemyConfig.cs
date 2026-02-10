using System;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// Sample id-keyed config for demonstrating <see cref="ConfigsProvider.AddConfigs{T}"/>.
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

