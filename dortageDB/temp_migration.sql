-- Update role names from topraktar to visioner
UPDATE AspNetRoles 
SET Name = 'visioner', NormalizedName = 'VISIONER'
WHERE Name = 'topraktar' OR NormalizedName = 'TOPRAKTAR';

-- Verify the change
SELECT * FROM AspNetRoles WHERE Name LIKE '%vision%' OR Name LIKE '%toprak%';
