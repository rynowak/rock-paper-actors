using Newtonsoft.Json;
using System;
using System.Text;

namespace Rochambot
{
    public static class StringExtensions
    {
        public static string ToJson(this object value)
            => JsonConvert.SerializeObject(value);

        public static T To<T>(this string value)
            => JsonConvert.DeserializeObject<T>(value);

        public static string FromUTF8Bytes(this byte[] bytes)
            => Encoding.UTF8.GetString(bytes);

        public static byte[] ToUTF8Bytes(this string value)
            => Encoding.UTF8.GetBytes(value);

        public static TEnum ToEnum<TEnum>(this string value) 
            where TEnum : struct, Enum
            => !string.IsNullOrWhiteSpace(value) 
            && Enum.TryParse(value, true, out TEnum result)
                ? result
                : default;
    }
}