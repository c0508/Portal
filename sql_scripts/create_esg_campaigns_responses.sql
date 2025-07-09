-- ===================================================================
-- ESG Portfolio Analytics Test Data - Part 2: Campaigns & Responses
-- ===================================================================
-- Creates 4 annual campaigns (2021-2024) and realistic response patterns
-- showing ESG progression over time

-- Set proper SQL Server options
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET NUMERIC_ROUNDABORT OFF;

USE [ESGPlatform]
GO

BEGIN TRANSACTION;

PRINT 'Creating ESG Campaigns and Response Data...';

-- Get the platform organization and questionnaire IDs
DECLARE @PlatformOrgId INT, @QuestionnaireId INT, @QuestionnaireVersionId INT, @PlatformUserId NVARCHAR(450);

SELECT @PlatformOrgId = Id FROM Organizations WHERE Name = 'Sterling Capital Partners';
SELECT @QuestionnaireId = Id FROM Questionnaires WHERE Title = 'ESG Impact Assessment 2024';
SELECT @QuestionnaireVersionId = Id FROM QuestionnaireVersions WHERE QuestionnaireId = @QuestionnaireId;
SELECT @PlatformUserId = Id FROM Users WHERE OrganizationId = @PlatformOrgId;

IF @PlatformOrgId IS NULL OR @QuestionnaireId IS NULL OR @QuestionnaireVersionId IS NULL
BEGIN
    PRINT 'ERROR: Required base data not found. Run Part 1 script first.';
    ROLLBACK TRANSACTION;
    RETURN;
END;

PRINT 'Found Platform Org ID: ' + CAST(@PlatformOrgId AS VARCHAR);
PRINT 'Found Questionnaire ID: ' + CAST(@QuestionnaireId AS VARCHAR);

-- ===================================================================
-- 1. CREATE 4 ANNUAL CAMPAIGNS (2021-2024)
-- ===================================================================

DECLARE @CampaignIds TABLE (Year INT, CampaignId INT);

-- Campaign 2021
DECLARE @Campaign2021Id INT;
INSERT INTO Campaigns (Name, Description, OrganizationId, Status, StartDate, EndDate, Deadline, ReportingPeriodStart, ReportingPeriodEnd, Instructions, CreatedAt, CreatedById)
VALUES (
    'ESG Assessment 2021', 
    'Annual ESG assessment for portfolio companies covering 2020 performance data',
    @PlatformOrgId, 
    4, -- Completed
    '2021-01-15', 
    '2021-03-31', 
    '2021-02-28',
    '2020-01-01', 
    '2020-12-31',
    'Please provide accurate ESG data for the 2020 reporting period. Focus on establishing baseline metrics.',
    '2021-01-15',
    @PlatformUserId
);
SET @Campaign2021Id = SCOPE_IDENTITY();
INSERT INTO @CampaignIds VALUES (2021, @Campaign2021Id);

-- Campaign 2022
DECLARE @Campaign2022Id INT;
INSERT INTO Campaigns (Name, Description, OrganizationId, Status, StartDate, EndDate, Deadline, ReportingPeriodStart, ReportingPeriodEnd, Instructions, CreatedAt, CreatedById)
VALUES (
    'ESG Assessment 2022', 
    'Annual ESG assessment for portfolio companies covering 2021 performance data',
    @PlatformOrgId, 
    4, -- Completed
    '2022-01-15', 
    '2022-03-31', 
    '2022-02-28',
    '2021-01-01', 
    '2021-12-31',
    'Please provide ESG data for 2021. We expect to see progress on initiatives identified in previous year.',
    '2022-01-15',
    @PlatformUserId
);
SET @Campaign2022Id = SCOPE_IDENTITY();
INSERT INTO @CampaignIds VALUES (2022, @Campaign2022Id);

-- Campaign 2023
DECLARE @Campaign2023Id INT;
INSERT INTO Campaigns (Name, Description, OrganizationId, Status, StartDate, EndDate, Deadline, ReportingPeriodStart, ReportingPeriodEnd, Instructions, CreatedAt, CreatedById)
VALUES (
    'ESG Assessment 2023', 
    'Annual ESG assessment for portfolio companies covering 2022 performance data',
    @PlatformOrgId, 
    4, -- Completed
    '2023-01-15', 
    '2023-03-31', 
    '2023-02-28',
    '2022-01-01', 
    '2022-12-31',
    'Please provide ESG data for 2022. Focus on demonstrating measurable improvements in key ESG metrics.',
    '2023-01-15',
    @PlatformUserId
);
SET @Campaign2023Id = SCOPE_IDENTITY();
INSERT INTO @CampaignIds VALUES (2023, @Campaign2023Id);

-- Campaign 2024
DECLARE @Campaign2024Id INT;
INSERT INTO Campaigns (Name, Description, OrganizationId, Status, StartDate, EndDate, Deadline, ReportingPeriodStart, ReportingPeriodEnd, Instructions, CreatedAt, CreatedById)
VALUES (
    'ESG Assessment 2024', 
    'Annual ESG assessment for portfolio companies covering 2023 performance data',
    @PlatformOrgId, 
    1, -- Active
    '2024-01-15', 
    '2024-03-31', 
    '2024-02-28',
    '2023-01-01', 
    '2023-12-31',
    'Please provide ESG data for 2023. This assessment will help track progress toward our 2025 ESG targets.',
    '2024-01-15',
    @PlatformUserId
);
SET @Campaign2024Id = SCOPE_IDENTITY();
INSERT INTO @CampaignIds VALUES (2024, @Campaign2024Id);

PRINT 'Created 4 annual campaigns (2021-2024)';

-- ===================================================================
-- 2. CREATE CAMPAIGN ASSIGNMENTS FOR ALL COMPANIES
-- ===================================================================

DECLARE @CompanyAssignments TABLE (
    Year INT,
    CampaignId INT,
    AssignmentId INT,
    CompanyId INT,
    CompanyName VARCHAR(200),
    RelationshipId INT
);

-- Get all portfolio companies
DECLARE companies_cursor CURSOR FOR
SELECT o.Id, o.Name, r.Id as RelationshipId
FROM Organizations o
INNER JOIN OrganizationRelationships r ON o.Id = r.SupplierOrganizationId
WHERE r.PlatformOrganizationId = @PlatformOrgId AND o.Type = 2;

DECLARE @CompanyId INT, @CompanyName VARCHAR(200), @RelationshipId INT;

OPEN companies_cursor;
FETCH NEXT FROM companies_cursor INTO @CompanyId, @CompanyName, @RelationshipId;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Create assignments for each year
    DECLARE year_cursor CURSOR FOR
    SELECT Year, CampaignId FROM @CampaignIds;
    
    DECLARE @Year INT, @CampaignId INT;
    
    OPEN year_cursor;
    FETCH NEXT FROM year_cursor INTO @Year, @CampaignId;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @AssignmentId INT;
        DECLARE @AssignmentStatus INT;
        
        -- Set assignment status based on year
        SET @AssignmentStatus = CASE 
            WHEN @Year < 2024 THEN 4 -- Approved (completed)
            ELSE 1 -- InProgress (current year)
        END;
        
        INSERT INTO CampaignAssignments (
            CampaignId, TargetOrganizationId, QuestionnaireVersionId, OrganizationRelationshipId,
            Status, CreatedAt, StartedAt, SubmittedAt, ReviewedAt
        )
        VALUES (
            @CampaignId, @CompanyId, @QuestionnaireVersionId, @RelationshipId,
            @AssignmentStatus, 
            CASE @Year WHEN 2021 THEN '2021-01-15' WHEN 2022 THEN '2022-01-15' WHEN 2023 THEN '2023-01-15' ELSE '2024-01-15' END,
            CASE @Year WHEN 2021 THEN '2021-01-20' WHEN 2022 THEN '2022-01-20' WHEN 2023 THEN '2023-01-20' ELSE '2024-01-20' END,
            CASE WHEN @Year < 2024 THEN 
                CASE @Year WHEN 2021 THEN '2021-02-15' WHEN 2022 THEN '2022-02-15' WHEN 2023 THEN '2023-02-15' END
            ELSE NULL END,
            CASE WHEN @Year < 2024 THEN 
                CASE @Year WHEN 2021 THEN '2021-02-20' WHEN 2022 THEN '2022-02-20' WHEN 2023 THEN '2023-02-20' END
            ELSE NULL END
        );
        
        SET @AssignmentId = SCOPE_IDENTITY();
        
        INSERT INTO @CompanyAssignments VALUES (@Year, @CampaignId, @AssignmentId, @CompanyId, @CompanyName, @RelationshipId);
        
        FETCH NEXT FROM year_cursor INTO @Year, @CampaignId;
    END;
    
    CLOSE year_cursor;
    DEALLOCATE year_cursor;
    
    FETCH NEXT FROM companies_cursor INTO @CompanyId, @CompanyName, @RelationshipId;
END;

CLOSE companies_cursor;
DEALLOCATE companies_cursor;

PRINT 'Created campaign assignments for all companies across 4 years';

-- ===================================================================
-- 3. CREATE REALISTIC ESG RESPONSES WITH PROGRESSION
-- ===================================================================

-- Get question IDs for reference
DECLARE @Questions TABLE (
    QuestionId INT,
    QuestionText VARCHAR(500),
    QuestionType INT,
    Section VARCHAR(100),
    IsPercentage BIT,
    Unit VARCHAR(50)
);

INSERT INTO @Questions
SELECT Id, QuestionText, QuestionType, Section, IsPercentage, Unit
FROM Questions 
WHERE QuestionnaireId = @QuestionnaireId;

PRINT 'Found ' + CAST(@@ROWCOUNT AS VARCHAR) + ' questions to populate with responses';

-- Company performance profiles for realistic progression
DECLARE @CompanyProfiles TABLE (
    CompanyId INT,
    CompanyName VARCHAR(200),
    Sector VARCHAR(50),
    ESGMaturityLevel VARCHAR(20), -- 'High', 'Medium', 'Low'
    ImprovementRate VARCHAR(20),   -- 'Fast', 'Steady', 'Slow'
    BaselineScore DECIMAL(5,2)
);

-- Insert company profiles based on sector and size
INSERT INTO @CompanyProfiles
SELECT DISTINCT 
    ca.CompanyId,
    ca.CompanyName,
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
    END as ImprovementRate,
    CASE ra.AttributeValue
        WHEN 'TECH' THEN 75.0
        WHEN 'HEALTHCARE' THEN 70.0
        WHEN 'FINANCIAL' THEN 65.0
        WHEN 'RETAIL' THEN 60.0
        WHEN 'MANUFACTURING' THEN 55.0
        ELSE 60.0
    END as BaselineScore
FROM @CompanyAssignments ca
INNER JOIN OrganizationRelationshipAttributes ra ON ca.RelationshipId = ra.OrganizationRelationshipId
WHERE ra.AttributeType = 'SECTOR' AND ca.Year = 2021;

PRINT 'Created ESG maturity profiles for ' + CAST(@@ROWCOUNT AS VARCHAR) + ' companies';

-- Continue with detailed response generation...
PRINT 'ESG Campaigns and Assignments created. Continue with response generation...';

COMMIT TRANSACTION; 