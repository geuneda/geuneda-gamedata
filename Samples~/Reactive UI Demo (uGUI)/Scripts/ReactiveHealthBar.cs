using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Geuneda.DataExtensions.Samples.ReactiveUiDemo
{
	/// <summary>
	/// <see cref="ObservableField{T}"/> 체력 값에 바인딩하는 uGUI 뷰입니다.
	/// </summary>
	public sealed class ReactiveHealthBar : MonoBehaviour
	{
		[SerializeField] private Slider _slider;
		[SerializeField] private TMP_Text _label;
		[SerializeField] private int _maxHealth = 100;

		private IObservableFieldReader<int> _health;

		public void Bind(IObservableFieldReader<int> health, int maxHealth)
		{
			_health = health;
			_maxHealth = Mathf.Max(1, maxHealth);

			_health.InvokeObserve(OnHealthChanged);
		}

		private void OnDestroy()
		{
			_health?.StopObservingAll(this);
		}

		private void OnHealthChanged(int previous, int current)
		{
			if (_slider != null)
			{
				_slider.value = Mathf.Clamp01(current / (float)_maxHealth);
			}

			if (_label != null)
			{
				_label.text = $"Health: {current}/{_maxHealth}";
			}
		}
	}
}

