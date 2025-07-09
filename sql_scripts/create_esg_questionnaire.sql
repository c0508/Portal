-- ===================================================================
-- ESG Questionnaire Creation Script
-- ===================================================================
-- Creates the ESG questionnaire and questions
-- Run this AFTER create_esg_test_data.sql and create_esg_test_users.sql

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRANSACTION;

PRINT 'Creating ESG Questionnaire and Questions...';

-- Get the platform organization ID and user ID
DECLARE @PlatformOrgId INT;
DECLARE @PlatformUserId NVARCHAR(450);

SELECT @PlatformOrgId = Id FROM Organizations WHERE Name = 'Sterling Capital Partners';
SELECT @PlatformUserId = Id FROM Users WHERE Email = 'sarah.mitchell@sterlingcp.com';

IF @PlatformOrgId IS NULL
BEGIN
    PRINT 'ERROR: Platform organization not found. Run create_esg_test_data.sql first.';
    ROLLBACK;
    RETURN;
END;

IF @PlatformUserId IS NULL
BEGIN
    PRINT 'ERROR: Platform admin user not found. Run create_esg_test_users.sql first.';
    ROLLBACK;
    RETURN;
END;

-- ===================================================================
-- 1. CREATE ESG QUESTIONNAIRE
-- ===================================================================

DECLARE @QuestionnaireId INT, @QuestionnaireVersionId INT;

-- Check if questionnaire already exists
IF NOT EXISTS (SELECT 1 FROM Questionnaires WHERE Title = 'ESG Impact Assessment 2024' AND OrganizationId = @PlatformOrgId)
BEGIN
    -- Create comprehensive ESG questionnaire
    INSERT INTO Questionnaires (Title, Description, OrganizationId, Category, CreatedByUserId, IsTemplate, IsActive, CreatedAt)
    VALUES ('ESG Impact Assessment 2024', 'Comprehensive ESG questionnaire covering environmental, social, and governance metrics for portfolio companies', @PlatformOrgId, 'ESG', @PlatformUserId, 0, 1, GETUTCDATE());

    SET @QuestionnaireId = SCOPE_IDENTITY();

    -- Create questionnaire version
    INSERT INTO QuestionnaireVersions (QuestionnaireId, VersionNumber, ChangeDescription, CreatedByUserId, IsActive, IsLocked, CreatedAt)
    VALUES (@QuestionnaireId, 'v1.0', 'Initial comprehensive ESG questionnaire', @PlatformUserId, 1, 1, GETUTCDATE());

    SET @QuestionnaireVersionId = SCOPE_IDENTITY();

    PRINT 'Created ESG questionnaire (ID: ' + CAST(@QuestionnaireId AS VARCHAR) + ')';
END
ELSE
BEGIN
    SELECT @QuestionnaireId = Id FROM Questionnaires WHERE Title = 'ESG Impact Assessment 2024' AND OrganizationId = @PlatformOrgId;
    PRINT 'ESG questionnaire already exists (ID: ' + CAST(@QuestionnaireId AS VARCHAR) + ')';
END;

-- ===================================================================
-- 2. CREATE COMPREHENSIVE ESG QUESTIONS
-- ===================================================================

-- Check if questions already exist
IF NOT EXISTS (SELECT 1 FROM Questions WHERE QuestionnaireId = @QuestionnaireId)
BEGIN
    -- Environmental Questions
    INSERT INTO Questions (QuestionnaireId, QuestionText, HelpText, Section, QuestionType, DisplayOrder, OrganizationId, IsRequired, CreatedAt, UpdatedAt)
    VALUES 
        -- Company Size Questions (questionnaire-based segmentation)
        (@QuestionnaireId, 'Number of employees', 'Total number of full-time equivalent employees', 'Company Information', 2, 1, @PlatformOrgId, 1, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Annual revenue (USD)', 'Total annual revenue in US dollars', 'Company Information', 2, 2, @PlatformOrgId, 1, GETUTCDATE(), GETUTCDATE()),
        
        -- Environmental - Policy Questions (Y/N)
        (@QuestionnaireId, 'Does your company have a formal climate change policy?', 'A documented policy addressing climate risks and commitments', 'Environmental', 7, 10, @PlatformOrgId, 1, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Do you have formal environmental management system?', 'ISO 14001 or equivalent environmental management certification', 'Environmental', 7, 11, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Do you have renewable energy targets?', 'Specific targets for renewable energy adoption', 'Environmental', 7, 12, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        
        -- Environmental - Multiple Choice
        (@QuestionnaireId, 'Which emission scopes do you measure?', 'Select all emission scopes that your company actively measures and tracks', 'Environmental', 5, 20, @PlatformOrgId, 1, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Which renewable energy sources does your company use?', 'Select all renewable energy sources currently utilized', 'Environmental', 5, 21, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        
        -- Environmental - Quantitative
        (@QuestionnaireId, 'Total scope 1 emissions (tCO2e)', 'Direct emissions from owned or controlled sources', 'Environmental', 2, 30, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Total scope 2 emissions (tCO2e)', 'Indirect emissions from purchased energy', 'Environmental', 2, 31, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Water consumption (cubic meters)', 'Total annual water consumption', 'Environmental', 2, 32, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Waste generation (tons)', 'Total annual waste generated', 'Environmental', 2, 33, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Energy consumption (MWh)', 'Total annual energy consumption', 'Environmental', 2, 34, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        
        -- Social - Policy Questions
        (@QuestionnaireId, 'Does your company have a diversity and inclusion policy?', 'Formal policy promoting workplace diversity and inclusion', 'Social', 7, 40, @PlatformOrgId, 1, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Do you have health and safety policies?', 'Documented occupational health and safety policies', 'Social', 7, 41, @PlatformOrgId, 1, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Do you have human rights policies?', 'Formal human rights policy covering operations and supply chain', 'Social', 7, 42, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Do you provide employee training and development programs?', 'Structured training and professional development opportunities', 'Social', 7, 43, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        
        -- Social - Quantitative (Percentages)
        (@QuestionnaireId, 'Percentage of women in leadership positions', 'Percentage of women in senior management roles', 'Social', 2, 50, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Percentage of ethnic minorities in workforce', 'Percentage of employees from ethnic minority backgrounds', 'Social', 2, 51, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Employee retention rate', 'Percentage of employees retained over 12 months', 'Social', 2, 52, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        
        -- Social - Quantitative (Numbers)
        (@QuestionnaireId, 'Number of workplace injuries', 'Total workplace injuries requiring medical attention', 'Social', 2, 60, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Training hours per employee', 'Average training hours provided per employee annually', 'Social', 2, 61, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        
        -- Governance - Policy Questions
        (@QuestionnaireId, 'Does your company have anti-corruption policies?', 'Formal anti-corruption and anti-bribery policies', 'Governance', 7, 70, @PlatformOrgId, 1, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Do you have data privacy and security policies?', 'Comprehensive data protection and cybersecurity policies', 'Governance', 7, 71, @PlatformOrgId, 1, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Does your board have independent directors?', 'Board includes independent, non-executive directors', 'Governance', 7, 72, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Do you have whistleblower protection policies?', 'Policies protecting employees who report misconduct', 'Governance', 7, 73, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        
        -- Governance - Multiple Choice
        (@QuestionnaireId, 'Which ESG reporting frameworks do you follow?', 'Select all ESG reporting frameworks your company follows', 'Governance', 5, 80, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Which certifications does your company hold?', 'Select all relevant certifications held by your company', 'Governance', 5, 81, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        
        -- Governance - Quantitative
        (@QuestionnaireId, 'Number of board meetings per year', 'Total number of board meetings held annually', 'Governance', 2, 90, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Percentage of women on board', 'Percentage of female board members', 'Governance', 2, 91, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        
        -- Text Questions for qualitative insights
        (@QuestionnaireId, 'Describe your top 3 ESG priorities for the next year', 'Please provide specific ESG goals and initiatives planned', 'Strategy', 1, 100, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE()),
        (@QuestionnaireId, 'Describe your supply chain sustainability initiatives', 'Detail any programs to improve supply chain sustainability', 'Strategy', 1, 101, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE());

    PRINT 'Created ' + CAST(@@ROWCOUNT AS VARCHAR) + ' ESG questions';
END
ELSE
BEGIN
    DECLARE @ExistingQuestionCount INT;
    SELECT @ExistingQuestionCount = COUNT(*) FROM Questions WHERE QuestionnaireId = @QuestionnaireId;
    PRINT 'Questions already exist for this questionnaire (' + CAST(@ExistingQuestionCount AS VARCHAR) + ' questions)';
END;

-- ===================================================================
-- 3. UPDATE QUESTION PROPERTIES
-- ===================================================================

-- Update multiple choice questions with options
UPDATE Questions SET Options = '["Scope 1 (Direct emissions)", "Scope 2 (Purchased energy)", "Scope 3 (Value chain emissions)"]' 
WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%emission scopes%';

UPDATE Questions SET Options = '["Solar", "Wind", "Hydroelectric", "Geothermal", "Biomass", "Other renewable sources"]' 
WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%renewable energy sources%';

UPDATE Questions SET Options = '["GRI Standards", "SASB", "TCFD", "CDP", "UNGC", "IFRS Sustainability Standards", "EU Taxonomy"]' 
WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%ESG reporting frameworks%';

UPDATE Questions SET Options = '["ISO 14001 (Environmental)", "ISO 45001 (Health & Safety)", "ISO 27001 (Information Security)", "B Corp Certification", "LEED Certification", "Fair Trade Certified", "Other"]' 
WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%certifications%';

-- Update percentage questions
UPDATE Questions SET IsPercentage = 1, ValidationRules = '{"min": 0, "max": 100}' 
WHERE QuestionnaireId = @QuestionnaireId AND (QuestionText LIKE '%percentage%' OR QuestionText LIKE '%retention rate%');

-- Update unit-based questions
UPDATE Questions SET Unit = 'tCO2e' WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%emissions%';
UPDATE Questions SET Unit = 'm³' WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%water consumption%';
UPDATE Questions SET Unit = 'tons' WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%waste generation%';
UPDATE Questions SET Unit = 'MWh' WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%energy consumption%';
UPDATE Questions SET Unit = 'hours' WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%training hours%';
UPDATE Questions SET Unit = 'USD' WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%revenue%';

PRINT 'Updated question properties (options, percentages, units)';

-- ===================================================================
-- SUMMARY
-- ===================================================================

DECLARE @TotalQuestions INT;
SELECT @TotalQuestions = COUNT(*) FROM Questions WHERE QuestionnaireId = @QuestionnaireId;

PRINT '=== ESG Questionnaire Creation Complete ===';
PRINT 'Questionnaire ID: ' + CAST(@QuestionnaireId AS VARCHAR);
PRINT 'Total questions: ' + CAST(@TotalQuestions AS VARCHAR);
PRINT 'Platform Organization: ' + CAST(@PlatformOrgId AS VARCHAR);

COMMIT; 