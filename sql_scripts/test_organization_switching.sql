-- First, run the create_test_organizations.sql to create the new orgs
-- Then run this to test organization switching

-- Check current admin user
SELECT Id, UserName, Email, OrganizationId FROM Users WHERE Email = 'admin@esgplatform.com';

-- Switch admin user to Green Energy Corp (ID 2)
UPDATE Users 
SET OrganizationId = 2 
WHERE Email = 'admin@esgplatform.com';

-- After testing, switch to Tech Innovations Ltd (ID 3)  
-- UPDATE Users SET OrganizationId = 3 WHERE Email = 'admin@esgplatform.com';

-- Switch back to original organization (ID 1)
-- UPDATE Users SET OrganizationId = 1 WHERE Email = 'admin@esgplatform.com';

-- Check questionnaires by organization
SELECT q.Id, q.Title, q.OrganizationId, o.Name as OrganizationName
FROM Questionnaires q
JOIN Organizations o ON q.OrganizationId = o.Id
ORDER BY o.Name; 