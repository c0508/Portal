-- Fix organizations that have Type = 0 (invalid value)
-- Set them as Supplier Organizations by default

UPDATE Organizations 
SET Type = 2  -- SupplierOrganization
WHERE Type = 0;

-- Verify the update
SELECT Id, Name, Type, 
       CASE 
           WHEN Type = 1 THEN 'Platform Organization'
           WHEN Type = 2 THEN 'Supplier Organization'
           ELSE 'Unknown'
       END as TypeName
FROM Organizations
ORDER BY Id; 