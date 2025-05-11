-- Disable foreign key checks
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'

-- Drop existing FK constraint if it exists for Ratings to Users
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Ratings_dbo.Users_UserId')
BEGIN
    ALTER TABLE [dbo].[Ratings] DROP CONSTRAINT [FK_dbo.Ratings_dbo.Users_UserId]
    PRINT 'Dropped FK_dbo.Ratings_dbo.Users_UserId constraint'
END

-- Recreate the constraint with NO ACTION for delete and update
ALTER TABLE [dbo].[Ratings] 
ADD CONSTRAINT [FK_dbo.Ratings_dbo.Users_UserId] 
FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) 
ON DELETE NO ACTION ON UPDATE NO ACTION
PRINT 'Created FK_dbo.Ratings_dbo.Users_UserId constraint with NO ACTION'

-- Drop existing FK constraint if it exists for Ratings to Dukkans
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Ratings_dbo.Dukkans_DukkanId')
BEGIN
    ALTER TABLE [dbo].[Ratings] DROP CONSTRAINT [FK_dbo.Ratings_dbo.Dukkans_DukkanId]
    PRINT 'Dropped FK_dbo.Ratings_dbo.Dukkans_DukkanId constraint'
END

-- Recreate the constraint with NO ACTION for delete and update
ALTER TABLE [dbo].[Ratings] 
ADD CONSTRAINT [FK_dbo.Ratings_dbo.Dukkans_DukkanId] 
FOREIGN KEY ([DukkanId]) REFERENCES [dbo].[Dukkans] ([Id]) 
ON DELETE NO ACTION ON UPDATE NO ACTION
PRINT 'Created FK_dbo.Ratings_dbo.Dukkans_DukkanId constraint with NO ACTION'

-- Re-enable foreign key checks
EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL'
PRINT 'Re-enabled all constraints'

-- Print success message
PRINT 'Database schema updated successfully' 