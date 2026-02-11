using Geuneda.DataExtensions;
using UnityEngine.UIElements;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// 에디터 마이그레이션 시스템에서 검색된 마이그레이션을 나열하고
	/// 선택된 행에 대한 인메모리 마이그레이션 미리보기("Migration Preview")를 제공하는 UI Toolkit 패널입니다.
	/// </summary>
	public sealed class MigrationPanelElement : VisualElement
	{
		private readonly MigrationPanelView _view;
		private readonly MigrationPanelController _controller;

		public MigrationPanelElement()
		{
			style.flexGrow = 1;
			_view = new MigrationPanelView();
			_controller = new MigrationPanelController(_view);
			Add(_view);
		}

		/// <summary>
		/// 마이그레이션을 검사할 설정 프로바이더를 설정합니다.
		/// </summary>
		public void SetProvider(IConfigsProvider provider)
		{
			_controller.SetProvider(provider);
		}

		/// <summary>
		/// 현재 프로바이더를 기반으로 마이그레이션 목록 및 미리보기 패널을 다시 빌드합니다.
		/// </summary>
		public void Rebuild()
		{
			_controller.Rebuild();
		}
	}
}
