-- Debug script to find the JSON parsing issue
-- Check Questions with potentially invalid Options JSON

-- 1. First, let's see all questions and their options
SELECT 
    q.Id,
    q.QuestionText,
    q.QuestionType,
    q.Options,
    LEN(q.Options) as OptionsLength,
    CASE 
        WHEN q.Options IS NULL THEN 'NULL'
        WHEN q.Options = '' THEN 'EMPTY'
        WHEN LEFT(q.Options, 1) = '[' THEN 'VALID_JSON_START'
        ELSE 'INVALID_JSON - Starts with: ' + LEFT(q.Options, 10)
    END as OptionsStatus
FROM Questions q
ORDER BY q.Id;

-- 2. Check campaign assignments and their related questionnaire versions
SELECT 
    ca.Id as AssignmentId,
    ca.Status,
    c.Name as CampaignName,
    qv.VersionNumber,
    qq.Title as QuestionnaireTitle,
    ca.LeadResponderId
FROM CampaignAssignments ca
JOIN Campaigns c ON ca.CampaignId = c.Id
JOIN QuestionnaireVersions qv ON ca.QuestionnaireVersionId = qv.Id
JOIN Questionnaires qq ON qv.QuestionnaireId = qq.Id
ORDER BY ca.Id;

-- 3. Check specific questions for the questionnaire version being accessed
-- (We'll need to run this after we identify the problematic assignment)

-- 4. Look for questions with Options that start with 'M' (causing the error)
SELECT 
    Id,
    QuestionText,
    QuestionType,
    Options
FROM Questions 
WHERE Options IS NOT NULL 
  AND Options != '' 
  AND LEFT(Options, 1) = 'M'; 