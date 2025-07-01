-- Create test users for the new organizations with all required fields

-- User for Green Energy Corp (ID 2)
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
    SecurityStamp,
    ConcurrencyStamp,
    FirstName, 
    LastName, 
    OrganizationId, 
    IsActive, 
    CreatedAt
) VALUES (
    NEWID(),
    'admin@greenenergy.com',
    'admin@greenenergy.com',
    'ADMIN@GREENENERGY.COM',
    'ADMIN@GREENENERGY.COM',
    1, -- EmailConfirmed
    0, -- PhoneNumberConfirmed
    0, -- TwoFactorEnabled
    1, -- LockoutEnabled
    0, -- AccessFailedCount
    NEWID(), -- SecurityStamp
    NEWID(), -- ConcurrencyStamp
    'Green Energy',
    'Admin',
    2, -- Green Energy Corp ID
    1, -- IsActive
    GETUTCDATE()
);

-- User for Tech Innovations Ltd (ID 3)
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
    SecurityStamp,
    ConcurrencyStamp,
    FirstName, 
    LastName, 
    OrganizationId, 
    IsActive, 
    CreatedAt
) VALUES (
    NEWID(),
    'admin@techinnovations.com',
    'admin@techinnovations.com',
    'ADMIN@TECHINNOVATIONS.COM',
    'ADMIN@TECHINNOVATIONS.COM',
    1, -- EmailConfirmed
    0, -- PhoneNumberConfirmed
    0, -- TwoFactorEnabled
    1, -- LockoutEnabled
    0, -- AccessFailedCount
    NEWID(), -- SecurityStamp
    NEWID(), -- ConcurrencyStamp
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