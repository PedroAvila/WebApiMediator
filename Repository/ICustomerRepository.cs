using System.Threading.Tasks;
using WebApiMediator.Entities;

namespace WebApiMediator.Repository
{
    public interface ICustomerRepository
    {
        Task<int> CreateAsync(Customer customer);
    }
}
