IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OrderDetails]') AND name = 'ShopProductId')
BEGIN
    ALTER TABLE [dbo].[OrderDetails]
    ADD [ShopProductId] INT NULL
END 