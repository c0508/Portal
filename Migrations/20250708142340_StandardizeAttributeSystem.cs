using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeAttributeSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add standard attribute types for relationship attributes
            // Note: We keep OrganizationAttributeTypes for standardization but eliminate OrganizationAttributeValues
            // Each relationship can have its own attribute values based on context
            
            migrationBuilder.Sql(@"
                -- Insert standard attribute types if they don't exist
                IF NOT EXISTS (SELECT 1 FROM OrganizationAttributeTypes WHERE Code = 'SECTOR')
                INSERT INTO OrganizationAttributeTypes (Code, Name, Description, IsActive, CreatedAt)
                VALUES ('SECTOR', 'Sector', 'Business sector classification', 1, GETUTCDATE());

                IF NOT EXISTS (SELECT 1 FROM OrganizationAttributeTypes WHERE Code = 'INDUSTRY')
                INSERT INTO OrganizationAttributeTypes (Code, Name, Description, IsActive, CreatedAt)
                VALUES ('INDUSTRY', 'Industry', 'Industry classification', 1, GETUTCDATE());

                IF NOT EXISTS (SELECT 1 FROM OrganizationAttributeTypes WHERE Code = 'ABC_SEGMENTATION')
                INSERT INTO OrganizationAttributeTypes (Code, Name, Description, IsActive, CreatedAt)
                VALUES ('ABC_SEGMENTATION', 'ABC Segmentation', 'Priority classification (A=High, B=Medium, C=Low)', 1, GETUTCDATE());

                IF NOT EXISTS (SELECT 1 FROM OrganizationAttributeTypes WHERE Code = 'SUPPLIER_CLASSIFICATION')
                INSERT INTO OrganizationAttributeTypes (Code, Name, Description, IsActive, CreatedAt)
                VALUES ('SUPPLIER_CLASSIFICATION', 'Supplier Classification', 'Business relationship type', 1, GETUTCDATE());

                IF NOT EXISTS (SELECT 1 FROM OrganizationAttributeTypes WHERE Code = 'REGION')
                INSERT INTO OrganizationAttributeTypes (Code, Name, Description, IsActive, CreatedAt)
                VALUES ('REGION', 'Region', 'Geographic region', 1, GETUTCDATE());

                IF NOT EXISTS (SELECT 1 FROM OrganizationAttributeTypes WHERE Code = 'SIZE_CATEGORY')
                INSERT INTO OrganizationAttributeTypes (Code, Name, Description, IsActive, CreatedAt)
                VALUES ('SIZE_CATEGORY', 'Size Category', 'Organization size classification', 1, GETUTCDATE());
            ");

            // Add a comment to OrganizationAttributeValues table indicating it's deprecated
            migrationBuilder.Sql(@"
                -- Mark OrganizationAttributeValues as deprecated in favor of relationship-specific attributes
                IF NOT EXISTS (
                    SELECT 1 FROM sys.extended_properties 
                    WHERE major_id = OBJECT_ID('OrganizationAttributeValues') 
                    AND name = 'MS_Description'
                )
                EXEC sys.sp_addextendedproperty 
                    @name = N'MS_Description', 
                    @value = N'DEPRECATED: Use OrganizationRelationshipAttributes instead. This table will be removed in a future version.', 
                    @level0type = N'SCHEMA', @level0name = N'dbo', 
                    @level1type = N'TABLE', @level1name = N'OrganizationAttributeValues';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the deprecation comment
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.extended_properties 
                    WHERE major_id = OBJECT_ID('OrganizationAttributeValues') 
                    AND name = 'MS_Description'
                )
                EXEC sys.sp_dropextendedproperty 
                    @name = N'MS_Description', 
                    @level0type = N'SCHEMA', @level0name = N'dbo', 
                    @level1type = N'TABLE', @level1name = N'OrganizationAttributeValues';
            ");

            // Note: We don't remove the standard attribute types in rollback 
            // as they might already be in use in relationships
        }
    }
}
