-- Insert sample question attributes covering major ESG standards
-- GRI Standards (Global Reporting Initiative)
INSERT INTO QuestionAttributes (Code, Name, Description, Category, Subcategory, Tags, IsActive, DisplayOrder, CreatedAt) VALUES
('GRI305-1', 'Direct (Scope 1) GHG emissions', 'Direct greenhouse gas emissions from sources owned or controlled by the organization', 'GRI', 'Environmental', 'emissions,ghg,scope1,climate', 1, 10, GETUTCDATE()),
('GRI305-2', 'Energy indirect (Scope 2) GHG emissions', 'Indirect greenhouse gas emissions from the generation of purchased energy', 'GRI', 'Environmental', 'emissions,ghg,scope2,energy,climate', 1, 20, GETUTCDATE()),
('GRI305-3', 'Other indirect (Scope 3) GHG emissions', 'Other indirect greenhouse gas emissions from the value chain', 'GRI', 'Environmental', 'emissions,ghg,scope3,value-chain,climate', 1, 30, GETUTCDATE()),
('GRI302-1', 'Energy consumption within the organization', 'Energy consumption from renewable and non-renewable sources', 'GRI', 'Environmental', 'energy,consumption,renewable', 1, 40, GETUTCDATE()),
('GRI302-3', 'Energy intensity', 'Energy consumed per unit of activity or output', 'GRI', 'Environmental', 'energy,intensity,efficiency', 1, 50, GETUTCDATE()),
('GRI303-3', 'Water withdrawal', 'Total water withdrawn by source', 'GRI', 'Environmental', 'water,withdrawal,sustainability', 1, 60, GETUTCDATE()),
('GRI306-3', 'Waste generated', 'Total weight of waste generated by type and disposal method', 'GRI', 'Environmental', 'waste,circular-economy', 1, 70, GETUTCDATE()),
('GRI401-1', 'New employee hires and employee turnover', 'Total number and rates of new employee hires and employee turnover', 'GRI', 'Social', 'employees,hiring,turnover,hr', 1, 80, GETUTCDATE()),
('GRI405-1', 'Diversity of governance bodies and employees', 'Percentage of individuals in governance bodies and employees by diversity categories', 'GRI', 'Social', 'diversity,inclusion,governance', 1, 90, GETUTCDATE()),
('GRI403-9', 'Work-related injuries', 'Number and rate of fatalities and high-consequence work-related injuries', 'GRI', 'Social', 'safety,health,injuries,workplace', 1, 100, GETUTCDATE());

-- SASB Standards (Sustainability Accounting Standards Board)
INSERT INTO QuestionAttributes (Code, Name, Description, Category, Subcategory, Tags, IsActive, DisplayOrder, CreatedAt) VALUES
('SASB-EM-EP-110a.1', 'Air emissions', 'Gross global Scope 1 emissions, percentage covered under emissions-limiting regulations', 'SASB', 'Environmental', 'emissions,air-quality,regulations', 1, 110, GETUTCDATE()),
('SASB-EM-EP-140a.1', 'Water management', 'Total fresh water withdrawn, percentage recycled, percentage in regions with high water stress', 'SASB', 'Environmental', 'water,management,recycling,stress', 1, 120, GETUTCDATE()),
('SASB-EM-EP-320a.1', 'Ecological impacts', 'Terrestrial acreage disturbed, percentage of impacted area restored', 'SASB', 'Environmental', 'biodiversity,restoration,land-use', 1, 130, GETUTCDATE()),
('SASB-EM-EP-510a.1', 'Workforce health and safety', 'Total recordable incident rate, fatality rate, near miss frequency rate', 'SASB', 'Social', 'safety,health,incidents,workforce', 1, 140, GETUTCDATE()),
('SASB-RT-AE-230a.1', 'Product safety', 'Number of recalls issued, number of units recalled', 'SASB', 'Social', 'safety,product,recalls,quality', 1, 150, GETUTCDATE()),
('SASB-TC-SI-220a.1', 'Data privacy', 'Description of policies and practices relating to behavioral advertising and user privacy', 'SASB', 'Social', 'privacy,data-protection,advertising', 1, 160, GETUTCDATE()),
('SASB-FB-AG-110a.1', 'Environmental impacts', 'Percentage of production in accordance with third-party environmental standards', 'SASB', 'Environmental', 'standards,certification,production', 1, 170, GETUTCDATE()),
('SASB-HC-BP-240a.1', 'Product design', 'Revenue from products designed for use-phase resource efficiency', 'SASB', 'Environmental', 'design,efficiency,revenue,lifecycle', 1, 180, GETUTCDATE());

-- TCFD (Task Force on Climate-related Financial Disclosures)
INSERT INTO QuestionAttributes (Code, Name, Description, Category, Subcategory, Tags, IsActive, DisplayOrder, CreatedAt) VALUES
('TCFD-Gov-A', 'Board oversight', 'Oversight of climate-related risks and opportunities by the board', 'TCFD', 'Governance', 'board,oversight,climate,governance', 1, 190, GETUTCDATE()),
('TCFD-Gov-B', 'Management role', 'Management role in assessing and managing climate-related risks and opportunities', 'TCFD', 'Governance', 'management,climate,assessment', 1, 200, GETUTCDATE()),
('TCFD-Strat-A', 'Climate risks and opportunities', 'Climate-related risks and opportunities identified over short, medium, and long term', 'TCFD', 'Strategy', 'risks,opportunities,timeline,strategy', 1, 210, GETUTCDATE()),
('TCFD-Strat-B', 'Impact on business', 'Impact of climate-related risks and opportunities on business, strategy, and financial planning', 'TCFD', 'Strategy', 'impact,business,strategy,financial', 1, 220, GETUTCDATE()),
('TCFD-Strat-C', 'Scenario analysis', 'Resilience of strategy considering different climate-related scenarios', 'TCFD', 'Strategy', 'scenarios,resilience,planning', 1, 230, GETUTCDATE()),
('TCFD-RM-A', 'Risk identification', 'Processes for identifying and assessing climate-related risks', 'TCFD', 'Risk Management', 'identification,assessment,processes', 1, 240, GETUTCDATE()),
('TCFD-RM-B', 'Risk management', 'Processes for managing climate-related risks', 'TCFD', 'Risk Management', 'management,processes,mitigation', 1, 250, GETUTCDATE()),
('TCFD-RM-C', 'Risk integration', 'Integration of climate-related risk processes into overall risk management', 'TCFD', 'Risk Management', 'integration,enterprise-risk', 1, 260, GETUTCDATE()),
('TCFD-Met-A', 'Metrics used', 'Metrics used to assess climate-related risks and opportunities', 'TCFD', 'Metrics & Targets', 'metrics,measurement,kpis', 1, 270, GETUTCDATE()),
('TCFD-Met-B', 'Scope 1 and 2 emissions', 'Scope 1, Scope 2, and if appropriate, Scope 3 greenhouse gas emissions', 'TCFD', 'Metrics & Targets', 'emissions,scope1,scope2,scope3', 1, 280, GETUTCDATE()),
('TCFD-Met-C', 'Climate-related targets', 'Targets used to manage climate-related risks and opportunities', 'TCFD', 'Metrics & Targets', 'targets,goals,management', 1, 290, GETUTCDATE());

-- EU Taxonomy
INSERT INTO QuestionAttributes (Code, Name, Description, Category, Subcategory, Tags, IsActive, DisplayOrder, CreatedAt) VALUES
('EU-TAX-CC', 'Climate change mitigation', 'Economic activities contributing to climate change mitigation objectives', 'EU Taxonomy', 'Environmental', 'climate,mitigation,activities', 1, 300, GETUTCDATE()),
('EU-TAX-CCA', 'Climate change adaptation', 'Economic activities contributing to climate change adaptation objectives', 'EU Taxonomy', 'Environmental', 'climate,adaptation,resilience', 1, 310, GETUTCDATE()),
('EU-TAX-WTR', 'Water and marine resources', 'Sustainable use and protection of water and marine resources', 'EU Taxonomy', 'Environmental', 'water,marine,sustainable-use', 1, 320, GETUTCDATE()),
('EU-TAX-CE', 'Circular economy', 'Transition to a circular economy', 'EU Taxonomy', 'Environmental', 'circular,economy,transition', 1, 330, GETUTCDATE()),
('EU-TAX-PPC', 'Pollution prevention', 'Pollution prevention and control', 'EU Taxonomy', 'Environmental', 'pollution,prevention,control', 1, 340, GETUTCDATE()),
('EU-TAX-BIO', 'Biodiversity', 'Protection and restoration of biodiversity and ecosystems', 'EU Taxonomy', 'Environmental', 'biodiversity,ecosystems,protection', 1, 350, GETUTCDATE());

-- Custom attributes for common ESG topics
INSERT INTO QuestionAttributes (Code, Name, Description, Category, Subcategory, Tags, IsActive, DisplayOrder, CreatedAt) VALUES
('CUSTOM-SUPP-01', 'Supplier assessment', 'Assessment of suppliers using social and environmental criteria', 'Custom', 'Social', 'suppliers,assessment,due-diligence', 1, 360, GETUTCDATE()),
('CUSTOM-SUPP-02', 'Supplier incidents', 'Number of suppliers identified as having significant actual and potential negative impacts', 'Custom', 'Social', 'suppliers,incidents,risks', 1, 370, GETUTCDATE()),
('CUSTOM-GOV-01', 'Board composition', 'Composition of governance bodies by gender, age group, and other diversity indicators', 'Custom', 'Governance', 'board,composition,diversity', 1, 380, GETUTCDATE()),
('CUSTOM-GOV-02', 'Ethics training', 'Total number and percentage of employees that have received training on anti-corruption', 'Custom', 'Governance', 'ethics,training,anti-corruption', 1, 390, GETUTCDATE()),
('CUSTOM-SOC-01', 'Community investment', 'Total amount of monetary and in-kind contributions to communities', 'Custom', 'Social', 'community,investment,contributions', 1, 400, GETUTCDATE()),
('CUSTOM-ENV-01', 'Renewable energy', 'Percentage of energy consumption from renewable sources', 'Custom', 'Environmental', 'renewable,energy,percentage', 1, 410, GETUTCDATE()),
('CUSTOM-ENV-02', 'Carbon intensity', 'Carbon intensity per unit of revenue or production', 'Custom', 'Environmental', 'carbon,intensity,efficiency', 1, 420, GETUTCDATE());

-- Display total count
SELECT 'Question attributes inserted successfully!' as Message, COUNT(*) as TotalCount FROM QuestionAttributes; 