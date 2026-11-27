using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Synq.Common.Events;

/// <summary>Konfigūracija <c>RabbitMq:*</c> (compose: RabbitMq__Host ir t. t.).</summary>
public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>false – outbox relay ir vartotojai nestartuoja (naudojama testuose be brokerio).</summary>
    public bool Enabled { get; set; } = true;

    public static RabbitMqOptions From(IConfiguration configuration)
    {
        var options = new RabbitMqOptions();
        configuration.GetSection("RabbitMq").Bind(options);
        return options;
    }
}

/// <summary>
/// Viena ilgalaikė AMQP jungtis procesui (rekomenduojama RabbitMQ praktika: jungtis brangi, kanalai pigūs).
/// Jungiamasi tingiai – servisas startuoja ir aptarnauja REST net kai brokeris neveikia (outbox publikuos vėliau).
/// <c>AutomaticRecoveryEnabled</c> – klientas pats atkuria jungtį, kanalus, eiles ir vartotojus.
/// </summary>
public sealed class RabbitMqConnection(RabbitMqOptions options, string clientName, ILogger<RabbitMqConnection> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public bool IsOpen => _connection?.IsOpen == true;

    public async Task<IConnection> GetAsync(CancellationToken ct)
    {
        if (_connection is not null)
            return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is not null)
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.User,
                Password = options.Password,
                ClientProvidedName = clientName,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(2),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3),
            };
            var connection = await factory.CreateConnectionAsync(ct);
            await RabbitMqTopology.DeclareAsync(connection, ct);
            logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", options.Host, options.Port);
            _connection = connection;
            return connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _lock.Dispose();
    }
}
