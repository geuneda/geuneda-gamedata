using System;
using Geuneda.DataExtensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geuneda.DataExtensions.Tests
{
	[TestFixture]
	public class EnumSelectorTest
	{
		public enum EnumExample
		{
			Value1,
			Value2
		}

		public class EnumSelectorExample : EnumSelector<EnumExample>
		{
			public EnumSelectorExample() : base(EnumExample.Value1)
			{
			}

			public EnumSelectorExample(EnumExample data) : base(data)
			{
			}

			public static implicit operator EnumSelectorExample(EnumExample value)
			{
				return new EnumSelectorExample(value);
			}
		}

		private EnumSelectorExample _enumSelector;

		[SetUp]
		public void Init()
		{
			_enumSelector = new EnumSelectorExample(EnumExample.Value1);
		}

		[Test]
		public void ValueCheck()
		{
			Assert.AreEqual(EnumExample.Value1, _enumSelector.GetSelection());
			Assert.AreEqual((int)EnumExample.Value1, _enumSelector.GetSelectedIndex());
			Assert.AreEqual(EnumExample.Value1.ToString(), _enumSelector.GetSelectionString());
			Assert.IsTrue(_enumSelector.HasValidSelection());
		}

		[Test]
		public void ValueSetCheck()
		{
			_enumSelector.SetSelection(EnumExample.Value2);

			Assert.AreEqual(EnumExample.Value2, _enumSelector.GetSelection());
			Assert.AreEqual((int)EnumExample.Value2, _enumSelector.GetSelectedIndex());
			Assert.AreEqual(EnumExample.Value2.ToString(), _enumSelector.GetSelectionString());
			Assert.IsTrue(_enumSelector.HasValidSelection());
		}

		[Test]
		public void GetSelection_InvalidSelection_ReturnsFirstValue()
		{
			var type = typeof(EnumSelector<EnumExample>);
			var field = type.GetField("_selection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			field.SetValue(_enumSelector, "InvalidValue");

			// 유효하지 않은 선택을 가져올 때 오류 로그를 예상합니다
			LogAssert.Expect(LogType.Error, "Could not load enum for string: InvalidValue");

			// 선택이 유효하지 않을 때 EnumSelector는 첫 번째 열거형 값을 반환합니다
			Assert.AreEqual(EnumExample.Value1, _enumSelector.GetSelection());
		}

		[Test]
		public void HasValidSelection_RemovedEnumValue_ReturnsFalse()
		{
			var type = typeof(EnumSelector<EnumExample>);
			var field = type.GetField("_selection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			field.SetValue(_enumSelector, "NonExistentValue");

			// GetSelectedIndex에서의 오류 로그를 예상합니다
			LogAssert.Expect(LogType.Error, "Could not load enum for string: NonExistentValue");

			Assert.IsFalse(_enumSelector.HasValidSelection());
		}

		[Test]
		public void HasValidSelection_EmptyString_ReturnsFalse()
		{
			var type = typeof(EnumSelector<EnumExample>);
			var field = type.GetField("_selection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			field.SetValue(_enumSelector, "");

			// GetSelectedIndex에서의 오류 로그를 예상합니다
			LogAssert.Expect(LogType.Error, "Could not load enum for string: ");

			Assert.IsFalse(_enumSelector.HasValidSelection());
		}

		[Test]
		public void ImplicitConversion_ToEnum_Works()
		{
			EnumExample val = _enumSelector;
			Assert.AreEqual(EnumExample.Value1, val);
		}
	}
}