using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPI_IA.Migrations
{
    /// <inheritdoc />
    public partial class TablaSolicitudesAprobacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesAprobacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConversacionChatId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreTool = table.Column<string>(type: "TEXT", nullable: false),
                    ArgumentosJson = table.Column<string>(type: "TEXT", nullable: false),
                    SolicitudJson = table.Column<string>(type: "TEXT", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaResolucionUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesAprobacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesAprobacion_Conversaciones_ConversacionChatId",
                        column: x => x.ConversacionChatId,
                        principalTable: "Conversaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesAprobacion_ConversacionChatId",
                table: "SolicitudesAprobacion",
                column: "ConversacionChatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesAprobacion");
        }
    }
}
