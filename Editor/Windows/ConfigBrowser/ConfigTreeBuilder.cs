using System;
using System.Collections.Generic;
using System.Linq;
using Geuneda.DataExtensions;
using UnityEngine.UIElements;

namespace Geuneda.DataExtensions.Editor
{
	/// <summary>
	/// Config Browser 트리 뷰에서 사용하는 계층적 <see cref="TreeViewItemData{T}"/> 구조를 빌드합니다.
	/// 설정을 "Singletons"과 "Collections" 헤더로 그룹화하고
	/// 타입 이름 또는 항목 ID로 텍스트 기반 검색 필터링을 지원합니다.
	/// </summary>
	internal static class ConfigTreeBuilder
	{
		private const int SingleConfigId = 0;

		/// <summary>
		/// 주어진 <paramref name="provider"/>에서 루트 <see cref="TreeViewItemData{T}"/> 항목 목록을 빌드합니다.
		/// <paramref name="search"/>가 비어 있지 않으면, 타입 이름이나 ID가
		/// 검색어와 일치하는 항목만 포함됩니다.
		/// </summary>
		public static IList<TreeViewItemData<ConfigNode>> BuildTreeItems(IConfigsProvider provider, string search)
		{
			var id = 1;
			var childrenSingletons = new List<TreeViewItemData<ConfigNode>>();
			var childrenCollections = new List<TreeViewItemData<ConfigNode>>();

			var hasSearch = !string.IsNullOrWhiteSpace(search);
			var searchLower = hasSearch ? search.Trim().ToLowerInvariant() : string.Empty;

			if (provider == null)
			{
				return new List<TreeViewItemData<ConfigNode>>
				{
					new TreeViewItemData<ConfigNode>(id++, ConfigNode.Header("No providers available.\nEnter Play Mode to create a ConfigsProvider."))
				};
			}

			var allConfigs = provider.GetAllConfigs();
			foreach (var kv in allConfigs.OrderBy(k => k.Key.Name))
			{
				var type = kv.Key;
				var container = kv.Value;

				if (!ConfigsEditorUtil.TryReadConfigs(container, out var entries))
				{
					continue;
				}

				var isSingleton = entries.Count == 1 && entries[0].Id == SingleConfigId;
				var typeMatches = !hasSearch || type.Name.ToLowerInvariant().Contains(searchLower);

				var entryNodes = new List<TreeViewItemData<ConfigNode>>();
				for (int i = 0; i < entries.Count; i++)
				{
					var entry = entries[i];
					var idStr = isSingleton ? "singleton" : entry.Id.ToString();
					var label = $"{idStr}: {type.Name}";

					if (!typeMatches && hasSearch)
					{
						// ID로 검색을 허용합니다.
						if (!idStr.Contains(searchLower))
						{
							continue;
						}
					}

					entryNodes.Add(new TreeViewItemData<ConfigNode>(id++, ConfigNode.Entry(type, entry.Id, entry.Value, label)));
				}

				if (entryNodes.Count == 0)
				{
					continue;
				}

				var typeNode = new TreeViewItemData<ConfigNode>(id++, ConfigNode.Type(type, $"{type.Name} ({entryNodes.Count})"), entryNodes);
				if (isSingleton)
				{
					childrenSingletons.Add(typeNode);
				}
				else
				{
					childrenCollections.Add(typeNode);
				}
			}

			var roots = new List<TreeViewItemData<ConfigNode>>();
			roots.Add(new TreeViewItemData<ConfigNode>(id++, ConfigNode.Header("Singletons"), childrenSingletons));
			roots.Add(new TreeViewItemData<ConfigNode>(id++, ConfigNode.Header("Collections"), childrenCollections));
			return roots;
		}

		/// <summary>
		/// 주어진 <paramref name="roots"/> 트리에서 지정된
		/// 설정 <paramref name="type"/>과 <paramref name="id"/>에 일치하는 항목 노드를 검색합니다. 찾으면 트리 항목 ID를 반환하고,
		/// 일치하는 항목이 없으면 null을 반환합니다.
		/// </summary>
		public static int? FindTreeItemIdForEntry(IList<TreeViewItemData<ConfigNode>> roots, Type type, int id)
		{
			foreach (var root in roots)
			{
				if (TryFind(root, out var found))
				{
					return found;
				}
			}
			return null;

			bool TryFind(TreeViewItemData<ConfigNode> node, out int foundId)
			{
				if (node.data.Kind == ConfigNodeKind.Entry && node.data.ConfigType == type && node.data.ConfigId == id)
				{
					foundId = node.id;
					return true;
				}

				if (node.hasChildren)
				{
					foreach (var child in node.children)
					{
						if (TryFind(child, out foundId))
						{
							return true;
						}
					}
				}

				foundId = 0;
				return false;
			}
		}
	}
}
