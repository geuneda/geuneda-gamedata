using System.Collections.Generic;

namespace GeunedaEditor.GameData
{
	/// <summary>
	/// 오류 없이 유효성 검사를 통과한 설정을 나타냅니다.
	/// 설정 타입과 ID에 대한 정보를 포함합니다.
	/// </summary>
	public class ValidConfig
	{
		/// <summary>
		/// 유효성 검사를 통과한 설정 타입의 이름입니다.
		/// </summary>
		public string ConfigType { get; set; }

		/// <summary>
		/// 설정 인스턴스의 ID, 싱글톤 설정의 경우 null입니다.
		/// </summary>
		public int? ConfigId { get; set; }

		/// <summary>
		/// 유효한 설정의 포맷된 문자열 표현을 반환합니다.
		/// 형식: "[ConfigType ID:X]" 또는 싱글톤의 경우 "[ConfigType]".
		/// </summary>
		public override string ToString()
		{
			var idStr = ConfigId.HasValue ? $" ID:{ConfigId.Value}" : "";
			return $"[{ConfigType}{idStr}]";
		}
	}

	/// <summary>
	/// 설정 유효성 검사 중 발견된 단일 유효성 검사 오류를 나타냅니다.
	/// 설정 타입, ID, 필드 이름, 오류 메시지에 대한 정보를 포함합니다.
	/// </summary>
	public class ValidationError
	{
		/// <summary>
		/// 유효성 검사 오류를 포함하는 설정 타입의 이름입니다.
		/// </summary>
		public string ConfigType { get; set; }

		/// <summary>
		/// 설정 인스턴스의 ID, 싱글톤 설정의 경우 null입니다.
		/// </summary>
		public int? ConfigId { get; set; }

		/// <summary>
		/// 유효성 검사에 실패한 필드 또는 프로퍼티의 이름입니다.
		/// </summary>
		public string FieldName { get; set; }

		/// <summary>
		/// 무엇이 잘못되었는지 설명하는 유효성 검사 오류 메시지입니다.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// 유효성 검사 오류의 포맷된 문자열 표현을 반환합니다.
		/// 형식: "[ConfigType ID:X] FieldName: Message" 또는 싱글톤의 경우 "[ConfigType] FieldName: Message".
		/// </summary>
		public override string ToString()
		{
			var idStr = ConfigId.HasValue ? $" ID:{ConfigId.Value}" : "";
			return $"[{ConfigType}{idStr}] {FieldName}: {Message}";
		}
	}

	/// <summary>
	/// 설정 유효성 검사 작업의 결과를 포함합니다.
	/// 발견된 유효성 검사 오류에 대한 접근과 전체 유효성을 확인하는 편의 프로퍼티를 제공합니다.
	/// </summary>
	public class ValidationResult
	{
		/// <summary>
		/// 유효성 검사가 오류 없이 통과했는지 여부를 가져옵니다.
		/// <see cref="Errors"/>가 비어 있으면 true, 그렇지 않으면 false를 반환합니다.
		/// </summary>
		public bool IsValid => Errors.Count == 0;

		/// <summary>
		/// 유효성 검사 중 발견된 유효성 검사 오류 목록을 가져옵니다.
		/// 모든 유효성 검사가 통과되면 비어 있습니다.
		/// </summary>
		public List<ValidationError> Errors { get; } = new List<ValidationError>();

		/// <summary>
		/// 오류 없이 유효성 검사를 통과한 설정 목록을 가져옵니다.
		/// </summary>
		public List<ValidConfig> ValidConfigs { get; } = new List<ValidConfig>();
	}
}
