CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "FullName" character varying(200) NOT NULL,
    "Email" character varying(256) NOT NULL,
    "PasswordHash" text NOT NULL,
    "Role" text NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "FlowImports" (
    "Id" uuid NOT NULL,
    "Name" character varying(300) NOT NULL,
    "RawJson" jsonb NOT NULL,
    "ActionsCount" integer NOT NULL,
    "ImportedByUserId" uuid NOT NULL,
    "ImportedAtUtc" timestamp with time zone NOT NULL,
    "IsValid" boolean NOT NULL,
    "ValidationError" text,
    CONSTRAINT "PK_FlowImports" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FlowImports_Users_ImportedByUserId" FOREIGN KEY ("ImportedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Documentations" (
    "Id" uuid NOT NULL,
    "FlowImportId" uuid NOT NULL,
    "Title" character varying(300) NOT NULL,
    "Status" text NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "CurrentVersionNumber" integer NOT NULL,
    CONSTRAINT "PK_Documentations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Documentations_FlowImports_FlowImportId" FOREIGN KEY ("FlowImportId") REFERENCES "FlowImports" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Documentations_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "DocumentationVersions" (
    "Id" uuid NOT NULL,
    "DocumentationId" uuid NOT NULL,
    "VersionNumber" integer NOT NULL,
    "FunctionalSummary" text NOT NULL,
    "StructuredContentJson" jsonb NOT NULL,
    "IsManuallyEdited" boolean NOT NULL,
    "EditedByUserId" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "ChangeNote" text,
    CONSTRAINT "PK_DocumentationVersions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DocumentationVersions_Documentations_DocumentationId" FOREIGN KEY ("DocumentationId") REFERENCES "Documentations" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DocumentationVersions_Users_EditedByUserId" FOREIGN KEY ("EditedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Documentations_CreatedByUserId" ON "Documentations" ("CreatedByUserId");

CREATE INDEX "IX_Documentations_FlowImportId" ON "Documentations" ("FlowImportId");

CREATE UNIQUE INDEX "IX_DocumentationVersions_DocumentationId_VersionNumber" ON "DocumentationVersions" ("DocumentationId", "VersionNumber");

CREATE INDEX "IX_DocumentationVersions_EditedByUserId" ON "DocumentationVersions" ("EditedByUserId");

CREATE INDEX "IX_FlowImports_ImportedByUserId" ON "FlowImports" ("ImportedByUserId");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260730093225_InitialCreate', '8.0.10');

COMMIT;

START TRANSACTION;

ALTER TABLE "FlowImports" ADD "Source" text NOT NULL DEFAULT 'JsonFile';

ALTER TABLE "FlowImports" ADD "PowerPlatformEnvironmentId" character varying(128);

ALTER TABLE "FlowImports" ADD "PowerPlatformTenantId" character varying(64);

ALTER TABLE "FlowImports" ADD "PowerPlatformWorkflowId" character varying(64);

CREATE INDEX "IX_FlowImports_PowerPlatformEnvironmentId_PowerPlatformWorkflowId" ON "FlowImports" ("PowerPlatformEnvironmentId", "PowerPlatformWorkflowId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260820100000_AddPowerPlatformFlowImport', '8.0.10');

COMMIT;

