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

            return new CreateCustomerResult(
                id,
                customer.Name,
                customer.Address,
                customer.Phone);
        }
    }
}
