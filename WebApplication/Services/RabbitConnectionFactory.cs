using RabbitMQ.Client;

namespace WebApplication.Services;

public class RabbitMQConfiguration
{
    public RabbitMQConfiguration()
    {
        LoggingExchangeName = "log-exchange";
    }

    public List<string> Hostnames { get; set; } = [];
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string LoggingExchangeName { get; set; }
    public string ExchangeType { get; set; } = string.Empty;
    public string RouteKey { get; set; } = string.Empty;
    public int Port { get; set; }
    public string VHost { get; set; } = string.Empty;
    public IProtocol Protocol { get; set; }
    public ushort Heartbeat { get; set; }
    public SslOption SslOption { get; set; }
}
/// <summary>
/// Configuration class for RabbitMqClient
/// </summary>
public class RabbitMQClientConfiguration : RabbitMQConfiguration
{
    /// <summary>
    /// The maximum number of events to include in a single batch.
    /// </summary>
    public int BatchPostingLimit { get; set; }

    /// <summary>The time to wait between checking for event batches.</summary>
    public TimeSpan Period { get; set; }

    public bool AutoCreateExchange { get; set; }

    public RabbitMQClientConfiguration()
    {
        BatchPostingLimit = 5;
        Period = TimeSpan.FromSeconds(2);
        AutoCreateExchange = true;
    }
}
public class RabbitMqClientConsumerConfiguration : RabbitMQClientConfiguration
{
    public RabbitMqClientConsumerConfiguration()
    {
        LoggingQueueName = "log-queue";
    }

    /// <summary>
    /// Name of the RabbitMq queue for log events
    /// </summary>
    public string LoggingQueueName { get; set; }

}

/// <summary>
/// RabbitMqClient - this class is the engine that lets you send messages to RabbitMq
/// </summary>
public class RabbitConnectionFactory : IDisposable, IRabbitConnectionFactory
{
    // synchronization lock
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private readonly CancellationTokenSource _closeTokenSource = new();
    private readonly CancellationToken _closeToken;

    // configuration member
    private readonly RabbitMQClientConfiguration _config;

    // endpoint members
    private readonly IConnectionFactory _connectionFactory;
    private volatile IConnection? _connection;

    /// <summary>
    /// Constructor for RabbitMqClient
    /// </summary>
    /// <param name="configuration">mandatory</param>
    public RabbitConnectionFactory(RabbitMqClientConsumerConfiguration configuration)
    {
        _closeToken = _closeTokenSource.Token;

        // load configuration
        _config = configuration;
        // initialize
        _connectionFactory = GetConnectionFactory();
    }

    /// <summary>
    /// Configures a new ConnectionFactory, and returns it
    /// </summary>
    /// <returns></returns>
    public IConnectionFactory GetConnectionFactory()
    {
        // prepare connection factory
        var connectionFactory = new ConnectionFactory
        {
            UserName = _config.Username,
            Password = _config.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(2),
            ClientProvidedName = "serilog.sinks.rabbitmq consumer",
            DispatchConsumersAsync = true,
            ConsumerDispatchConcurrency = 1
        };

        if (_config.SslOption != null)
        {
            connectionFactory.Ssl.Version = _config.SslOption.Version;
            connectionFactory.Ssl.CertPath = _config.SslOption.CertPath;
            connectionFactory.Ssl.ServerName = _config.SslOption.ServerName;
            connectionFactory.Ssl.Enabled = _config.SslOption.Enabled;
            connectionFactory.Ssl.AcceptablePolicyErrors = _config.SslOption.AcceptablePolicyErrors;
        }
        // setup heartbeat if needed
        if (_config.Heartbeat > 0)
            connectionFactory.RequestedHeartbeat = TimeSpan.FromMilliseconds(_config.Heartbeat);

        // only set, if has value, otherwise leave default
        if (_config.Port > 0) connectionFactory.Port = _config.Port;
        if (!string.IsNullOrEmpty(_config.VHost)) connectionFactory.VirtualHost = _config.VHost;

        // return factory
        return connectionFactory;
    }

    /// <summary>

    public void Close()
    {
        IList<Exception> exceptions = new List<Exception>();
        try
        {
            _closeTokenSource.Cancel();
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        try
        {
            _connectionLock.Wait(10);
            _connection?.Close();
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException(exceptions);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _closeTokenSource.Dispose();
        _connectionLock.Dispose();
        _connection?.Dispose();
    }

    public async Task<IConnection> GetConnectionAsync()
    {
        if (_connection == null)
        {
            await _connectionLock.WaitAsync(_closeToken);
            try
            {
                if (_connection == null)
                {
                    _connection = _config.Hostnames.Count == 0
                        ? _connectionFactory.CreateConnection()
                        : _connectionFactory.CreateConnection(_config.Hostnames);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        return _connection;
    }
}