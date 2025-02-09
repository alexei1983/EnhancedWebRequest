using System.Text.Json;
using System.Net.Mime;
using System.Net.Http.Headers;
using System.Net;

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
        /// <param name="jsonOptions">JSON serialization options and settings.</param>
        /// <returns><seealso cref="StringContent"/> containing the entity serialized as JSON.</returns>
        public static StringContent ToJsonStringContent<TRequest>(this TRequest obj, JsonSerializerOptions? jsonOptions = null) where TRequest : class, new()
        {
            if (obj is null)
                return new StringContent(string.Empty);
            var jsonContent = new StringContent(JsonSerializer.Serialize(obj, jsonOptions ?? JsonSerializerOptions.Default));
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
            return jsonContent;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="message"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static TEntity? AsEntity<TEntity>(this HttpResponseMessage message, Func<HttpResponseMessage, TEntity?> func) where TEntity : class, new()
        {
            if (message is null || message.Content is null)
                return default;

            return func(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="message"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static async Task<TEntity?> AsEntityAsync<TEntity>(this HttpResponseMessage message, Func<HttpResponseMessage, Task<TEntity?>> func) where TEntity : class, new()
        {
            return await func(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="message"></param>
        /// <param name="jsonSettings"></param>
        /// <returns></returns>
        public static async Task<TEntity?> AsJsonEntityAsync<TEntity>(this HttpResponseMessage message, JsonSerializerOptions? jsonSettings = null) where TEntity : class, new()
        {
            if (message is not null && message.Content is not null)
            {
                return await message.AsEntityAsync(async (message) =>
                {
                    return JsonSerializer.Deserialize<TEntity>(await message.Content.ReadAsStringAsync(), jsonSettings ?? JsonSerializerOptions.Default);
                });
            }
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="message"></param>
        /// <param name="jsonSettings"></param>
        /// <returns></returns>
        public static TEntity? AsJsonEntity<TEntity>(this HttpResponseMessage message, JsonSerializerOptions? jsonSettings = null) where TEntity : class, new()
        {
            if (message is not null && message.Content is not null)
            {
                return message.AsEntity((message) =>
                {
                    return JsonSerializer.Deserialize<TEntity>(message.AsString() ?? string.Empty, jsonSettings ?? JsonSerializerOptions.Default);
                });
            }
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string? AsString(this HttpResponseMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (message.Content is not null)
                return message.Content.ReadAsStringAsync().Result;
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task<string?> AsStringAsync(this HttpResponseMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (message.Content is not null)
                return await message.Content.ReadAsStringAsync();
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TError"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        public static HttpResponseMessage OnError(this HttpResponseMessage message, Action<HttpResponseMessage> action)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (!message.IsSuccessStatusCode)
                action(message);
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> OnErrorAsync(this HttpResponseMessage message, Func<HttpResponseMessage, Task> action)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (!message.IsSuccessStatusCode)
                await action(message);
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TError"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task<TError?> AsJsonEntityError<TError>(this HttpResponseMessage message) where TError : class, new()
        {
            if (message is not null && !message.IsSuccessStatusCode)
                return await message.AsJsonEntityAsync<TError>();
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        /// <exception cref="HttpException"></exception>
        public static HttpResponseMessage ExpectStatus(this HttpResponseMessage message, HttpStatusCode status) 
        {
            if (message is not null && message.StatusCode == status)
                return message;
            throw new HttpException($"Unexpected status code in response: {message?.StatusCode}", message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="message"></param>
        /// <param name="statuses"></param>
        /// <returns></returns>
        /// <exception cref="HttpException"></exception>
        public static HttpResponseMessage ExpectStatusIn(this HttpResponseMessage message, params HttpStatusCode[] statuses) 
        {
            if (message is not null && statuses.Contains(message.StatusCode))
                return message;
            throw new HttpException($"Unexpected status code in response: {message?.StatusCode}", message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        /// <exception cref="HttpException"></exception>
        public static HttpResponseMessage OnStatus(this HttpResponseMessage message, Action<HttpResponseMessage> action, HttpStatusCode status)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (message.StatusCode == status)
                action(message);
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="action"></param>
        /// <param name="statuses"></param>
        /// <returns></returns>
        public static HttpResponseMessage OnStatusIn(this HttpResponseMessage message, Action<HttpResponseMessage> action, params HttpStatusCode[] statuses)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (statuses.Contains(message.StatusCode))
                action(message);
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="HttpException"></exception>
        public static HttpResponseMessage ExpectSuccess(this HttpResponseMessage message)
        {
            if (message is not null && message.IsSuccessStatusCode)
                return message;
            throw new HttpException($"Unexpected status code in response: {message?.StatusCode}", message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async static Task<bool> HasContent(this HttpResponseMessage message)
        {
            if (message is not null)
            {
                if (message.Content is not null)
                {
                    if (message.Content.Headers.ContentLength == 0)
                        return false;

                    var bytes = await message.Content.ReadAsByteArrayAsync();
                    return bytes.Length > 0;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TError"></typeparam>
        /// <param name="message"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static HttpResponseMessage OnSuccess<TError>(this HttpResponseMessage message, Action<HttpResponseMessage> action) where TError : class, new()
        {
            ArgumentNullException.ThrowIfNull(message);
            if (message.IsSuccessStatusCode)
                action(message);
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TError"></typeparam>
        /// <param name="message"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> OnSuccessAsync<TError>(this HttpResponseMessage message, Func<HttpResponseMessage, Task> action) where TError : class, new()
        {
            ArgumentNullException.ThrowIfNull(message);
            if (message.IsSuccessStatusCode)
                await action(message);
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task<Stream?> AsStreamAsync(this HttpResponseMessage message)
        {
            if (message is not null && message.Content is not null)
                return await message.Content.ReadAsStreamAsync();
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Stream? AsStream(this HttpResponseMessage message)
        {
            if (message is not null && message.Content is not null)
                return message.Content.ReadAsStream();
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="headerKey"></param>
        /// <returns></returns>
        public static IEnumerable<string> HeaderValues(this HttpResponseMessage message, string headerKey, 
                                                       StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            ArgumentNullException.ThrowIfNull(message);
            return message.Headers.Where(h => h.Key.Equals(headerKey, stringComparison))
                                  .SelectMany(h => h.Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="headerKey"></param>
        /// <returns></returns>
        public static bool HasHeader(this HttpResponseMessage message, string headerKey, 
                                     StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            ArgumentNullException.ThrowIfNull(message);
            return message.Headers.Any(h => h.Key.Equals(headerKey, stringComparison));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="headerKey"></param>
        /// <param name="headerValue"></param>
        /// <param name="keyStringComparison"></param>
        /// <param name="valueStringComparison"></param>
        /// <returns></returns>
        public static bool HasHeaderValue(this HttpResponseMessage message, string headerKey, string headerValue, 
                                          StringComparison keyStringComparison = StringComparison.OrdinalIgnoreCase,
                                          StringComparison valueStringComparison = StringComparison.OrdinalIgnoreCase)
        {
            ArgumentNullException.ThrowIfNull(message);
            return message.Headers.Where(h => h.Key.Equals(headerKey, keyStringComparison))
                                  .Any(h => h.Value.Any(v => headerValue.Equals(v, valueStringComparison)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool IsContentType(this HttpResponseMessage message, string contentType)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (string.IsNullOrEmpty(contentType))
                throw new ArgumentException("Content type is required.", nameof(contentType));
            return message.Content is not null && contentType.Equals(message.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="message"></param>
        /// <param name="jsonSettings"></param>
        /// <returns></returns>
        public static async Task<List<TEntity>> AsJsonEntitiesAsync<TEntity>(this HttpResponseMessage message, JsonSerializerOptions? jsonSettings = null) where TEntity : class, new()
        {
            if (message is not null && message.Content is not null)
            {
                return await message.AsEntityAsync(async (message) =>
                {
                    return JsonSerializer.Deserialize<List<TEntity>>(await message.Content.ReadAsStringAsync(), jsonSettings ?? JsonSerializerOptions.Default);
                }) ?? [];
            }
            return [];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="message"></param>
        /// <param name="jsonSettings"></param>
        /// <returns></returns>
        public static List<TEntity> AsJsonEntities<TEntity>(this HttpResponseMessage message, JsonSerializerOptions? jsonSettings = null) where TEntity : class, new()
        {
            if (message is not null && message.Content is not null)
            {
                return message.AsEntity((message) =>
                {
                    return JsonSerializer.Deserialize<List<TEntity>>(message.AsString() ?? string.Empty, jsonSettings ?? JsonSerializerOptions.Default);
                }) ?? [];
            }
            return [];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="matchTag"></param>
        /// <param name="weakTag"></param>
        /// <returns></returns>
        public static HttpRequestMessage IfNoneMatch(this HttpRequestMessage message, string matchTag, bool weakTag = true)
        {
            ArgumentNullException.ThrowIfNull(message);
            message.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(matchTag, weakTag));
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="matchTag"></param>
        /// <param name="weakTag"></param>
        /// <returns></returns>
        public static HttpRequestMessage IfMatch(this HttpRequestMessage message, string matchTag, bool weakTag = true)
        {
            ArgumentNullException.ThrowIfNull(message);
            message.Headers.IfMatch.Add(new EntityTagHeaderValue(matchTag, weakTag));
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ifModifiedSince"></param>
        /// <returns></returns>
        public static HttpRequestMessage IfModifiedSince(this HttpRequestMessage message, DateTimeOffset ifModifiedSince)
        {
            ArgumentNullException.ThrowIfNull(message);
            message.Headers.IfModifiedSince = ifModifiedSince;
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ifUnmodifiedSince"></param>
        /// <returns></returns>
        public static HttpRequestMessage IfUnmodifiedSince(this HttpRequestMessage message, DateTimeOffset ifUnmodifiedSince)
        {
            ArgumentNullException.ThrowIfNull(message);
            message.Headers.IfUnmodifiedSince = ifUnmodifiedSince;
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="message"></param>
        /// <param name="entity"></param>
        /// <param name="jsonSettings"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static HttpRequestMessage WithJsonEntity<TEntity>(this HttpRequestMessage message, TEntity entity, JsonSerializerOptions? jsonSettings = null)
        {
            ArgumentNullException.ThrowIfNull(message);

            var jsonContent = JsonSerializer.Serialize(entity, jsonSettings ?? JsonSerializerOptions.Default);
            if (!string.IsNullOrEmpty(jsonContent))
            {
                message.Content = new StringContent(jsonContent);
                message.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
            }

            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="message"></param>
        /// <param name="entity"></param>
        /// <param name="jsonSettings"></param>
        /// <returns></returns>
        public static HttpRequestMessage WithHeader(this HttpRequestMessage message, string headerKey, string headerValue)
        {
            ArgumentNullException.ThrowIfNull(message);
            message.Headers.Add(headerKey, headerValue);
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="headerKey"></param>
        /// <param name="headerValues"></param>
        /// <returns></returns>
        public static HttpRequestMessage WithHeader(this HttpRequestMessage message, string headerKey, IEnumerable<string> headerValues)
        {
            ArgumentNullException.ThrowIfNull(message);
            message.Headers.Add(headerKey, headerValues);
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="message"></param>
        /// <param name="entity"></param>
        /// <param name="jsonSettings"></param>
        /// <returns></returns>
        public static HttpRequestMessage WithJsonEntities<TEntity>(this HttpRequestMessage message, IEnumerable<TEntity> entity, JsonSerializerOptions? jsonSettings = null)
        {
            ArgumentNullException.ThrowIfNull(message);

            var jsonContent = JsonSerializer.Serialize(entity, jsonSettings ?? JsonSerializerOptions.Default);
            if (!string.IsNullOrEmpty(jsonContent))
            {
                message.Content = new StringContent(jsonContent);
                message.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
            }
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static string GuessContentTypeFromFileExtension(this FileInfo fileInfo)
        {
            var extension = fileInfo.Extension.ToLower().TrimStart('.');
            return extension switch
            {
                "pdf" => MediaTypeNames.Application.Pdf,
                "zip" => MediaTypeNames.Application.Zip,
                "png" => MediaTypeNames.Image.Png,
                "jpg" or "jpeg" or "jpe" => MediaTypeNames.Image.Jpeg,
                "bmp" => MediaTypeNames.Image.Bmp,
                "tif" or "tiff" => MediaTypeNames.Image.Tiff,
                "txt" or "text" or "asc" => MediaTypeNames.Text.Plain,
                "csv" => MediaTypeNames.Text.Csv,
                "html" or "htm" => MediaTypeNames.Text.Html,
                "xml" => MediaTypeNames.Text.Xml,
                "json" => MediaTypeNames.Application.Json,
                "gif" => MediaTypeNames.Image.Gif,
                "svg" => MediaTypeNames.Image.Svg,
                "css" => MediaTypeNames.Text.Css,
                "rtf" => MediaTypeNames.Text.RichText,
                "md" or "markdown" => MediaTypeNames.Text.Markdown,
                "woff2" => MediaTypeNames.Font.Woff2,
                "woff" => MediaTypeNames.Font.Woff,
                "yaml" or "yml" => "application/yaml",
                "webp" => MediaTypeNames.Image.Webp,
                "ico" => MediaTypeNames.Image.Icon,
                "dtd" => MediaTypeNames.Application.XmlDtd,
                "js" => MediaTypeNames.Text.JavaScript,
                "ttf" => MediaTypeNames.Font.Ttf,
                _ => MediaTypeNames.Application.Octet,
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="filePath"></param>
        /// <param name="fileKey"></param>
        /// <param name="fileContentType"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileUploadPreReqException"></exception>
        public static HttpRequestMessage WithMultipartFormFile(this HttpRequestMessage message, string filePath, string fileKey, string? fileContentType = null)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (!Path.IsPathRooted(filePath))
                filePath = Path.GetFullPath(filePath);

            var fi = new FileInfo(filePath);
            if (!fi.Exists)
                throw new FileNotFoundException($"File {filePath} cannot be uploaded because it does not exist.", filePath);

            if (fi.Length > int.MaxValue)
                throw new FileUploadPreReqException($"File is too large to upload: {fi.Length} bytes.");

            var bytes = File.ReadAllBytes(filePath);

            if (string.IsNullOrEmpty(fileContentType))
                fileContentType = fi.GuessContentTypeFromFileExtension();

            return message.WithMultipartFormFile(bytes, fileContentType, fi.Name, fileKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="fileContents"></param>
        /// <param name="fileContentType"></param>
        /// <param name="fileName"></param>
        /// <param name="fileKey"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static HttpRequestMessage WithMultipartFormFile(this HttpRequestMessage message, byte[] fileContents, string fileContentType, string fileName, string fileKey)
        {
            ArgumentNullException.ThrowIfNull(message);

            var byteArrayContent = new ByteArrayContent(fileContents, 0, fileContents.Length);

            if (!string.IsNullOrEmpty(fileContentType))
                byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue(fileContentType);

            if (message.Content is null)
            {
                message.Content = new MultipartFormDataContent
                {
                    { byteArrayContent, fileKey, fileName },
                };
            }
            else if (message.Content is MultipartFormDataContent multiPart)
            {
                multiPart.Add(byteArrayContent, fileKey, fileName);
            }
            else if (message.Content is FormUrlEncodedContent formContent)
            {
                message.Content = new MultipartFormDataContent
                {
                    { byteArrayContent, fileKey, fileName },
                    { formContent }
                };
            }
            else
                throw new InvalidOperationException("Cannot append multi-part form file upload to existing content.");

            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="queryParameters"></param>
        /// <returns></returns>
        public static HttpRequestMessage WithQueryString(this HttpRequestMessage message, IDictionary<string, string> queryParameters)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (!queryParameters.Any())
                return message;

            if (message.RequestUri == null)
                throw new ArgumentException("Cannot build query parameter string: no request URI was found in the request object.", nameof(message));

            var uriBuilder = new UriBuilder(message.RequestUri)
            {
                Query = string.Join("&", queryParameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))
            };

            message.RequestUri = uriBuilder.Uri;
            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static HttpRequestMessage WithFormValues(this HttpRequestMessage message, IDictionary<string, string> values)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (message.Content is null)
            {
                message.Content = new FormUrlEncodedContent(values);
                if (message.Content.Headers.ContentType == null)
                    message.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.FormUrlEncoded);
            }
            else if (message.Content is MultipartContent multiPart)
            {
                var formContent = new FormUrlEncodedContent(values); ;
                formContent.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.FormUrlEncoded);
                multiPart.Add(formContent);
            }
            else
                throw new InvalidOperationException("Cannot append URL-encoded form values to existing content: incompatible content type.");

            return message;
        }
    }
}
