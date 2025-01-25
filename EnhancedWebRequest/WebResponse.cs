using System.Net;

namespace Llc.GoodConsulting.Web.EnhancedWebRequest
{
    /// <summary>
    /// Represents a response received from a remote HTTP endpoint.
    /// </summary>
    /// <remarks>
    /// Creates a new instance of the <see cref="WebResponse"/> class.
    /// </remarks>
    /// <param name="response"><seealso cref="HttpResponseMessage"/> from the remote endpoint.</param>
    public class WebResponse(HttpResponseMessage response)
    {
        /// <summary>
        /// The HTTP status code for the response.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; } = response.StatusCode;

        /// <summary>
        /// <seealso cref="HttpResponseMessage"/> from the remote endpoint.
        /// </summary>
        protected HttpResponseMessage Response { get; set; } = response;

        /// <summary>
        /// Whether or not the response was successful from the remote endpoint's perspective.
        /// </summary>
        public bool IsSuccess { get; private set; } = response.IsSuccessStatusCode;

        /// <summary>
        /// 
        /// </summary>
        public List<Exception> Exceptions { get; set; } = [];

        /// <summary>
        /// Whether or not the response has content in its body.
        /// </summary>
        public bool HasContent
        {
            get
            {
                return Response != null && Response.Content != null &&
                       Response.StatusCode != HttpStatusCode.NoContent;
            }
        }

        /// <summary>
        /// Retrieves the content of the response as a byte array.
        /// </summary>
        /// <returns>Byte array of the response content.</returns>
        public async Task<byte[]> GetBytes()
        {
            if (HasContent)
                return await Response.Content.ReadAsByteArrayAsync();
            return [];
        }

        /// <summary>
        /// Retrieves the content of the response as a string.
        /// </summary>
        /// <returns>String content from the response.</returns>
        public async Task<string> GetString()
        {
            if (HasContent)
                return await Response.Content.ReadAsStringAsync();
            return string.Empty;
        }

        /// <summary>
        /// Retrieves the content of the response as a stream.
        /// </summary>
        /// <returns><see cref="Stream"/> containing the response content.</returns>
        public async Task<Stream> GetStream()
        {
            if (HasContent)
                return await Response.Content.ReadAsStreamAsync();
            return Stream.Null;
        }

        /// <summary>
        /// Processes the entity from the HTTP response and returns it as type <typeparamref name="TEntity"/>.
        /// </summary>
        /// <typeparam name="TEntity">Type of the entity to retrieve from the HTTP response.</typeparam>
        /// <param name="entityProcessor">Method to process the HTTP response and retrieve the entity.</param>
        /// <returns><seealso cref="Task{TEntity}"/></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async virtual Task<TEntity?> GetEntity<TEntity>(Func<HttpResponseMessage, TEntity> entityProcessor)
        {
            ArgumentNullException.ThrowIfNull(entityProcessor);

            if (HasContent)
                return await Task.Run(() => entityProcessor.Invoke(Response));

            return default;
        }

        /// <summary>
        /// Retrieves all values for the specified header.
        /// </summary>
        /// <param name="key">Name of the header to retrieve (e.g., Link).</param>
        /// <returns>String values of the specified header.</returns>
        public IEnumerable<string> GetHeaderValues(string key)
        {
            if (Response.Headers.TryGetValues(key, out var value))
                return value;
            return [];
        }

        /// <summary>
        /// Retrieves the first value for the specified header.
        /// </summary>
        /// <param name="key">Name of the header to retrieve (e.g., Link).</param>
        /// <returns>First string value of the specified header.</returns>
        public string? GetHeaderValue(string key)
        {
            return GetHeaderValues(key).FirstOrDefault();
        }
    }
}
