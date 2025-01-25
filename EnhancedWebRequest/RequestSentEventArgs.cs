
namespace Llc.GoodConsulting.Web.EnhancedWebRequest
{
    /// <summary>
    /// Event data associated with a specific HTTP request sent to a remote endpoint.
    /// </summary>
    public class RequestSentEventArgs : EventArgs
    {
        public HttpRequestMessage? RequestMessage { get; set; }
        public string? Url { get; set; }
        public string? ContentType { get; set; }
        public string? HttpMethod { get; set; }
    }
}
