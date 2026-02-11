using System;
using System.Collections.Generic;

namespace Geuneda.DataExtensions.Samples.ReactiveUiDemo
{
	/// <summary>
	/// Reactive UI Demo (uGUI) 샘플에서 사용하는 간단한 데이터 모델입니다.
	/// </summary>
	[Serializable]
	public sealed class ReactiveUGuiPlayerData : IDisposable
	{
		public ObservableField<int> Health { get; } = new ObservableField<int>(100);
		public ObservableField<int> BaseDamage { get; } = new ObservableField<int>(10);
		public ObservableField<int> WeaponBonus { get; } = new ObservableField<int>(5);
		public ComputedField<int> TotalDamage { get; }

		public ObservableList<string> Inventory { get; } = new ObservableList<string>(new List<string>());

		public ReactiveUGuiPlayerData()
		{
			TotalDamage = new ComputedField<int>(() => BaseDamage.Value + WeaponBonus.Value);
		}

		public void Dispose()
		{
			TotalDamage.Dispose();
		}
	}
}

