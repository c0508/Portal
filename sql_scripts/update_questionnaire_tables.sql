-- Script to manually update Questionnaire and Question tables
-- Run this in TablePlus or SQL Server Management Studio AFTER running mark_migrations_applied.sql

-- First, let's check current table structures
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Questionnaires'
ORDER BY ORDINAL_POSITION;

SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Questions'
ORDER BY ORDINAL_POSITION;

-- Update Questionnaires table
-- Add missing columns if they don't exist

-- Check and add Title column (rename from Name if it exists)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questionnaires' AND COLUMN_NAME = 'Name')
BEGIN
    -- Rename Name to Title if Name exists
    EXEC sp_rename 'Questionnaires.Name', 'Title', 'COLUMN';
END
ELSE IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questionnaires' AND COLUMN_NAME = 'Title')
BEGIN
    ALTER TABLE Questionnaires ADD Title nvarchar(200) NOT NULL DEFAULT '';
END

-- Add Category column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questionnaires' AND COLUMN_NAME = 'Category')
BEGIN
    ALTER TABLE Questionnaires ADD Category nvarchar(100) NOT NULL DEFAULT 'General';
END

-- Add CreatedByUserId column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questionnaires' AND COLUMN_NAME = 'CreatedByUserId')
BEGIN
    ALTER TABLE Questionnaires ADD CreatedByUserId nvarchar(450) NOT NULL DEFAULT '';
    
    -- Add foreign key constraint
    ALTER TABLE Questionnaires 
    ADD CONSTRAINT FK_Questionnaires_Users_CreatedByUserId 
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id);
END

-- Update Questions table
-- Add missing columns if they don't exist

-- Check and add QuestionText column (rename from Text if it exists)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'Text')
BEGIN
    -- Rename Text to QuestionText if Text exists
    EXEC sp_rename 'Questions.Text', 'QuestionText', 'COLUMN';
END
ELSE IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'QuestionText')
BEGIN
    ALTER TABLE Questions ADD QuestionText nvarchar(500) NOT NULL DEFAULT '';
END

-- Add QuestionnaireId column (direct relationship instead of through versions)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'QuestionnaireId')
BEGIN
    ALTER TABLE Questions ADD QuestionnaireId int NOT NULL DEFAULT 0;
    
    -- Add foreign key constraint
    ALTER TABLE Questions 
    ADD CONSTRAINT FK_Questions_Questionnaires_QuestionnaireId 
    FOREIGN KEY (QuestionnaireId) REFERENCES Questionnaires(Id) ON DELETE CASCADE;
END

-- Add DisplayOrder column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'DisplayOrder')
BEGIN
    ALTER TABLE Questions ADD DisplayOrder int NOT NULL DEFAULT 0;
END

-- Add HelpText column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'HelpText')
BEGIN
    ALTER TABLE Questions ADD HelpText nvarchar(1000) NULL;
END

-- Add OrganizationId column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'OrganizationId')
BEGIN
    ALTER TABLE Questions ADD OrganizationId int NOT NULL DEFAULT 0;
    
    -- Add foreign key constraint
    ALTER TABLE Questions 
    ADD CONSTRAINT FK_Questions_Organizations_OrganizationId 
    FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id);
END

-- Add UpdatedAt column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'UpdatedAt')
BEGIN
    ALTER TABLE Questions ADD UpdatedAt datetime2 NULL;
END

-- Add ValidationRules column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'ValidationRules')
BEGIN
    ALTER TABLE Questions ADD ValidationRules nvarchar(max) NULL;
END

-- Update QuestionnaireVersions table
-- Add ChangeDescription column (rename from ChangeNotes if it exists)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QuestionnaireVersions' AND COLUMN_NAME = 'ChangeNotes')
BEGIN
    -- Rename ChangeNotes to ChangeDescription if ChangeNotes exists
    EXEC sp_rename 'QuestionnaireVersions.ChangeNotes', 'ChangeDescription', 'COLUMN';
END
ELSE IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QuestionnaireVersions' AND COLUMN_NAME = 'ChangeDescription')
BEGIN
    ALTER TABLE QuestionnaireVersions ADD ChangeDescription nvarchar(1000) NULL;
END

-- Add CreatedByUserId to QuestionnaireVersions
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QuestionnaireVersions' AND COLUMN_NAME = 'CreatedByUserId')
BEGIN
    ALTER TABLE QuestionnaireVersions ADD CreatedByUserId nvarchar(450) NOT NULL DEFAULT '';
    
    -- Add foreign key constraint
    ALTER TABLE QuestionnaireVersions 
    ADD CONSTRAINT FK_QuestionnaireVersions_Users_CreatedByUserId 
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id);
END

-- Final verification - show updated table structures
PRINT 'Updated Questionnaires table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Questionnaires'
ORDER BY ORDINAL_POSITION;

PRINT 'Updated Questions table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Questions'
ORDER BY ORDINAL_POSITION; 