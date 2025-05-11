-- Close all existing connections to the database
USE [master]
GO
ALTER DATABASE [LaranaDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
GO

-- Drop the database if it exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'LaranaDB')
BEGIN
    DROP DATABASE [LaranaDB]
    PRINT 'LaranaDB dropped successfully.'
END
GO

-- Create a new clean database
CREATE DATABASE [LaranaDB]
GO

PRINT 'LaranaDB created successfully. Run your application now to initialize the schema.' 