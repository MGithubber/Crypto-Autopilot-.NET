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
                    
                    -- Check if all inserted records have the same [Position Side]
					IF EXISTS (SELECT 1 FROM inserted WHERE [Position Side] != (SELECT TOP 1 [Position Side] FROM inserted))
					BEGIN
						RAISERROR('All inserted records must have the same position side', 16, 1);
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
                    
                    -- Throw if records that opened a position have a NULL PositionId
                    IF EXISTS (SELECT * FROM inserted WHERE ([Order Type] = 'Market' OR ([Order Type] = 'Limit' AND [Order Status] = 'Filled')) AND PositionId IS NULL)
                    BEGIN
                        RAISERROR('Records with Type ""Market"" or Type ""Limit"" and Status ""Filled"" must have a PositionId', 16, 1);
                        ROLLBACK TRANSACTION;
                        RETURN;
                    END
                    
                    -- Throw if records that did not open a position have PositionId which is not NULL
                    IF EXISTS (SELECT * FROM inserted WHERE ([Order Type] = 'Limit' AND [Order Status] = 'Created') AND PositionId IS NOT NULL)
                    BEGIN
                        RAISERROR('Records with Type ""Market"" or Type ""Limit"" and Status ""Filled"" must have a PositionId', 16, 1);
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
