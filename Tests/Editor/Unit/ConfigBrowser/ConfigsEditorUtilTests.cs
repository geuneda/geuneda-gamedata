using System.Collections.Generic;
using Geuneda.DataExtensions;
using Geuneda.DataExtensions.Editor;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests
{
	/// <summary>
	/// <see cref="ConfigsEditorUtil"/>의 유닛 테스트로, 리플렉션 기반
	/// <c>Dictionary&lt;int, T&gt;</c> 컨테이너에서의 설정 읽기가 올바르게 작동하고
	/// 지원되지 않는 컨테이너 타입을 거부하는지 확인합니다.
	/// </summary>
	[TestFixture]
	public class ConfigsEditorUtilTests
	{
		[Test]
		public void TryReadConfigs_WithDictionaryIntKey_ReturnsSortedEntries()
		{
			var dict = new Dictionary<int, string>
			{
				{ 2, "B" },
				{ 1, "A" }
			};

			var success = ConfigsEditorUtil.TryReadConfigs(dict, out var entries);

			Assert.IsTrue(success);
			Assert.AreEqual(2, entries.Count);
			Assert.AreEqual(1, entries[0].Id);
			Assert.AreEqual("A", entries[0].Value);
			Assert.AreEqual(2, entries[1].Id);
			Assert.AreEqual("B", entries[1].Value);
		}

		[Test]
		public void TryReadConfigs_WithNonDictionary_ReturnsFalse()
		{
			var list = new List<int> { 1, 2, 3 };

			var success = ConfigsEditorUtil.TryReadConfigs(list, out var entries);

			Assert.IsFalse(success);
			Assert.AreEqual(0, entries.Count);
		}

		[Test]
		public void TryReadConfigs_WithNonIntKey_ReturnsFalse()
		{
			var dict = new Dictionary<string, int>
			{
				{ "one", 1 }
			};

			var success = ConfigsEditorUtil.TryReadConfigs(dict, out var entries);

			Assert.IsFalse(success);
			Assert.AreEqual(0, entries.Count);
		}
	}
}
