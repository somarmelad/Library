using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library2.Migrations
{
    /// <inheritdoc />
    public partial class MakeAuthorMiddleNameNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
            name: "MiddleName",
            table: "Authors", // Название таблицы
            type: "nvarchar(45)", // Ваш тип данных
            nullable: true,      // <<< ГЛАВНОЕ: Установлено в true
            oldClrType: typeof(string),
            oldType: "nvarchar(45)",
            oldNullable: false); // Было false, стало true
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
