using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synq.Common.Events.Outbox;

namespace Synq.Common.Events;

public static class EventsSetup
{
    /// <summary>Bendra RabbitMQ jungtis + <see cref="OutboxWriter"/>.</summary>
    public static IServiceCollection AddSynqRabbitMq(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var options = RabbitMqOptions.From(configuration);
        services.AddSingleton(options);
        services.AddSingleton(sp => new RabbitMqConnection(options, serviceName, sp.GetRequiredService<ILogger<RabbitMqConnection>>()));
        services.AddScoped<OutboxWriter>();
        return services;
    }

    /// <summary>Outbox relay šio serviso DbContext'ui. Nestartuoja, jei <c>RabbitMq:Enabled=false</c>.</summary>
    public static IServiceCollection AddOutboxRelay<TDb>(this IServiceCollection services, IConfiguration configuration) where TDb : DbContext
    {
        if (RabbitMqOptions.From(configuration).Enabled)
            services.AddHostedService<OutboxRelay<TDb>>();
        return services;
    }

    /// <summary>Patvarus vartotojas. Nestartuoja, jei <c>RabbitMq:Enabled=false</c>.</summary>
    public static IServiceCollection AddEventConsumer<TConsumer>(this IServiceCollection services, IConfiguration configuration)
        where TConsumer : class, IHostedService
    {
        if (RabbitMqOptions.From(configuration).Enabled)
            services.AddHostedService<TConsumer>();
        return services;
    }
}
