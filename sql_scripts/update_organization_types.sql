-- Update existing organization to be Platform Organization
-- OrganizationType: 1 = PlatformOrganization, 2 = SupplierOrganization

UPDATE Organizations 
SET Type = 1  -- PlatformOrganization
WHERE Name = 'Sample ESG Organization';

-- Verify the update
SELECT Id, Name, Type, 
       CASE 
           WHEN Type = 1 THEN 'Platform Organization'
           WHEN Type = 2 THEN 'Supplier Organization'
           ELSE 'Unknown'
       END as TypeName
FROM Organizations; 