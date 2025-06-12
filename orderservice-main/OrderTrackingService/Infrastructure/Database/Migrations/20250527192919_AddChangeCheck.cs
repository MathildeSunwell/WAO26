﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderTrackingService.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ChangeCheck",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangeCheck",
                table: "Orders");
        }
    }
}
