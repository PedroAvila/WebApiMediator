using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using WebApiMediator.Entities;

namespace WebApiMediator.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(
                    "Debe configurar DB_CONNECTION_STRING o la cadena de conexion CustomerDb.",
                    nameof(connectionString));
            }

            _connectionString = connectionString;
        }

        public async Task<int> CreateAsync(Customer customer)
        {
            const string sqlQuery = @"
                INSERT INTO Customers(Name, Address, Phone)
                VALUES(@Name, @Address, @Phone);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sqlQuery, connection))
            {
                command.Parameters.AddWithValue("@Name", (object)customer.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("@Address", (object)customer.Address ?? DBNull.Value);
                command.Parameters.AddWithValue("@Phone", (object)customer.Phone ?? DBNull.Value);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();

                return Convert.ToInt32(result);
            }
        }
    }
}
