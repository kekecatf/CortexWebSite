using System;
using System.Data.Entity;
using System.Linq;
using Larana.Models;

namespace Larana.Data
{
    public static class ReviewTableInitializer
    {
        public static void Initialize()
        {
            using (var context = new ApplicationDbContext())
            {
                // Check if the table already exists
                if (!TableExists(context, "Reviews"))
                {
                    // Create the table manually using SQL
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
                    
                    // Create indexes
                    string createIndexesSql = @"
                    CREATE INDEX [IX_Reviews_ProductId] ON [dbo].[Reviews]([ProductId]);
                    CREATE INDEX [IX_Reviews_UserId] ON [dbo].[Reviews]([UserId]);";
                    
                    try
                    {
                        context.Database.ExecuteSqlCommand(createTableSql);
                        context.Database.ExecuteSqlCommand(createIndexesSql);
                        
                        // Log success
                        System.Diagnostics.Debug.WriteLine("Reviews table created successfully.");
                    }
                    catch (Exception ex)
                    {
                        // Log error
                        System.Diagnostics.Debug.WriteLine("Error creating Reviews table: " + ex.Message);
                    }
                }
            }
        }
        
        // Helper method to check if a table exists
        private static bool TableExists(DbContext context, string tableName)
        {
            var result = context.Database.SqlQuery<int>(
                $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'").ToList();
            return result.Count > 0 && result[0] > 0;
        }
    }
} 