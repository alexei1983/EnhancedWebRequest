

namespace Llc.GoodConsulting.Web.EnhancedWebRequest
{
    ///// <summary>
    ///// Represents a response received from a remote HTTP endpoint, including the status and associated entities.
    ///// </summary>
    ///// <typeparam name="TEntity">Type of the entity from the remote endpoint on success.</typeparam>
    ///// <typeparam name="TError">Type of the entity from the remote endpoint on error.</typeparam>
    //public class WebResponseWithEntityOrError<TEntity, TError> : WebResponseWithEntity<TEntity> where TError : class, new() where TEntity : class, new()
    //{
    //    /// <summary>
    //    /// Entity representing one or more errors from the remote endpoint.
    //    /// </summary>
    //    public TError? Error { get; set; }

    //    /// <summary>
    //    /// Creates a new instance of the <see cref="WebResponseWithEntityOrError{TEntity, TError}"/> class.
    //    /// </summary>
    //    /// <param name="response"><seealso cref="HttpResponseMessage"/> from the remote endpoint.</param>
    //    public WebResponseWithEntityOrError(HttpResponseMessage response) : base(response)
    //    {
    //    }

    //    /// <summary>
    //    /// Creates a new instance of the <see cref="WebResponseWithEntityOrError{TEntity, TError}"/> class.
    //    /// </summary>
    //    /// <param name="entity">Entity from the remote endpoint on success.</param>
    //    /// <param name="response"><seealso cref="HttpResponseMessage"/> from the remote endpoint.</param>
    //    public WebResponseWithEntityOrError(TEntity entity, HttpResponseMessage response) : base(entity, response)
    //    {
    //        Entity = entity;
    //    }

    //    /// <summary>
    //    /// Creates a new instance of the <see cref="WebResponseWithEntityOrError{TEntity, TError}"/> class.
    //    /// </summary>
    //    /// <param name="error">Entity from the remote endpoint on error.</param>
    //    /// <param name="response"><seealso cref="HttpResponseMessage"/> from the remote endpoint.</param>
    //    public WebResponseWithEntityOrError(TError error, HttpResponseMessage response) : this(response)
    //    {
    //        Error = error;
    //    }

    //    /// <summary>
    //    /// Processes the error entity from the HTTP response and assigns it to the <see cref="Error"/> property.
    //    /// </summary>
    //    /// <param name="errorProcessor">Method to process the HTTP response and retrieve the error entity.</param>
    //    /// <returns><seealso cref="Task"/></returns>
    //    public async Task HandleError(Func<HttpResponseMessage, TError> errorProcessor)
    //    {
    //        Error = await GetEntity(errorProcessor);
    //    }

    //    /// <summary>
    //    /// Processes the HTTP response, handling the appropriate entity depending on whether or not the 
    //    /// response was successful from the remote endpoint's perspective.
    //    /// </summary>
    //    /// <param name="entityProcessor">Method to process the HTTP response and retrieve the success entity.</param>
    //    /// <param name="errorProcessor">Method to process the HTTP response and retrieve the error entity.</param>
    //    /// <returns><seealso cref="Task"/></returns>
    //    /// <exception cref="ArgumentNullException"></exception>
    //    public async Task HandleEntityOrError(Func<HttpResponseMessage, TEntity> entityProcessor,
    //                                          Func<HttpResponseMessage, TError> errorProcessor)
    //    {
    //        ArgumentNullException.ThrowIfNull(entityProcessor);
    //        ArgumentNullException.ThrowIfNull(errorProcessor);

    //        if (Response.IsSuccessStatusCode)
    //            await HandleEntity(entityProcessor);
    //        else
    //            await HandleError(errorProcessor);
    //    }
    //}
}
