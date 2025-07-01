-- Script to check and fix QuestionnaireVersions table structure
-- Run this in TablePlus

-- Check current QuestionnaireVersions table structure
PRINT 'Current QuestionnaireVersions table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'QuestionnaireVersions'
ORDER BY ORDINAL_POSITION;

-- Check if VersionNumber is int (it should be nvarchar)
SELECT DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'QuestionnaireVersions' AND COLUMN_NAME = 'VersionNumber';

-- Fix the VersionNumber column if it's int instead of nvarchar
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'QuestionnaireVersions' 
           AND COLUMN_NAME = 'VersionNumber' 
           AND DATA_TYPE = 'int')
BEGIN
    PRINT 'Fixing VersionNumber column type from int to nvarchar(20)';
    
    -- First, check if there are any existing records
    IF EXISTS (SELECT 1 FROM QuestionnaireVersions)
    BEGIN
        PRINT 'Warning: QuestionnaireVersions table has data. Converting existing int values to strings.';
        
        -- Add a temporary column
        ALTER TABLE QuestionnaireVersions ADD VersionNumber_temp nvarchar(20);
        
        -- Copy data with conversion
        UPDATE QuestionnaireVersions SET VersionNumber_temp = CAST(VersionNumber AS nvarchar(20));
        
        -- Drop the old column
        ALTER TABLE QuestionnaireVersions DROP COLUMN VersionNumber;
        
        -- Rename the temp column
        EXEC sp_rename 'QuestionnaireVersions.VersionNumber_temp', 'VersionNumber', 'COLUMN';
        
        -- Make it NOT NULL
        ALTER TABLE QuestionnaireVersions ALTER COLUMN VersionNumber nvarchar(20) NOT NULL;
    END
    ELSE
    BEGIN
        PRINT 'No existing data. Simply changing column type.';
        ALTER TABLE QuestionnaireVersions ALTER COLUMN VersionNumber nvarchar(20) NOT NULL;
    END
END
ELSE
BEGIN
    PRINT 'VersionNumber column is already the correct type (nvarchar)';
END

-- Final verification
PRINT 'Updated QuestionnaireVersions table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'QuestionnaireVersions'
ORDER BY ORDINAL_POSITION; 