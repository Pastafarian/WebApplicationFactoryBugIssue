using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;


namespace WebApplication.IntTest;

public class ConsumerWebApplicationFactory(Func<IServiceCollection, bool>? registerCustomIoc) : WebApplicationFactory<Program>
{
    public Func<IServiceCollection, bool>? RegisterCustomIoc { get; } = registerCustomIoc;


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
            {
                if (RegisterCustomIoc != null)
                {
                    RegisterCustomIoc(services);
                }
            }
        );

        //builder.UseEnvironment("consumer");
    }

    //protected override IHost CreateHost(IHostBuilder builder)
    //{
    //    builder.ConfigureHostConfiguration(config =>
    //    {
    //        var configuration = new ConfigurationBuilder()
    //            .AddJsonFile("appsettings.consumer.json")
    //            .Build();

    //        config.AddConfiguration(configuration);
    //    });

    //    return base.CreateHost(builder);
    //}

}