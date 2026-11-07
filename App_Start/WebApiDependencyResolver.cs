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

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _serviceProvider.GetServices(serviceType);
        }

        public IDependencyScope BeginScope()
        {
            if (_scope != null)
            {
                return this;
            }

            return new WebApiDependencyResolver(_serviceProvider.CreateScope());
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}
