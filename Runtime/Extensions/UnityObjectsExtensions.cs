using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#nullable enable
// ReSharper disable once CheckNamespace

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// Unity 객체를 위한 확장 메서드입니다.
	/// </summary>
	public static class UnityObjectsExtensions
	{
		/// <summary>
		/// Transform의 로컬 공간에서 <see cref="RectTransform"/>의 코너를 가져옵니다.
		/// </summary>
		public static Vector3[] GetLocalCornersArray(this RectTransform transform)
		{
			var corners =  new Vector3[4];
			var rect = transform.rect;
			var x = rect.x;
			var y = rect.y;
			var xMax = rect.xMax;
			var yMax = rect.yMax;

			corners[0] = new Vector3(x, y, 0f);
			corners[1] = new Vector3(x, yMax, 0f);
			corners[2] = new Vector3(xMax, yMax, 0f);
			corners[3] = new Vector3(xMax, y, 0f);

			return corners;
		}

		/// <summary>
		/// Transform의 월드 공간에서 <see cref="RectTransform"/>의 코너를 가져옵니다.
		/// </summary>
		public static Vector3[] GetWorldCornersArray(this RectTransform transform)
		{
			var corners = transform.GetLocalCornersArray();
			var matrix4x = transform.localToWorldMatrix;
			for (int i = 0; i < 4; i++)
			{
				corners[i] = matrix4x.MultiplyPoint(corners[i]);
			}

			return corners;
		}

		/// <summary>
		/// 지정된 화면 지점에서 레이캐스트를 수행하는 GraphicRaycaster의 확장 메서드입니다.
		/// 객체가 적중되면 true를 반환합니다.
		/// </summary>
		public static bool RaycastPoint(this GraphicRaycaster raycaster, Vector2 screenPoint, out List<RaycastResult> results)
		{
			var eventData = new PointerEventData(EventSystem.current)
			{
				position = screenPoint,
				displayIndex = 0
			};

			results = new List<RaycastResult>();

			raycaster.Raycast(eventData, results);

			return results.Count > 0;
		}
	}
}
