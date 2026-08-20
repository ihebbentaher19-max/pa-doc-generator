using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PADocGenerator.Api.Migrations;

/// <summary>Ajoute la traçabilité des flux importés depuis Power Platform.</summary>
public partial class AddPowerPlatformFlowImport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Source",
            table: "FlowImports",
            type: "text",
            nullable: false,
            defaultValue: "JsonFile");

        migrationBuilder.AddColumn<string>(
            name: "PowerPlatformEnvironmentId",
            table: "FlowImports",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PowerPlatformTenantId",
            table: "FlowImports",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PowerPlatformWorkflowId",
            table: "FlowImports",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_FlowImports_PowerPlatformEnvironmentId_PowerPlatformWorkflowId",
            table: "FlowImports",
            columns: new[] { "PowerPlatformEnvironmentId", "PowerPlatformWorkflowId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FlowImports_PowerPlatformEnvironmentId_PowerPlatformWorkflowId",
            table: "FlowImports");

        migrationBuilder.DropColumn(name: "Source", table: "FlowImports");
        migrationBuilder.DropColumn(name: "PowerPlatformEnvironmentId", table: "FlowImports");
        migrationBuilder.DropColumn(name: "PowerPlatformTenantId", table: "FlowImports");
        migrationBuilder.DropColumn(name: "PowerPlatformWorkflowId", table: "FlowImports");
    }
}
