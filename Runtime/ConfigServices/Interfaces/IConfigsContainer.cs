using System.Collections.Generic;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 컨테이너에서 직렬화할 설정의 제네릭 계약입니다
	/// </summary>
	public interface IConfig
	{
		int ConfigId { get; }
	}

	/// <summary>
	/// ConfigsImporter 스크립트로 가져온 설정의 제네릭 컨테이너입니다
	/// 주어진 <typeparamref name="T"/> 타입은 스크립터블 오브젝트에서 직렬화되도록 정의된 설정 구조체와 동일합니다
	/// </summary>
	public interface IConfigsContainer<T>
	{
		List<T> Configs { get; set; }
	}

	/// <summary>
	/// ConfigsImporter 스크립트로 가져온 고유 단일 설정의 제네릭 컨테이너입니다
	/// 주어진 <typeparamref name="T"/> 타입은 스크립터블 오브젝트에서 직렬화되도록 정의된 설정 구조체와 동일합니다
	/// </summary>
	public interface ISingleConfigContainer<T>
	{
		T Config { get; set; }
	}

	/// <inheritdoc />
	/// <remarks>
	/// 주어진 <typeparamref name="TKey"/> ID로 매핑된 쌍으로 설정 데이터를 보유하려면 이 설정 컨테이너를 사용하세요
	/// </remarks>
	public interface IPairConfigsContainer<TKey, TValue> : IConfigsContainer<Pair<TKey, TValue>>
	{
	}

	/// <inheritdoc />
	/// <remarks>
	/// 주어진 <typeparamref name="TKey"/> ID로 매핑된 쌍으로 설정 데이터를 보유하려면 이 설정 컨테이너를 사용하세요
	/// </remarks>
	public interface IStructPairConfigsContainer<TKey, TValue> : IConfigsContainer<StructPair<TKey, TValue>>
		where TKey : struct
		where TValue : struct
	{
	}
}
