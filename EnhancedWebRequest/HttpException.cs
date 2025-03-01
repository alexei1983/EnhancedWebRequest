using System.Net;

namespace Llc.GoodConsulting.Web
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    /// <param name="message"></param>
    /// <param name="response"></param>
    /// <param name="innerException"></param>
    public class HttpException(string message, HttpResponseMessage? response, Exception? innerException) : Exception(message, innerException ?? new Exception())
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="response"></param>
        public HttpException(string message, HttpResponseMessage? response) : this(message, response, null)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public HttpStatusCode Code { get; set; } = response?.StatusCode ?? HttpStatusCode.Continue;

        /// <summary>
        /// 
        /// </summary>
        public string? Reason { get; set; } = response?.ReasonPhrase;
    }
}
