using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;
using WebApplication.Services;

namespace WebApplication.IntTest
{

    [Collection("Database collection")]
    public class UnitTest1
    {
        private bool _exchangeCreated;
        private readonly Fixture _fixture;
        private IBasicProperties _properties;
        private IModel _model;
        private readonly RabbitConnectionFactory _rabbitConnectionFactory;
        private readonly RabbitMqClientConsumerConfiguration _rabbitMqClientConsumerConfiguration =
            new()
            {
                AutoCreateExchange = true,
                ExchangeType = "fanout",
                LoggingExchangeName = "log-exchange",
                LoggingQueueName = "log-queue",
                Username = "guest",
                Password = "guest",
                Hostnames = new List<string> { "localhost" },
                Port = 5672
            };
        public UnitTest1(Fixture fixture)
        {
            _fixture = fixture;
            _rabbitConnectionFactory = new RabbitConnectionFactory(_rabbitMqClientConsumerConfiguration);
        }


        [Fact]
        public async Task FirstTest1()
        {
            await SetupRabbitMq();
            WorkerService? workerService = null;

            _fixture.BuildConsumerHttpClient((service) =>
            {
                service.Replace(ServiceDescriptor.Transient<IMessage, MessageTest1>());
                //service.AddSingleton<WorkerService>();
                var serviceProvider = service.BuildServiceProvider();

                workerService = serviceProvider.GetService<WorkerService>();
                return true;
            });

            BasicPublish(
                new PublicationAddress(_rabbitMqClientConsumerConfiguration.ExchangeType,
                    _rabbitMqClientConsumerConfiguration.LoggingExchangeName, string.Empty), "MessageTest1"u8.ToArray());

            await Task.Delay(10000);
            await workerService!.StopAsync(new CancellationToken());
        }


        [Fact]
        public async Task SecondTest2()
        {
            await SetupRabbitMq();
            WorkerService? workerService = null;
            _fixture.BuildConsumerHttpClient((service) =>
            {
                service.Replace(ServiceDescriptor.Transient<IMessage, MessageTest2>());
                var serviceProvider = service.BuildServiceProvider();

                workerService = serviceProvider.GetService<WorkerService>();
                return true;
            });

            BasicPublish(
                new PublicationAddress(_rabbitMqClientConsumerConfiguration.ExchangeType,
                    _rabbitMqClientConsumerConfiguration.LoggingExchangeName, string.Empty), "MessageTest2"u8.ToArray());
            await Task.Delay(10000);
            await workerService?.StopAsync(new CancellationToken());
        }

        private async Task SetupRabbitMq()
        {
            var connection = await _rabbitConnectionFactory.GetConnectionAsync();

            var model = connection.CreateModel();

            CreateExchange(model);

            _model = model;
            _properties = model.CreateBasicProperties();
        }

        private void BasicPublish(PublicationAddress address, ReadOnlyMemory<byte> body)
        {
            _model.BasicPublish(address, _properties, body);
        }

        private void CreateExchange(IModel model)
        {
            if (!_exchangeCreated && _rabbitMqClientConsumerConfiguration.AutoCreateExchange)
            {
                model.ExchangeDeclare(_rabbitMqClientConsumerConfiguration.LoggingExchangeName, _rabbitMqClientConsumerConfiguration.ExchangeType, true);
                _exchangeCreated = true;
            }
        }
    }
}