using System;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Workaround
    {
        public static IServiceCollection RegisterActorWithDI<TActor>(this IServiceCollection services)
            where TActor : Actor
        {
            services.AddOptions<ActorRuntimeOptions>().Configure<IServiceProvider>((actors, sp) =>
            {
                actors.RegisterActor<TActor>(type =>
                {
                    Func<ActorService, ActorId, Actor> factory = (ActorService service, ActorId id) => 
                    {
                        return (Actor)ActivatorUtilities.CreateInstance<TActor>(sp, id, service);
                    };
                    return new ActorService(type, sp.GetRequiredService<ILoggerFactory>(), factory);
                });
            });

            return services;
        }
    }
}