-- ===================================================================
-- ESG Responses Creation Script (Fixed)
-- ===================================================================
-- Creates realistic ESG responses with temporal progression
-- Run this AFTER all previous scripts

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRANSACTION;

PRINT 'Creating ESG Responses with Realistic Progression...';

-- Get required IDs
DECLARE @PlatformOrgId INT;
DECLARE @QuestionnaireId INT;

SELECT @PlatformOrgId = Id FROM Organizations WHERE Name = 'Sterling Capital Partners';
SELECT @QuestionnaireId = Id FROM Questionnaires WHERE Title = 'ESG Impact Assessment 2024';

IF @PlatformOrgId IS NULL OR @QuestionnaireId IS NULL
BEGIN
    PRINT 'ERROR: Platform organization or questionnaire not found.';
    ROLLBACK;
    RETURN;
END;

-- ===================================================================
-- 1. GET COMPANY USERS (Existing)
-- ===================================================================

DECLARE @CompanyUsers TABLE (
    CompanyId INT,
    CompanyName VARCHAR(200),
    UserId NVARCHAR(450)
);

-- Get existing users for portfolio companies
INSERT INTO @CompanyUsers
SELECT o.Id, o.Name, u.Id
FROM Organizations o
INNER JOIN OrganizationRelationships r ON o.Id = r.SupplierOrganizationId
INNER JOIN Users u ON o.Id = u.OrganizationId
WHERE r.PlatformOrganizationId = @PlatformOrgId AND o.Type = 2;

PRINT 'Found ' + CAST(@@ROWCOUNT AS VARCHAR) + ' company users for response generation';

-- ===================================================================
-- 2. GET CAMPAIGN ASSIGNMENTS
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
    PRINT 'Creating responses for ' + @RCompanyName + ' (' + CAST(@Year AS VARCHAR) + ')';
    
    -- Calculate progression factors
    DECLARE @YearIndex DECIMAL(3,1) = @Year - 2020; -- 2021=1, 2022=2, etc.
    DECLARE @BaseESGScore DECIMAL(5,2) = CASE @MaturityLevel 
        WHEN 'High' THEN 75.0 WHEN 'Medium' THEN 60.0 ELSE 45.0 END;
    DECLARE @ImprovementFactor DECIMAL(3,2) = CASE @ImprovementRate 
        WHEN 'Fast' THEN 1.20 WHEN 'Steady' THEN 1.10 ELSE 1.05 END;
    
    -- === COMPANY SIZE QUESTIONS ===
    
    -- Employee count
    DECLARE @EmployeeCount INT = CASE @Sector
        WHEN 'TECH' THEN 350 + (@Year - 2021) * 45
        WHEN 'MANUFACTURING' THEN 1200 + (@Year - 2021) * 80
        WHEN 'HEALTHCARE' THEN 280 + (@Year - 2021) * 35
        WHEN 'RETAIL' THEN 420 + (@Year - 2021) * 55
        WHEN 'FINANCIAL' THEN 180 + (@Year - 2021) * 25
        ELSE 300 + (@Year - 2021) * 40
    END;
    
    -- Revenue
    DECLARE @Revenue BIGINT = CASE @Sector
        WHEN 'TECH' THEN 45000000 + (@Year - 2021) * 8000000
        WHEN 'MANUFACTURING' THEN 125000000 + (@Year - 2021) * 15000000  
        WHEN 'HEALTHCARE' THEN 35000000 + (@Year - 2021) * 7000000
        WHEN 'RETAIL' THEN 85000000 + (@Year - 2021) * 12000000
        WHEN 'FINANCIAL' THEN 28000000 + (@Year - 2021) * 5000000
        ELSE 50000000 + (@Year - 2021) * 8000000
    END;
    
    -- Insert employee count
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, NumericValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @EmployeeCount, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%employees%';
    
    -- Insert revenue
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, NumericValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @Revenue, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%revenue%';
    
    -- === POLICY QUESTIONS (Y/N with progression) ===
    
    -- Climate policy (improves over time)
    DECLARE @ClimatePolicy BIT = CASE 
        WHEN (@BaseESGScore / 100.0) * POWER(@ImprovementFactor, @YearIndex - 1) > 0.6 THEN 1 
        ELSE 0 END;
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, BooleanValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @ClimatePolicy, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%climate change policy%';
    
    -- Diversity policy
    DECLARE @DiversityPolicy BIT = CASE 
        WHEN (@BaseESGScore / 100.0) * POWER(@ImprovementFactor, @YearIndex - 1) > 0.5 THEN 1 
        ELSE 0 END;
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, BooleanValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @DiversityPolicy, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%diversity and inclusion policy%';
    
    -- === QUANTITATIVE QUESTIONS ===
    
    -- Emissions (reducing over time)
    DECLARE @BaseEmissions DECIMAL(10,2) = CASE @Sector
        WHEN 'MANUFACTURING' THEN 8500.0 WHEN 'TECH' THEN 450.0 WHEN 'RETAIL' THEN 1200.0 
        WHEN 'HEALTHCARE' THEN 800.0 WHEN 'FINANCIAL' THEN 350.0 ELSE 900.0 END;
    
    DECLARE @Scope1Emissions DECIMAL(10,2) = @BaseEmissions * (1.0 - (@YearIndex - 1) * 0.08);
    DECLARE @Scope2Emissions DECIMAL(10,2) = (@BaseEmissions * 0.6) * (1.0 - (@YearIndex - 1) * 0.12);
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, NumericValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @Scope1Emissions, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%scope 1 emissions%';
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, NumericValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @Scope2Emissions, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%scope 2 emissions%';
    
    -- Women in leadership (improving over time)
    DECLARE @WomenLeadership DECIMAL(5,2) = CASE @Sector
        WHEN 'TECH' THEN 25.0 WHEN 'HEALTHCARE' THEN 35.0 WHEN 'RETAIL' THEN 30.0
        WHEN 'FINANCIAL' THEN 28.0 WHEN 'MANUFACTURING' THEN 18.0 ELSE 25.0 END;
    SET @WomenLeadership = @WomenLeadership + (@YearIndex - 1) * 3.5;
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, NumericValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @WomenLeadership, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%women in leadership%';
    
    -- === MULTI-SELECT QUESTIONS ===
    
    -- Emission scopes measured (expanding over time)
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
    
    -- === TEXT QUESTIONS ===
    
    -- ESG priorities (more sophisticated over time)
    DECLARE @ESGPriorities NVARCHAR(MAX) = CASE @Year
        WHEN 2021 THEN 'Focus on establishing baseline measurements for emissions and diversity metrics.'
        WHEN 2022 THEN 'Expand renewable energy adoption, enhance diversity hiring programs.'
        WHEN 2023 THEN 'Achieve carbon neutrality targets, implement comprehensive ESG training programs.'
        ELSE 'Drive towards science-based targets, integrate ESG into core business strategy.'
    END;
    
    INSERT INTO Responses (QuestionId, CampaignAssignmentId, ResponderId, TextValue, Status, CreatedAt, UpdatedAt)
    SELECT Id, @AssignmentId, @ResponderId, @ESGPriorities, 3, GETUTCDATE(), GETUTCDATE()
    FROM Questions WHERE QuestionnaireId = @QuestionnaireId AND QuestionText LIKE '%ESG priorities%';
    
    FETCH NEXT FROM response_cursor INTO @Year, @AssignmentId, @RCompanyId, @RCompanyName, @Sector, @MaturityLevel, @ImprovementRate, @ResponderId;
END;

CLOSE response_cursor;
DEALLOCATE response_cursor;

-- ===================================================================
-- SUMMARY
-- ===================================================================

DECLARE @TotalResponses INT;
SELECT @TotalResponses = COUNT(*) FROM Responses r
INNER JOIN Questions q ON r.QuestionId = q.Id
WHERE q.QuestionnaireId = @QuestionnaireId;

PRINT '=== ESG Response Generation Complete ===';
PRINT 'Total responses created: ' + CAST(@TotalResponses AS VARCHAR);
PRINT 'ESG test data setup is now complete!';

COMMIT; 