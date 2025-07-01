-- Check organization branding data
SELECT 
    Id,
    Name,
    Type,
    LogoUrl,
    PrimaryColor,
    SecondaryColor,
    Theme,
    CreatedAt
FROM Organizations
ORDER BY Type, Name;

-- Check current user's organization
SELECT 
    u.Id as UserId,
    u.Email,
    u.OrganizationId,
    o.Name as OrganizationName,
    o.Type as OrganizationType,
    o.LogoUrl,
    o.PrimaryColor,
    o.SecondaryColor,
    o.Theme
FROM Users u
JOIN Organizations o ON u.OrganizationId = o.Id
WHERE u.Email = 'admin@esgplatform.com'; 