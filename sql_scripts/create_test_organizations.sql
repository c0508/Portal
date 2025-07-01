-- Create additional test organizations
INSERT INTO Organizations (Name, Description, PrimaryColor, SecondaryColor, IsActive, CreatedAt)
VALUES 
    ('Green Energy Corp', 'Renewable energy company focused on sustainability', '#28a745', '#20c997', 1, GETUTCDATE()),
    ('Tech Innovations Ltd', 'Technology company with strong ESG commitments', '#007bff', '#6f42c1', 1, GETUTCDATE());

-- Check the new organization IDs
SELECT Id, Name FROM Organizations; 