using System;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        var connectionString = "Server=136.110.27.218;Database=IELTSWebApplication;User Id=sqlserver;Password=ielts12345;TrustServerCertificate=True;Encrypt=False;";
        using var connection = new SqlConnection(connectionString);

        try
        {
            connection.Open();
            Console.WriteLine("✅ Connected successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Connection failed: " + ex.Message);
        }
    }
}
