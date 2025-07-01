-- Check Organizations table
SELECT * FROM Organizations;

-- Check Users table
SELECT Id, UserName, Email, FirstName, LastName, OrganizationId FROM Users;

-- Check Roles table
SELECT * FROM Roles;

-- Check UserRoles table
SELECT ur.UserId, ur.RoleId, r.Name as RoleName, u.Email
FROM UserRoles ur
JOIN Roles r ON ur.RoleId = r.Id
JOIN Users u ON ur.UserId = u.Id; 