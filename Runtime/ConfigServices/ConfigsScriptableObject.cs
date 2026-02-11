using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// 키-값 쌍을 저장하는 설정 스크립터블 오브젝트의 추상 기본 클래스입니다.
	/// Provides a foundation for config containers with serializable dictionary collections using Unity's serialization workaround pattern.
	/// </summary>
	/// <typeparam name="TId">식별자/키의 타입입니다.</typeparam>
	/// <typeparam name="TAsset">에셋/값의 타입입니다.</typeparam>
	public abstract class ConfigsScriptableObject<TId, TAsset> : 
		ScriptableObject, IPairConfigsContainer<TId, TAsset>, ISerializationCallbackReceiver
	{
		[SerializeField] private List<Pair<TId, TAsset>> _configs = new();

		/// <inheritdoc />
		public List<Pair<TId, TAsset>> Configs
		{
			get => _configs;
			set => _configs = value;
		}

		/// <summary>
		/// 효율적인 조회 작업을 위해 설정을 읽기 전용 딕셔너리로 제공합니다.
		/// </summary>
		public IReadOnlyDictionary<TId, TAsset> ConfigsDictionary { get; private set; }

		/// <inheritdoc />
		public void OnBeforeSerialize()
		{
			// Unity 직렬화가 리스트 형식을 자동으로 처리합니다
			// 직렬화 전 변환이 필요하지 않습니다
		}

		/// <inheritdoc />
		public virtual void OnAfterDeserialize()
		{
			// 효율적인 조회를 위해 직렬화된 리스트를 딕셔너리로 변환합니다
			var dictionary = new Dictionary<TId, TAsset>();

			foreach (var config in Configs)
			{
				if (!dictionary.TryAdd(config.Key, config.Value))
				{
					Debug.LogError($"Duplicate key '{config.Key}' found in {GetType().Name}. Skipping.");
				}
			}

			ConfigsDictionary = new ReadOnlyDictionary<TId, TAsset>(dictionary);
		}
	}
}
