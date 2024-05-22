using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebApplication.Services;

public interface IAsyncEventingBasicConsumerFactory
{
    AsyncEventingBasicConsumer CreateConsumer(IModel channel);
}