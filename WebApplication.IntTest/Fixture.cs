using Microsoft.Extensions.DependencyInjection;
using Testcontainers.RabbitMq;

namespace WebApplication.IntTest;

public class Fixture : IDisposable
{
    public HttpClient? ConsumerHttpClient;
    public RabbitMqContainer RabbitMqContainer;
    private ConsumerWebApplicationFactory factory;
    public void BuildTestContainers()
    {
        RabbitMqContainer = new RabbitMqBuilder().WithPassword("guest").WithUsername("guest").WithName("RabbitMqContainer").WithPortBinding(5672, 5672).Build();
    }

    public Fixture()
    {
        BuildTestContainers();
        RabbitMqContainer.StartAsync().GetAwaiter().GetResult();
    }

    public void BuildConsumerHttpClient(Func<IServiceCollection, bool>? registerCustomIocForConsumer = null)
    {
        factory = new ConsumerWebApplicationFactory((service) =>
        {

            registerCustomIocForConsumer?.Invoke(service);

            return true;
        });
        ConsumerHttpClient = factory.CreateClient();
    }

    public void KillApplication()
    {
        factory.Dispose();
    }

    public void Dispose()
    {
        RabbitMqContainer.DisposeAsync().GetAwaiter().GetResult();
    }
}