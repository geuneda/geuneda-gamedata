using GeunedalEditor.ConfigsContainer;
using UnityEditor;

// ReSharper disable once CheckNamespace

namespace Geuneda.Samples
{
	/// <summary>
	/// Property Drawer for the enum <seealso cref="EnumExample"/> implementation example.
	/// IMPORTANT: Property drawer implementations should be done on editor namespace folders and the class should have
	/// the defined <seealso cref="CustomPropertyDrawer"/> attribute defined
	/// </summary>
	[CustomPropertyDrawer(typeof(EnumSelectorExample))]
	public class EnumSelectorExamplePropertyDrawer : EnumSelectorPropertyDrawer<EnumExample>
	{
	}
}