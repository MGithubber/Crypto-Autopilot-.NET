using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersValidationTriggerMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE TRIGGER [OrdersValidationTrigger]
	            ON [dbo].[FuturesOrders]
	            FOR INSERT, UPDATE
	            AS
	            BEGIN
		            SET NOCOUNT ON
                    
		            -- Check if all inserted records have the same PositionId
					IF EXISTS (SELECT 1 FROM inserted WHERE PositionId != (SELECT TOP 1 PositionId FROM inserted))
					BEGIN
						RAISERROR('All inserted records must have the same PositionId', 16, 1);
						ROLLBACK TRANSACTION;
						RETURN;
					END
                    
					-- Check if all inserted records have the same CurrencyPair
					IF EXISTS (SELECT 1 FROM inserted WHERE CurrencyPair != (SELECT TOP 1 CurrencyPair FROM inserted))
					BEGIN
						RAISERROR('All inserted records must have the same CurrencyPair', 16, 1);
						ROLLBACK TRANSACTION;
						RETURN;
					END
	            END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER [dbo].[FuturesOrders].[OrdersValidationTrigger]");
        }
    }
}
