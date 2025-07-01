-- First, let's check what organization IDs we have
SELECT Id, Name FROM Organizations;

-- Create test users for the new organizations
-- Note: These are just Users table records. For full ASP.NET Identity integration,
-- you'd need to also create records in AspNetUsers, but for testing organization
-- isolation, we can create Users records with placeholder Identity IDs

-- User for Green Energy Corp (assuming it gets ID 2)
INSERT INTO Users (
    Id, 
    UserName, 
    Email, 
    NormalizedUserName, 
    NormalizedEmail, 
    EmailConfirmed,
    PhoneNumberConfirmed,
    TwoFactorEnabled,
    LockoutEnabled,
    AccessFailedCount,
    FirstName, 
    LastName, 
    OrganizationId, 
    IsActive, 
    CreatedAt
) VALUES (
    NEWID(), -- Generate a new GUID
    'admin@greenenergy.com',
    'admin@greenenergy.com',
    'ADMIN@GREENENERGY.COM',
    'ADMIN@GREENENERGY.COM',
    1, -- EmailConfirmed
    0, -- PhoneNumberConfirmed
    0, -- TwoFactorEnabled
    1, -- LockoutEnabled
    0, -- AccessFailedCount
    'Green Energy',
    'Admin',
    2, -- Green Energy Corp ID
    1, -- IsActive
    GETUTCDATE()
);

-- User for Tech Innovations Ltd (assuming it gets ID 3)  
INSERT INTO Users (
    Id, 
    UserName, 
    Email, 
    NormalizedUserName, 
    NormalizedEmail, 
    EmailConfirmed,
    PhoneNumberConfirmed,
    TwoFactorEnabled,
    LockoutEnabled,
    AccessFailedCount,
    FirstName, 
    LastName, 
    OrganizationId, 
    IsActive, 
    CreatedAt
) VALUES (
    NEWID(), -- Generate a new GUID
    'admin@techinnovations.com',
    'admin@techinnovations.com',
    'ADMIN@TECHINNOVATIONS.COM',
    'ADMIN@TECHINNOVATIONS.COM',
    1, -- EmailConfirmed
    0, -- PhoneNumberConfirmed
    0, -- TwoFactorEnabled
    1, -- LockoutEnabled
    0, -- AccessFailedCount
    'Tech Innovations',
    'Admin',
    3, -- Tech Innovations Ltd ID
    1, -- IsActive
    GETUTCDATE()
);

-- Check all users and their organizations
SELECT 
    u.Id,
    u.UserName,
    u.Email,
    u.FirstName,
    u.LastName,
    u.OrganizationId,
    o.Name as OrganizationName
FROM Users u
JOIN Organizations o ON u.OrganizationId = o.Id
ORDER BY o.Name, u.UserName; 