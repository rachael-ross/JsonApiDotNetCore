using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.Resources
{
    /// <summary>
    /// Provides a resource-centric extensibility point for executing custom code when something happens with a resource. The goal here is to reduce the need
    /// for overriding the service and repository layers.
    /// </summary>
    /// <typeparam name="TResource">
    /// The resource type.
    /// </typeparam>
    [PublicAPI]
    public interface IResourceDefinition<TResource> : IResourceDefinition<TResource, int>
        where TResource : class, IIdentifiable<int>
    {
    }

    /// <summary>
    /// Provides a resource-centric extensibility point for executing custom code when something happens with a resource. The goal here is to reduce the need
    /// for overriding the service and repository layers.
    /// </summary>
    /// <typeparam name="TResource">
    /// The resource type.
    /// </typeparam>
    /// <typeparam name="TId">
    /// The resource identifier type.
    /// </typeparam>
    [PublicAPI]
    // ReSharper disable once TypeParameterCanBeVariant -- Justification: making TId contravariant is a breaking change.
    public interface IResourceDefinition<TResource, TId>
        where TResource : class, IIdentifiable<TId>
    {
        /// <summary>
        /// Enables to extend, replace or remove includes that are being applied on this resource type.
        /// </summary>
        /// <param name="existingIncludes">
        /// An optional existing set of includes, coming from query string. Never <c>null</c>, but may be empty.
        /// </param>
        /// <returns>
        /// The new set of includes. Return an empty collection to remove all inclusions (never return <c>null</c>).
        /// </returns>
        IReadOnlyCollection<IncludeElementExpression> OnApplyIncludes(IReadOnlyCollection<IncludeElementExpression> existingIncludes);

        /// <summary>
        /// Enables to extend, replace or remove a filter that is being applied on a set of this resource type.
        /// </summary>
        /// <param name="existingFilter">
        /// An optional existing filter, coming from query string. Can be <c>null</c>.
        /// </param>
        /// <returns>
        /// The new filter, or <c>null</c> to disable the existing filter.
        /// </returns>
        FilterExpression OnApplyFilter(FilterExpression existingFilter);

        /// <summary>
        /// Enables to extend, replace or remove a sort order that is being applied on a set of this resource type. Tip: Use
        /// <see cref="JsonApiResourceDefinition{TResource, TId}.CreateSortExpressionFromLambda" /> to build from a lambda expression.
        /// </summary>
        /// <param name="existingSort">
        /// An optional existing sort order, coming from query string. Can be <c>null</c>.
        /// </param>
        /// <returns>
        /// The new sort order, or <c>null</c> to disable the existing sort order and sort by ID.
        /// </returns>
        SortExpression OnApplySort(SortExpression existingSort);

        /// <summary>
        /// Enables to extend, replace or remove pagination that is being applied on a set of this resource type.
        /// </summary>
        /// <param name="existingPagination">
        /// An optional existing pagination, coming from query string. Can be <c>null</c>.
        /// </param>
        /// <returns>
        /// The changed pagination, or <c>null</c> to use the first page with default size from options. To disable paging, set
        /// <see cref="PaginationExpression.PageSize" /> to <c>null</c>.
        /// </returns>
        PaginationExpression OnApplyPagination(PaginationExpression existingPagination);

        /// <summary>
        /// Enables to extend, replace or remove a sparse fieldset that is being applied on a set of this resource type. Tip: Use
        /// <see cref="SparseFieldSetExpressionExtensions.Including{TResource}" /> and <see cref="SparseFieldSetExpressionExtensions.Excluding{TResource}" /> to
        /// safely change the fieldset without worrying about nulls.
        /// </summary>
        /// <remarks>
        /// This method executes twice for a single request: first to select which fields to retrieve from the data store and then to select which fields to
        /// serialize. Including extra fields from this method will retrieve them, but not include them in the json output. This enables you to expose calculated
        /// properties whose value depends on a field that is not in the sparse fieldset.
        /// </remarks>
        /// <param name="existingSparseFieldSet">
        /// The incoming sparse fieldset from query string. At query execution time, this is <c>null</c> if the query string contains no sparse fieldset. At
        /// serialization time, this contains all viewable fields if the query string contains no sparse fieldset.
        /// </param>
        /// <returns>
        /// The new sparse fieldset, or <c>null</c> to discard the existing sparse fieldset and select all viewable fields.
        /// </returns>
        SparseFieldSetExpression OnApplySparseFieldSet(SparseFieldSetExpression existingSparseFieldSet);

        /// <summary>
        /// Enables to adapt the Entity Framework Core <see cref="IQueryable{T}" /> query, based on custom query string parameters. Note this only works on
        /// primary resource requests, such as /articles, but not on /blogs/1/articles or /blogs?include=articles.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// protected override QueryStringParameterHandlers OnRegisterQueryableHandlersForQueryStringParameters()
        /// {
        ///     return new QueryStringParameterHandlers
        ///     {
        ///         ["isActive"] = (source, parameterValue) => source
        ///             .Include(model => model.Children)
        ///             .Where(model => model.LastUpdateTime > DateTime.Now.AddMonths(-1)),
        ///         ["isHighRisk"] = FilterByHighRisk
        ///     };
        /// }
        /// 
        /// private static IQueryable<Model> FilterByHighRisk(IQueryable<Model> source, StringValues parameterValue)
        /// {
        ///     bool isFilterOnHighRisk = bool.Parse(parameterValue);
        ///     return isFilterOnHighRisk ? source.Where(model => model.RiskLevel >= 5) : source.Where(model => model.RiskLevel < 5);
        /// }
        /// ]]></code>
        /// </example>
#pragma warning disable AV1130 // Return type in method signature should be a collection interface instead of a concrete type
        QueryStringParameterHandlers<TResource> OnRegisterQueryableHandlersForQueryStringParameters();
#pragma warning restore AV1130 // Return type in method signature should be a collection interface instead of a concrete type

        /// <summary>
        /// Enables to add JSON:API meta information, specific to this resource.
        /// </summary>
        IDictionary<string, object> GetMeta(TResource resource);

        /// <summary>
        /// Enables to execute custom logic to initialize a newly instantiated resource during a POST request. This is typically used to assign default values to
        /// properties or to side-load-and-attach required relationships.
        /// </summary>
        /// <param name="resource">
        /// A freshly instantiated resource object.
        /// </param>
        /// <param name="cancellationToken">
        /// Propagates notification that request handling should be canceled.
        /// </param>
        Task OnInitializeResourceAsync(TResource resource, CancellationToken cancellationToken);

        /// <summary>
        /// Enables to execute custom logic, just before a resource is inserted in the underlying data store, during a POST request. This is typically used to
        /// overwrite attributes from the incoming request, such as a creation-timestamp. Another use case is to add a notification message to an outbox table,
        /// which gets committed along with the resource write in a single transaction (see https://microservices.io/patterns/data/transactional-outbox.html).
        /// </summary>
        /// <param name="resource">
        /// The resource with incoming request data applied on it.
        /// </param>
        /// <param name="cancellationToken">
        /// Propagates notification that request handling should be canceled.
        /// </param>
        Task OnBeforeCreateResourceAsync(TResource resource, CancellationToken cancellationToken);

        /// <summary>
        /// Enables to execute custom logic after a resource has been inserted in the underlying data store, during a POST request. A typical use case is to
        /// enqueue a notification message on a service bus.
        /// </summary>
        /// <param name="resource">
        /// The re-fetched resource after a successful insertion.
        /// </param>
        /// <param name="cancellationToken">
        /// Propagates notification that request handling should be canceled.
        /// </param>
        Task OnAfterCreateResourceAsync(TResource resource, CancellationToken cancellationToken);

        /// <summary>
        /// Enables to execute custom logic to validate if the update request can be processed, based on the currently stored resource. A typical use case is to
        /// throw when the resource is soft-deleted or archived.
        /// </summary>
        /// <param name="resource">
        /// The resource as currently stored in the underlying data store.
        /// </param>
        /// <param name="cancellationToken">
        /// Propagates notification that request handling should be canceled.
        /// </param>
        Task OnAfterGetForUpdateResourceAsync(TResource resource, CancellationToken cancellationToken);

        /// <summary>
        /// Enables to execute custom logic, just before a resource is updated in the underlying data store, during a PATCH request. This is typically used to
        /// overwrite attributes from the incoming request, such as a last-modification-timestamp. Another use case is to add a notification message to an outbox
        /// table, which gets committed along with the resource write in a single transaction (see
        /// https://microservices.io/patterns/data/transactional-outbox.html).
        /// </summary>
        /// <param name="resource">
        /// The stored resource with incoming request data applied on it.
        /// </param>
        /// <param name="cancellationToken">
        /// Propagates notification that request handling should be canceled.
        /// </param>
        Task OnBeforeUpdateResourceAsync(TResource resource, CancellationToken cancellationToken);

        /// <summary>
        /// Enables to execute custom logic after a resource has been updated in the underlying data store, during a PATCH request. A typical use case is to
        /// enqueue a notification message on a service bus.
        /// </summary>
        /// <param name="resource">
        /// The re-fetched resource after a successful update.
        /// </param>
        /// <param name="cancellationToken">
        /// Propagates notification that request handling should be canceled.
        /// </param>
        Task OnAfterUpdateResourceAsync(TResource resource, CancellationToken cancellationToken);

        /// <summary>
        /// Enables to execute custom logic, just before a resource is deleted from the underlying data store, during a DELETE request. This enables to throw in
        /// case the user does not have permission, an attempt is made to delete an unarchived resource or a non-closed work item etc. Another use case is to add
        /// a notification message to an outbox table, which gets committed along with the resource write in a single transaction (see
        /// https://microservices.io/patterns/data/transactional-outbox.html).
        /// </summary>
        /// <param name="id">
        /// The identifier of the resource to delete.
        /// </param>
        /// <param name="cancellationToken">
        /// Propagates notification that request handling should be canceled.
        /// </param>
        Task OnBeforeDeleteResourceAsync(TId id, CancellationToken cancellationToken);

        /// <summary>
        /// Enables to execute custom logic after a resource has been deleted from the underlying data store, during a DELETE request. A typical use case is to
        /// enqueue a notification message on a service bus.
        /// </summary>
        /// <param name="id">
        /// The identifier of the resource to delete.
        /// </param>
        /// <param name="cancellationToken">
        /// Propagates notification that request handling should be canceled.
        /// </param>
        Task OnAfterDeleteResourceAsync(TId id, CancellationToken cancellationToken);
    }
}
