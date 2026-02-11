using System;
using System.Collections.Generic;
using System.Linq;
using Geuneda.DataExtensions;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// Config Browser 도구 모음의 프로바이더 선택 드롭다운을 관리합니다.
	/// 사용 가능한 <see cref="ConfigsProviderDebugRegistry.ProviderSnapshot"/> 인스턴스 목록을 주기적으로 새로고침하고
	/// 활성 프로바이더가 변경될 때 <see cref="ProviderChanged"/>를 발생시킵니다.
	/// </summary>
	internal sealed class ProviderMenuController
	{
		private readonly ConfigBrowserView _view;
		private readonly List<ConfigsProviderDebugRegistry.ProviderSnapshot> _snapshots = new List<ConfigsProviderDebugRegistry.ProviderSnapshot>();

		private string _lastMenuText;
		private int _lastSnapshotCount = -1;

		private IConfigsProvider _provider;
		private int _selectedProviderId = -1;

		public event Action ProviderChanged;

		/// <summary>현재 선택된 프로바이더, 선택된 것이 없으면 null입니다.</summary>
		public IConfigsProvider Provider => _provider;
		/// <summary>현재 선택된 프로바이더 스냅샷의 ID, 없으면 -1입니다.</summary>
		public int SelectedProviderId => _selectedProviderId;
		/// <summary>ID 순으로 정렬된 모든 알려진 프로바이더 스냅샷입니다.</summary>
		public IReadOnlyList<ConfigsProviderDebugRegistry.ProviderSnapshot> Snapshots => _snapshots;

		public ProviderMenuController(ConfigBrowserView view)
		{
			_view = view;
		}

		/// <summary>
		/// <see cref="ConfigsProviderDebugRegistry"/>를 다시 읽고, 선택된 것이 없거나
		/// 현재 프로바이더가 사라졌을 때 첫 번째 프로바이더를 자동 선택하고, 도구 모음 메뉴를 다시 빌드합니다.
		/// </summary>
		public void RefreshSnapshots()
		{
			_snapshots.Clear();
			_snapshots.AddRange(ConfigsProviderDebugRegistry.EnumerateSnapshots().OrderBy(s => s.Id));

			var currentProviderGone = _provider != null && !_snapshots.Any(s => s.Id == _selectedProviderId);
			if (currentProviderGone || (_provider == null && _snapshots.Count > 0))
			{
				var first = _snapshots.FirstOrDefault();
				if (first.ProviderRef != null && first.ProviderRef.TryGetTarget(out var target))
				{
					SetProviderInternal(target, first.Id, fireEvent: true);
				}
				else
				{
					SetProviderInternal(null, -1, fireEvent: true);
				}
			}

			UpdateProviderMenu();
		}

		/// <summary>
		/// 현재 프로바이더 선택을 지우고 도구 모음 메뉴를 새로고침합니다.
		/// </summary>
		public void ClearSelection()
		{
			SetProviderInternal(null, -1, fireEvent: true);
			UpdateProviderMenu();
		}

		private void SetProviderInternal(IConfigsProvider provider, int selectedId, bool fireEvent)
		{
			var changed = !ReferenceEquals(_provider, provider) || _selectedProviderId != selectedId;
			_provider = provider;
			_selectedProviderId = selectedId;
			if (fireEvent && changed)
			{
				ProviderChanged?.Invoke();
			}
		}

		private void UpdateProviderMenu()
		{
			var providerMenu = _view.ProviderMenu;
			var toolbar = _view.Toolbar;
			if (providerMenu == null || toolbar == null) return;

			var currentSnapshot = _snapshots.FirstOrDefault(s => s.Id == _selectedProviderId);
			var menuText = _snapshots.Count == 0
				? "No providers"
				: currentSnapshot.Id != 0 ? $"{currentSnapshot.Name} ({currentSnapshot.ConfigTypeCount} types)" : "Select Provider";

			if (_lastMenuText == menuText && _lastSnapshotCount == _snapshots.Count)
			{
				return;
			}

			_lastMenuText = menuText;
			_lastSnapshotCount = _snapshots.Count;

			var newMenu = new ToolbarMenu { text = menuText };
			newMenu.style.minWidth = 280;

			// 선택을 지우기 위한 "None" 옵션 추가
			newMenu.menu.AppendAction("None", a =>
			{
				SetProviderInternal(null, -1, fireEvent: true);
			}, a => _provider == null ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

			if (_snapshots.Count > 0)
			{
				newMenu.menu.AppendSeparator();
			}

			foreach (var snap in _snapshots)
			{
				var s = snap;
				newMenu.menu.AppendAction($"{s.Name} ({s.ConfigTypeCount} types) ##{s.Id}", a =>
				{
					if (s.ProviderRef.TryGetTarget(out var target))
					{
						SetProviderInternal(target, s.Id, fireEvent: true);
					}
				}, a => s.Id == _selectedProviderId ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
			}

			_view.ReplaceProviderMenu(newMenu);
		}
	}
}
