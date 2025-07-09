-- ===================================================================
-- ESG Portfolio Analytics Test Data Creation Script
-- ===================================================================
-- Creates realistic test data for private equity ESG analytics:
-- - 10 diverse portfolio companies 
-- - 1 comprehensive ESG questionnaire
-- - 4 annual campaigns (2021-2024)
-- - Realistic response patterns showing ESG progression

-- Set proper SQL Server options
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRANSACTION;

PRINT 'Creating ESG Portfolio Analytics Test Data...';

-- ===================================================================
-- 1. CREATE PLATFORM ORGANIZATION (PE FIRM)
-- ===================================================================

DECLARE @PlatformOrgId INT;

-- Create the PE firm as platform organization
INSERT INTO Organizations (Name, Description, Type, IsActive, CreatedAt)
VALUES ('Sterling Capital Partners', 'Private Equity firm specializing in growth investments with focus on ESG impact', 1, 1, GETUTCDATE());

SET @PlatformOrgId = SCOPE_IDENTITY();
PRINT 'Created Platform Organization: Sterling Capital Partners (ID: ' + CAST(@PlatformOrgId AS VARCHAR) + ')';

-- ===================================================================
-- 2. CREATE ORGANIZATION ATTRIBUTE TYPES
-- ===================================================================

-- Sector/Industry attributes
DECLARE @SectorTypeId INT, @IndustryTypeId INT, @SizeTypeId INT, @RegionTypeId INT;

-- Create attribute types (skip if already exist)
INSERT INTO OrganizationAttributeTypes (Code, Name, Description, IsActive, CreatedAt)
SELECT 'SECTOR', 'Business Sector', 'Primary business sector classification', 1, GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM OrganizationAttributeTypes WHERE Code = 'SECTOR');

INSERT INTO OrganizationAttributeTypes (Code, Name, Description, IsActive, CreatedAt)
SELECT 'INDUSTRY', 'Industry Type', 'Specific industry within sector', 1, GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM OrganizationAttributeTypes WHERE Code = 'INDUSTRY');

INSERT INTO OrganizationAttributeTypes (Code, Name, Description, IsActive, CreatedAt)
SELECT 'SIZE_CATEGORY', 'Company Size', 'Employee count category', 1, GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM OrganizationAttributeTypes WHERE Code = 'SIZE_CATEGORY');

INSERT INTO OrganizationAttributeTypes (Code, Name, Description, IsActive, CreatedAt)
SELECT 'REGION', 'Geographic Region', 'Primary operating region', 1, GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM OrganizationAttributeTypes WHERE Code = 'REGION');

SELECT @SectorTypeId = Id FROM OrganizationAttributeTypes WHERE Code = 'SECTOR';
SELECT @IndustryTypeId = Id FROM OrganizationAttributeTypes WHERE Code = 'INDUSTRY';
SELECT @SizeTypeId = Id FROM OrganizationAttributeTypes WHERE Code = 'SIZE_CATEGORY';
SELECT @RegionTypeId = Id FROM OrganizationAttributeTypes WHERE Code = 'REGION';

-- Create attribute values
INSERT INTO OrganizationAttributeValues (AttributeTypeId, Code, Name, Description, IsActive, DisplayOrder, CreatedAt)
VALUES 
    -- Sectors
    (@SectorTypeId, 'TECH', 'Technology', 'Software, hardware, and tech services', 1, 1, GETUTCDATE()),
    (@SectorTypeId, 'MANUFACTURING', 'Manufacturing', 'Industrial manufacturing and production', 1, 2, GETUTCDATE()),
    (@SectorTypeId, 'HEALTHCARE', 'Healthcare', 'Medical devices, pharmaceuticals, services', 1, 3, GETUTCDATE()),
    (@SectorTypeId, 'RETAIL', 'Retail', 'Consumer retail and e-commerce', 1, 4, GETUTCDATE()),
    (@SectorTypeId, 'FINANCIAL', 'Financial Services', 'Banking, insurance, fintech', 1, 5, GETUTCDATE()),
    
    -- Industries (subset for diversity)
    (@IndustryTypeId, 'SAAS', 'SaaS', 'Software as a Service', 1, 1, GETUTCDATE()),
    (@IndustryTypeId, 'AUTOMOTIVE', 'Automotive', 'Vehicle manufacturing', 1, 2, GETUTCDATE()),
    (@IndustryTypeId, 'MEDTECH', 'Medical Technology', 'Medical devices and equipment', 1, 3, GETUTCDATE()),
    (@IndustryTypeId, 'ECOMMERCE', 'E-commerce', 'Online retail platforms', 1, 4, GETUTCDATE()),
    (@IndustryTypeId, 'FINTECH', 'FinTech', 'Financial technology solutions', 1, 5, GETUTCDATE()),
    (@IndustryTypeId, 'BIOTECH', 'Biotechnology', 'Biotechnology and life sciences', 1, 6, GETUTCDATE()),
    (@IndustryTypeId, 'LOGISTICS', 'Logistics', 'Supply chain and logistics', 1, 7, GETUTCDATE()),
    (@IndustryTypeId, 'FASHION', 'Fashion Retail', 'Apparel and fashion retail', 1, 8, GETUTCDATE()),
    
    -- Company Sizes
    (@SizeTypeId, 'SMALL', 'Small (50-200)', '50-200 employees', 1, 1, GETUTCDATE()),
    (@SizeTypeId, 'MEDIUM', 'Medium (200-1000)', '200-1000 employees', 1, 2, GETUTCDATE()),
    (@SizeTypeId, 'LARGE', 'Large (1000+)', 'Over 1000 employees', 1, 3, GETUTCDATE()),
    
    -- Regions
    (@RegionTypeId, 'NORTH_AMERICA', 'North America', 'United States and Canada', 1, 1, GETUTCDATE()),
    (@RegionTypeId, 'EUROPE', 'Europe', 'European Union and UK', 1, 2, GETUTCDATE());

PRINT 'Created organization attribute types and values';

-- ===================================================================
-- 3. CREATE 10 PORTFOLIO COMPANIES
-- ===================================================================

-- Portfolio company data
DECLARE @Companies TABLE (
    Id INT IDENTITY(1,1),
    Name VARCHAR(200),
    Description VARCHAR(1000),
    Sector VARCHAR(50),
    Industry VARCHAR(50),
    Size VARCHAR(50),
    Region VARCHAR(50)
);

INSERT INTO @Companies (Name, Description, Sector, Industry, Size, Region)
VALUES 
    ('TechFlow Solutions', 'Cloud-based workflow automation platform for enterprise clients', 'TECH', 'SAAS', 'MEDIUM', 'NORTH_AMERICA'),
    ('GreenMotors Inc', 'Electric vehicle manufacturer specializing in commercial fleet vehicles', 'MANUFACTURING', 'AUTOMOTIVE', 'LARGE', 'NORTH_AMERICA'),
    ('MediCore Technologies', 'AI-powered diagnostic imaging solutions for hospitals', 'HEALTHCARE', 'MEDTECH', 'MEDIUM', 'EUROPE'),
    ('ShopSmart Digital', 'Omnichannel retail platform with sustainability focus', 'RETAIL', 'ECOMMERCE', 'MEDIUM', 'NORTH_AMERICA'),
    ('FinanceForward', 'Digital banking platform for SME customers', 'FINANCIAL', 'FINTECH', 'SMALL', 'EUROPE'),
    ('BioAdvance Labs', 'Biotechnology company developing sustainable materials', 'HEALTHCARE', 'BIOTECH', 'SMALL', 'EUROPE'),
    ('LogisTech Global', 'Smart logistics and supply chain optimization platform', 'MANUFACTURING', 'LOGISTICS', 'LARGE', 'NORTH_AMERICA'),
    ('StyleSustain Co', 'Sustainable fashion retailer with circular economy model', 'RETAIL', 'FASHION', 'MEDIUM', 'EUROPE'),
    ('DataSecure Pro', 'Cybersecurity solutions for financial institutions', 'TECH', 'SAAS', 'SMALL', 'NORTH_AMERICA'),
    ('EcoManufacture Ltd', 'Sustainable packaging manufacturer with renewable materials', 'MANUFACTURING', 'MANUFACTURING', 'MEDIUM', 'EUROPE');

-- Create organizations and relationships
DECLARE @CompanyCount INT = 1;
WHILE @CompanyCount <= 10
BEGIN
    DECLARE @CompanyName VARCHAR(200), @CompanyDesc VARCHAR(1000);
    DECLARE @Sector VARCHAR(50), @Industry VARCHAR(50), @Size VARCHAR(50), @Region VARCHAR(50);
    DECLARE @CompanyId INT, @RelationshipId INT;
    
    SELECT @CompanyName = Name, @CompanyDesc = Description, @Sector = Sector, 
           @Industry = Industry, @Size = Size, @Region = Region
    FROM @Companies WHERE Id = @CompanyCount;
    
    -- Create supplier organization
    INSERT INTO Organizations (Name, Description, Type, IsActive, CreatedAt)
    VALUES (@CompanyName, @CompanyDesc, 2, 1, GETUTCDATE());
    
    SET @CompanyId = SCOPE_IDENTITY();
    
    -- Create relationship between platform and supplier
    INSERT INTO OrganizationRelationships (PlatformOrganizationId, SupplierOrganizationId, RelationshipType, Description, IsActive, CreatedAt)
    VALUES (@PlatformOrgId, @CompanyId, 'Portfolio Company', 'Private equity portfolio investment', 1, GETUTCDATE());
    
    SET @RelationshipId = SCOPE_IDENTITY();
    
    -- Add relationship attributes
    INSERT INTO OrganizationRelationshipAttributes (OrganizationRelationshipId, AttributeType, AttributeValue, IsActive, CreatedAt)
    VALUES 
        (@RelationshipId, 'SECTOR', @Sector, 1, GETUTCDATE()),
        (@RelationshipId, 'INDUSTRY', @Industry, 1, GETUTCDATE()),
        (@RelationshipId, 'SIZE_CATEGORY', @Size, 1, GETUTCDATE()),
        (@RelationshipId, 'REGION', @Region, 1, GETUTCDATE());
    
    PRINT 'Created company ' + CAST(@CompanyCount AS VARCHAR) + ': ' + @CompanyName + ' (' + @Sector + ')';
    SET @CompanyCount = @CompanyCount + 1;
END;

-- ===================================================================
-- 4. CREATE ESG QUESTIONNAIRE
-- ===================================================================

DECLARE @QuestionnaireId INT, @QuestionnaireVersionId INT;

-- Get platform admin user ID (will be created by separate script)
DECLARE @PlatformUserId NVARCHAR(450);
SELECT @PlatformUserId = Id FROM Users WHERE FirstName = 'Sarah' AND LastName = 'Mitchell' AND OrganizationId = @PlatformOrgId;

-- If user doesn't exist, create a placeholder (should be created by create_esg_test_users.sql)
IF @PlatformUserId IS NULL
BEGIN
    PRINT 'WARNING: Platform admin user not found. Questionnaire will be created without CreatedByUserId.';
    PRINT 'Run create_esg_test_users.sql first to create users properly.';
    -- We'll create the questionnaire without a user for now
END;

-- Create comprehensive ESG questionnaire
INSERT INTO Questionnaires (Title, Description, OrganizationId, Category, CreatedByUserId, IsTemplate, IsActive, CreatedAt)
VALUES ('ESG Impact Assessment 2024', 'Comprehensive ESG questionnaire covering environmental, social, and governance metrics for portfolio companies', @PlatformOrgId, 'ESG', @PlatformUserId, 0, 1, GETUTCDATE());

SET @QuestionnaireId = SCOPE_IDENTITY();

-- Create questionnaire version
INSERT INTO QuestionnaireVersions (QuestionnaireId, VersionNumber, ChangeDescription, CreatedByUserId, IsActive, IsLocked, CreatedAt)
VALUES (@QuestionnaireId, 'v1.0', 'Initial comprehensive ESG questionnaire', @PlatformUserId, 1, 1, GETUTCDATE());

SET @QuestionnaireVersionId = SCOPE_IDENTITY();

PRINT 'Created ESG questionnaire (ID: ' + CAST(@QuestionnaireId AS VARCHAR) + ')';

-- ===================================================================
-- 5. CREATE COMPREHENSIVE ESG QUESTIONS
-- ===================================================================

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
    (@QuestionnaireId, 'Describe your supply chain sustainability initiatives', 'Detail any programs to improve supply chain sustainability', 'Strategy', 2, 101, @PlatformOrgId, 0, GETUTCDATE(), GETUTCDATE());

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

PRINT 'Created ' + CAST(@@ROWCOUNT AS VARCHAR) + ' ESG questions';

-- Continue with campaigns and responses in next section...

PRINT 'ESG Test Data Part 1 Complete. Continue with campaigns and responses...';

COMMIT TRANSACTION; 