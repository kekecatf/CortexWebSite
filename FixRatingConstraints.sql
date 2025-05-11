-- Check if Ratings table exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Ratings]') AND type in (N'U'))
BEGIN
    PRINT 'Ratings table exists, proceeding with constraint fixes'
    
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
    
    PRINT 'Rating constraints fixed successfully'
END
ELSE
BEGIN
    PRINT 'Ratings table does not exist. No changes made.'
END 