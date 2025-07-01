-- Add sample branding data to test the organizational hierarchy and styling system

-- Update Sample ESG Organization with branding
UPDATE Organizations 
SET 
    PrimaryColor = '#0066cc',
    SecondaryColor = '#004499',
    Theme = 'Professional',
    LogoUrl = 'https://via.placeholder.com/200x80/0066cc/ffffff?text=ESG+Platform'
WHERE Name = 'Sample ESG Organization';

-- Update Green Energy Corp with branding
UPDATE Organizations 
SET 
    PrimaryColor = '#28a745',
    SecondaryColor = '#20c997',
    Theme = 'Eco',
    LogoUrl = 'https://via.placeholder.com/200x80/28a745/ffffff?text=Green+Energy'
WHERE Name = 'Green Energy Corp';

-- Update Tech Innovations Ltd with branding
UPDATE Organizations 
SET 
    PrimaryColor = '#6f42c1',
    SecondaryColor = '#e83e8c',
    Theme = 'Tech',
    LogoUrl = 'https://via.placeholder.com/200x80/6f42c1/ffffff?text=Tech+Innovations'
WHERE Name = 'Tech Innovations Ltd';

-- Create some organization relationships with branding context
-- Make Sample ESG Organization the primary relationship creator for Green Energy Corp
INSERT INTO OrganizationRelationships (
    PlatformOrganizationId, 
    SupplierOrganizationId, 
    RelationshipType, 
    Description, 
    CreatedByOrganizationId,
    IsPrimaryRelationship,
    IsActive,
    CreatedAt
)
SELECT 
    p.Id as PlatformOrganizationId,
    s.Id as SupplierOrganizationId,
    'Supplier' as RelationshipType,
    'Primary ESG reporting relationship' as Description,
    p.Id as CreatedByOrganizationId,
    1 as IsPrimaryRelationship,
    1 as IsActive,
    GETUTCDATE() as CreatedAt
FROM Organizations p, Organizations s
WHERE p.Name = 'Sample ESG Organization' 
  AND s.Name = 'Green Energy Corp'
  AND NOT EXISTS (
      SELECT 1 FROM OrganizationRelationships 
      WHERE PlatformOrganizationId = p.Id AND SupplierOrganizationId = s.Id
  );

-- Create secondary relationship for Tech Innovations
INSERT INTO OrganizationRelationships (
    PlatformOrganizationId, 
    SupplierOrganizationId, 
    RelationshipType, 
    Description, 
    CreatedByOrganizationId,
    IsPrimaryRelationship,
    IsActive,
    CreatedAt
)
SELECT 
    p.Id as PlatformOrganizationId,
    s.Id as SupplierOrganizationId,
    'Technology Partner' as RelationshipType,
    'Technology and innovation assessment partnership' as Description,
    p.Id as CreatedByOrganizationId,
    0 as IsPrimaryRelationship,
    1 as IsActive,
    GETUTCDATE() as CreatedAt
FROM Organizations p, Organizations s
WHERE p.Name = 'Sample ESG Organization' 
  AND s.Name = 'Tech Innovations Ltd'
  AND NOT EXISTS (
      SELECT 1 FROM OrganizationRelationships 
      WHERE PlatformOrganizationId = p.Id AND SupplierOrganizationId = s.Id
  );

-- Show the results
SELECT 
    o.Name,
    o.Type,
    o.PrimaryColor,
    o.SecondaryColor,
    o.Theme,
    o.LogoUrl
FROM Organizations o
ORDER BY o.Type, o.Name;

SELECT 
    p.Name as PlatformOrg,
    s.Name as SupplierOrg,
    or.RelationshipType,
    or.IsPrimaryRelationship,
    c.Name as CreatedByOrg
FROM OrganizationRelationships or
JOIN Organizations p ON or.PlatformOrganizationId = p.Id
JOIN Organizations s ON or.SupplierOrganizationId = s.Id
LEFT JOIN Organizations c ON or.CreatedByOrganizationId = c.Id
WHERE or.IsActive = 1; 