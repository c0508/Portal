-- Script to drop and recreate Questionnaire and Question tables with correct structure
-- Run this in TablePlus

-- Drop foreign key constraints first
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Questions_Questionnaires_QuestionnaireId')
    ALTER TABLE Questions DROP CONSTRAINT FK_Questions_Questionnaires_QuestionnaireId;

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Questions_QuestionnaireVersions_QuestionnaireVersionId')
    ALTER TABLE Questions DROP CONSTRAINT FK_Questions_QuestionnaireVersions_QuestionnaireVersionId;

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Questions_Organizations_OrganizationId')
    ALTER TABLE Questions DROP CONSTRAINT FK_Questions_Organizations_OrganizationId;

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Questionnaires_Organizations_OrganizationId')
    ALTER TABLE Questionnaires DROP CONSTRAINT FK_Questionnaires_Organizations_OrganizationId;

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Questionnaires_Users_CreatedByUserId')
    ALTER TABLE Questionnaires DROP CONSTRAINT FK_Questionnaires_Users_CreatedByUserId;

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QuestionnaireVersions_Questionnaires_QuestionnaireId')
    ALTER TABLE QuestionnaireVersions DROP CONSTRAINT FK_QuestionnaireVersions_Questionnaires_QuestionnaireId;

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QuestionnaireVersions_Users_CreatedByUserId')
    ALTER TABLE QuestionnaireVersions DROP CONSTRAINT FK_QuestionnaireVersions_Users_CreatedByUserId;

-- Drop tables
DROP TABLE IF EXISTS Questions;
DROP TABLE IF EXISTS QuestionnaireVersions;
DROP TABLE IF EXISTS Questionnaires;

-- Recreate Questionnaires table
CREATE TABLE Questionnaires (
    Id int IDENTITY(1,1) NOT NULL,
    Title nvarchar(200) NOT NULL,
    Description nvarchar(1000) NULL,
    OrganizationId int NOT NULL,
    Category nvarchar(100) NOT NULL,
    CreatedByUserId nvarchar(450) NOT NULL,
    IsTemplate bit NOT NULL DEFAULT 0,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 NULL,
    CONSTRAINT PK_Questionnaires PRIMARY KEY (Id),
    CONSTRAINT FK_Questionnaires_Organizations_OrganizationId 
        FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Questionnaires_Users_CreatedByUserId 
        FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Recreate QuestionnaireVersions table
CREATE TABLE QuestionnaireVersions (
    Id int IDENTITY(1,1) NOT NULL,
    QuestionnaireId int NOT NULL,
    VersionNumber nvarchar(20) NOT NULL,
    ChangeDescription nvarchar(1000) NULL,
    CreatedByUserId nvarchar(450) NOT NULL,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_QuestionnaireVersions PRIMARY KEY (Id),
    CONSTRAINT FK_QuestionnaireVersions_Questionnaires_QuestionnaireId 
        FOREIGN KEY (QuestionnaireId) REFERENCES Questionnaires(Id) ON DELETE CASCADE,
    CONSTRAINT FK_QuestionnaireVersions_Users_CreatedByUserId 
        FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Recreate Questions table
CREATE TABLE Questions (
    Id int IDENTITY(1,1) NOT NULL,
    QuestionnaireId int NOT NULL,
    QuestionText nvarchar(500) NOT NULL,
    QuestionType int NOT NULL DEFAULT 0,
    IsRequired bit NOT NULL DEFAULT 0,
    DisplayOrder int NOT NULL DEFAULT 0,
    Options nvarchar(max) NULL,
    HelpText nvarchar(1000) NULL,
    ValidationRules nvarchar(max) NULL,
    OrganizationId int NOT NULL,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 NULL,
    CONSTRAINT PK_Questions PRIMARY KEY (Id),
    CONSTRAINT FK_Questions_Questionnaires_QuestionnaireId 
        FOREIGN KEY (QuestionnaireId) REFERENCES Questionnaires(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Questions_Organizations_OrganizationId 
        FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id) ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE INDEX IX_Questionnaires_OrganizationId ON Questionnaires(OrganizationId);
CREATE INDEX IX_Questionnaires_CreatedByUserId ON Questionnaires(CreatedByUserId);
CREATE INDEX IX_QuestionnaireVersions_QuestionnaireId ON QuestionnaireVersions(QuestionnaireId);
CREATE INDEX IX_QuestionnaireVersions_CreatedByUserId ON QuestionnaireVersions(CreatedByUserId);
CREATE INDEX IX_Questions_QuestionnaireId ON Questions(QuestionnaireId);
CREATE INDEX IX_Questions_OrganizationId ON Questions(OrganizationId);
CREATE INDEX IX_Questions_DisplayOrder ON Questions(DisplayOrder);

-- Verify the new structure
PRINT 'New Questionnaires table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Questionnaires'
ORDER BY ORDINAL_POSITION;

PRINT 'New Questions table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Questions'
ORDER BY ORDINAL_POSITION;

PRINT 'New QuestionnaireVersions table structure:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'QuestionnaireVersions'
ORDER BY ORDINAL_POSITION;

PRINT 'Tables recreated successfully!'; 