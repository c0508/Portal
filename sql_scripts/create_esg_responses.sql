-- ===================================================================
-- ESG Portfolio Analytics Test Data - Part 3: Realistic Responses
-- ===================================================================
-- Creates realistic ESG responses showing progression over 4 years (2021-2024)

-- Set proper SQL Server options
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET NUMERIC_ROUNDABORT OFF;

USE [ESGPlatform]
GO

BEGIN TRANSACTION;

PRINT 'Creating ESG Response Data with Progression Patterns...';

-- Get required IDs
DECLARE @PlatformOrgId INT, @QuestionnaireId INT;
SELECT @PlatformOrgId = Id FROM Organizations WHERE Name = 'Sterling Capital Partners';
SELECT @QuestionnaireId = Id FROM Questionnaires WHERE Title = 'ESG Impact Assessment 2024';

IF @PlatformOrgId IS NULL OR @QuestionnaireId IS NULL
BEGIN
    PRINT 'ERROR: Required base data not found. Run previous scripts first.';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ===================================================================
-- 1. CREATE USERS FOR EACH PORTFOLIO COMPANY
-- ===================================================================

DECLARE @CompanyUsers TABLE (
    CompanyId INT,
    CompanyName VARCHAR(200),
    UserId NVARCHAR(450)
);

-- Create users for each portfolio company
DECLARE companies_cursor CURSOR FOR
SELECT o.Id, o.Name
FROM Organizations o
INNER JOIN OrganizationRelationships r ON o.Id = r.SupplierOrganizationId
WHERE r.PlatformOrganizationId = @PlatformOrgId AND o.Type = 2;

DECLARE @CompanyId INT, @CompanyName VARCHAR(200);

OPEN companies_cursor;
FETCH NEXT FROM companies_cursor INTO @CompanyId, @CompanyName;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @UserId NVARCHAR(450) = NEWID();
    DECLARE @Email VARCHAR(255) = 'esg@' + LOWER(REPLACE(@CompanyName, ' ', '')) + '.com';
    DECLARE @FirstName VARCHAR(100) = 'ESG';
    DECLARE @LastName VARCHAR(100) = 'Manager';
    
    -- Create Identity User
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
    VALUES (@UserId, @Email, UPPER(@Email), @Email, UPPER(@Email), 1, NEWID(), NEWID(), 0, 0, 1, 0);
    
    -- Create User record
    INSERT INTO Users (Id, FirstName, LastName, OrganizationId, IsActive, CreatedAt)
    VALUES (@UserId, @FirstName, @LastName, @CompanyId, 1, '2020-12-01');
    
    INSERT INTO @CompanyUsers VALUES (@CompanyId, @CompanyName, @UserId);
    
    FETCH NEXT FROM companies_cursor INTO @CompanyId, @CompanyName;
END;

CLOSE companies_cursor;
DEALLOCATE companies_cursor;

PRINT 'Created users for ' + CAST(@@ROWCOUNT AS VARCHAR) + ' portfolio companies';

-- ===================================================================
-- 2. GET CAMPAIGN ASSIGNMENTS AND QUESTIONS
-- ===================================================================

DECLARE @Assignments TABLE (
    Year INT,
    AssignmentId INT,
    CompanyId INT,
    CompanyName VARCHAR(200),
    Sector VARCHAR(50),
    ESGMaturityLevel VARCHAR(20),
    ImprovementRate VARCHAR(20)
);

-- Get all assignments with company details
INSERT INTO @Assignments
SELECT DISTINCT
    YEAR(c.ReportingPeriodStart) + 1 as Year, -- 2020 data = 2021 reporting
    ca.Id as AssignmentId,
    o.Id as CompanyId,
    o.Name as CompanyName,
    ra.AttributeValue as Sector,
    CASE ra.AttributeValue
        WHEN 'TECH' THEN 'High'
        WHEN 'HEALTHCARE' THEN 'High'
        WHEN 'FINANCIAL' THEN 'Medium'
        WHEN 'RETAIL' THEN 'Medium'
        WHEN 'MANUFACTURING' THEN 'Low'
        ELSE 'Medium'
    END as ESGMaturityLevel,
    CASE ra.AttributeValue
        WHEN 'TECH' THEN 'Fast'
        WHEN 'HEALTHCARE' THEN 'Steady'
        WHEN 'FINANCIAL' THEN 'Fast'
        WHEN 'RETAIL' THEN 'Steady'
        WHEN 'MANUFACTURING' THEN 'Slow'
        ELSE 'Steady'
    END as ImprovementRate
FROM CampaignAssignments ca
INNER JOIN Campaigns c ON ca.CampaignId = c.Id
INNER JOIN Organizations o ON ca.TargetOrganizationId = o.Id
INNER JOIN OrganizationRelationshipAttributes ra ON ca.OrganizationRelationshipId = ra.OrganizationRelationshipId
WHERE c.OrganizationId = @PlatformOrgId 
  AND ra.AttributeType = 'SECTOR';

PRINT 'Found ' + CAST(@@ROWCOUNT AS VARCHAR) + ' assignments to populate with responses';

-- Get questions by type for easier processing
DECLARE @PolicyQuestions TABLE (QuestionId INT, QuestionText VARCHAR(500));
DECLARE @QuantitativeQuestions TABLE (QuestionId INT, QuestionText VARCHAR(500), Unit VARCHAR(50), IsPercentage BIT);
DECLARE @MultiSelectQuestions TABLE (QuestionId INT, QuestionText VARCHAR(500), Options NVARCHAR(2000));
DECLARE @TextQuestions TABLE (QuestionId INT, QuestionText VARCHAR(500));

INSERT INTO @PolicyQuestions
SELECT Id, QuestionText FROM Questions 
WHERE QuestionnaireId = @QuestionnaireId AND QuestionType = 7; -- YesNo

INSERT INTO @QuantitativeQuestions
SELECT Id, QuestionText, Unit, IsPercentage FROM Questions 
WHERE QuestionnaireId = @QuestionnaireId AND QuestionType = 2; -- Number

INSERT INTO @MultiSelectQuestions
SELECT Id, QuestionText, Options FROM Questions 
WHERE QuestionnaireId = @QuestionnaireId AND QuestionType = 5; -- MultiSelect

INSERT INTO @TextQuestions
SELECT Id, QuestionText FROM Questions 
WHERE QuestionnaireId = @QuestionnaireId AND QuestionType IN (1, 2); -- Text, LongText

PRINT 'Question breakdown: ' + 
      CAST((SELECT COUNT(*) FROM @PolicyQuestions) AS VARCHAR) + ' policy, ' +
      CAST((SELECT COUNT(*) FROM @QuantitativeQuestions) AS VARCHAR) + ' quantitative, ' +
      CAST((SELECT COUNT(*) FROM @MultiSelectQuestions) AS VARCHAR) + ' multi-select, ' +
      CAST((SELECT COUNT(*) FROM @TextQuestions) AS VARCHAR) + ' text';

-- ===================================================================
-- 3. CREATE RESPONSES WITH REALISTIC PROGRESSION
-- ===================================================================

-- Response generation cursor
DECLARE response_cursor CURSOR FOR
SELECT a.Year, a.AssignmentId, a.CompanyId, a.CompanyName, a.Sector, a.ESGMaturityLevel, a.ImprovementRate, u.UserId
FROM @Assignments a
INNER JOIN @CompanyUsers u ON a.CompanyId = u.CompanyId;

DECLARE @Year INT, @AssignmentId INT, @RCompanyId INT, @RCompanyName VARCHAR(200);
DECLARE @Sector VARCHAR(50), @MaturityLevel VARCHAR(20), @ImprovementRate VARCHAR(20), @ResponderId NVARCHAR(450);

OPEN response_cursor;
FETCH NEXT FROM response_cursor INTO @Year, @AssignmentId, @RCompanyId, @RCompanyName, @Sector, @MaturityLevel, @ImprovementRate, @ResponderId;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Calculate progression factors
    DECLARE @YearIndex DECIMAL(3,1) = @Year - 2020; -- 2021=1, 2022=2, etc.
    DECLARE @BaseESGScore DECIMAL(5,2) = CASE @MaturityLevel 
        WHEN 'High' THEN 75.0 WHEN 'Medium' THEN 60.0 ELSE 45.0 END;
    DECLARE @ImprovementFactor DECIMAL(3,2) = CASE @ImprovementRate 
        WHEN 'Fast' THEN 1.20 WHEN 'Steady' THEN 1.10 ELSE 1.05 END;
    
    -- === COMPANY SIZE QUESTIONS (Baseline data) ===
    
    -- Employee count (varies by company size category, grows slightly each year)
    DECLARE @EmployeeCount INT = CASE @Sector
        WHEN 'TECH' THEN 350 + (@Year - 2021) * 45
        WHEN 'MANUFACTURING' THEN 1200 + (@Year - 2021) * 80
        WHEN 'HEALTHCARE' THEN 280 + (@Year - 2021) * 35
        WHEN 'RETAIL' THEN 420 + (@Year - 2021) * 55
        WHEN 'FINANCIAL' THEN 180 + (@Year - 2021) * 25
        ELSE 300 + (@Year - 2021) * 40
    END;
    
    -- Revenue (grows each year)
    DECLARE @Revenue BIGINT = CASE @Sector
        WHEN 'TECH' THEN 45000000 + (@Year - 2021) * 8000000
        WHEN 'MANUFACTURING' THEN 125000000 + (@Year - 2021) * 15000000  
        WHEN 'HEALTHCARE' THEN 35000000 + (@Year - 2021) * 7000000
        WHEN 'RETAIL' THEN 85000000 + (@Year - 2021) * 12000000
        WHEN 'FINANCIAL' THEN 28000000 + (@Year - 2021) * 5000000
        ELSE 50000000 + (@Year - 2021) * 8000000
    END;
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, NumericValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @EmployeeCount, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%employees%';
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, NumericValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @Revenue, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%revenue%';
    
    -- === POLICY QUESTIONS (Y/N with progression) ===
    
    -- Policy adoption improves over time based on maturity and improvement rate
    DECLARE policy_cursor CURSOR FOR
    SELECT QuestionId, QuestionText FROM @PolicyQuestions;
    
    DECLARE @PolicyQuestionId INT, @PolicyText VARCHAR(500);
    
    OPEN policy_cursor;
    FETCH NEXT FROM policy_cursor INTO @PolicyQuestionId, @PolicyText;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @PolicyAdoptionRate DECIMAL(3,2) = 
            (@BaseESGScore / 100.0) * POWER(@ImprovementFactor, @YearIndex - 1);
        
        -- Higher chance for critical policies
        IF @PolicyText LIKE '%climate%' OR @PolicyText LIKE '%anti-corruption%' OR @PolicyText LIKE '%diversity%' OR @PolicyText LIKE '%health and safety%'
            SET @PolicyAdoptionRate = @PolicyAdoptionRate * 1.2;
        
        -- Cap at 95% to keep realistic
        IF @PolicyAdoptionRate > 0.95 SET @PolicyAdoptionRate = 0.95;
        
        DECLARE @HasPolicy BIT = CASE WHEN RAND() < @PolicyAdoptionRate THEN 1 ELSE 0 END;
        
        INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, BooleanValue, Status, CreatedAt, UpdatedAt)
        VALUES (@PolicyQuestionId, @AssignmentId, @ResponderId, @HasPolicy, 3, GETUTCDATE(), GETUTCDATE());
        
        FETCH NEXT FROM policy_cursor INTO @PolicyQuestionId, @PolicyText;
    END;
    
    CLOSE policy_cursor;
    DEALLOCATE policy_cursor;
    
    -- === QUANTITATIVE QUESTIONS (with realistic progression) ===
    
    -- Environmental metrics
    DECLARE @BaseEmissions DECIMAL(10,2) = CASE @Sector
        WHEN 'MANUFACTURING' THEN 8500.0 WHEN 'TECH' THEN 450.0 WHEN 'RETAIL' THEN 1200.0 
        WHEN 'HEALTHCARE' THEN 800.0 WHEN 'FINANCIAL' THEN 350.0 ELSE 900.0 END;
    
    -- Emissions reduce over time (improvement)
    DECLARE @Scope1Emissions DECIMAL(10,2) = @BaseEmissions * (1.0 - (@YearIndex - 1) * 0.08);
    DECLARE @Scope2Emissions DECIMAL(10,2) = (@BaseEmissions * 0.6) * (1.0 - (@YearIndex - 1) * 0.12);
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, NumericValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @Scope1Emissions, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%scope 1%';
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, NumericValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @Scope2Emissions, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%scope 2%';
    
    -- Social metrics (improving over time)
    DECLARE @WomenLeadership DECIMAL(5,2) = CASE @Sector
        WHEN 'TECH' THEN 25.0 WHEN 'HEALTHCARE' THEN 35.0 WHEN 'RETAIL' THEN 30.0
        WHEN 'FINANCIAL' THEN 28.0 WHEN 'MANUFACTURING' THEN 18.0 ELSE 25.0 END;
    SET @WomenLeadership = @WomenLeadership + (@YearIndex - 1) * 3.5; -- Improving ~3.5% per year
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, NumericValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @WomenLeadership, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%women in leadership%';
    
    -- Training hours (increasing over time)
    DECLARE @TrainingHours DECIMAL(5,2) = 15.0 + (@YearIndex - 1) * 2.5;
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, NumericValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @TrainingHours, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%training hours%';
    
    -- === MULTI-SELECT QUESTIONS (expanding scope over time) ===
    
    -- Emission scopes measured (more scopes over time)
    DECLARE @EmissionScopes NVARCHAR(500);
    IF @Year = 2021 
        SET @EmissionScopes = '["Scope 1 (Direct emissions)"]';
    ELSE IF @Year = 2022 
        SET @EmissionScopes = '["Scope 1 (Direct emissions)", "Scope 2 (Purchased energy)"]';
    ELSE 
        SET @EmissionScopes = '["Scope 1 (Direct emissions)", "Scope 2 (Purchased energy)", "Scope 3 (Value chain emissions)"]';
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, SelectedValues, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @EmissionScopes, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%emission scopes%';
    
    -- ESG frameworks (more frameworks adopted over time)
    DECLARE @ESGFrameworks NVARCHAR(500);
    IF @Year = 2021 
        SET @ESGFrameworks = '["GRI Standards"]';
    ELSE IF @Year = 2022 
        SET @ESGFrameworks = '["GRI Standards", "SASB"]';
    ELSE IF @Year = 2023
        SET @ESGFrameworks = '["GRI Standards", "SASB", "TCFD"]';
    ELSE 
        SET @ESGFrameworks = '["GRI Standards", "SASB", "TCFD", "CDP"]';
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, SelectedValues, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @ESGFrameworks, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%ESG reporting frameworks%';
    
    -- === TEXT QUESTIONS (increasingly sophisticated responses) ===
    
    -- ESG priorities (more detailed over time)
    DECLARE @ESGPriorities NVARCHAR(MAX) = CASE @Year
        WHEN 2021 THEN 'Focus on establishing baseline measurements for emissions and diversity metrics. Implement basic sustainability practices.'
        WHEN 2022 THEN 'Expand renewable energy adoption, enhance diversity hiring programs, strengthen governance policies including anti-corruption measures.'
        WHEN 2023 THEN 'Achieve carbon neutrality targets, implement comprehensive ESG training programs, establish ESG-linked executive compensation, expand supply chain sustainability requirements.'
        ELSE 'Drive towards science-based targets, integrate ESG into core business strategy, enhance stakeholder engagement, pursue B-Corp certification, implement circular economy principles.'
    END;
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, TextValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @ESGPriorities, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%ESG priorities%';
    
    FETCH NEXT FROM response_cursor INTO @Year, @AssignmentId, @RCompanyId, @RCompanyName, @Sector, @MaturityLevel, @ImprovementRate, @ResponderId;
END;

CLOSE response_cursor;
DEALLOCATE response_cursor;

PRINT 'ESG response data generation completed successfully';

-- ===================================================================
-- 4. SUMMARY STATISTICS
-- ===================================================================

SELECT 
    'Data Creation Summary' as Category,
    (SELECT COUNT(*) FROM Organizations WHERE Type = 1) as PlatformOrgs,
    (SELECT COUNT(*) FROM Organizations WHERE Type = 2) as PortfolioCompanies,
    (SELECT COUNT(*) FROM Campaigns WHERE OrganizationId = @PlatformOrgId) as Campaigns,
    (SELECT COUNT(*) FROM Questions WHERE QuestionnaireId = @QuestionnaireId) as Questions,
    (SELECT COUNT(*) FROM CampaignAssignments ca INNER JOIN Campaigns c ON ca.CampaignId = c.Id WHERE c.OrganizationId = @PlatformOrgId) as Assignments,
    (SELECT COUNT(*) FROM Responses r INNER JOIN CampaignAssignments ca ON r.CampaignAssignmentId = ca.Id INNER JOIN Campaigns c ON ca.CampaignId = c.Id WHERE c.OrganizationId = @PlatformOrgId) as Responses;

PRINT 'ESG Portfolio Analytics Test Data Creation Complete!';
PRINT 'You can now test the ESG analytics features with realistic 4-year progression data.';

COMMIT TRANSACTION; 