using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorefrontAppCore.Migrations
{
    /// <inheritdoc />
    public partial class RenamedPaymentMethodIsDeactivated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDeactivated",
                table: "PaymentMethods",
                newName: "Deactivated");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Deactivated",
                table: "PaymentMethods",
                newName: "IsDeactivated");
        }
    }
}
