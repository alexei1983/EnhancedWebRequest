
namespace Llc.GoodConsulting.Web.EnhancedWebRequest
{
    /// <summary>
    /// Type of authentication for an HTTP web request.
    /// </summary>
    public enum AuthorizationType
    {
        /// <summary>
        /// No HTTP authentication.
        /// </summary>
        None,

        /// <summary>
        /// Basic HTTP authentication.
        /// </summary>
        Basic,

        /// <summary>
        /// Bearer token HTTP authentication.
        /// </summary>
        BearerToken,

        /// <summary>
        /// Custom HTTP authentication.
        /// </summary>
        Custom
    }
}
