using System.Net;
using System.Text.Json;

namespace Llc.GoodConsulting.Web
{
    /// <summary>
    /// 
    /// </summary>
    public class EnhancedWebRequestOptions
    {
        /// <summary>
        /// User agent for the web request.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// User agent version for the web request.
        /// </summary>
        public Version? UserAgentVersion { get; set; }

        /// <summary>
        /// Token for bearer HTTP authentication.
        /// </summary>
        public string? BearerToken { get; set; }

        /// <summary>
        /// Type of authorization for the web request.
        /// </summary>
        public AuthorizationType AuthorizationType
        {
            get
            {
                if (!string.IsNullOrEmpty(BearerToken))
                    return AuthorizationType.BearerToken;
                else if (!string.IsNullOrEmpty(Username))
                    return AuthorizationType.Basic;
                else if (!string.IsNullOrEmpty(CustomAuthorizationType)) 
                    return AuthorizationType.Custom;
                else 
                    return AuthorizationType.None;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string? CustomAuthorizationType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? CustomAuthorizationValue { get; set; }

        /// <summary>
        /// Acceptable content types for the web request.
        /// </summary>
        public string[] Accept { get; set; } = [];

        /// <summary>
        /// Whether or not to skip SSL certificate validation.
        /// </summary>
        public bool SkipCertificateValidation { get; set; }

        /// <summary>
        /// Username for basic HTTP authentication.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ProxyAddress { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int? ProxyPort { get; set; }

        /// <summary>
        /// Username for basic HTTP authentication.
        /// </summary>
        public string? ProxyUsername { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ProxyPassword { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool ProxyBypassLocal { get; set; }

        /// <summary>
        /// Security protocol settings for the web request.
        /// </summary>
        public SecurityProtocolType SecurityProtocolType { get; set; } = SecurityProtocolType.Tls12;

        /// <summary>
        /// JSON serialization/deserialization options for handling JSON entities.
        /// </summary>
        public JsonSerializerOptions JsonSerializerSettings { get; set; } = JsonSerializerOptions.Default;
    }
}
