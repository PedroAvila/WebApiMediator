# WebApiMediator

Uso de la librería **PAN.Mediator 2.0.0** en el proyecto **WebApiMediator** (ASP.NET Web API 2, .NET Framework 4.8).

---

# PAN.Mediator — Documentación de uso en WebApiMediator

PAN.Mediator es un mediador inmutable y ligero para CQRS y Arquitectura Hexagonal en .NET. Es un reemplazo hecho desde cero de MediatR con paridad de superficie (`Send`/`Publish`, comportamientos de pipeline, `Unit`, extensión `AddPanMediator` para DI).

En este proyecto se consume desde **WebApiMediator**, una aplicación ASP.NET Web API 2 que apunta a .NET Framework 4.8 y referencia `PAN.Mediator 2.0.0` (ensamblado `netstandard2.0`).

## 1. Instalación del paquete

En `packages.config` de WebApiMediator:

```xml
<package id="PAN.Mediator" version="2.0.0" targetFramework="net48" />
<package id="Microsoft.Extensions.DependencyInjection" version="6.0.0" targetFramework="net48" />
<package id="Microsoft.Extensions.DependencyInjection.Abstractions" version="6.0.0" targetFramework="net48" />
```

En `WebApiMediator.csproj` se referencia el ensamblado directamente desde la carpeta de paquetes locales:

```xml
<Reference Include="PAN.Mediator, Version=2.0.0.0, Culture=neutral, processorArchitecture=MSIL">
  <HintPath>..\PruebaMediatorNetFramework\packages\PAN.Mediator.2.0.0\lib\netstandard2.0\PAN.Mediator.dll</HintPath>
</Reference>
```

## 2. Registro del mediador al iniciar

`App_Start/MediatorConfig.cs` se invoca desde `Global.asax.cs` (`Application_Start`):

```csharp
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
```

`RegisterServicesFromAssembly` escanea el ensamblado y registra automáticamente toda clase concreta que implemente `IPanRequestHandler<,>` o `IPanNotificationHandler<>`. Los controladores se registran como transitorios para que Web API los resuelva por inyección de constructor.

## 3. Puente de Web API 2 a Microsoft.Extensions.DependencyInjection

`App_Start/WebApiDependencyResolver.cs` adapta el `IServiceProvider` como `IDependencyResolver` de Web API 2, creando un alcance por solicitud:

```csharp
using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiMediator
{
    public sealed class WebApiDependencyResolver : IDependencyResolver
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScope _scope;

        public WebApiDependencyResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider
                ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private WebApiDependencyResolver(IServiceScope scope)
        {
            _scope = scope;
            _serviceProvider = scope.ServiceProvider;
        }

        public object GetService(Type serviceType) =>
            _serviceProvider.GetService(serviceType);

        public IEnumerable<object> GetServices(Type serviceType) =>
            _serviceProvider.GetServices(serviceType);

        public IDependencyScope BeginScope() =>
            _scope == null
                ? new WebApiDependencyResolver(_serviceProvider.CreateScope())
                : this;

        public void Dispose() => _scope?.Dispose();
    }
}
```

## 4. Definir una solicitud y su handler

`Features/Customers/CreateCustomer.cs`:

```csharp
using System.Threading;
using System.Threading.Tasks;
using PAN.Mediator.Abstractions;
using WebApiMediator.Entities;
using WebApiMediator.Repository;

namespace WebApiMediator.Features.Customers
{
    public class CreateCustomerCommand : IPanRequest<CreateCustomerResult>
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
    }

    public class CreateCustomerResult
    {
        public CreateCustomerResult(int id, string name, string address, string phone)
        {
            Id = id;
            Name = name;
            Address = address;
            Phone = phone;
        }

        public int Id { get; }
        public string Name { get; }
        public string Address { get; }
        public string Phone { get; }
    }

    public class CreateCustomerCommandHandler
        : IPanRequestHandler<CreateCustomerCommand, CreateCustomerResult>
    {
        private readonly ICustomerRepository _customerRepository;

        public CreateCustomerCommandHandler(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async ValueTask<CreateCustomerResult> Handle(
            CreateCustomerCommand command,
            CancellationToken cancellationToken)
        {
            var customer = new Customer
            {
                Name = command.Name,
                Address = command.Address,
                Phone = command.Phone,
            };

            var id = await _customerRepository.CreateAsync(customer);

            return new CreateCustomerResult(id, customer.Name, customer.Address, customer.Phone);
        }
    }
}
```

Nota: los handlers retornan `ValueTask`/`ValueTask<T>` (convención de PAN.Mediator), no `Task`/`Task<T>`.

## 5. Enviar desde un controlador

`Controllers/CustomerController.cs`:

```csharp
using System.Threading.Tasks;
using System.Web.Http;
using PAN.Mediator.Abstractions;
using WebApiMediator.Features.Customers;

namespace WebApiMediator.Controllers
{
    [RoutePrefix("api/Customer")]
    public class CustomerController : ApiController
    {
        private readonly IPanMediator _mediator;

        public CustomerController(IPanMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create([FromBody] CreateCustomerCommand dto)
        {
            var result = await _mediator.Send(dto);
            return Created(string.Empty, result);
        }
    }
}
```

El controlador recibe `IPanMediator` por inyección de constructor (resuelto por `WebApiDependencyResolver`) y llama a `mediator.Send(dto)`.

## 6. Resultado

`POST api/Customer` con cuerpo:

```json
{
  "Name": "...",
  "Address": "...",
  "Phone": "..."
}
```

ejecuta `CreateCustomerCommandHandler`, que inserta el cliente en SQL Server a través de `CustomerRepository` y devuelve `201 Created` con el `CreateCustomerResult`.

---

### Archivos clave del proyecto WebApiMediator

| Archivo | Ruta | Propósito |
|---|---|---|
| Registro DI | `App_Start\MediatorConfig.cs` | `AddPanMediator`, registro de handlers y controladores |
| Adaptador DI | `App_Start\WebApiDependencyResolver.cs` | Puente Web API 2 → `Microsoft.Extensions.DependencyInjection` |
| Comando y handler | `Features\Customers\CreateCustomer.cs` | `CreateCustomerCommand`, `CreateCustomerResult`, handler |
| Controlador | `Controllers\CustomerController.cs` | `POST api/Customer`, llama a `mediator.Send` |
| Repositorio | `Repository\CustomerRepository.cs` | Acceso a SQL Server (`ICustomerRepository`) |
| Entidad | `Entities\Customer.cs` | Entidad `Customer` |
| Paquetes | `packages.config` | `PAN.Mediator 2.0.0`, `Microsoft.Extensions.DependencyInjection 6.0.0` |
