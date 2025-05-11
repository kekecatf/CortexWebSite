using System;
using System.Data.SqlClient;
using System.IO;

namespace DatabaseFixer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Database Fixer Starting...");
            try
            {
                string connectionString = "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=LaranaDB;Integrated Security=True;MultipleActiveResultSets=True";
                string script = File.ReadAllText("FixDatabaseSchema.sql");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(script, connection);
                    command.ExecuteNonQuery();
                    Console.WriteLine("Database fix applied successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
} 