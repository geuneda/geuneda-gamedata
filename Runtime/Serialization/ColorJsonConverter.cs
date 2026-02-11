using Newtonsoft.Json;
using UnityEngine;
using System;

namespace Geuneda.DataExtensions
{
    /// <summary>
    /// 다음 형식으로의 직렬화/역직렬화를 처리하는 Unity의 Color 구조체용 JSON 변환기입니다:
    /// - 16진수 색상 문자열 (예: 빨간색의 "#FF0000FF")
    /// - RGBA 객체 형식 (예: {"r":1,"g":0,"b":0,"a":1})
    /// </summary>
    /// <remarks>
    /// 이 변환기는 Newtonsoft.Json으로 Unity Color 객체의 적절한 직렬화를 가능하게 합니다.
    /// JSON 설정 파일이나 네트워크 페이로드에 색상 데이터를 저장하는 데 특히 유용합니다.
    /// </remarks>
    public class ColorJsonConverter : JsonConverter<Color>
    {
        /// <summary>
        /// JSON 데이터를 읽고 Unity Color 객체로 변환합니다
        /// </summary>
        /// <param name="reader">입력 데이터를 제공하는 JSON 리더입니다</param>
        /// <param name="objectType">역직렬화할 객체의 타입입니다(Color이어야 합니다)</param>
        /// <param name="existingValue">읽고 있는 객체의 기존 값입니다</param>
        /// <param name="hasExistingValue">기존 값이 있는지 여부입니다</param>
        /// <param name="serializer">JSON 직렬화기 인스턴스입니다</param>
        /// <returns>역직렬화된 Color 객체입니다</returns>
        /// <remarks>
        /// 16진수 색상 문자열과 RGBA 객체 형식 모두를 지원합니다:
        /// - "#RRGGBBAA" 또는 "#RRGGBB" (알파가 없으면 기본값 1)
        /// - {"r":1,"g":0,"b":0,"a":1} (누락된 컴포넌트 기본값 0, 알파만 기본값 1)
        /// </remarks>
        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string colorString = reader.Value.ToString();
                Color color;
                if (ColorUtility.TryParseHtmlString(colorString, out color))
                {
                    return color;
                }
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                float r = 0, g = 0, b = 0, a = 1;
                
                reader.Read();
                while (reader.TokenType != JsonToken.EndObject)
                {
                    string propertyName = reader.Value.ToString().ToLower();
                    reader.Read();
                    
                    switch (propertyName)
                    {
                        case "r": r = Convert.ToSingle(reader.Value); break;
                        case "g": g = Convert.ToSingle(reader.Value); break;
                        case "b": b = Convert.ToSingle(reader.Value); break;
                        case "a": a = Convert.ToSingle(reader.Value); break;
                    }
                    
                    reader.Read();
                }
                
                return new Color(r, g, b, a);
            }
            
            return Color.white;
        }

        /// <summary>
        /// Color 객체를 JSON 형식으로 씁니다
        /// </summary>
        /// <param name="writer">출력용 JSON 라이터입니다</param>
        /// <param name="value">직렬화할 Color 값입니다</param>
        /// <param name="serializer">JSON 직렬화기 인스턴스입니다</param>
        /// <remarks>
        /// 항상 16진수 문자열 형식으로 직렬화합니다(예: 빨간색의 "#FF0000FF")
        /// 이는 컴팩트하고 웹 친화적인 표현을 제공합니다
        /// </remarks>
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteValue("#" + ColorUtility.ToHtmlStringRGBA(value));
        }
    }
}
