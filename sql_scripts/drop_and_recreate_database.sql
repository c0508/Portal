-- Script to completely drop and recreate the ESGPlatform database
-- Run this in TablePlus (connect to master database first)

-- Switch to master database to drop ESGPlatform
USE master;
GO

-- Kill all connections to the database
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'ESGPlatform')
BEGIN
    PRINT 'Dropping ESGPlatform database...';
    
    -- Set database to single user mode to kill connections
    ALTER DATABASE ESGPlatform SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    
    -- Drop the database
    DROP DATABASE ESGPlatform;
    
    PRINT 'ESGPlatform database dropped successfully.';
END
ELSE
BEGIN
    PRINT 'ESGPlatform database does not exist.';
END

-- Create new empty database
PRINT 'Creating new ESGPlatform database...';
CREATE DATABASE ESGPlatform;
GO

PRINT 'ESGPlatform database created successfully!';
PRINT 'You can now run the application and it will create the schema automatically.'; 