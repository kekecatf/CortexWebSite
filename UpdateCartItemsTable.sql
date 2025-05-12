-- Add new columns to CartItems table
ALTER TABLE dbo.CartItems ADD ShopProductId INT NULL;
ALTER TABLE dbo.CartItems ADD DukkanId INT NULL;
ALTER TABLE dbo.CartItems ADD UnitPrice DECIMAL(18, 2) NOT NULL DEFAULT 0;

-- Add foreign keys for the new columns
ALTER TABLE dbo.CartItems ADD CONSTRAINT FK_CartItems_ShopProducts FOREIGN KEY (ShopProductId) REFERENCES dbo.ShopProducts(Id);
ALTER TABLE dbo.CartItems ADD CONSTRAINT FK_CartItems_Dukkans FOREIGN KEY (DukkanId) REFERENCES dbo.Dukkans(Id);

-- Update UnitPrice from Product price for existing cart items
UPDATE ci
SET ci.UnitPrice = p.Price
FROM dbo.CartItems ci
JOIN dbo.Products p ON ci.ProductId = p.Id
WHERE ci.UnitPrice = 0; 