
namespace Llc.GoodConsulting.Web
{
    /// <summary>
    /// 
    /// </summary>
    public class FileUploadPreReqException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public FileUploadPreReqException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public FileUploadPreReqException(string message) : base(message) { }
    }
}
