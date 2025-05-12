using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using Larana.Data;

namespace Larana.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            ContextKey = "Larana.Data.ApplicationDbContext";
        }

        protected override void Seed(ApplicationDbContext context)
        {
            // Seed method runs when database is created or migrated
            // Apply foreign key constraint fixes
            var script = @"
                -- Drop existing FK constraint if it exists
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Ratings_dbo.Users_UserId')
                BEGIN
                    ALTER TABLE [dbo].[Ratings] DROP CONSTRAINT [FK_dbo.Ratings_dbo.Users_UserId]
                    ALTER TABLE [dbo].[Ratings] ADD CONSTRAINT [FK_dbo.Ratings_dbo.Users_UserId] 
                    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) 
                    ON DELETE NO ACTION ON UPDATE NO ACTION
                END

                -- Drop existing FK constraint if it exists for DukkanId
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Ratings_dbo.Dukkans_DukkanId')
                BEGIN
                    ALTER TABLE [dbo].[Ratings] DROP CONSTRAINT [FK_dbo.Ratings_dbo.Dukkans_DukkanId]
                    ALTER TABLE [dbo].[Ratings] ADD CONSTRAINT [FK_dbo.Ratings_dbo.Dukkans_DukkanId] 
                    FOREIGN KEY ([DukkanId]) REFERENCES [dbo].[Dukkans] ([Id]) 
                    ON DELETE NO ACTION ON UPDATE NO ACTION
                END
            ";
            
            context.Database.ExecuteSqlCommand(script);
        }
    }
} 