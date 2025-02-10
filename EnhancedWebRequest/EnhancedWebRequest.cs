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
        #region Events

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

        #endregion

        #region Public Properties

        /// <summary>
        /// Base URL for the web request.
        /// </summary>
        public string BaseUrl { get; internal set; }

        #endregion

        #region Constants

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

        #endregion

        #region Private Fields

        readonly HttpClient internalClient;
        readonly JsonSerializerOptions jsonOptions = JsonSerializerOptions.Default;

        #endregion

        #region Constructors

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

        #endregion

        #region JSON Entity Methods

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PatchJsonEntity<TEntity>(TEntity entity, string? url) where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, GetUrl(url)).WithJsonEntity(entity, jsonOptions);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PostJsonEntity<TEntity>(TEntity entity, string? url = null) where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url)).WithJsonEntity(entity, jsonOptions);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutJsonEntity<TEntity>(TEntity entity, string? url = null) where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url)).WithJsonEntity(entity, jsonOptions);
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
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url)).WithJsonEntity(entity, jsonOptions)
                                                                             .IfMatch(eTag, weakTag);
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
        public async Task<HttpResponseMessage> PatchJsonEntityIfMatch<TEntity>(TEntity entity, string eTag, bool weakTag = true, string? url = null)
            where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, GetUrl(url)).WithJsonEntity(entity, jsonOptions)
                                                                               .IfMatch(eTag, weakTag);
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
        public async Task<HttpResponseMessage> PostJsonEntityIfNoneMatch<TEntity>(TEntity entity, string eTag, bool weakTag = true, string? url = null)
            where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url)).WithJsonEntity(entity, jsonOptions)
                                                                              .IfNoneMatch(eTag, weakTag);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="unmodifiedSince"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutJsonEntityIfUnmodifiedSince<TEntity>(TEntity entity, DateTimeOffset unmodifiedSince, string? url = null)
            where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(url))
                              .WithJsonEntity(entity, jsonOptions)
                              .IfUnmodifiedSince(unmodifiedSince);
            return await Execute(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="unmodifiedSince"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PatchJsonEntityIfUnmodifiedSince<TEntity>(TEntity entity, DateTimeOffset unmodifiedSince, string? url = null)
            where TEntity : class, new()
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, GetUrl(url))
                              .WithJsonEntity(entity, jsonOptions)
                              .IfUnmodifiedSince(unmodifiedSince);
            return await Execute(request);
        }

        #endregion

        #region Misc Methods

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
                throw new HttpException("Cannot retrieve allowed HTTP methods: OPTIONS method is not allowed.", response);
            },
                HttpStatusCode.MethodNotAllowed);
            return [.. response.HeaderValues("Allow", StringComparison.OrdinalIgnoreCase)];
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
        public async Task<HttpResponseMessage> Options(string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, GetUrl(url));
            return await Execute(request);
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

        #endregion

        #region HttpContent Methods

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

        #region Get Methods

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
        /// <param name="queryParameters"></param>
        /// <param name="ifModifiedSince"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetIfModifiedSince(IDictionary<string, string> queryParameters, DateTimeOffset ifModifiedSince, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetUrl(url)).WithQueryString(queryParameters)
                                                                             .IfModifiedSince(ifModifiedSince);
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
        /// <param name="queryParameters"></param>
        /// <param name="matchTag"></param>
        /// <param name="weakTag"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetIfNoneMatch(IDictionary<string, string> queryParameters, string matchTag, bool weakTag = true, string? url = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetUrl(url)).WithQueryString(queryParameters)
                                                                             .IfNoneMatch(matchTag, weakTag);
            return await Execute(request);
        }

        #endregion

        #region Multipart Form File Upload

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

        #region Private Methods

        /// <summary>
        /// Creates an appropriate <seealso cref="HttpClient"/> object to use in the web request.
        /// </summary>
        /// <param name="url">Base URL for the client.</param>
        /// <returns><seealso cref="HttpClient"/> object.</returns>
        HttpClient GetClient(EnhancedWebRequestOptions? options = null, HttpClientHandler? clientHandler = null)
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

        #endregion

        #region Event Raising Methods

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

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            internalClient?.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
