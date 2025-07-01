-- Check the current user's details
SELECT Id, UserName, Email, FirstName, LastName, OrganizationId, IsActive 
FROM Users 
WHERE Id = '6c8218d9-114e-4b4f-86b0-eab881f6792c';

-- Check all users
SELECT Id, UserName, Email, FirstName, LastName, OrganizationId, IsActive 
FROM Users; 