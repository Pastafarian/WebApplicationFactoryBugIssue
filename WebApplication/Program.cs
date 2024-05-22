using WebApplication.Services;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Add services to the container.
var rabbitMqConfig = builder.Configuration.GetSection("RabbitMq") ?? throw new InvalidOperationException("RabbitMq Configuration Null");

var rabbitMqClientConsumerConfiguration = new RabbitMqClientConsumerConfiguration();

rabbitMqConfig.Bind(rabbitMqClientConsumerConfiguration);
builder.Services.Configure<List<string>>(_ =>
    rabbitMqClientConsumerConfiguration.Hostnames = rabbitMqConfig.GetSection("Hostnames").Get<List<string>>() ?? throw new InvalidOperationException("RabbitMq Configuration Null"));
builder.Services.AddSingleton(rabbitMqClientConsumerConfiguration);
builder.Services.AddSingleton<IRabbitConnectionFactory, RabbitConnectionFactory>();
builder.Services.AddSingleton<IAsyncEventingBasicConsumerFactory, AsyncEventingBasicConsumerFactory>();
builder.Services.AddScoped<IMessage, MessageDefault>();
//builder.Services.AddSingleton<WorkerService>();
builder.Services.AddHostedService<WorkerService>();
//builder.Services.AddSingleton<WorkerService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

namespace WebApplication
{
    public partial class Program { }
}