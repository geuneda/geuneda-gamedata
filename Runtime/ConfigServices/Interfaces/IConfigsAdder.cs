using System;
using System.Collections;
using System.Collections.Generic;

namespace Geuneda.DataExtensions
{
	/// <inheritdoc />
	/// <remarks>
	/// 프로바이더에 설정을 추가할 수 있도록 <see cref="IConfigsProvider"/> 동작을 확장합니다
	/// </remarks>
	public interface IConfigsAdder : IConfigsProvider
	{
		/// <summary>
		/// 주어진 고유 단일 <paramref name="config"/>을 컨테이너에 추가합니다.
		/// </summary>
		void AddSingletonConfig<T>(T config);

		/// <summary>
		/// 주어진 <paramref name="configList"/>를 컨테이너에 추가합니다.
		/// 설정은 주어진 <paramref name="referenceIdResolver"/>를 사용하여 각 설정을 정의된 ID에 매핑합니다.
		/// </summary>
		void AddConfigs<T>(Func<T, int> referenceIdResolver, IList<T> configList);

		/// <summary>
		/// 주어진 설정 목록 딕셔너리를 설정에 추가합니다.
		/// </summary>
		void AddAllConfigs(IReadOnlyDictionary<Type, IEnumerable> configs);

		/// <summary>
		/// 주어진 설정을 주어진 버전으로 업데이트합니다
		/// </summary>
		void UpdateTo(ulong version, IReadOnlyDictionary<Type, IEnumerable> toUpdate);
	}
}
