using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.DTO.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services;

public static class OrderComboSelectionSchema
{
    public static async Task EnsureAndBackfillAsync(CinemaManagementContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[dbo].[ORDERCOMBOSELECTIONS]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ORDERCOMBOSELECTIONS]
                (
                    [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ORDERCOMBOSELECTIONS] PRIMARY KEY,
                    [OrderDetailId] INT NOT NULL,
                    [ComboId] INT NOT NULL,
                    [FoodId] INT NOT NULL,
                    [FoodNameSnapshot] NVARCHAR(255) NOT NULL,
                    [CategorySnapshot] NVARCHAR(100) NULL,
                    [Quantity] INT NOT NULL,
                    [CreatedAt] DATETIME NOT NULL CONSTRAINT [DF_ORDERCOMBOSELECTIONS_CREATED] DEFAULT(GETDATE()),
                    CONSTRAINT [FK_ORDERCOMBOSELECTIONS_ORDERITEM]
                        FOREIGN KEY ([OrderDetailId]) REFERENCES [dbo].[ORDERITEMS]([OrderItemId]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_ORDERCOMBOSELECTIONS_ORDERDETAIL]
                    ON [dbo].[ORDERCOMBOSELECTIONS]([OrderDetailId]);
            END
            """);

        var legacyItems = await context.Orderitems
            .AsNoTracking()
            .Where(item => item.ComboId != null && item.ComboSelectionSnapshot != null)
            .Where(item => !context.OrderComboSelections.Any(selection => selection.OrderDetailId == item.OrderItemId))
            .Select(item => new
            {
                item.OrderItemId,
                ComboId = item.ComboId!.Value,
                item.ComboSelectionSnapshot
            })
            .ToListAsync();

        foreach (var item in legacyItems)
        {
            var selections = OrderItemSnapshotHelper.Parse(item.ComboSelectionSnapshot).ComboSelections;
            foreach (var selection in selections.Where(selection => selection.FoodId > 0 && selection.Quantity > 0))
            {
                context.OrderComboSelections.Add(new OrderComboSelection
                {
                    OrderDetailId = item.OrderItemId,
                    ComboId = item.ComboId,
                    FoodId = selection.FoodId,
                    FoodNameSnapshot = selection.FoodName,
                    CategorySnapshot = selection.Category,
                    Quantity = selection.Quantity,
                    CreatedAt = DateTime.Now
                });
            }
        }

        if (context.ChangeTracker.HasChanges())
            await context.SaveChangesAsync();
    }
}
