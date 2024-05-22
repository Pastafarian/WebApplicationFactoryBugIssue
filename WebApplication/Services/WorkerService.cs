using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;
using IConnectionFactory = RabbitMQ.Client.IConnectionFactory;

namespace WebApplication.Services;

public interface IRabbitConnectionFactory : IDisposable
{
    Task<IConnection> GetConnectionAsync();
    IConnectionFactory GetConnectionFactory();
    void Close();
}

public interface IMessage
{
    string Message { get; }
}

public class MessageTest1 : IMessage
{
    public string Message => "MessageTest1";
}
public class MessageTest2 : IMessage
{
    public string Message => "MessageTest2";
}
public class MessageDefault : IMessage
{
    public string Message => "MessageDefault";
}
public class WorkerService : BackgroundService
{
    private RabbitMqClientConsumerConfiguration _rabbitMqConfiguration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private IAsyncEventingBasicConsumerFactory _asyncEventingBasicConsumerFactory;
    private readonly Guid _guid;
    protected IConnection? connection;
    public WorkerService(IServiceScopeFactory serviceScopeFactory
        )
    {
        _serviceScopeFactory = serviceScopeFactory;
        _guid = Guid.NewGuid();
        Debug.Print("WorkerService Guid: " + _guid);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IRabbitConnectionFactory>();
        _rabbitMqConfiguration = scope.ServiceProvider.GetRequiredService<RabbitMqClientConsumerConfiguration>();
        var message = scope.ServiceProvider.GetRequiredService<IMessage>();

        Debug.Print("WorkerService Guid ExecuteAsync: " + _guid + ". Message: " + message.Message);

        connection = await connectionFactory.GetConnectionAsync();
        var channel = connection.CreateModel();

        CreateExchangesAndQueues(channel);

        _asyncEventingBasicConsumerFactory = scope.ServiceProvider.GetRequiredService<IAsyncEventingBasicConsumerFactory>();
        var consumer = _asyncEventingBasicConsumerFactory.CreateConsumer(channel);

        consumer.Received += async (_, ea) =>
        {
            var messageString = string.Empty;
            try
            {
                var body = ea.Body.ToArray();
                messageString = Encoding.UTF8.GetString(body);
                if (messageString == "MessageTest1")
                {
                    if (message.Message != "MessageTest1")
                    {
                        Debug.Print("Error! Message: " + messageString + ". Message Service: " + message.Message + " " + _guid);
                        throw new Exception("MessageTest1");
                    }
                }

                if (messageString == "MessageTest2")
                {
                    if (message.Message != "MessageTest2")
                    {
                        Debug.Print("Error! Message: " + messageString + ". Message Service: " + message.Message + " " + _guid);
                        throw new Exception("MessageTest2");
                    }
                }

                Debug.Print("Message: " + messageString + ". Message Service: " + message.Message);
                Console.WriteLine("Message: " + messageString + ". Message Service: " + message.Message);
            }
            catch (Exception)
            {
                throw;
            }
        };

        channel.BasicConsume(_rabbitMqConfiguration.LoggingQueueName, autoAck: false, consumer);
    }

    private void CreateExchangesAndQueues(IModel model)
    {
        if (!_rabbitMqConfiguration.AutoCreateExchange) return;
        model.ExchangeDeclare(_rabbitMqConfiguration.LoggingExchangeName, _rabbitMqConfiguration.ExchangeType, true);
        model.QueueDeclare(_rabbitMqConfiguration.LoggingQueueName, true, false, false);
        model.QueueBind(_rabbitMqConfiguration.LoggingQueueName, _rabbitMqConfiguration.LoggingExchangeName, _rabbitMqConfiguration.RouteKey);
    }


    public override Task StopAsync(CancellationToken stoppingToken)
    {
        connection?.Close();
        return base.StopAsync(stoppingToken);
    }
}