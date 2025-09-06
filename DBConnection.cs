using Microsoft.Data.SqlClient;

namespace Donation_Website
{
    public class DBConnection
    {

        private const string connectionString = @"Server=USER\SARAR;Initial Catalog=DonationManagementDB;Integrated Security=True;Encrypt=False;Trust Server Certificate=True";

        public SqlCommand GetQuery(string query)
            {
                var connection = new SqlConnection(connectionString);
                SqlCommand cmd = new SqlCommand(query, connection);

                return cmd;
            }

        

    }
}
