using System;
using System.Data.Entity.Migrations;

namespace Larana.Migrations
{
    public partial class CreatePopularityFields : DbMigration
    {
        public override void Up()
        {
            // Add ViewCount, OrderCount columns to Products table
            AddColumn("dbo.Products", "ViewCount", c => c.Int(nullable: false, defaultValue: 0));
            AddColumn("dbo.Products", "OrderCount", c => c.Int(nullable: false, defaultValue: 0));
            AddColumn("dbo.Products", "CreatedAt", c => c.DateTime(nullable: false, defaultValueSql: "GETDATE()"));
            
            // Add ViewCount, OrderCount columns to Dukkans table
            AddColumn("dbo.Dukkans", "ViewCount", c => c.Int(nullable: false, defaultValue: 0));
            AddColumn("dbo.Dukkans", "OrderCount", c => c.Int(nullable: false, defaultValue: 0));
            
            // Create indexes for fast querying
            CreateIndex("dbo.Products", "ViewCount");
            CreateIndex("dbo.Products", "OrderCount");
            CreateIndex("dbo.Products", "CreatedAt");
            CreateIndex("dbo.Dukkans", "ViewCount");
            CreateIndex("dbo.Dukkans", "OrderCount");
            CreateIndex("dbo.Dukkans", "CreatedAt");
        }
        
        public override void Down()
        {
            // Drop indexes
            DropIndex("dbo.Products", new[] { "ViewCount" });
            DropIndex("dbo.Products", new[] { "OrderCount" });
            DropIndex("dbo.Products", new[] { "CreatedAt" });
            DropIndex("dbo.Dukkans", new[] { "ViewCount" });
            DropIndex("dbo.Dukkans", new[] { "OrderCount" });
            DropIndex("dbo.Dukkans", new[] { "CreatedAt" });
            
            // Drop columns
            DropColumn("dbo.Products", "ViewCount");
            DropColumn("dbo.Products", "OrderCount");
            DropColumn("dbo.Products", "CreatedAt");
            DropColumn("dbo.Dukkans", "ViewCount");
            DropColumn("dbo.Dukkans", "OrderCount");
        }
    }
} 