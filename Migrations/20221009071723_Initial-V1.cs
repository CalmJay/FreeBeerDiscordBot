using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordBot.Migrations
{
    public partial class InitialV1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MoneyType",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoneyType", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Player",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerName = table.Column<string>(unicode: false, maxLength: 50, nullable: true),
                    PlayerId = table.Column<string>(unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerLoot",
                columns: table => new
                {
                    TypeID = table.Column<int>(nullable: false),
                    PlayerID = table.Column<int>(nullable: false),
                    Loot = table.Column<decimal>(type: "decimal(18, 0)", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Reason = table.Column<string>(unicode: false, maxLength: 250, nullable: false),
                    PartyLeader = table.Column<string>(unicode: false, maxLength: 250, nullable: false),
                    KillId = table.Column<string>(unicode: false, maxLength: 250, nullable: false),
                    QueueId = table.Column<string>(unicode: false, maxLength: 250, nullable: false),
                    Message = table.Column<string>(unicode: false, maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => new { x.TypeID, x.PlayerID });
                    table.ForeignKey(
                        name: "FK__PlayerLoo__Playe__29572725",
                        column: x => x.PlayerID,
                        principalTable: "Player",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK__PlayerLoo__TypeI__286302EC",
                        column: x => x.TypeID,
                        principalTable: "MoneyType",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerLoot_PlayerID",
                table: "PlayerLoot",
                column: "PlayerID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerLoot");

            migrationBuilder.DropTable(
                name: "Player");

            migrationBuilder.DropTable(
                name: "MoneyType");
        }
    }
}
