namespace Cedar.Projections
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public static class ProjectionHandlerResolverExtensions
    {
        private static readonly MethodInfo s_dispatchInternalMethod = typeof(ProjectionHandlerResolverExtensions).GetRuntimeMethods()
            .Single(m => m.Name.Equals("DispatchInternal", StringComparison.Ordinal));

        public static async Task Dispatch(
            this IProjectionHandlerResolver handlerResolver,
            string streamId,
            Guid eventId,
            int version,
            DateTimeOffset timeStamp,
            IReadOnlyDictionary<string, object> headers,
            object @event,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var eventType = @event.GetType();
            var dispatchMethod = s_dispatchInternalMethod.MakeGenericMethod(eventType);

            var paramaters = new[]
            {
                handlerResolver, streamId, eventId, version, timeStamp, headers, @event, cancellationToken
            };

            await (Task)dispatchMethod.Invoke(handlerResolver, paramaters);
        }

        private static async Task DispatchInternal<T>(
            IProjectionHandlerResolver eventHandlerResolver,
            string streamid,
            Guid eventId,
            int version,
            DateTimeOffset timeStamp,
            IReadOnlyDictionary<string, object> headers,
            T @event,
            CancellationToken cancellationToken) where T : class
        {
            var eventMessage = new ProjectionEvent<T>(streamid, eventId, version, timeStamp, headers, @event);
            var handlers = eventHandlerResolver.ResolveAll<T>();

            foreach (var handler in handlers)
            {
                await handler(eventMessage, cancellationToken);
            }
        }
    }
}