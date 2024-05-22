using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebApplication.Services;

public class AsyncEventingBasicConsumerFactory : IAsyncEventingBasicConsumerFactory
{
    public AsyncEventingBasicConsumer CreateConsumer(IModel channel)
    {
        return new AsyncEventingBasicConsumer(channel);
    }
}