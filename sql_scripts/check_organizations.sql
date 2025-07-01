-- Script to check Organizations and Users
-- Run this in TablePlus to see what data exists

-- Check Organizations
PRINT 'Organizations in database:';
SELECT Id, Name, Description, IsActive, CreatedAt 
FROM Organizations;

-- Check Users and their OrganizationId
PRINT 'Users and their Organizations:';
SELECT Id, Email, FirstName, LastName, OrganizationId, IsActive
FROM Users;

-- Check if the admin user exists and has a valid OrganizationId
PRINT 'Admin user details:';
SELECT u.Id, u.Email, u.OrganizationId, o.Name as OrganizationName
FROM Users u
LEFT JOIN Organizations o ON u.OrganizationId = o.Id
WHERE u.Email = 'admin@esgplatform.com'; 