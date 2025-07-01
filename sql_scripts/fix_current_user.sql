-- Add the current logged-in user to the Users table
INSERT INTO Users (Id, UserName, Email, FirstName, LastName, OrganizationId, IsActive, CreatedAt, NormalizedUserName, NormalizedEmail, EmailConfirmed)
VALUES (
    '6c8218d9-114e-4b4f-86b0-eab881f6792c',
    'admin@esgplatform.com',
    'admin@esgplatform.com', 
    'Current',
    'User',
    1,  -- Link to the Sample ESG Organization
    1,  -- IsActive = true
    GETUTCDATE(),
    'ADMIN@ESGPLATFORM.COM',
    'ADMIN@ESGPLATFORM.COM',
    1   -- EmailConfirmed = true
); 