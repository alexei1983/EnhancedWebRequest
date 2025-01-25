using System.Text.Json;
using System.Net.Mime;
using System.Net.Http.Headers;

namespace Llc.GoodConsulting.Web.EnhancedWebRequest
{
    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResponse">Type of the entity to de-serialize from JSON.</typeparam>
        /// <param name="response">Response received from a remote HTTP endpoint.</param>
        /// <param name="serializerSettings">JSON deserialization options and settings.</param>
        /// <returns><seealso cref="Task{TResponse}"/> containing the response entity de-serialized from JSON.</returns>
        public async static Task<TResponse?> ReadFromJsonAsync<TResponse>(this HttpResponseMessage response, JsonSerializerOptions? serializerSettings = null)
        {
            if (response is null)
                return default;

            serializerSettings ??= JsonSerializerOptions.Default;

            var jsonStr = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(jsonStr))
                return JsonSerializer.Deserialize<TResponse>(jsonStr, serializerSettings);

            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TRequest">Type of the entity to serialize as JSON into the <seealso cref="StringContent"/> object.</typeparam>
        /// <param name="obj">Entity to serialize as JSON.</param>
        /// <param name="serializerSettings">JSON serialization options and settings.</param>
        /// <returns><seealso cref="StringContent"/> containing the entity serialized as JSON.</returns>
        public static StringContent ToJsonStringContent<TRequest>(this TRequest obj, JsonSerializerOptions? serializerSettings = null) where TRequest : class, new()
        {
            if (obj is null)
                return new StringContent(string.Empty);

            serializerSettings ??= JsonSerializerOptions.Default;

            var jsonContent = new StringContent(JsonSerializer.Serialize(obj, serializerSettings));
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
            return jsonContent;
        }
    }
}
