-- Add AccentColor column to Organizations table
-- This script manually adds the AccentColor column that should have been added by migration

-- Check if the column already exists before adding it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Organizations]') AND name = 'AccentColor')
BEGIN
    ALTER TABLE [dbo].[Organizations]
    ADD [AccentColor] NVARCHAR(7) NULL;
    
    PRINT 'AccentColor column added successfully to Organizations table';
END
ELSE
BEGIN
    PRINT 'AccentColor column already exists in Organizations table';
END

-- Optionally, set some default accent colors for existing organizations
-- UPDATE [dbo].[Organizations] 
-- SET [AccentColor] = '#007bff' 
-- WHERE [AccentColor] IS NULL; 