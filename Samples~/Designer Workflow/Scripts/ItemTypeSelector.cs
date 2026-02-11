using System;

namespace Geuneda.DataExtensions.Samples.DesignerWorkflow
{
	/// <summary>
	/// <see cref="ItemType"/>에 대한 구체적인 <see cref="EnumSelector{T}"/>입니다.
	/// 열거형 값이 재정렬될 때 안정성을 위해 이름 기반 지속성을 사용합니다.
	/// </summary>
	[Serializable]
	public sealed class ItemTypeSelector : EnumSelector<ItemType>, IEquatable<ItemTypeSelector>
	{
		public ItemTypeSelector() : base(ItemType.Weapon)
		{
		}

		public ItemTypeSelector(ItemType value) : base(value)
		{
		}

		public bool Equals(ItemTypeSelector other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return string.Equals(GetSelectionString(), other.GetSelectionString(), StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			return ReferenceEquals(this, obj) || obj is ItemTypeSelector other && Equals(other);
		}

		public override int GetHashCode()
		{
			var selection = GetSelectionString();
			return selection == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(selection);
		}

		public static implicit operator ItemTypeSelector(ItemType value)
		{
			return new ItemTypeSelector(value);
		}
	}
}

