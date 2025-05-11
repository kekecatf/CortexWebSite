using System;
using System.Data.Entity.Migrations;

namespace Larana.Migrations
{
    public partial class FixForeignKeyConstraints : DbMigration
    {
        public override void Up()
        {
            // Drop existing FK constraint if it exists
            Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Ratings_dbo.Users_UserId')
                BEGIN
                    ALTER TABLE [dbo].[Ratings] DROP CONSTRAINT [FK_dbo.Ratings_dbo.Users_UserId]
                END
            ");

            // Recreate the constraint with NO ACTION for delete and update
            Sql(@"
                ALTER TABLE [dbo].[Ratings] 
                ADD CONSTRAINT [FK_dbo.Ratings_dbo.Users_UserId] 
                FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) 
                ON DELETE NO ACTION ON UPDATE NO ACTION
            ");

            // Drop existing FK constraint if it exists
            Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Ratings_dbo.Dukkans_DukkanId')
                BEGIN
                    ALTER TABLE [dbo].[Ratings] DROP CONSTRAINT [FK_dbo.Ratings_dbo.Dukkans_DukkanId]
                END
            ");

            // Recreate the constraint with NO ACTION for delete and update
            Sql(@"
                ALTER TABLE [dbo].[Ratings] 
                ADD CONSTRAINT [FK_dbo.Ratings_dbo.Dukkans_DukkanId] 
                FOREIGN KEY ([DukkanId]) REFERENCES [dbo].[Dukkans] ([Id]) 
                ON DELETE NO ACTION ON UPDATE NO ACTION
            ");
        }

        public override void Down()
        {
            // Restore the original cascade behavior if needed
            // Not implementing this as it would reintroduce the circular reference
        }
    }
} 