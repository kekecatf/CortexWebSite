-- Drop existing FK constraint if it exists
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Ratings_dbo.Users_UserId')
BEGIN
    ALTER TABLE [dbo].[Ratings] DROP CONSTRAINT [FK_dbo.Ratings_dbo.Users_UserId]
    ALTER TABLE [dbo].[Ratings] ADD CONSTRAINT [FK_dbo.Ratings_dbo.Users_UserId] 
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) 
    ON DELETE NO ACTION ON UPDATE NO ACTION
END

-- Drop existing FK constraint if it exists
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Ratings_dbo.Dukkans_DukkanId')
BEGIN
    ALTER TABLE [dbo].[Ratings] DROP CONSTRAINT [FK_dbo.Ratings_dbo.Dukkans_DukkanId]
    ALTER TABLE [dbo].[Ratings] ADD CONSTRAINT [FK_dbo.Ratings_dbo.Dukkans_DukkanId] 
    FOREIGN KEY ([DukkanId]) REFERENCES [dbo].[Dukkans] ([Id]) 
    ON DELETE NO ACTION ON UPDATE NO ACTION
END 