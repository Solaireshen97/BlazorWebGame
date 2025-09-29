using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorWebGame.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionTargets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PlayerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Progress = table.Column<double>(type: "REAL", nullable: false),
                    Duration = table.Column<double>(type: "REAL", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProgressDataJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionTargets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BattleRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BattleId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BattleType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "Normal"),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "InProgress"),
                    ParticipantsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    EnemiesJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    ActionsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    ResultsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    PartyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DungeonId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    WaveNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfflineData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PlayerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DataJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    IsSynced = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    Health = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxHealth = table.Column<int>(type: "INTEGER", nullable: false),
                    Gold = table.Column<int>(type: "INTEGER", nullable: false),
                    SelectedBattleProfession = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CurrentAction = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CurrentActionTargetId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PartyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AttributesJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    InventoryJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    SkillsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    EquipmentJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CaptainId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MaxMembers = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Active"),
                    MemberIdsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    CurrentBattleId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LastBattleAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionTargets_IsCompleted",
                table: "ActionTargets",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_ActionTargets_PlayerId",
                table: "ActionTargets",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionTargets_PlayerId_IsCompleted",
                table: "ActionTargets",
                columns: new[] { "PlayerId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionTargets_PlayerId_StartedAt",
                table: "ActionTargets",
                columns: new[] { "PlayerId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionTargets_StartedAt",
                table: "ActionTargets",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BattleRecords_BattleId",
                table: "BattleRecords",
                column: "BattleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BattleRecords_BattleType",
                table: "BattleRecords",
                column: "BattleType");

            migrationBuilder.CreateIndex(
                name: "IX_BattleRecords_PartyId",
                table: "BattleRecords",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleRecords_PartyId_Status",
                table: "BattleRecords",
                columns: new[] { "PartyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BattleRecords_StartedAt",
                table: "BattleRecords",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BattleRecords_StartedAt_Status_Type",
                table: "BattleRecords",
                columns: new[] { "StartedAt", "Status", "BattleType" });

            migrationBuilder.CreateIndex(
                name: "IX_BattleRecords_Status",
                table: "BattleRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BattleRecords_Status_StartedAt",
                table: "BattleRecords",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineData_CreatedAt_IsSynced",
                table: "OfflineData",
                columns: new[] { "CreatedAt", "IsSynced" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineData_DataType",
                table: "OfflineData",
                column: "DataType");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineData_IsSynced",
                table: "OfflineData",
                column: "IsSynced");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineData_PlayerId",
                table: "OfflineData",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineData_PlayerId_DataType",
                table: "OfflineData",
                columns: new[] { "PlayerId", "DataType" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineData_PlayerId_IsSynced",
                table: "OfflineData",
                columns: new[] { "PlayerId", "IsSynced" });

            migrationBuilder.CreateIndex(
                name: "IX_Players_IsOnline",
                table: "Players",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_Players_IsOnline_LastActiveAt",
                table: "Players",
                columns: new[] { "IsOnline", "LastActiveAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Players_LastActiveAt",
                table: "Players",
                column: "LastActiveAt");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Level_IsOnline",
                table: "Players",
                columns: new[] { "Level", "IsOnline" });

            migrationBuilder.CreateIndex(
                name: "IX_Players_Name",
                table: "Players",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_PartyId",
                table: "Players",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_PartyId_IsOnline",
                table: "Players",
                columns: new[] { "PartyId", "IsOnline" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CaptainId",
                table: "Teams",
                column: "CaptainId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatedAt",
                table: "Teams",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Status",
                table: "Teams",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Status_CreatedAt",
                table: "Teams",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionTargets");

            migrationBuilder.DropTable(
                name: "BattleRecords");

            migrationBuilder.DropTable(
                name: "OfflineData");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
