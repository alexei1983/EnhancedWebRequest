
using System.Net;

namespace Llc.GoodConsulting.Web
{
    /// <summary>
    /// Event data associated with a specific HTTP response received from a remote endpoint.
    /// </summary>
    public class ResponseReceivedEventArgs : EventArgs
    {
        public HttpResponseMessage? ResponseMessage { get; set; }
        public string? Url { get; set; }
        public string? ContentType { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
