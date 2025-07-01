-- Comprehensive script to add all missing columns
-- Run this in TablePlus after marking migrations as applied

-- First, let's see what we're working with
PRINT 'Current Questions table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Questions'
ORDER BY ORDINAL_POSITION;

PRINT 'Current QuestionnaireVersions table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'QuestionnaireVersions'
ORDER BY ORDINAL_POSITION;

-- Add missing columns to Questions table one by one

-- 1. Add DisplayOrder
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'DisplayOrder')
BEGIN
    PRINT 'Adding DisplayOrder column to Questions table';
    ALTER TABLE Questions ADD DisplayOrder int NOT NULL DEFAULT 0;
END
ELSE
BEGIN
    PRINT 'DisplayOrder column already exists in Questions table';
END

-- 2. Add HelpText
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'HelpText')
BEGIN
    PRINT 'Adding HelpText column to Questions table';
    ALTER TABLE Questions ADD HelpText nvarchar(1000) NULL;
END
ELSE
BEGIN
    PRINT 'HelpText column already exists in Questions table';
END

-- 3. Add OrganizationId
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'OrganizationId')
BEGIN
    PRINT 'Adding OrganizationId column to Questions table';
    ALTER TABLE Questions ADD OrganizationId int NOT NULL DEFAULT 1; -- Default to first organization
END
ELSE
BEGIN
    PRINT 'OrganizationId column already exists in Questions table';
END

-- 4. Add QuestionType (this might be the issue - enum values)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'QuestionType')
BEGIN
    PRINT 'Adding QuestionType column to Questions table';
    ALTER TABLE Questions ADD QuestionType int NOT NULL DEFAULT 0; -- 0 = Text type
END
ELSE
BEGIN
    PRINT 'QuestionType column already exists in Questions table';
END

-- 5. Add UpdatedAt
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'UpdatedAt')
BEGIN
    PRINT 'Adding UpdatedAt column to Questions table';
    ALTER TABLE Questions ADD UpdatedAt datetime2 NULL;
END
ELSE
BEGIN
    PRINT 'UpdatedAt column already exists in Questions table';
END

-- 6. Add ValidationRules
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'ValidationRules')
BEGIN
    PRINT 'Adding ValidationRules column to Questions table';
    ALTER TABLE Questions ADD ValidationRules nvarchar(max) NULL;
END
ELSE
BEGIN
    PRINT 'ValidationRules column already exists in Questions table';
END

-- Now add missing columns to QuestionnaireVersions table

-- 7. Add ChangeDescription to QuestionnaireVersions
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QuestionnaireVersions' AND COLUMN_NAME = 'ChangeDescription')
BEGIN
    PRINT 'Adding ChangeDescription column to QuestionnaireVersions table';
    ALTER TABLE QuestionnaireVersions ADD ChangeDescription nvarchar(1000) NULL;
END
ELSE
BEGIN
    PRINT 'ChangeDescription column already exists in QuestionnaireVersions table';
END

-- 8. Add CreatedByUserId to QuestionnaireVersions
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QuestionnaireVersions' AND COLUMN_NAME = 'CreatedByUserId')
BEGIN
    PRINT 'Adding CreatedByUserId column to QuestionnaireVersions table';
    -- First add the column as nullable
    ALTER TABLE QuestionnaireVersions ADD CreatedByUserId nvarchar(450) NULL;
    
    -- Update existing records with a default user (platform admin)
    UPDATE QuestionnaireVersions 
    SET CreatedByUserId = (SELECT TOP 1 Id FROM Users WHERE Email = 'admin@esgplatform.com')
    WHERE CreatedByUserId IS NULL;
    
    -- Now make it NOT NULL
    ALTER TABLE QuestionnaireVersions ALTER COLUMN CreatedByUserId nvarchar(450) NOT NULL;
END
ELSE
BEGIN
    PRINT 'CreatedByUserId column already exists in QuestionnaireVersions table';
END

-- Add foreign key constraints if they don't exist

-- Foreign key for Questions.OrganizationId
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Questions_Organizations_OrganizationId')
BEGIN
    PRINT 'Adding foreign key constraint FK_Questions_Organizations_OrganizationId';
    ALTER TABLE Questions 
    ADD CONSTRAINT FK_Questions_Organizations_OrganizationId 
    FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id);
END

-- Foreign key for QuestionnaireVersions.CreatedByUserId
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QuestionnaireVersions_Users_CreatedByUserId')
BEGIN
    PRINT 'Adding foreign key constraint FK_QuestionnaireVersions_Users_CreatedByUserId';
    ALTER TABLE QuestionnaireVersions 
    ADD CONSTRAINT FK_QuestionnaireVersions_Users_CreatedByUserId 
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id);
END

-- Final verification
PRINT 'Updated Questions table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Questions'
ORDER BY ORDINAL_POSITION;

PRINT 'Updated QuestionnaireVersions table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'QuestionnaireVersions'
ORDER BY ORDINAL_POSITION;

PRINT 'Script completed successfully!'; 