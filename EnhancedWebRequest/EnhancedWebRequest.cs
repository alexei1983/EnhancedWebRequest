using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Llc.GoodConsulting.Web.EnhancedWebRequest
{
    /// <summary>
    /// A utility class for interacting with HTTP web services.
    /// </summary>
    public class EnhancedWebRequest : IDisposable
    {
        /// <summary>
        /// Event raised when a response is received from a remote HTTP endpoint.
        /// </summary>
        public event EventHandler<ResponseReceivedEventArgs>? ResponseReceived;

        /// <summary>
        /// Event raised when a request is sent to a remote HTTP endpoint.
        /// </summary>
        public event EventHandler<RequestSentEventArgs>? RequestSent;

        /// <summary>
        /// Event raised when a non-success status code is returned from a remote HTTP endpoint.
        /// </summary>
        public event EventHandler<ErrorStatusCodeEventArgs>? ErrorStatusCode;

        /// <summary>
        /// Event raised when a remote HTTP endpoints indicates that content has not been modified.
        /// </summary>
        public event EventHandler<NotModifiedEventArgs>? NotModified;

        /// <summary>
        /// Base URL for the web request.
        /// </summary>
        public string BaseUrl { get; internal set; }

        /// <summary>
        /// HTTP Authorization header name.
        /// </summary>
        const string AUTH_HEADER = "Authorization";

        /// <summary>
        /// HTTP basic authentication type.
        /// </summary>
        const string AUTH_BASIC = "Basic";

        /// <summary>
        /// HTTP bearer authentication type.
        /// </summary>
        const string AUTH_BEARER = "Bearer";

        ///// <summary>
        ///// HTTP POST method.
        ///// </summary>
        //const string METHOD_POST = "POST";

        ///// <summary>
        ///// HTTP GET method.
        ///// </summary>
        //const string METHOD_GET = "GET";

        ///// <summary>
        ///// HTTP PUT method.
        ///// </summary>
        //const string METHOD_PUT = "PUT";

        ///// <summary>
        ///// HTTP PATCH method.
        ///// </summary>
        //const string METHOD_PATCH = "PATCH";

        ///// <summary>
        ///// HTTP DELETE method.
        ///// </summary>
        //const string METHOD_DELETE = "DELETE";

        ///// <summary>
        ///// HTTP OPTIONS method.
        ///// </summary>
        //const string METHOD_OPTIONS = "OPTIONS";

        ///// <summary>
        ///// HTTP HEAD method.
        ///// </summary>
        //const string METHOD_HEAD = "HEAD";

        /// <summary>
        /// 
        /// </summary>
        readonly HttpClient internalClient;
        readonly JsonSerializerOptions jsonOptions = JsonSerializerOptions.Default;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpClient"></param>
        public EnhancedWebRequest(HttpClient httpClient)
        {
            internalClient = httpClient;
            BaseUrl = httpClient.BaseAddress?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientHandler"></param>
        public EnhancedWebRequest(HttpClientHandler clientHandler)
        {
            internalClient = GetClient(null, clientHandler);
            BaseUrl = internalClient.BaseAddress?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUrl"></param>
        public EnhancedWebRequest(string baseUrl, EnhancedWebRequestOptions options)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
                throw new ArgumentException($"Base URI must be absolute: {baseUrl} is not an absolute URI.", nameof(baseUrl));

            BaseUrl = baseUrl;
            internalClient = GetClient(options, null);
            jsonOptions = options.JsonSerializerSettings;
        }

        /// <summary>
        /// Creates an appropriate <seealso cref="HttpClient"/> object to use in the web request.
        /// </summary>
        /// <param name="url">Base URL for the client.</param>
        /// <returns><seealso cref="HttpClient"/> object.</returns>
        private HttpClient GetClient(EnhancedWebRequestOptions? options = null, HttpClientHandler? clientHandler = null)
        {
            if (clientHandler != null)
                return new HttpClient(clientHandler);

            options ??= new EnhancedWebRequestOptions();

            var handler = new HttpClientHandler() { UseDefaultCredentials = false };

            if (options.SkipCertificateValidation)
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };
            }

            var client = new HttpClient(handler);

            try
            {
                client.BaseAddress = new Uri(BaseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
                if (options.Accept != null)
                {
                    foreach (var type in options.Accept)
                    {
                        try
                        {
                            var typeHeader = new MediaTypeWithQualityHeaderValue(type);
                            if (!client.DefaultRequestHeaders.Accept.Contains(typeHeader))
                                client.DefaultRequestHeaders.Accept.Add(typeHeader);
                        }
                        catch (Exception acceptEx)
                        {
                            Debug.WriteLine($"Error adding value '{type}' to Accept header: {acceptEx.Message}");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(options.UserAgent))
                {
                    try
                    {
                        var header = new ProductHeaderValue(options.UserAgent, options.UserAgentVersion?.ToString());
                        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(header));
                    }
                    catch (Exception userAgentEx)
                    {
                        Debug.WriteLine($"Error setting user agent '{options.UserAgent}': {userAgentEx.Message}");
                    }
                }

                if (options.AuthorizationType != AuthorizationType.None)
                {
                    if (options.AuthorizationType == AuthorizationType.Basic &&
                        !string.IsNullOrEmpty(options.Username) &&
                        !string.IsNullOrEmpty(options.Password))
                        client.DefaultRequestHeaders.Add(AUTH_HEADER,
                                                         $"{AUTH_BASIC} {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"))}");
                    else if (options.AuthorizationType == AuthorizationType.BearerToken && !string.IsNullOrEmpty(options.BearerToken))
                        client.DefaultRequestHeaders.Add(AUTH_HEADER,
                                                         $"{AUTH_BEARER} {options.BearerToken}");
                    else if (options.AuthorizationType == AuthorizationType.Custom && !string.IsNullOrEmpty(options.CustomAuthorizationType) && !string.IsNullOrEmpty(options.CustomAuthorizationValue))
                        client.DefaultRequestHeaders.Add(AUTH_HEADER,
                                                         $"{options.CustomAuthorizationType} {options.CustomAuthorizationValue}");
                }
            }
            catch (Exception)
            {
                throw;
            }
            return client;
        }

        #region Methods handling JSON entities

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<List<TEntity>> GetJsonEntities<TEntity>(string? url = null) where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetUrl(url));
            var response = await Execute(request);
            return await response.AsJsonEntitiesAsync<TEntity>(jsonOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<TEntity?> GetJsonEntity<TEntity>(string? url = null) where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetUrl(url));
            var response = await Execute(request);
            return await response.AsJsonEntityAsync<TEntity>(jsonOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<TEntity?> PutJsonEntity<TEntity>(TEntity entity, string? url = null) where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url)).WithJsonEntity(entity);
            var response = await Execute(request);
            return await response.AsJsonEntityAsync<TEntity>(jsonOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<TEntity?> PostJsonEntity<TEntity>(TEntity entity, string? url = null) where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url)).WithJsonEntity(entity);
            var response = await Execute(request);
            return await response.AsJsonEntityAsync<TEntity>(jsonOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="entity"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<TResponse?> PostJsonEntity<TRequest, TResponse>(TRequest entity, string? url = null) where TRequest : class, new()
            where TResponse : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url)).WithJsonEntity(entity);
            var response = await Execute(request);
            return await response.AsJsonEntityAsync<TResponse>(jsonOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="entity"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<TResponse?> PutJsonEntity<TRequest, TResponse>(TRequest entity, string? url = null) where TRequest : class, new()
           where TResponse : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url)).WithJsonEntity(entity);
            var response = await Execute(request);
            return await response.AsJsonEntityAsync<TResponse>(jsonOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="entity"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<TResponse?> PatchJsonEntity<TRequest, TResponse>(TRequest entity, string? url = null) where TRequest : class, new()
           where TResponse : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, GetUrl(url)).WithJsonEntity(entity);
            var response = await Execute(request);
            return await response.AsJsonEntityAsync<TResponse>(jsonOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<TEntity?> PatchJsonEntity<TEntity>(TEntity entity, string? url) where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, GetUrl(url)).WithJsonEntity(entity);
            var response = await Execute(request);
            return await response.AsJsonEntityAsync<TEntity>(jsonOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="unmodifiedSince"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<TEntity?> PutJsonEntityIfUnmodifiedSince<TEntity>(TEntity entity, DateTimeOffset unmodifiedSince, string? url = null)
            where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url))
                              .WithJsonEntity(entity)
                              .IfUnmodifiedSince(unmodifiedSince);
            var response = await Execute(request);
            response.OnStatus((_) =>
            {
                throw new HttpException($"Precondition failed: Remote entity has been modified since {unmodifiedSince}.", response);
            }, HttpStatusCode.PreconditionFailed);
            return await response.AsJsonEntityAsync<TEntity>(jsonOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="eTag"></param>
        /// <param name="weakTag"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<TEntity?> PostJsonEntityIfNoneMatch<TEntity>(TEntity entity, string eTag, bool weakTag = true, string? url = null)
            where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url)).WithJsonEntity(entity)
                                                                              .IfNoneMatch(eTag, weakTag);
            var response = await Execute(request);
            response.OnStatus((_) =>
            {
                throw new HttpException($"Precondition failed: Match found on remote server for tag {eTag}.", response);
            }, HttpStatusCode.PreconditionFailed);
            return await response.AsJsonEntityAsync<TEntity>(jsonOptions);
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<List<string>> GetAllowedMethods(string? url = null)
        {
            var response = await Options(url);
            response.OnStatus((_) =>
            {
                throw new HttpException("Cannot retrieve allowed HTTP methods: OPTIONS is not allowed.", response);
            },
                HttpStatusCode.MethodNotAllowed);
            return [.. response.HeaderValues("Allow", StringComparison.OrdinalIgnoreCase)];
        }

        #region Methods returning HttpResponseMessage

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PostForm(IDictionary<string, string> values, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url)).WithFormValues(values);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="ifModifiedSince"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutContentIfModifiedSince(HttpContent content, DateTimeOffset ifModifiedSince, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url))
            {
                Content = content
            }.IfModifiedSince(ifModifiedSince);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="ifUnmodifiedSince"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutContentIfUnmodifiedSince(HttpContent content, DateTimeOffset ifUnmodifiedSince, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url))
            {
                Content = content
            }.IfUnmodifiedSince(ifUnmodifiedSince);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="matchTag"></param>
        /// <param name="weakTag"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutContentIfNoneMatch(HttpContent content, string matchTag, bool weakTag = true, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url))
            {
                Content = content
            }.IfNoneMatch(matchTag, weakTag);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="matchTag"></param>
        /// <param name="weakTag"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutContentIfMatch(HttpContent content, string matchTag, bool weakTag = true, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url))
            {
                Content = content
            }.IfMatch(matchTag, weakTag);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="eTag"></param>
        /// <param name="weakTag"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutJsonEntityIfMatch<TEntity>(TEntity entity, string eTag, bool weakTag = true, string? url = null)
            where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url)).WithJsonEntity(entity)
                                                                             .IfMatch(eTag, weakTag);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> Delete(string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, GetUrl(url));
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> Get(string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetUrl(url));
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> Options(string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, GetUrl(url));
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queryParameters"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> Get(IDictionary<string, string> queryParameters, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetUrl(url)).WithQueryString(queryParameters);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ifModifiedSince"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetIfModifiedSince(DateTimeOffset ifModifiedSince, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetUrl(url)).IfModifiedSince(ifModifiedSince);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matchTag"></param>
        /// <param name="weakTag"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetIfNoneMatch(string matchTag, bool weakTag = true, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetUrl(url)).IfNoneMatch(matchTag, weakTag);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> DeleteContent(HttpContent content, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, GetUrl(url))
            {
                Content = content
            };
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PostContent(HttpContent content, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url))
            {
                Content = content
            };
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutContent(HttpContent content, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url))
            {
                Content = content
            };
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PatchContent(HttpContent content, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, GetUrl(url))
            {
                Content = content
            };
            return await Execute(request);
        }

        #endregion

        #region Multipart Form File Upload

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="fileContents"></param>
        /// <param name="fileContentType"></param>
        /// <param name="filename"></param>
        /// <param name="fileKey"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<TResponse?> PostMultipartFormFileWithJsonResponse<TResponse>(byte[] fileContents, string fileContentType, string filename, string fileKey, string? url = null)
            where TResponse : class, new()
        {
            var request = GetRequest(HttpMethod.Post, url)
                          .WithMultipartFormFile(fileContents, fileContentType, filename, fileKey);
            var response = await Execute(request);
            return await response.AsJsonEntityAsync<TResponse>(jsonOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileContents"></param>
        /// <param name="fileContentType"></param>
        /// <param name="filename"></param>
        /// <param name="fileKey"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PostMultipartFormFile(byte[] fileContents, string fileContentType, string filename, string fileKey, string? url = null)
        {
            var request = GetRequest(HttpMethod.Post, url)
                          .WithMultipartFormFile(fileContents, fileContentType, filename, fileKey);
            return await Execute(request);
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        async Task<HttpResponseMessage> Execute(HttpRequestMessage request)
        {
            OnRequestSent(request);
            var response = await internalClient.SendAsync(request);
            RaiseResponseEvents(response);
            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public HttpRequestMessage GetRequest(HttpMethod httpMethod, string? url = null)
        {
            return new HttpRequestMessage(httpMethod, GetUrl(url));
        }

        ///// <summary>
        ///// Performs a POST operation using the specified key/value pairs as URL-encoded data.
        ///// </summary>
        ///// <typeparam name="TResponse">Type of the entity received from the remote endpoint on response.</typeparam>
        ///// <param name="url">URL where the POST operation will be sent.</param>
        ///// <param name="values">Key/value pairs to include in the POST operation.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntity<TResponse>?> MakeFormPost<TResponse>(string url, IDictionary<string, string> values)
        //    where TResponse : class, new()
        //{
        //    var response = await InternalMakeFormPost(url, values);
        //    return await GetWebResponseWithEntity<TResponse>(response);
        //}

        ///// <summary>
        ///// Performs a POST operation using the specified key/value pairs as URL-encoded data.
        ///// </summary>
        ///// <typeparam name="TResponse">Type of the entity received from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity received from the remote endpoint on error.</typeparam>
        ///// <param name="url">URL where the POST operation will be sent.</param>
        ///// <param name="values">Key/value pairs to include in the POST operation.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TResponse, TError>?> MakeFormPost<TResponse, TError>(string url, IDictionary<string, string> values)
        //    where TResponse : class, new()
        //    where TError : class, new()
        //{
        //    var response = await InternalMakeFormPost(url, values);
        //    return await GetWebResponseWithEntityOrError<TResponse, TError>(response);
        //}

        ///// <summary>
        ///// Performs a POST operation using the specified key/value pairs as URL-encoded data.
        ///// </summary>
        ///// <param name="url">URL where the POST operation will be sent.</param>
        ///// <param name="values">Key/value pairs to include in the POST operation.</param>
        ///// <returns></returns>
        //public async Task<WebResponse?> MakeFormPost(string url, IDictionary<string, string> values)
        //{
        //    var response = await InternalMakeFormPost(url, values);
        //    return GetWebResponse(response);
        //}

        ///// <summary>
        ///// Uploads a file to a remote HTTP endpoint as a multi-part form entity with optional key/value pairs.
        ///// </summary>
        ///// <param name="url">URL where the request will be sent.</param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="fileContents">Byte array containing the contents of the file to upload.</param>
        ///// <param name="fileParameterName">Name of the field for the uploaded file.</param>
        ///// <param name="filename">The name of the file.</param>
        ///// <param name="fileContentType">The content type of the file (e.g., image/jpeg).</param>
        ///// <param name="additionalFormValues">Optional key/value pairs to include in the request.</param>
        ///// <returns></returns>
        //public async Task<WebResponse?> UploadFile(string url,
        //                                           string method,
        //                                           byte[] fileContents,
        //                                           string fileParameterName,
        //                                           string filename,
        //                                           string fileContentType,
        //                                           IDictionary<string, string?>? additionalFormValues = null)
        //{
        //    var response = await InternalUploadFile(url, method, fileContents, fileParameterName, filename, fileContentType, additionalFormValues);
        //    return GetWebResponse(response);

        //}

        ///// <summary>
        ///// Uploads a file to a remote HTTP endpoint as a multi-part form entity with optional key/value pairs.
        ///// </summary>
        ///// <typeparam name="TEntity"></typeparam>
        ///// <param name="url">URL where the request will be sent.</param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="fileContents">Byte array containing the contents of the file to upload.</param>
        ///// <param name="fileParameterName">Name of the field for the uploaded file.</param>
        ///// <param name="filename">The name of the file.</param>
        ///// <param name="fileContentType">The content type of the file (e.g., image/jpeg).</param>
        ///// <param name="additionalFormValues">Optional key/value pairs to include in the request.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntity<TEntity>?> UploadFile<TEntity>(string url,
        //                                                                       string method,
        //                                                                       byte[] fileContents,
        //                                                                       string fileParameterName,
        //                                                                       string filename,
        //                                                                       string fileContentType,
        //                                                                       IDictionary<string, string?>? additionalFormValues = null)
        //    where TEntity : class, new()
        //{
        //    var response = await InternalUploadFile(url, method, fileContents, fileParameterName, filename, fileContentType, additionalFormValues);
        //    return await GetWebResponseWithEntity<TEntity>(response);

        //}

        ///// <summary>
        ///// Uploads a file to a remote HTTP endpoint as a multi-part form entity with optional key/value pairs.
        ///// </summary>
        ///// <typeparam name="TEntity"></typeparam>
        ///// <typeparam name="TError"></typeparam>
        ///// <param name="url">URL where the request will be sent.</param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="fileContents">Byte array containing the contents of the file to upload.</param>
        ///// <param name="fileParameterName">Name of the field for the uploaded file.</param>
        ///// <param name="filename">The name of the file.</param>
        ///// <param name="fileContentType">The content type of the file (e.g., image/jpeg).</param>
        ///// <param name="additionalFormValues">Optional key/value pairs to include in the request.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> UploadFile<TEntity, TError>(string url,
        //                                                                                               string method,
        //                                                                                               byte[] fileContents,
        //                                                                                               string fileParameterName,
        //                                                                                               string filename,
        //                                                                                               string fileContentType,
        //                                                                                               IDictionary<string, string?>? additionalFormValues = null)
        //    where TEntity : class, new()
        //    where TError : class, new()
        //{
        //    var response = await InternalUploadFile(url, method, fileContents, fileParameterName, filename, fileContentType, additionalFormValues);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);

        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TEntity"></typeparam>
        ///// <typeparam name="TRequest"></typeparam>
        ///// <typeparam name="TError"></typeparam>
        ///// <param name="url"></param>
        ///// <param name="method"></param>
        ///// <param name="fileContents"></param>
        ///// <param name="fileParameterName"></param>
        ///// <param name="filename"></param>
        ///// <param name="fileContentType"></param>
        ///// <param name="requestEntity"></param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> UploadFile<TEntity, TRequest, TError>(string url,
        //                                                                                                        string method,
        //                                                                                                        byte[] fileContents,
        //                                                                                                        string fileParameterName,
        //                                                                                                        string filename,
        //                                                                                                        string fileContentType,
        //                                                                                                        TRequest? requestEntity)
        //    where TEntity : class, new()
        //    where TError : class, new()
        //    where TRequest : class, new()
        //{
        //    var response = await InternalUploadFileWithJsonEntity(url, method, fileContents, fileParameterName, filename, fileContentType, requestEntity);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);

        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="url"></param>
        ///// <param name="values"></param>
        ///// <returns></returns>
        //private async Task<HttpResponseMessage> InternalMakeFormPost(string url, IDictionary<string, string> values)
        //{
        //    var request = GetRequest(HttpMethod.Post, GetUrl(url), new FormUrlEncodedContent(values));
        //    if (request.Content != null)
        //        request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.FormUrlEncoded); 
        //    return await InternalMakeRequest(internalClient, request);
        //}

        ///// <summary>
        ///// Checks the specified file path to ensure it exists, reads the contents into a byte array,
        ///// and returns the content and name as a tuple.
        ///// </summary>
        ///// <param name="path">Path to the file.</param>
        ///// <returns>Tuple containing the file contents as a byte array and the file name.</returns>
        ///// <exception cref="FileNotFoundException">If the file does not exist.</exception>
        ///// <exception cref="FileUploadPreReqException">If the file is larger than the maximum value of an integer.</exception>
        //private static (byte[], string, string) GetFileForUpload(string path)
        //{
        //    var fi = new FileInfo(path);

        //    if (!fi.Exists)
        //        throw new FileNotFoundException($"File {path} cannot be uploaded because it does not exist.", path);

        //    if (fi.Length > int.MaxValue)
        //        throw new FileUploadPreReqException($"File is too large to upload: {fi.Length} bytes.");

        //    return (File.ReadAllBytes(path), fi.Name, fi.GuessContentTypeFromFileExtension());
        //}

        ///// <summary>
        ///// Uploads a file to a remote HTTP endpoint as a multi-part form entity with optional key/value pairs.
        ///// </summary>
        ///// <typeparam name="TResponse"></typeparam>
        ///// <typeparam name="TError"></typeparam>
        ///// <param name="url">URL where the request will be sent.</param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="filePath">Full path to the file to upload.</param>
        ///// <param name="parameterName">Name of the field for the uploaded file.</param>
        ///// <param name="additionalFormValues">Optional key/value pairs to include in the request.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TResponse, TError>?> UploadFile<TResponse, TError>(string url,
        //                                                                                                  string method,
        //                                                                                                  string filePath,
        //                                                                                                  string parameterName,
        //                                                                                                  IDictionary<string, string?>? additionalFormValues = null)
        //    where TResponse : class, new()
        //    where TError : class, new()
        //{
        //    var fileContents = GetFileForUpload(filePath);
        //    var response = await InternalUploadFile(url, method, fileContents.Item1, parameterName, fileContents.Item2, fileContents.Item3, additionalFormValues);
        //    return await GetWebResponseWithEntityOrError<TResponse, TError>(response);
        //}

        ///// <summary>
        ///// Uploads a file to a remote HTTP endpoint as a multi-part form entity with optional key/value pairs.
        ///// </summary>
        ///// <typeparam name="TResponse"></typeparam>
        ///// <param name="url">URL where the request will be sent.</param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="filePath">Full path to the file to upload.</param>
        ///// <param name="parameterName">Name of the field for the uploaded file.</param>
        ///// <param name="additionalFormValues">Optional key/value pairs to include in the request.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntity<TResponse>?> UploadFile<TResponse>(string url,
        //                                                                           string method,
        //                                                                           string filePath,
        //                                                                           string parameterName,
        //                                                                           IDictionary<string, string?>? additionalFormValues = null)
        //    where TResponse : class, new()
        //{
        //    var fileContents = GetFileForUpload(filePath);
        //    var response = await InternalUploadFile(url, method, fileContents.Item1, parameterName, fileContents.Item2, fileContents.Item3, additionalFormValues);
        //    return await GetWebResponseWithEntity<TResponse>(response);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TRequest"></typeparam>
        ///// <typeparam name="TResponse"></typeparam>
        ///// <param name="url"></param>
        ///// <param name="method"></param>
        ///// <param name="filePath"></param>
        ///// <param name="parameterName"></param>
        ///// <param name="requestEntity"></param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntity<TResponse>?> UploadFile<TRequest, TResponse>(string url,
        //                                                                                     string method,
        //                                                                                     string filePath,
        //                                                                                     string parameterName,
        //                                                                                     TRequest? requestEntity)
        //    where TResponse : class, new()
        //    where TRequest : class, new()
        //{
        //    var fileContents = GetFileForUpload(filePath);
        //    var response = await InternalUploadFileWithJsonEntity(url, method, fileContents.Item1, parameterName, fileContents.Item2, fileContents.Item3, requestEntity);
        //    return await GetWebResponseWithEntity<TResponse>(response);
        //}

        ///// <summary>
        ///// Uploads a file to a remote HTTP endpoint as a multi-part form entity with optional key/value pairs.
        ///// </summary>
        ///// <param name="url">URL where the request will be sent.</param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="fileContents">Byte array containing the contents of the file to upload.</param>
        ///// <param name="fileParameterName">Name of the field for the uploaded file.</param>
        ///// <param name="filename">The name of the file.</param>
        ///// <param name="fileContentType">The content type of the file (e.g., image/jpeg).</param>
        ///// <param name="additionalFormValues">Optional key/value pairs to include in the request.</param>
        ///// <returns></returns>
        //private async Task<HttpResponseMessage> InternalUploadFile(string url,
        //                                                           string method,
        //                                                           byte[] fileContents,
        //                                                           string fileParameterName,
        //                                                           string filename,
        //                                                           string fileContentType,
        //                                                           IDictionary<string, string?>? additionalFormValues = null)
        //{

        //    var byteArrayContent = new ByteArrayContent(fileContents, 0, fileContents.Length);

        //    if (!string.IsNullOrEmpty(fileContentType))
        //        byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue(fileContentType);

        //    var form = new MultipartFormDataContent
        //    {
        //        { byteArrayContent, fileParameterName, filename },
        //    };

        //    if (additionalFormValues?.Count > 0)
        //        form.Add(new FormUrlEncodedContent(additionalFormValues));

        //    var request = GetRequest(GetMethod(method), GetUrl(url), form);
        //    return await InternalMakeRequest(internalClient, request);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TEntity"></typeparam>
        ///// <param name="url"></param>
        ///// <param name="method"></param>
        ///// <param name="fileContents"></param>
        ///// <param name="fileParameterName"></param>
        ///// <param name="filename"></param>
        ///// <param name="fileContentType"></param>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //private async Task<HttpResponseMessage> InternalUploadFileWithJsonEntity<TEntity>(string url,
        //                                                                                 string method,
        //                                                                                 byte[] fileContents,
        //                                                                                 string fileParameterName,
        //                                                                                 string filename,
        //                                                                                 string fileContentType,
        //                                                                                 TEntity? entity)
        //    where TEntity : class, new()
        //{

        //    var byteArrayContent = new ByteArrayContent(fileContents, 0, fileContents.Length);

        //    if (!string.IsNullOrEmpty(fileContentType))
        //        byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue(fileContentType);

        //    var form = new MultipartFormDataContent
        //    {
        //        { byteArrayContent, fileParameterName, filename },
        //    };

        //    if (entity != null)
        //        form.Add(entity.ToJsonStringContent());

        //    var request = GetRequest(GetMethod(method), GetUrl(url), form);
        //    return await InternalMakeRequest(internalClient, request);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="uri">URL or URI for the request.</param>
        ///// <param name="content"></param>
        ///// <param name="modifyRequest">Optional method to modify additional properties of the request.</param>
        ///// <returns><seealso cref="HttpRequestMessage"/></returns>
        //private static HttpRequestMessage GetRequest(HttpMethod method,
        //                                              Uri? uri = null,
        //                                              HttpContent? content = null,
        //                                              Func<HttpRequestMessage, HttpRequestMessage>? modifyRequest = null)
        //{
        //    var request = new HttpRequestMessage(method, uri);

        //    if (content != null)
        //        request.Content = content;

        //    if (modifyRequest != null)
        //        request = modifyRequest(request);

        //    return request;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TResponse"></typeparam>
        ///// <param name="response"></param>
        ///// <returns></returns>
        //private async Task<TResponse?> GetResponse<TResponse>(HttpResponseMessage response) where TResponse : class, new()
        //{
        //    if (response is null)
        //        return default;

        //    if (response.Content != null)
        //        return await response.ReadFromJsonAsync<TResponse>(jsonOptions);

        //    return default;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TResponse"></typeparam>
        ///// <param name="response"></param>
        ///// <returns></returns>
        //private TResponse GetResponseEntity<TResponse>(HttpResponseMessage response) where TResponse : class, new()
        //{
        //    return GetResponse<TResponse>(response).Result ?? Activator.CreateInstance<TResponse>();
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TResponse"></typeparam>
        ///// <typeparam name="TError"></typeparam>
        ///// <param name="response"></param>
        ///// <returns></returns>
        //private async Task<WebResponseWithEntityOrError<TResponse, TError>?> GetWebResponseWithEntityOrError<TResponse, TError>(HttpResponseMessage response) where TResponse : class, new()
        //    where TError : class, new()
        //{
        //    if (response is null)
        //        return default;

        //    var entityOrError = new WebResponseWithEntityOrError<TResponse, TError>(response);
        //    await entityOrError.HandleEntityOrError(GetResponseEntity<TResponse>, GetResponseEntity<TError>);
        //    return entityOrError;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TResponse"></typeparam>
        ///// <param name="response"></param>
        ///// <returns></returns>
        //private async Task<WebResponseWithEntity<TResponse>?> GetWebResponseWithEntity<TResponse>(HttpResponseMessage response) where TResponse : class, new()
        //{
        //    if (response is null)
        //        return default;

        //    var entity = new WebResponseWithEntity<TResponse>(response);
        //    await entity.HandleEntity(GetResponseEntity<TResponse>);
        //    return entity;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="response"></param>
        ///// <returns></returns>
        //private static WebResponse? GetWebResponse(HttpResponseMessage response)
        //{
        //    if (response is null)
        //        return default;
        //    return new WebResponse(response);
        //}

        ///// <summary>
        ///// Returns the appropriate <seealso cref="HttpMethod"/> object for the specified HTTP method.
        ///// </summary>
        ///// <param name="method">HTTP method as a string.</param>
        ///// <returns><seealso cref="HttpMethod"/></returns>
        ///// <exception cref="Exception"></exception>
        //private static HttpMethod GetMethod(string method)
        //{
        //    return (method?.ToUpper()) switch
        //    {
        //        METHOD_POST => HttpMethod.Post,
        //        METHOD_PUT => HttpMethod.Put,
        //        METHOD_PATCH => new HttpMethod(METHOD_PATCH),
        //        METHOD_GET => HttpMethod.Get,
        //        METHOD_DELETE => HttpMethod.Delete,
        //        METHOD_HEAD => HttpMethod.Head,
        //        METHOD_OPTIONS => HttpMethod.Options,
        //        _ => throw new Exception("Invalid HTTP method: " + method),
        //    };
        //}

        ///// <summary>
        ///// Makes a conditional HTTP request to a remote endpoint, setting the If-Modified-Since header to the specified date/time.
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity returned from the remote endpoint on error.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="lastModified"><seealso cref="DateTimeOffset"/> value for the If-Modified-Since header.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> MakeRequestIfModifiedSince<TEntity, TError>(string url, string method, DateTimeOffset lastModified) 
        //    where TEntity : class, new()
        //    where TError : class, new()
        //{
        //    var request = GetRequest(GetMethod(method), GetUrl(url), null, (request) => { 
        //        request.Headers.IfModifiedSince = lastModified;
        //        return request;
        //    });

        //    var response = await InternalMakeRequest(internalClient, request);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);
        //}

        ///// <summary>
        ///// Makes a conditional HTTP request to a remote endpoint, setting the If-Unmodified-Since header to the specified date/time.
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity returned from the remote endpoint on error.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="lastModified"><seealso cref="DateTimeOffset"/> value for the If-Unmodified-Since header.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> MakeRequestIfUnmodifiedSince<TEntity, TError>(string url, string method, DateTimeOffset lastModified)
        //    where TEntity : class, new()
        //    where TError : class, new()
        //{
        //    var request = GetRequest(GetMethod(method), GetUrl(url), null, (request) => {
        //        request.Headers.IfUnmodifiedSince = lastModified;
        //        return request;
        //    });

        //    var response = await InternalMakeRequest(internalClient, request);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);
        //}

        ///// <summary>
        ///// Makes a conditional HTTP request to a remote endpoint, setting the If-Modified-Since header to the specified date/time.
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity returned from the remote endpoint on error.</typeparam>
        ///// <typeparam name="TRequest">Type of the entity sent to the remote endpoint in the request body.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="requestEntity"></param>
        ///// <param name="lastModified"><seealso cref="DateTimeOffset"/> value for the If-Modified-Since header.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> MakeJsonEntityRequestIfModifiedSince<TEntity, TError, TRequest>(string url, string method, TRequest requestEntity, DateTimeOffset lastModified)
        //    where TEntity : class, new()
        //    where TError : class, new()
        //    where TRequest : class, new()
        //{
        //    var request = GetRequest(GetMethod(method), GetUrl(url), requestEntity.ToJsonStringContent(jsonOptions), (request) => {
        //        request.Headers.IfModifiedSince = lastModified;
        //        return request;
        //    });

        //    var response = await InternalMakeRequest(internalClient, request);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);
        //}

        ///// <summary>
        ///// Makes a conditional HTTP request to a remote endpoint, setting the If-Unmodified-Since header to the specified date/time.
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity returned from the remote endpoint on error.</typeparam>
        ///// <typeparam name="TRequest">Type of the entity sent to the remote endpoint in the request body.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="requestEntity"></param>
        ///// <param name="lastModified"><seealso cref="DateTimeOffset"/> value for the If-Unmodified-Since header.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> MakeJsonEntityRequestIfUnmodifiedSince<TEntity, TError, TRequest>(string url, string method, TRequest requestEntity, DateTimeOffset lastModified)
        //    where TEntity : class, new()
        //    where TError : class, new()
        //    where TRequest : class, new()
        //{
        //    var request = GetRequest(GetMethod(method), GetUrl(url), requestEntity.ToJsonStringContent(jsonOptions), (request) => {
        //        request.Headers.IfUnmodifiedSince = lastModified;
        //        return request;
        //    });

        //    var response = await InternalMakeRequest(internalClient, request);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);
        //}

        ///// <summary>
        ///// Makes a conditional HTTP request to a remote endpoint with the specified entity body as JSON, setting the If-None-Match header 
        ///// to the specified entity tag.
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity returned from the remote endpoint on error.</typeparam>
        ///// <typeparam name="TRequest">Type of the entity sent to the remote endpoint in the request body.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="requestBody"></param>
        ///// <param name="entityTag"></param>
        ///// <returns></returns>
        ///// <exception cref="HttpException"></exception>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> MakeJsonEntityRequestIfNoneMatch<TEntity, TError, TRequest>(string url, string method, TRequest requestBody, string entityTag) 
        //    where TRequest : class, new()
        //    where TEntity : class, new()
        //    where TError : class, new()
        //{
        //    var request = GetConditionalRequestWithEntityTag(false, GetUrl(url), method, entityTag, false);
        //    request.Content = requestBody.ToJsonStringContent(jsonOptions);
        //    var response = await InternalMakeRequest(internalClient, request);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="ifMatch"></param>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="entityTag"></param>
        ///// <param name="entityTagIsWeak"></param>
        ///// <returns></returns>
        //private static HttpRequestMessage GetConditionalRequestWithEntityTag(bool ifMatch, Uri url, string method, string entityTag, bool entityTagIsWeak = false)
        //{
        //    return GetRequest(GetMethod(method), url, null, (request) =>
        //    {
        //        var entityTagValue = new EntityTagHeaderValue(entityTag, entityTagIsWeak);

        //        if (!ifMatch)
        //            request.Headers.IfNoneMatch.Add(entityTagValue);
        //        else
        //            request.Headers.IfMatch.Add(entityTagValue);

        //        return request;
        //    });
        //}

        ///// <summary>
        ///// Makes a conditional HTTP request to a remote endpoint, setting the If-Match header to the specified entity tag.
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity returned from the remote endpoint on error.</typeparam>
        ///// <typeparam name="TRequest">Type of the entity sent to the remote endpoint in the request body.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="requestBody"></param>
        ///// <param name="entityTag"></param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> MakeJsonEntityRequestIfMatch<TEntity, TError, TRequest>(string url, string method, TRequest requestBody, string entityTag)
        //    where TEntity : class, new() 
        //    where TRequest : class, new()
        //    where TError : class, new()
        //{
        //    var request = GetConditionalRequestWithEntityTag(true, GetUrl(url), method, entityTag, false);
        //    request.Content = requestBody.ToJsonStringContent(jsonOptions);
        //    var response = await InternalMakeRequest(internalClient, request);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);
        //}

        ///// <summary>
        ///// Makes a conditional HTTP request to a remote endpoint, setting the If-Match header to the specified entity tag.
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity returned from the remote endpoint on error.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="entityTag"></param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> MakeRequestIfMatch<TEntity, TError>(string url, string method, string entityTag) 
        //    where TEntity : class, new()
        //    where TError : class, new()
        //{
        //    var request = GetConditionalRequestWithEntityTag(true, GetUrl(url), method, entityTag, false);
        //    var response = await InternalMakeRequest(internalClient, request);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);
        //}

        ///// <summary>
        ///// Makes a conditional HTTP request to a remote endpoint, setting the If-None-Match header to the specified entity tag.
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity returned from the remote endpoint on error.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="entityTag"></param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> MakeRequestIfNoneMatch<TEntity, TError>(string url, string method, string entityTag)
        //    where TEntity : class, new()
        //    where TError : class, new()
        //{
        //    var request = GetConditionalRequestWithEntityTag(false, GetUrl(url), method, entityTag, false);
        //    var response = await InternalMakeRequest(internalClient, request);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity returned from the remote endpoint on error.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> MakeRequest<TEntity, TError>(string url, string method)
        //    where TEntity : class, new()
        //    where TError : class, new()
        //{
        //    var response = await PrepareInternalMakeRequest(url, method, null, null);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntity<TEntity>?> MakeRequest<TEntity>(string url, string method)
        //    where TEntity : class, new()
        //{
        //    var response = await PrepareInternalMakeRequest(url, method, null, null);
        //    return await GetWebResponseWithEntity<TEntity>(response);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity returned from the remote endpoint on error.</typeparam>
        ///// <typeparam name="TRequest">Type of the entity sent to the remote endpoint in the request body.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="requestEntity"></param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> MakeJsonEntityRequest<TEntity, TError, TRequest>(string url, string method, TRequest requestEntity)
        //    where TRequest : class, new()
        //    where TEntity : class, new()
        //    where TError : class, new()
        //{
        //    var response = await InternalMakeJsonEntityRequest(url, method, requestEntity);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint.</typeparam>
        ///// <typeparam name="TRequest">Type of the entity sent to the remote endpoint in the request body.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="requestEntity"></param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntity<TEntity>?> MakeJsonEntityRequest<TEntity, TRequest>(string url, string method, TRequest requestEntity)
        //    where TRequest : class, new()
        //    where TEntity : class, new()
        //{
        //    var response = await InternalMakeJsonEntityRequest(url, method, requestEntity);
        //    return await GetWebResponseWithEntity<TEntity>(response);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="requestEntity"></param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntity<TEntity>?> MakeJsonEntityRequest<TEntity>(string url, string method, dynamic requestEntity)
        //    where TEntity : class, new()
        //{
        //    var response = await InternalMakeDynamicJsonEntityRequest(url, method, requestEntity);
        //    return await GetWebResponseWithEntity<TEntity>(response);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the request entity.</typeparam>
        ///// <param name="url">URI for the request.</param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="requestEntity">Request entity.</param>
        ///// <returns></returns>
        //public async Task<WebResponse?> MakeJsonEntityRequest<TEntity>(string url, string method, TEntity requestEntity)
        //   where TEntity : class, new()
        //{
        //    var response = await InternalMakeJsonEntityRequest(url, method, requestEntity);
        //    return GetWebResponse(response);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TEntity">Type of the entity returned from the remote endpoint on success.</typeparam>
        ///// <typeparam name="TError">Type of the entity returned from the remote endpoint on error.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="requestEntity"></param>
        ///// <returns></returns>
        //public async Task<WebResponseWithEntityOrError<TEntity, TError>?> MakeJsonEntityRequest<TEntity, TError>(string url, string method, dynamic requestEntity)
        //    where TEntity : class, new()
        //    where TError : class, new()
        //{
        //    var response = await InternalMakeDynamicJsonEntityRequest(url, method, requestEntity);
        //    return await GetWebResponseWithEntityOrError<TEntity, TError>(response);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="TRequest">Type of the entity sent to the remote endpoint in the request body.</typeparam>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="body"></param>
        ///// <returns></returns>
        //private async Task<HttpResponseMessage> InternalMakeJsonEntityRequest<TRequest>(string url, string method, TRequest body) where TRequest : class, new()
        //{
        //    return await PrepareInternalMakeRequest(url, method, body.ToJsonStringContent(jsonOptions), MediaTypeNames.Application.Json);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="body"></param>
        ///// <returns></returns>
        //async Task<HttpResponseMessage> InternalMakeDynamicJsonEntityRequest(string url, string method, dynamic body)
        //{
        //    return await PrepareInternalMakeRequest(url, method, new StringContent(JsonSerializer.Serialize(body, jsonOptions)), MediaTypeNames.Application.Json);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Uri GetUrl(string? url = null)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri? urlResult))
                    return urlResult;

                if (string.IsNullOrEmpty(BaseUrl))
                    throw new Exception($"URI {url} is not absolute and no base URI was found.");

                if (Uri.TryCreate($"{BaseUrl}/{url.TrimStart('/')}", UriKind.Absolute, out Uri? builtUrl))
                    return builtUrl;

                throw new ArgumentException($"Invalid URI: {url}", url);
            }

            if (string.IsNullOrEmpty(BaseUrl))
                throw new Exception("No URI found for the current request.");

            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUrl))
                throw new Exception($"URI {BaseUrl} is not absolute.");

            return baseUrl;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="client"></param>
        ///// <param name="request"></param>
        ///// <returns></returns>
        //private async Task<HttpResponseMessage> InternalMakeRequest(HttpClient client, HttpRequestMessage request)
        //{
        //    ArgumentNullException.ThrowIfNull(client);
        //    ArgumentNullException.ThrowIfNull(request);

        //    OnRequestSent(request);
        //    var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        //    OnResponseReceived(response);
        //    return response;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="url"></param>
        ///// <param name="method">HTTP method for the request.</param>
        ///// <param name="body"></param>
        ///// <param name="contentType"></param>
        ///// <returns></returns>
        ///// <exception cref="Exception"></exception>
        //private async Task<HttpResponseMessage> PrepareInternalMakeRequest(string url,
        //                                                                   string method,
        //                                                                   HttpContent? body = null,
        //                                                                   string? contentType = null)
        //{
        //    HttpMethod httpMethod = GetMethod(method);

        //    var requestObj = new HttpRequestMessage(httpMethod, GetUrl(url));

        //    if (body != null)
        //    {
        //        requestObj.Content = body;
        //        if (!string.IsNullOrEmpty(contentType))
        //            requestObj.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        //    }

        //    return await InternalMakeRequest(internalClient, requestObj);
        //}

        /// <summary>
        /// Raises the <see cref="ResponseReceived"/> event.
        /// </summary>
        protected virtual void OnResponseReceived(ResponseReceivedEventArgs e)
        {
            if (e is null)
                return;
            ResponseReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="ErrorStatusCode"/> event.
        /// </summary>
        protected virtual void OnErrorStatusCode(ErrorStatusCodeEventArgs e)
        {
            if (e is null)
                return;
            ErrorStatusCode?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the events associated with the specified <see cref="HttpResponseMessage"/> instance.
        /// </summary>
        void RaiseResponseEvents(HttpResponseMessage response)
        {
            if (response is null)
                return;

            var url = response.RequestMessage?.RequestUri?.ToString();
            var method = response.RequestMessage?.Method?.ToString();
            var status = response.StatusCode;
            var statusReason = response.ReasonPhrase;
            string? contentType = null;

            if (response.Content is not null)
                contentType = response.Content.Headers.ContentType?.ToString();

            OnResponseReceived(new ResponseReceivedEventArgs()
            {
                ContentType = contentType,
                StatusCode = status,
                ResponseMessage = response,
                Url = url
            });

            OnNotModified(new NotModifiedEventArgs()
            {
                HttpMethod = method,
                Url = url
            });

            OnErrorStatusCode(new ErrorStatusCodeEventArgs()
            {
                ContentType = contentType,
                HttpMethod = method,
                StatusCode = status,
                StatusReason = statusReason,
                ResponseMessage = response,
                Url = url
            });
        }

        /// <summary>
        /// Raises the <see cref="NotModified"/> event.
        /// </summary>
        protected virtual void OnNotModified(NotModifiedEventArgs e)
        {
            if (e is null)
                return;
            NotModified?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="RequestSent"/> event for the specified <paramref name="request"/>.
        /// </summary>
        /// <param name="request">Request associated with the event.</param>
        protected virtual void OnRequestSent(HttpRequestMessage request)
        {
            if (request is null)
                return;

            if (RequestSent is not null)
            {
                var eventArgs = new RequestSentEventArgs()
                {
                    Url = request.RequestUri?.ToString(),
                    HttpMethod = request.Method?.ToString(),
                    RequestMessage = request,

                };

                if (request.Content != null)
                    eventArgs.ContentType = request.Content.Headers.ContentType?.ToString();

                RequestSent.Invoke(this, eventArgs);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            internalClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
