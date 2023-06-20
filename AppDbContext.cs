using Vks.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vks.Shared.Other;
using System.Text.Json;

namespace Vks.Server.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            };

            // Konverze sloupcu na JSON
            builder.Entity<EfOrder>().OwnsOne(
                it => it.Properties, ownedNavigationBuilder =>
                {
                    ownedNavigationBuilder.ToJson();
                }
            );

            // Konverze - OrderType
            var orderTypeConvert = new ValueConverter<OrderType, string>(
                    it => it.Name,
                    it => OrderType.FromDisplayName<OrderType>(it));

            builder.Entity<EfOrderTypeEnum>()
                .Property(it => it.Type)
                .HasConversion(orderTypeConvert);

            // Konverze - OrderStepType
            var orderStepTypeConvert = new ValueConverter<OrderStepType, string>(
                    it => it.Name,
                    it => OrderStepType.FromDisplayName<OrderStepType>(it));

            builder.Entity<EfOrderStepTypeEnum>()
                .Property(it => it.Type)
                .HasConversion(orderStepTypeConvert);

            // Konverze - OrderStepType
            var productTypeConvert = new ValueConverter<ProductCategoryType, string>(
                    it => it.Name,
                    it => ProductCategoryType.FromDisplayName<ProductCategoryType>(it));

            builder.Entity<EfProductCategoryTypeEnum>()
                .Property(it => it.Type)
                .HasConversion(productTypeConvert);

            // Konverze - AmountUnit
            var amountUnitConvert = new ValueConverter<AmountUnitType, string>(
                    it => it.Name,
                    it => AmountUnitType.FromDisplayName<AmountUnitType>(it));

            builder.Entity<EfAmountUnitEnum>()
                .Property(it => it.Type)
                .HasConversion(amountUnitConvert);

            // Konverze - ControlType
            var controlTypeConvert = new ValueConverter<ControlType, string>(
                    it => it.Name,
                    it => ControlType.FromDisplayName<ControlType>(it));

            builder.Entity<EfControlTypeEnum>()
                .Property(it => it.Type)
                .HasConversion(controlTypeConvert);

            // Konverze - StorageType
            var storageTypeConvert = new ValueConverter<StorageType, string>(
                    it => it.Name,
                    it => StorageType.FromDisplayName<StorageType>(it));

            builder.Entity<EfFile>()
                .Property(it => it.Storage)
                .HasConversion(storageTypeConvert);

            // Konverze - StockItemStepType
            var stockStepTypeConvert = new ValueConverter<StockItemStepType, string>(
                    it => it.Name,
                    it => StockItemStepType.FromDisplayName<StockItemStepType>(it));

            builder.Entity<EfStockItemStepTypeEnum>()
                .Property(it => it.Type)
                .HasConversion(stockStepTypeConvert);

            // Konverze - ProductFileType
            var productFileTypeConvert = new ValueConverter<ProductFileType, string>(
                    it => it.Name,
                    it => ProductFileType.FromDisplayName<ProductFileType>(it));

            builder.Entity<EfProductFileTypeEnum>()
                .Property(it => it.Type)
                .HasConversion(productFileTypeConvert);

            // Konverze - StockItemStepType
            var productPermissionsConvert = new ValueConverter<ProductPermission, string>(
                    it => JsonSerializer.Serialize(it, options),
                    it => it == null ? new ProductPermission() : JsonSerializer.Deserialize<ProductPermission>(it, options));

            builder.Entity<EfPermissionProduct>()
                .Property(it => it.Permissions)
                .HasConversion(productPermissionsConvert);

            var groupPermissionsConvert = new ValueConverter<GroupPermission, string>(
                    it => JsonSerializer.Serialize(it, options),
                    it => it == null ? new GroupPermission() : JsonSerializer.Deserialize<GroupPermission>(it, options));

            builder.Entity<EfPermissionGroup>()
                .Property(it => it.Permissions)
                .HasConversion(groupPermissionsConvert);


            // StockItemOptionValue - deaktivace kaskadoveho mazani
            builder.Entity<EfStockItemOptionValue>()
                .HasOne(it => it.StockItemOption)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // StockItemOption - deaktivace kaskadoveho mazani
            builder.Entity<EfStockItemOption>()
                .HasOne(it => it.StockItem)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // StockItemOption - deaktivace kaskadoveho mazani
            builder.Entity<EfOptionAccessorySet>()
                .HasOne(it => it.Product)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // OrderItemOptionValue - deaktivace kaskadoveho mazani
            builder.Entity<EfOrderItemOptionValue>()
                .HasOne(it => it.OptionValue)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // EfProductSubproduct
            builder.Entity<EfProductSubproduct>()
                .HasOne(it => it.Product)
                .WithMany()
                .HasForeignKey(it => it.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<EfProductSubproduct>()
                .HasOne(it => it.SubProduct)
                .WithMany()
                .HasForeignKey(it => it.SubProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // EfPermissionGroup..
            builder.Entity<EfPermissionGroupStep>()
                .HasOne(it => it.PermissionGroup)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<EfPermissionGroupUser>()
                .HasOne(it => it.PermissionGroup)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // EfOrderState
            builder.Entity<EfOrderState>()
                .HasOne(it => it.Order)
                .WithMany(it => it.OrderStates)
                .OnDelete(DeleteBehavior.Restrict);

            // EfPermissionGroup..
            builder.Entity<EfOrderStateLast>()
                .HasOne(it => it.Order)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // EfOrderStateLast
            builder.Entity<EfOrder>()
                .HasOne(it => it.LastState)
                .WithOne(it => it.Order);

            // EfEfOrderItem
            builder.Entity<EfOrderItem>()
                .HasOne(it => it.LastState)
                .WithOne(it => it.OrderItem);

            // EfOrderItemStateLast
            builder.Entity<EfOrderItemStateLast>()
                .HasOne(it => it.State)
                .WithOne()
                .OnDelete(DeleteBehavior.Restrict);

            // EfOrderItemStateLast
            builder.Entity<EfPermissionGroup>()
                .HasMany(it => it.ProductPermissions)
                .WithOne(it => it.Group);




			// STOCK ITEM STATE
			builder.Entity<EfStockItem>()
				.HasMany(it => it.States)
				.WithOne(it => it.StockItem)
                .HasForeignKey(it => it.StockItemId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<EfStockItemState>()
				.HasOne(it => it.StockItem)
				.WithMany(it => it.States)
				.HasForeignKey(it => it.StockItemId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<EfStockItem>()
				.HasOne(it => it.LastState)
				.WithMany()
				.HasForeignKey(it => it.LastStateId)
				.OnDelete(DeleteBehavior.Restrict);
		}

        public DbSet<EfAppSettings> AppSettings { get; set; }

        public DbSet<EfCountry> Country { get; set; }
        public DbSet<EfCustomer> Customer { get; set; }

        public DbSet<EfOrderTypeEnum> OrderTypeEnum { get; set; }
        public DbSet<EfOrder> Order { get; set; }
        public DbSet<EfOrderState> OrderState { get; set; }
        public DbSet<EfOrderStateLast> OrderStateLast { get; set; }
        public DbSet<EfOrderStepTypeEnum> OrderStepTypeEnum { get; set; }

        public DbSet<EfOrderItem> OrderItem { get; set; }
        public DbSet<EfOrderItemState> OrderItemState { get; set; }
        public DbSet<EfOrderItemStateLast> OrderItemStateLast { get; set; }
        public DbSet<EfOrderItemOption> OrderItemOption { get; set; }
        public DbSet<EfOrderItemOptionValue> OrderItemOptionValue { get; set; }

        public DbSet<EfProductCategoryTypeEnum> ProductCategoryTypeEnum { get; set; }
        public DbSet<EfProductCategory> ProductCategory { get; set; }
        public DbSet<EfAmountUnitEnum> AmountUnitEnum { get; set; }
        public DbSet<EfProductSubproduct> ProductSubproduct { get; set; }
        public DbSet<EfProduct> Product { get; set; }

        public DbSet<EfControlTypeEnum> ControlTypeEnum { get; set; }
        public DbSet<EfOption> Option { get; set; }
        public DbSet<EfOptionValue> OptionValue { get; set; }
        public DbSet<EfOptionAccessorySet> OptionAccessorySet { get; set; }

        public DbSet<EfOrderWorkflow> OrderWorkflow { get; set; }
        public DbSet<EfOrderWorkflowStep> OrderWorkflowStep { get; set; }
        
        public DbSet<EfPermissionGroup> PermissionGroup { get; set; }
        public DbSet<EfPermissionGroupStep> PermissionGroupStep { get; set; }
        public DbSet<EfPermissionGroupUser> PermissionGroupUser { get; set; }
        public DbSet<EfPermissionProduct> PermissionProduct { get; set; }

        public DbSet<EfStockItemOption> StockItemOption { get; set; }
        public DbSet<EfStockItemOptionValue> StockItemOptionValue { get; set; }
        public DbSet<EfStockItemState> StockItemState { get; set; }
        public DbSet<EfStockItemStepTypeEnum> StockItemStepTypeEnum { get; set; }
        public DbSet<EfStockItem> StockItem { get; set; }

        public DbSet<EfFile> File { get; set; }
        public DbSet<EfProductFileTypeEnum> ProductFileTypeEnum { get; set; }
        public DbSet<EfProductFile> ProductFile { get; set; }
        public DbSet<EfStockItemFile> StockItemFile { get; set; }
    }
}
