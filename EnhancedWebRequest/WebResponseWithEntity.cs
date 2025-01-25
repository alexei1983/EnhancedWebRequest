namespace Llc.GoodConsulting.Web.EnhancedWebRequest
{
    /// <summary>
    /// Represents a response received from a remote HTTP endpoint, including the status and associated entities.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity from the remote endpoint.</typeparam>
    /// <remarks>
    /// Creates a new instance of the <see cref="WebResponseWithEntity{TEntity}"/> class.
    /// </remarks>
    /// <param name="response"><seealso cref="HttpResponseMessage"/> from the remote endpoint.</param>
    public class WebResponseWithEntity<TEntity>(HttpResponseMessage response) : WebResponse(response) where TEntity : class, new()
    {
        /// <summary>
        /// Entity from the remote endpoint.
        /// </summary>
        public TEntity? Entity { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="WebResponseWithEntity{TEntity}"/> class.
        /// </summary>
        /// <param name="entity">Entity from the remote endpoint.</param>
        /// <param name="response"><seealso cref="HttpResponseMessage"/> from the remote endpoint.</param>
        public WebResponseWithEntity(TEntity entity, HttpResponseMessage response) : this(response)
        {
            Entity = entity;
        }

        /// <summary>
        /// Processes the entity from the HTTP response and assigns it to the <see cref="Entity"/> property.
        /// </summary>
        /// <param name="entityProcessor">Method to process the HTTP response and retrieve the entity.</param>
        /// <returns><seealso cref="Task"/></returns>
        public async virtual Task HandleEntity(Func<HttpResponseMessage, TEntity> entityProcessor)
        {
            Entity = await GetEntity(entityProcessor);
        }
    }
}
