using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dortageDB.Migrations
{
    /// <inheritdoc />
    public partial class MigrateTopraktarRoleToVisioner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Move all topraktar users to visioner role if visioner role exists
            // First, find all users with topraktar role
            migrationBuilder.Sql(
                @"IF EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'topraktar')
                  BEGIN
                    DECLARE @topraktarRoleId INT;
                    DECLARE @visionerRoleId INT;

                    SELECT @topraktarRoleId = Id FROM AspNetRoles WHERE Name = 'topraktar';
                    SELECT @visionerRoleId = Id FROM AspNetRoles WHERE Name = 'visioner';

                    -- If visioner role exists, move all topraktar users to it
                    IF @visionerRoleId IS NOT NULL
                    BEGIN
                      -- Update user roles
                      UPDATE AspNetUserRoles
                      SET RoleId = @visionerRoleId
                      WHERE RoleId = @topraktarRoleId;
                    END

                    -- Delete topraktar role
                    DELETE FROM AspNetRoles WHERE Name = 'topraktar';
                  END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration is not reversible - we cannot safely revert without losing data
            // If you need to revert, you would need to:
            // 1. Create the topraktar role again
            // 2. Manually reassign users back to topraktar role
            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'topraktar')
                  BEGIN
                    INSERT INTO AspNetRoles (Name, NormalizedName, ConcurrencyStamp)
                    VALUES ('topraktar', 'TOPRAKTAR', NEWID());
                  END");
        }
    }
}
