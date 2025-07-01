-- Comprehensive script to fix database schema issues
-- Run this in TablePlus

PRINT 'Starting database schema fixes...';

-- ==============================================
-- Fix QuestionnaireVersions table
-- ==============================================

PRINT 'Fixing QuestionnaireVersions table...';

-- 1. Fix VersionNumber column type (int -> nvarchar)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'QuestionnaireVersions' 
           AND COLUMN_NAME = 'VersionNumber' 
           AND DATA_TYPE = 'int')
BEGIN
    PRINT 'Converting VersionNumber from int to nvarchar(20)';
    
    -- Add temporary column
    ALTER TABLE QuestionnaireVersions ADD VersionNumber_New nvarchar(20);
    
    -- Copy data with conversion (convert int to string)
    UPDATE QuestionnaireVersions SET VersionNumber_New = CAST(VersionNumber AS nvarchar(20)) + '.0';
    
    -- Drop old column
    ALTER TABLE QuestionnaireVersions DROP COLUMN VersionNumber;
    
    -- Rename new column
    EXEC sp_rename 'QuestionnaireVersions.VersionNumber_New', 'VersionNumber', 'COLUMN';
    
    -- Make it NOT NULL
    ALTER TABLE QuestionnaireVersions ALTER COLUMN VersionNumber nvarchar(20) NOT NULL;
END

-- 2. Remove duplicate CreatedById column (keep CreatedByUserId)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'QuestionnaireVersions' 
           AND COLUMN_NAME = 'CreatedById')
BEGIN
    PRINT 'Removing duplicate CreatedById column from QuestionnaireVersions';
    
    -- Drop foreign key constraint if exists
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QuestionnaireVersions_Users_CreatedById')
        ALTER TABLE QuestionnaireVersions DROP CONSTRAINT FK_QuestionnaireVersions_Users_CreatedById;
    
    -- Drop the duplicate column
    ALTER TABLE QuestionnaireVersions DROP COLUMN CreatedById;
END

-- 3. Remove unnecessary columns that EF doesn't expect
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QuestionnaireVersions' AND COLUMN_NAME = 'VersionName')
    ALTER TABLE QuestionnaireVersions DROP COLUMN VersionName;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QuestionnaireVersions' AND COLUMN_NAME = 'IsActive')
    ALTER TABLE QuestionnaireVersions DROP COLUMN IsActive;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QuestionnaireVersions' AND COLUMN_NAME = 'IsLocked')
    ALTER TABLE QuestionnaireVersions DROP COLUMN IsLocked;

-- ==============================================
-- Fix Questions table
-- ==============================================

PRINT 'Fixing Questions table...';

-- 1. Remove duplicate Type column (keep QuestionType)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'Type')
BEGIN
    PRINT 'Removing duplicate Type column from Questions (keeping QuestionType)';
    
    -- Copy data from Type to QuestionType if QuestionType is empty/default
    UPDATE Questions SET QuestionType = Type WHERE QuestionType = 0 OR QuestionType IS NULL;
    
    -- Drop the duplicate column
    ALTER TABLE Questions DROP COLUMN Type;
END

-- 2. Remove duplicate OrderIndex column (keep DisplayOrder)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'OrderIndex')
BEGIN
    PRINT 'Removing duplicate OrderIndex column from Questions (keeping DisplayOrder)';
    
    -- Copy data from OrderIndex to DisplayOrder if DisplayOrder is empty/default
    UPDATE Questions SET DisplayOrder = OrderIndex WHERE DisplayOrder = 0 OR DisplayOrder IS NULL;
    
    -- Drop the duplicate column
    ALTER TABLE Questions DROP COLUMN OrderIndex;
END

-- 3. Remove QuestionnaireVersionId column (we use QuestionnaireId directly)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'QuestionnaireVersionId')
BEGIN
    PRINT 'Removing QuestionnaireVersionId column from Questions';
    
    -- Drop foreign key constraint if exists
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Questions_QuestionnaireVersions_QuestionnaireVersionId')
        ALTER TABLE Questions DROP CONSTRAINT FK_Questions_QuestionnaireVersions_QuestionnaireVersionId;
    
    -- Drop the column
    ALTER TABLE Questions DROP COLUMN QuestionnaireVersionId;
END

-- 4. Remove unnecessary columns that EF doesn't expect
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'Description')
    ALTER TABLE Questions DROP COLUMN Description;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'ConditionalLogic')
    ALTER TABLE Questions DROP COLUMN ConditionalLogic;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'StandardVariableCode')
    ALTER TABLE Questions DROP COLUMN StandardVariableCode;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'IsActive')
    ALTER TABLE Questions DROP COLUMN IsActive;

-- ==============================================
-- Add missing foreign key constraints
-- ==============================================

PRINT 'Adding missing foreign key constraints...';

-- QuestionnaireVersions.CreatedByUserId -> Users.Id
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QuestionnaireVersions_Users_CreatedByUserId')
BEGIN
    PRINT 'Adding FK_QuestionnaireVersions_Users_CreatedByUserId constraint';
    ALTER TABLE QuestionnaireVersions 
    ADD CONSTRAINT FK_QuestionnaireVersions_Users_CreatedByUserId 
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id);
END

-- Questions.QuestionnaireId -> Questionnaires.Id
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Questions_Questionnaires_QuestionnaireId')
BEGIN
    PRINT 'Adding FK_Questions_Questionnaires_QuestionnaireId constraint';
    ALTER TABLE Questions 
    ADD CONSTRAINT FK_Questions_Questionnaires_QuestionnaireId 
    FOREIGN KEY (QuestionnaireId) REFERENCES Questionnaires(Id) ON DELETE CASCADE;
END

-- Questions.OrganizationId -> Organizations.Id
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Questions_Organizations_OrganizationId')
BEGIN
    PRINT 'Adding FK_Questions_Organizations_OrganizationId constraint';
    ALTER TABLE Questions 
    ADD CONSTRAINT FK_Questions_Organizations_OrganizationId 
    FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id);
END

-- ==============================================
-- Final verification
-- ==============================================

PRINT 'Schema fixes completed. Verifying final structure...';

PRINT 'Final QuestionnaireVersions structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'QuestionnaireVersions'
ORDER BY ORDINAL_POSITION;

PRINT 'Final Questions structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Questions'
ORDER BY ORDINAL_POSITION;

PRINT 'Database schema fixes completed successfully!'; 