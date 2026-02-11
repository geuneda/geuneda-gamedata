using System;
using System.Collections;
using System.Collections.Generic;

namespace Geuneda.DataExtensions
{
	/// <inheritdoc />
	public class ConfigsProvider : IConfigsAdder
	{
		private const int _singleConfigId = 0;
		private ulong _version;

		public ulong Version => _version;
		
		private readonly IDictionary<Type, IEnumerable> _configs = new Dictionary<Type, IEnumerable>();

		public ConfigsProvider()
		{
#if UNITY_EDITOR
			ConfigsProviderDebugRegistry.Register(this);
#endif
		}

		/// <inheritdoc />
		public bool TryGetConfig<T>(int id, out T config)
		{
			return GetConfigsDictionary<T>().TryGetValue(id, out config);
		}

		/// <inheritdoc />
		public bool TryGetConfig<T>(out T config)
		{
			config = default;

			if (!_configs.TryGetValue(typeof(T), out var enumerable))
			{
				return false;
			}

			return enumerable is IReadOnlyDictionary<int, T> dictionary && dictionary.TryGetValue(_singleConfigId, out config);
		}

		/// <inheritdoc />
		public T GetConfig<T>()
		{
			if (!TryGetConfig<T>(out var config))
			{
				throw new InvalidOperationException($"The Config container for {typeof(T)} is not a single config container. " +
				                                    $"Use either 'GetConfig<T>(int id)' or 'GetConfigsList<T>()' to get your needed config");
			}
			
			return config;
		}

		/// <inheritdoc />
		public T GetConfig<T>(int id)
		{
			return GetConfigsDictionary<T>()[id];
		}

		/// <inheritdoc />
		public List<T> GetConfigsList<T>()
		{
			return new List<T>(GetConfigsDictionary<T>().Values);
		}

		/// <inheritdoc />
		public virtual IReadOnlyDictionary<int, T> GetConfigsDictionary<T>() 
		{
			return _configs[typeof(T)] as IReadOnlyDictionary<int, T>;
		}

		/// <inheritdoc />
		public IEnumerable<T> EnumerateConfigs<T>()
		{
			return GetConfigsDictionary<T>().Values;
		}

		/// <inheritdoc />
		public IEnumerable<KeyValuePair<int, T>> EnumerateConfigsWithIds<T>()
		{
			return GetConfigsDictionary<T>();
		}

		/// <inheritdoc />
		public void AddSingletonConfig<T>(T config)
		{
			_configs.Add(typeof(T), new Dictionary<int, T> {{ _singleConfigId, config }});
		}

		/// <inheritdoc />
		public void AddConfigs<T>(Func<T, int> referenceIdResolver, IList<T> configList)
		{
			if (referenceIdResolver == null)
			{
				throw new ArgumentNullException(nameof(referenceIdResolver));
			}

			if (configList == null)
			{
				throw new ArgumentNullException(nameof(configList));
			}

			var dictionary = new Dictionary<int, T>(configList.Count);

			for (int i = 0; i < configList.Count; i++)
			{
				dictionary.Add(referenceIdResolver(configList[i]), configList[i]);
			}

			_configs.Add(typeof(T), dictionary);
		}
		
		/// <inheritdoc />
		public IReadOnlyDictionary<Type, IEnumerable> GetAllConfigs()
		{
			return _configs as IReadOnlyDictionary<Type, IEnumerable>;
		}

		/// <inheritdoc />
		public void AddAllConfigs(IReadOnlyDictionary<Type, IEnumerable> configs)
		{
			foreach (var type in configs.Keys)
			{
				_configs[type] = configs[type];
			}
		}

		/// <inheritdoc />
		public void UpdateTo(ulong version, IReadOnlyDictionary<Type, IEnumerable> toUpdate)
		{
			AddAllConfigs(toUpdate);
			SetVersion(version);
		}

		/// <summary>
		/// 현재 설정의 현재 버전 번호를 설정합니다.
		/// 원자성을 위해 <see cref="UpdateTo"/>를 통해서만 버전이 업데이트되도록 internal로 제한합니다.
		/// </summary>
		internal void SetVersion(ulong version)
		{
			_version = version;
		}
	}
}
