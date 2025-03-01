
using System.Net;

namespace Llc.GoodConsulting.Web
{
    /// <summary>
    /// Event data associated with a non-success HTTP status code in a response.
    /// </summary>
    public class ErrorStatusCodeEventArgs : EventArgs
    {
        public HttpResponseMessage? ResponseMessage { get; set; }
        public string? Url { get; set; }
        public string? ContentType { get; set; }
        public string? HttpMethod { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string? StatusReason { get; set; }
    }
}
