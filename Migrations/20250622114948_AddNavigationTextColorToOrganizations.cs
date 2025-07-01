﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationTextColorToOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NavigationTextColor",
                table: "Organizations",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NavigationTextColor",
                table: "Organizations");
        }
    }
}
