-- Add branding information to organizations with better contrast colors
UPDATE Organizations 
SET 
    LogoUrl = 'https://via.placeholder.com/150x50/1a365d/ffffff?text=Sample+ESG',
    PrimaryColor = '#1a365d',
    SecondaryColor = '#2d3748',
    NavigationTextColor = '#ffffff',
    Theme = 'Corporate'
WHERE Name = 'Sample ESG Organization';

UPDATE Organizations 
SET 
    LogoUrl = 'https://via.placeholder.com/150x50/1a5f3f/ffffff?text=Green+Energy',
    PrimaryColor = '#1a5f3f',
    SecondaryColor = '#2f855a',
    NavigationTextColor = '#ffffff',
    Theme = 'Environmental'
WHERE Name = 'Green Energy Corp';

UPDATE Organizations 
SET 
    LogoUrl = 'https://via.placeholder.com/150x50/553c9a/ffffff?text=Tech+Innovations',
    PrimaryColor = '#553c9a',
    SecondaryColor = '#805ad5',
    NavigationTextColor = '#ffffff',
    Theme = 'Technology'
WHERE Name = 'Tech Innovations Ltd';

-- Verify the updates
SELECT 
    Id,
    Name,
    Type,
    LogoUrl,
    PrimaryColor,
    SecondaryColor,
    NavigationTextColor,
    Theme,
    CreatedAt
FROM Organizations
ORDER BY Type, Name; 