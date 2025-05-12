using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Data.Entity;
using Larana.Models;

namespace Larana.Data
{
    public static class DatabaseHelper
    {
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["LaranaConnection"].ConnectionString;
        
        public static void EnsureReviewTableExists()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    
                    // Check if table exists
                    bool tableExists = false;
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Reviews'", connection))
                    {
                        var result = cmd.ExecuteScalar();
                        tableExists = Convert.ToInt32(result) > 0;
                    }
                    
                    if (!tableExists)
                    {
                        // Create table if it doesn't exist
                        string createTableSql = @"
                        CREATE TABLE [dbo].[Reviews] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [ProductId] INT NOT NULL,
                            [UserId] INT NOT NULL,
                            [Rating] INT NOT NULL,
                            [Comment] NVARCHAR(500) NOT NULL,
                            [CreatedAt] DATETIME NOT NULL,
                            CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_Reviews_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]),
                            CONSTRAINT [FK_Reviews_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
                        )";
                        
                        using (var cmd = new SqlCommand(createTableSql, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        
                        // Create indexes
                        string createIndexesSql = @"
                        CREATE INDEX [IX_Reviews_ProductId] ON [dbo].[Reviews]([ProductId]);
                        CREATE INDEX [IX_Reviews_UserId] ON [dbo].[Reviews]([UserId]);";
                        
                        using (var cmd = new SqlCommand(createIndexesSql, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        
                        System.Diagnostics.Debug.WriteLine("Reviews table created successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error creating Reviews table: " + ex.Message);
            }
        }
    }
} 