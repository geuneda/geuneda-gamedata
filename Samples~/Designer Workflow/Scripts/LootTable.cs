using System;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// Concrete dictionary type so Unity can serialize a loot table in the inspector.
	/// </summary>
	[Serializable]
	public sealed class LootTable : UnitySerializedDictionary<ItemTypeSelector, float>
	{
	}
}

