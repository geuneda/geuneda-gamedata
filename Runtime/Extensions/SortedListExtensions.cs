using System;
using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 정렬 순서가 유지되도록 이미 정렬된 것으로 간주되는 리스트에 요소를 삽입하는
	/// System.Collections.Generic.IList{T} 및 System.Collections.IList 인터페이스용 확장 함수 컨테이너입니다
	/// </summary>
	/// <author>Jackson Dunstan, http://JacksonDunstan.com/articles/3189</author>
	/// <license>MIT</license>
	public static class SortedListExtensions
	{
		/// <summary>
		/// 정렬 순서가 유지되도록 이미 정렬된 것으로 간주되는 IList{T}에
		/// 값을 삽입합니다
		/// </summary>
		/// <param name="list">삽입할 목록입니다</param>
		/// <param name="value">삽입할 값입니다</param>
		/// <typeparam name="T">삽입할 요소의 타입이자 목록 내 요소의 타입입니다</typeparam>
		public static void InsertIntoSortedList<T>(this IList<T> list, T value) where T : IComparable<T>
		{
			InsertIntoSortedList(list, value, (a, b) => a.CompareTo(b));
		}

		/// <summary>
		/// 정렬 순서가 유지되도록 이미 정렬된 것으로 간주되는 IList{T}에
		/// 값을 삽입합니다
		/// </summary>
		/// <param name="list">삽입할 목록입니다</param>
		/// <param name="value">삽입할 값입니다</param>
		/// <param name="comparison">정렬 순서를 결정할 비교입니다</param>
		/// <typeparam name="T">삽입할 요소의 타입이자 목록 내 요소의 타입입니다</typeparam>
		public static void InsertIntoSortedList<T>(this IList<T> list, T value, Comparison<T> comparison)
		{
			var startIndex = 0;
			var endIndex = list.Count;

			while (endIndex > startIndex)
			{
				var windowSize = endIndex - startIndex;
				var middleIndex = startIndex + (windowSize / 2);
				var middleValue = list[middleIndex];
				var compareToResult = comparison(middleValue, value);

				if (compareToResult == 0)
				{
					list.Insert(middleIndex, value);
					return;
				}

				if (compareToResult < 0)
				{
					startIndex = middleIndex + 1;
				}
				else
				{
					endIndex = middleIndex;
				}
			}

			list.Insert(startIndex, value);
		}

		/// <summary>
		/// 정렬 순서가 유지되도록 이미 정렬된 것으로 간주되는 IList{T}에
		/// 값을 삽입합니다
		/// </summary>
		/// <param name="list">삽입할 목록입니다</param>
		/// <param name="value">삽입할 값입니다</param>
		/// <param name="comparer">정렬 순서를 결정할 비교자입니다</param>
		/// <typeparam name="T">삽입할 요소의 타입이자 목록 내 요소의 타입입니다</typeparam>
		public static void InsertIntoSortedList<T>(this IList<T> list, T value, IComparer<T> comparer)
		{
			var startIndex = 0;
			var endIndex = list.Count;

			while (endIndex > startIndex)
			{
				var windowSize = endIndex - startIndex;
				var middleIndex = startIndex + (windowSize / 2);
				var middleValue = list[middleIndex];
				var compareToResult = comparer.Compare(middleValue, value);

				if (compareToResult == 0)
				{
					list.Insert(middleIndex, value);
					return;
				}

				if (compareToResult < 0)
				{
					startIndex = middleIndex + 1;
				}
				else
				{
					endIndex = middleIndex;
				}
			}

			list.Insert(startIndex, value);
		}

		/// <summary>
		/// 정렬 순서가 유지되도록 이미 정렬된 것으로 간주되는 IList에 값을 삽입합니다
		/// </summary>
		/// <param name="list">삽입할 목록입니다</param>
		/// <param name="value">삽입할 값입니다</param>
		public static void InsertIntoSortedList(this IList list, IComparable value)
		{
			InsertIntoSortedList(list, value, (a, b) => a.CompareTo(b));
		}

		/// <summary>
		/// 정렬 순서가 유지되도록 이미 정렬된 것으로 간주되는 IList에 값을 삽입합니다
		/// </summary>
		/// <param name="list">삽입할 목록입니다</param>
		/// <param name="value">삽입할 값입니다</param>
		/// <param name="comparison">정렬 순서를 결정할 비교입니다</param>
		public static void InsertIntoSortedList(this IList list, IComparable value, Comparison<IComparable> comparison)
		{
			var startIndex = 0;
			var endIndex = list.Count;
			while (endIndex > startIndex)
			{
				var windowSize = endIndex - startIndex;
				var middleIndex = startIndex + (windowSize / 2);
				var middleValue = (IComparable)list[middleIndex];
				var compareToResult = comparison(middleValue, value);
				if (compareToResult == 0)
				{
					list.Insert(middleIndex, value);
					return;
				}
				if (compareToResult < 0)
				{
					startIndex = middleIndex + 1;
				}
				else
				{
					endIndex = middleIndex;
				}
			}
			list.Insert(startIndex, value);
		}
	}
}