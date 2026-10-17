using Geuneda.Core;

namespace Geuneda.DataExtensions
{
	/// <summary>
	/// gamedata 패키지의 로거입니다.
	/// </summary>
	/// <remarks>
	/// 제네릭 형식(<see cref="ConfigsScriptableObject{TId,TAsset}"/>, <see cref="EnumSelector{T}"/>, <see cref="SerializableType{T}"/>)의
	/// static 필드는 닫힌 형식마다 따로 생기므로, 로거는 제네릭이 아닌 이 클래스에 하나만 둡니다.
	/// </remarks>
	internal static class GameDataLog
	{
		internal static readonly ModuleLogger Logger = new ModuleLogger("Geuneda.GameData");
	}
}
