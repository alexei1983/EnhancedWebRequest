

namespace Llc.GoodConsulting.Web.EnhancedWebRequest
{
    /// <summary>
    /// Event data associated with an indication that content has not been modified on a remote HTTP endpoint.
    /// </summary>
    public class NotModifiedEventArgs : EventArgs
    {
        public string? Url { get; set; }
        public string? HttpMethod { get; set; }
    }
}
