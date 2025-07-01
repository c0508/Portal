-- Simple branding update script

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

-- Show results
SELECT Name, Type, PrimaryColor, SecondaryColor, Theme FROM Organizations; 