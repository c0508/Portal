using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESGPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddNumericEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPercentage",
                table: "Questions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Questions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            // Seed predefined units
            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Code", "Name", "Description", "Category", "Symbol", "IsActive", "DisplayOrder", "CreatedAt" },
                values: new object[,]
                {
                    // Energy units
                    { "MWh", "Megawatt Hours", "Energy consumption in megawatt hours", "Energy", "MWh", true, 1, DateTime.UtcNow },
                    { "kWh", "Kilowatt Hours", "Energy consumption in kilowatt hours", "Energy", "kWh", true, 2, DateTime.UtcNow },
                    { "GWh", "Gigawatt Hours", "Energy consumption in gigawatt hours", "Energy", "GWh", true, 3, DateTime.UtcNow },
                    { "BTU", "British Thermal Units", "Energy measurement in British thermal units", "Energy", "BTU", true, 4, DateTime.UtcNow },
                    { "J", "Joules", "Energy measurement in joules", "Energy", "J", true, 5, DateTime.UtcNow },
                    
                    // Weight/Mass units
                    { "kg", "Kilograms", "Mass measurement in kilograms", "Weight", "kg", true, 10, DateTime.UtcNow },
                    { "tonnes", "Tonnes", "Mass measurement in metric tonnes", "Weight", "tonnes", true, 11, DateTime.UtcNow },
                    { "lbs", "Pounds", "Mass measurement in pounds", "Weight", "lbs", true, 12, DateTime.UtcNow },
                    { "g", "Grams", "Mass measurement in grams", "Weight", "g", true, 13, DateTime.UtcNow },
                    { "mt", "Metric Tons", "Mass measurement in metric tons", "Weight", "mt", true, 14, DateTime.UtcNow },
                    
                    // Distance units
                    { "km", "Kilometers", "Distance measurement in kilometers", "Distance", "km", true, 20, DateTime.UtcNow },
                    { "miles", "Miles", "Distance measurement in miles", "Distance", "miles", true, 21, DateTime.UtcNow },
                    { "m", "Meters", "Distance measurement in meters", "Distance", "m", true, 22, DateTime.UtcNow },
                    { "ft", "Feet", "Distance measurement in feet", "Distance", "ft", true, 23, DateTime.UtcNow },
                    
                    // Volume units
                    { "L", "Liters", "Volume measurement in liters", "Volume", "L", true, 30, DateTime.UtcNow },
                    { "gal", "Gallons", "Volume measurement in gallons", "Volume", "gal", true, 31, DateTime.UtcNow },
                    { "m3", "Cubic Meters", "Volume measurement in cubic meters", "Volume", "m³", true, 32, DateTime.UtcNow },
                    { "ml", "Milliliters", "Volume measurement in milliliters", "Volume", "ml", true, 33, DateTime.UtcNow },
                    
                    // Time units
                    { "hours", "Hours", "Time measurement in hours", "Time", "hours", true, 40, DateTime.UtcNow },
                    { "days", "Days", "Time measurement in days", "Time", "days", true, 41, DateTime.UtcNow },
                    { "months", "Months", "Time measurement in months", "Time", "months", true, 42, DateTime.UtcNow },
                    { "years", "Years", "Time measurement in years", "Time", "years", true, 43, DateTime.UtcNow },
                    
                    // Greenhouse Gas Emissions
                    { "tCO2e", "Tonnes CO2 Equivalent", "Greenhouse gas emissions in tonnes of CO2 equivalent", "Emissions", "tCO2e", true, 50, DateTime.UtcNow },
                    { "kgCO2e", "Kilograms CO2 Equivalent", "Greenhouse gas emissions in kilograms of CO2 equivalent", "Emissions", "kgCO2e", true, 51, DateTime.UtcNow },
                    { "MTCO2e", "Million Tonnes CO2 Equivalent", "Greenhouse gas emissions in million tonnes of CO2 equivalent", "Emissions", "MTCO2e", true, 52, DateTime.UtcNow },
                    
                    // Water usage
                    { "m3water", "Cubic Meters Water", "Water usage in cubic meters", "Water", "m³ water", true, 60, DateTime.UtcNow },
                    { "galwater", "Gallons Water", "Water usage in gallons", "Water", "gal water", true, 61, DateTime.UtcNow },
                    { "Lwater", "Liters Water", "Water usage in liters", "Water", "L water", true, 62, DateTime.UtcNow },
                    
                    // Count/Quantity
                    { "count", "Count", "Simple count or quantity", "Count", "", true, 70, DateTime.UtcNow },
                    { "pieces", "Pieces", "Number of pieces or items", "Count", "pcs", true, 71, DateTime.UtcNow },
                    { "employees", "Employees", "Number of employees", "Count", "employees", true, 72, DateTime.UtcNow }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropColumn(
                name: "IsPercentage",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Questions");
        }
    }
}
