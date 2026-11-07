using System;
using System.Configuration;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using Microsoft.Extensions.DependencyInjection;
using PAN.Mediator.DependencyInjection;
using WebApiMediator.Repository;

namespace WebApiMediator
{
    public static class MediatorConfig
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        public static void Register()
        {
            var services = new ServiceCollection();

            services.AddPanMediator(config =>
            {
                config.RegisterServicesFromAssembly(typeof(WebApiApplication).Assembly);
            });

            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var connectionStringSettings = ConfigurationManager.ConnectionStrings["CustomerDb"];
                connectionString = connectionStringSettings?.ConnectionString;
            }

            services.AddTransient<ICustomerRepository>(
                _ => new CustomerRepository(connectionString));

            foreach (var controllerType in typeof(WebApiApplication).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && typeof(IHttpController).IsAssignableFrom(type)))
            {
                services.AddTransient(controllerType);
            }

            ServiceProvider = services.BuildServiceProvider();

            GlobalConfiguration.Configuration.DependencyResolver =
                new WebApiDependencyResolver(ServiceProvider);
        }
    }
}
