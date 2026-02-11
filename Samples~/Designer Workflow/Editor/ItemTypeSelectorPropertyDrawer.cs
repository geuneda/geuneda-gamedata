using GeunedaEditor.GameData;
using UnityEditor;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// 공유 EnumSelector 드로어를 사용하는 <see cref="ItemTypeSelector"/>의 PropertyDrawer입니다.
	/// </summary>
	[CustomPropertyDrawer(typeof(ItemTypeSelector))]
	public sealed class ItemTypeSelectorPropertyDrawer : EnumSelectorPropertyDrawer<ItemType>
	{
	}
}

