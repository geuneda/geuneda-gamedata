using System.Collections.Generic;
using System.Linq;
using Geuneda.DataExtensions;
using Geuneda.DataExtensions.Editor;
using NUnit.Framework;

namespace Geuneda.DataExtensions.Tests
{
	/// <summary>
	/// <see cref="ConfigValidationService"/>의 유닛 테스트로, 설정 필드의 유효성 검사 어트리뷰트가
	/// 전체 프로바이더 및 단일 항목 범위에서 올바르게 평가되는지 확인합니다.
	/// </summary>
	[TestFixture]
	public class ConfigValidationServiceTests
	{
		/// <summary>
		/// 프로바이더는 타입마다 컨테이너를 하나만 가지므로, 컬렉션과 같은 타입을 싱글톤으로 등록할 수 없습니다.
		/// 싱글톤 검증용으로 <see cref="MockValidatableConfig"/>의 유효성 검사 필드를 그대로 상속한 별도 타입을 사용합니다.
		/// </summary>
		private class MockValidatableSingletonConfig : MockValidatableConfig
		{
		}

		[Test]
		public void ValidateAll_ReportsFieldsAndConfigIds()
		{
			var provider = new ConfigsProvider();
			var invalid = new MockValidatableConfigBuilder().Invalid().Build();
			var invalidSingleton = new MockValidatableSingletonConfig
			{
				Name = invalid.Name,
				Health = invalid.Health,
				Tag = invalid.Tag
			};
			provider.AddSingletonConfig(invalidSingleton);

			var invalidList = new List<MockValidatableConfig>
			{
				new MockValidatableConfigBuilder().Invalid().Build(),
				new MockValidatableConfigBuilder().Invalid().Build()
			};

			var id = 5;
			provider.AddConfigs(_ => id++, invalidList);

			var errors = ConfigValidationService.ValidateAll(provider);

			Assert.IsTrue(errors.Any(e => e.ConfigId == null));
			Assert.IsTrue(errors.Any(e => e.ConfigId == 5));
			Assert.IsTrue(errors.Any(e => e.ConfigId == 6));

			Assert.IsTrue(errors.Any(e => e.FieldName == "Name"));
			Assert.IsTrue(errors.Any(e => e.FieldName == "Health"));
			Assert.IsTrue(errors.Any(e => e.FieldName == "Tag"));
		}

		[Test]
		public void ValidateSingle_UsesSelectionConfigId()
		{
			var invalid = new MockValidatableConfigBuilder().Invalid().Build();
			var selection = new ConfigSelection(typeof(MockValidatableConfig), 42, invalid);

			var errors = ConfigValidationService.ValidateSingle(selection);

			Assert.AreEqual(3, errors.Count);
			Assert.IsTrue(errors.All(e => e.ConfigId == 42));
			Assert.IsTrue(errors.All(e => e.ConfigTypeName == nameof(MockValidatableConfig)));
		}
	}
}
