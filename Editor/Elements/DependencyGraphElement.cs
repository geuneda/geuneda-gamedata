using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// 계산된 Observable의 최소 의존성 뷰입니다.
	/// 현재 리플렉션을 통해 <see cref="ComputedField{T}"/>의 의존성 목록을 표시합니다.
	/// </summary>
	public sealed class DependencyGraphElement : VisualElement
	{
		private readonly Label _header;
		private readonly ScrollView _list;

		public DependencyGraphElement()
		{
			style.flexGrow = 1;
			style.paddingLeft = 8;
			style.paddingRight = 8;
			style.paddingTop = 6;
			style.paddingBottom = 6;
			style.borderTopWidth = 1;
			style.borderTopColor = new StyleColor(new Color(0f, 0f, 0f, 0.2f));

			_header = new Label("Dependency Graph");
			_header.style.unityFontStyleAndWeight = FontStyle.Bold;
			_header.style.marginBottom = 4;

			_list = new ScrollView(ScrollViewMode.Vertical);
			_list.style.flexGrow = 1;

			Add(_header);
			Add(_list);
		}

		/// <summary>
		/// 의존성을 표시할 대상 Observable 스냅샷을 설정합니다.
		/// <see cref="ComputedField{T}"/> 인스턴스만 의존성을 가집니다.
		/// </summary>
		/// <param name="snapshot">검사할 Observable 스냅샷, 또는 초기화하려면 기본값을 전달합니다.</param>
		public void SetTarget(ObservableDebugRegistry.EntrySnapshot snapshot)
		{
			_list.Clear();

			if (snapshot.Name == null || snapshot.Kind != "Computed")
			{
				_header.text = "Dependency Graph (select a Computed observable)";
				return;
			}

			if (snapshot.InstanceRef == null || !snapshot.InstanceRef.TryGetTarget(out var instance) || instance == null)
			{
				_header.text = "Dependency Graph (instance collected)";
				return;
			}

			_header.text = $"Dependency Graph: {FormatName(snapshot)}";

			var deps = TryReadDependencies(instance);
			if (deps == null)
			{
				_list.Add(new Label("No dependencies found (or not supported)."));
				return;
			}

			var count = 0;
			foreach (var d in deps)
			{
				count++;
				var depSnapshot = ObservableDebugRegistry.FindByInstance(d);
				if (depSnapshot.HasValue)
				{
					var s = depSnapshot.Value;
					var label = new Label($"- {FormatName(s)} = {s.Value}");
					_list.Add(label);
				}
				else
				{
					_list.Add(new Label($"- {d?.GetType().Name ?? "null"} (untracked)"));
				}
			}

			if (count == 0)
			{
				_list.Add(new Label("No dependencies registered yet."));
			}
		}

		private static string FormatName(ObservableDebugRegistry.EntrySnapshot s)
		{
			// 제네릭 이름에서 백틱-숫자 패턴(예: `1, `2)을 제거합니다
			var formattedName = Regex.Replace(s.Name ?? string.Empty, @"`\d+", "");

			// 소스 위치가 있으면 추가합니다
			var sourceLocation = s.SourceLocation;
			if (!string.IsNullOrEmpty(sourceLocation))
			{
				return $"{formattedName} ({sourceLocation})";
			}

			// 소스 위치가 없으면 ID로 대체합니다
			return $"{formattedName}#{s.Id}";
		}

		private static IEnumerable TryReadDependencies(object computedFieldInstance)
		{
			// ComputedField<T>는 "_dependencies"라는 private 필드에 의존성을 저장합니다.
			var type = computedFieldInstance.GetType();
			var field = type.GetField("_dependencies", BindingFlags.Instance | BindingFlags.NonPublic);
			return field?.GetValue(computedFieldInstance) as IEnumerable;
		}
	}
}

