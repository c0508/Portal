using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class FixExistingAssignmentRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing CampaignAssignments to set OrganizationRelationshipId
            // This fixes the critical issue where existing assignments lack relationship context
            migrationBuilder.Sql(@"
                UPDATE ca 
                SET OrganizationRelationshipId = oro.Id
                FROM CampaignAssignments ca
                INNER JOIN Campaigns c ON ca.CampaignId = c.Id
                INNER JOIN OrganizationRelationships oro ON 
                    oro.PlatformOrganizationId = c.OrganizationId 
                    AND oro.SupplierOrganizationId = ca.TargetOrganizationId
                    AND oro.IsActive = 1
                WHERE ca.OrganizationRelationshipId IS NULL
            ");

            // Log the number of assignments that were updated
            migrationBuilder.Sql(@"
                PRINT 'Updated ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + ' existing campaign assignments with relationship IDs'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
