using System;
using System.Collections;
using System.Collections.Generic;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 게임 디자인 데이터를 포함한 게임의 모든 정적 설정 데이터를 제공합니다
	/// 웹의 Universal Google Sheet 파일에서 가져온 데이터를 보유합니다
	/// </summary>
	public interface IConfigsProvider
	{
		/// <summary>
		///  현재 설정의 현재 버전을 가져옵니다
		/// </summary>
		ulong Version { get; }
		
		/// <summary>
		/// <typeparamref name="T"/> 타입이고 주어진 <paramref name="id"/>를 가진 설정을 요청합니다.
		/// <typeparamref name="T"/> 타입이고 주어진 <paramref name="id"/>를 가진 <paramref name="config"/>이 있으면 true를 반환하고,
		/// 그렇지 않으면 false를 반환합니다.
		/// </summary>
		bool TryGetConfig<T>(int id, out T config);
		
		/// <summary>
		/// <typeparamref name="T"/> 타입의 고유 단일 설정을 요청합니다.
		/// <typeparamref name="T"/> 타입의 <paramref name="config"/>이 있으면 true, 그렇지 않으면 false를 반환합니다.
		/// </summary>
		bool TryGetConfig<T>(out T config);
		
		/// <summary>
		/// <typeparamref name="T"/> 타입의 고유 단일 설정을 요청합니다
		/// </summary>
		T GetConfig<T>();
		
		/// <summary>
		/// <typeparamref name="T"/> 타입이고 주어진 <paramref name="id"/>를 가진 설정을 요청합니다
		/// </summary>
		T GetConfig<T>(int id);

		/// <summary>
		/// <typeparamref name="T"/> 타입의 설정 목록을 요청합니다
		/// </summary>
		List<T> GetConfigsList<T>();

		/// <summary>
		/// <typeparamref name="T"/> 타입의 설정 딕셔너리를 요청합니다
		/// </summary>
		IReadOnlyDictionary<int, T> GetConfigsDictionary<T>();
		
		/// <summary>
		/// 새 목록을 할당하지 않고 <typeparamref name="T"/> 타입의 모든 설정을 열거합니다.
		/// </summary>
		IEnumerable<T> EnumerateConfigs<T>();

		/// <summary>
		/// 새 목록을 할당하지 않고 <typeparamref name="T"/> 타입의 모든 설정을 ID와 함께 열거합니다.
		/// </summary>
		IEnumerable<KeyValuePair<int, T>> EnumerateConfigsWithIds<T>();

		/// <summary>
		/// 모든 설정을 읽기 전용 딕셔너리로 가져옵니다
		/// </summary>
		IReadOnlyDictionary<Type, IEnumerable> GetAllConfigs();
	}
}
