-- Script to mark Entity Framework migrations as applied
-- Run this in TablePlus or SQL Server Management Studio

-- Create the migration history table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END

-- Mark both migrations as applied
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
VALUES ('20250622084216_UpdateQuestionnaireModel', '9.0.6');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
VALUES ('20250622091808_UpdateQuestionnaireAndQuestionModels', '9.0.6');

-- Verify the migration history
SELECT * FROM [__EFMigrationsHistory]; 