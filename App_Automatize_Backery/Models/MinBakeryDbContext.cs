using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace App_Automatize_Backery.Models;

public partial class MinBakeryDbContext : DbContext
{
    public MinBakeryDbContext()
    {
    }

    public MinBakeryDbContext(DbContextOptions<MinBakeryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Expence> Expences { get; set; }

    public virtual DbSet<ExpencesReportsParish> ExpencesReportsParishes { get; set; }

    public virtual DbSet<MeasurementConversion> MeasurementConversions { get; set; }

    public virtual DbSet<MeasurementUnit> MeasurementUnits { get; set; }

    public virtual DbSet<Parish> Parishes { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Production> Productions { get; set; }

    public virtual DbSet<ProductionsRawMaterialsMeasurementUnitRecipe> ProductionsRawMaterialsMeasurementUnitRecipes { get; set; }

    public virtual DbSet<RawMaterial> RawMaterials { get; set; }

    public virtual DbSet<RawMaterialMeasurementUnitRecipe> RawMaterialMeasurementUnitRecipes { get; set; }

    public virtual DbSet<RawMaterialsWarehousesProduct> RawMaterialsWarehousesProducts { get; set; }

    public virtual DbSet<Recipe> Recipes { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SaleProduct> SaleProducts { get; set; }

    public virtual DbSet<SalesProduction> SalesProductions { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<SupplyRequest> SupplyRequests { get; set; }

    public virtual DbSet<SupplyRequestsRawMaterial> SupplyRequestsRawMaterials { get; set; }

    public virtual DbSet<TypesProduct> TypesProducts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=NEONOVIYY\\SQLEXPRESS;Database=MinBakeryDB;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Cyrillic_General_100_CI_AI_KS_SC_UTF8");

        modelBuilder.Entity<Expence>(entity =>
        {
            entity.Property(e => e.ExpenceId).HasColumnName("expenceID");
            entity.Property(e => e.ExpenceCoast)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("expenceCoast");
            entity.Property(e => e.ExpenceDate)
                .HasColumnType("datetime")
                .HasColumnName("expenceDate");
            entity.Property(e => e.SupplyRequestId).HasColumnName("supplyRequestId");

            entity.HasOne(d => d.SupplyRequest).WithMany(p => p.Expences)
                .HasForeignKey(d => d.SupplyRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Expences_SupplyRequests");
        });

        modelBuilder.Entity<ExpencesReportsParish>(entity =>
        {
            entity.HasKey(e => e.ExpenceReportParisheId);

            entity.Property(e => e.ExpenceReportParisheId).HasColumnName("expenceReportParisheID");
            entity.Property(e => e.ExpenceId).HasColumnName("expenceId");
            entity.Property(e => e.ParisheId).HasColumnName("parisheId");
            entity.Property(e => e.ReportId).HasColumnName("reportId");

            entity.HasOne(d => d.Expence).WithMany(p => p.ExpencesReportsParishes)
                .HasForeignKey(d => d.ExpenceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ExpencesReportsParishes_Expences");

            entity.HasOne(d => d.Parishe).WithMany(p => p.ExpencesReportsParishes)
                .HasForeignKey(d => d.ParisheId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ExpencesReportsParishes_Parishes");

            entity.HasOne(d => d.Report).WithMany(p => p.ExpencesReportsParishes)
                .HasForeignKey(d => d.ReportId)
                .HasConstraintName("FK_ExpencesReportsParishes_Reports");
        });

        modelBuilder.Entity<MeasurementConversion>(entity =>
        {
            entity.ToTable("MeasurementConversion");

            entity.Property(e => e.MeasurementConversionId).HasColumnName("measurementConversionID");
            entity.Property(e => e.ConversionFactor).HasColumnName("conversionFactor");
            entity.Property(e => e.FromMeasureUnitId).HasColumnName("fromMeasureUnitID");
            entity.Property(e => e.ToMeasureUnitId).HasColumnName("toMeasureUnitID");

            entity.HasOne(d => d.FromMeasureUnit).WithMany(p => p.MeasurementConversionFromMeasureUnits)
                .HasForeignKey(d => d.FromMeasureUnitId)
                .HasConstraintName("FK_MeasurementConversion_MeasurementUnit");

            entity.HasOne(d => d.ToMeasureUnit).WithMany(p => p.MeasurementConversionToMeasureUnits)
                .HasForeignKey(d => d.ToMeasureUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MeasurementConversion_MeasurementUnit1");
        });

        modelBuilder.Entity<MeasurementUnit>(entity =>
        {
            entity.ToTable("MeasurementUnit");

            entity.Property(e => e.MeasurementUnitId).HasColumnName("measurementUnitID");
            entity.Property(e => e.MeasurementUnitName)
                .HasMaxLength(50)
                .HasColumnName("measurementUnitName");
        });

        modelBuilder.Entity<Parish>(entity =>
        {
            entity.HasKey(e => e.ParisheId);

            entity.Property(e => e.ParisheId).HasColumnName("parisheID");
            entity.Property(e => e.ParisheDateTime)
                .HasColumnType("datetime")
                .HasColumnName("parisheDateTime");
            entity.Property(e => e.ParisheSize)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("parisheSize");
            entity.Property(e => e.SaleId).HasColumnName("saleId");

            entity.HasOne(d => d.Sale).WithMany(p => p.Parishes)
                .HasForeignKey(d => d.SaleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Parishes_Sales");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.ProductId).HasColumnName("productID");
            entity.Property(e => e.ProductCoast)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("productCoast");
            entity.Property(e => e.ProductName)
                .HasMaxLength(150)
                .HasColumnName("productName");
            entity.Property(e => e.StatusProduct)
                .HasMaxLength(150)
                .HasColumnName("statusProduct");
            entity.Property(e => e.TypeProductId).HasColumnName("typeProductId");

            entity.HasOne(d => d.TypeProduct).WithMany(p => p.Products)
                .HasForeignKey(d => d.TypeProductId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Products_TypesProduct");
        });

        modelBuilder.Entity<Production>(entity =>
        {
            entity.Property(e => e.ProductionId).HasColumnName("productionID");
            entity.Property(e => e.DateTimeEnd)
                .HasColumnType("datetime")
                .HasColumnName("dateTimeEnd");
            entity.Property(e => e.DateTimeStart)
                .HasColumnType("datetime")
                .HasColumnName("dateTimeStart");
        });

        modelBuilder.Entity<ProductionsRawMaterialsMeasurementUnitRecipe>(entity =>
        {
            entity.HasKey(e => e.ProductionRawMaterialMeasurementUnitRecipeId);

            entity.Property(e => e.ProductionRawMaterialMeasurementUnitRecipeId).HasColumnName("productionRawMaterialMeasurementUnitRecipeID");
            entity.Property(e => e.CountProduct).HasColumnName("countProduct");
            entity.Property(e => e.ProductionId).HasColumnName("productionId");
            entity.Property(e => e.RawMaterialMeasurementUnitRecipeId).HasColumnName("rawMaterialMeasurementUnitRecipeId");

            entity.HasOne(d => d.Production).WithMany(p => p.ProductionsRawMaterialsMeasurementUnitRecipes)
                .HasForeignKey(d => d.ProductionId)
                .HasConstraintName("FK_ProductionsRawMaterialsMeasurementUnitRecipes_Productions");

            entity.HasOne(d => d.RawMaterialMeasurementUnitRecipe).WithMany(p => p.ProductionsRawMaterialsMeasurementUnitRecipes)
                .HasForeignKey(d => d.RawMaterialMeasurementUnitRecipeId)
                .HasConstraintName("FK_ProductionsRawMaterialsMeasurementUnitRecipes_RawMaterialMeasurementUnitRecipe");
        });

        modelBuilder.Entity<RawMaterial>(entity =>
        {
            entity.Property(e => e.RawMaterialId).HasColumnName("rawMaterialID");
            entity.Property(e => e.MeasurementUnitId).HasColumnName("measurementUnitId");
            entity.Property(e => e.RawMaterialCoast)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("rawMaterialCoast");
            entity.Property(e => e.RawMaterialName)
                .HasMaxLength(150)
                .HasColumnName("rawMaterialName");
            entity.Property(e => e.ShelfLifeDays).HasColumnName("shelfLifeDays");
            entity.Property(e => e.StatusRawMaterial)
                .HasMaxLength(150)
                .HasColumnName("statusRawMaterial");

            entity.HasOne(d => d.MeasurementUnit).WithMany(p => p.RawMaterials)
                .HasForeignKey(d => d.MeasurementUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RawMaterials_MeasurementUnit");
        });

        modelBuilder.Entity<RawMaterialMeasurementUnitRecipe>(entity =>
        {
            entity.ToTable("RawMaterialMeasurementUnitRecipe");

            entity.Property(e => e.RawMaterialMeasurementUnitRecipeId).HasColumnName("rawMaterialMeasurementUnitRecipeID");
            entity.Property(e => e.CountRawMaterial).HasColumnName("countRawMaterial");
            entity.Property(e => e.MeasurementUnitId).HasColumnName("measurementUnitId");
            entity.Property(e => e.RawMaterialId).HasColumnName("rawMaterialId");
            entity.Property(e => e.RecipeId).HasColumnName("recipeId");

            entity.HasOne(d => d.MeasurementUnit).WithMany(p => p.RawMaterialMeasurementUnitRecipes)
                .HasForeignKey(d => d.MeasurementUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RawMaterialMeasurementUnitRecipe_MeasurementUnit");

            entity.HasOne(d => d.RawMaterial).WithMany(p => p.RawMaterialMeasurementUnitRecipes)
                .HasForeignKey(d => d.RawMaterialId)
                .HasConstraintName("FK_RawMaterialMeasurementUnitRecipe_RawMaterials");

            entity.HasOne(d => d.Recipe).WithMany(p => p.RawMaterialMeasurementUnitRecipes)
                .HasForeignKey(d => d.RecipeId)
                .HasConstraintName("FK_RawMaterialMeasurementUnitRecipe_Recipe");
        });

        modelBuilder.Entity<RawMaterialsWarehousesProduct>(entity =>
        {
            entity.HasKey(e => e.RawMaterialWarehouseId).HasName("PK_RawMaterialsWarehouses");

            entity.Property(e => e.RawMaterialWarehouseId).HasColumnName("rawMaterialWarehouseID");
            entity.Property(e => e.DateSupplyOrProduction)
                .HasColumnType("datetime")
                .HasColumnName("dateSupplyOrProduction");
            entity.Property(e => e.MeasurementUnitId).HasColumnName("measurementUnitId");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.RawMaterialCount).HasColumnName("rawMaterialCount");
            entity.Property(e => e.RawMaterialId).HasColumnName("rawMaterialId");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouseId");

            entity.HasOne(d => d.MeasurementUnit).WithMany(p => p.RawMaterialsWarehousesProducts)
                .HasForeignKey(d => d.MeasurementUnitId)
                .HasConstraintName("FK_RawMaterialsWarehousesProducts_MeasurementUnit");

            entity.HasOne(d => d.Product).WithMany(p => p.RawMaterialsWarehousesProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_RawMaterialsWarehousesProducts_Products");

            entity.HasOne(d => d.RawMaterial).WithMany(p => p.RawMaterialsWarehousesProducts)
                .HasForeignKey(d => d.RawMaterialId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_RawMaterialsWarehousesProducts_RawMaterials");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.RawMaterialsWarehousesProducts)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RawMaterialsWarehousesProducts_Warehouses");
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("Recipe");

            entity.Property(e => e.RecipeId).HasColumnName("recipeID");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.RecipeDescription).HasColumnName("recipeDescription");
            entity.Property(e => e.StatusRecipe)
                .HasMaxLength(150)
                .HasColumnName("statusRecipe");

            entity.HasOne(d => d.Product).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Recipe_Products");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.Property(e => e.ReportId).HasColumnName("reportID");
            entity.Property(e => e.ReportDate)
                .HasColumnType("datetime")
                .HasColumnName("reportDate");
            entity.Property(e => e.ReportType)
                .HasMaxLength(150)
                .HasColumnName("reportType");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.User).WithMany(p => p.Reports)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reports_Users");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(e => e.SaleId).HasColumnName("saleID");
            entity.Property(e => e.CoastSale)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("coastSale");
            entity.Property(e => e.DateTimeSale)
                .HasColumnType("datetime")
                .HasColumnName("dateTimeSale");
            entity.Property(e => e.SaleStatus)
                .HasMaxLength(100)
                .HasColumnName("saleStatus");
            entity.Property(e => e.TypeSale)
                .HasMaxLength(100)
                .HasColumnName("typeSale");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.User).WithMany(p => p.Sales)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sales_Users");
        });

        modelBuilder.Entity<SaleProduct>(entity =>
        {
            entity.ToTable("SaleProduct");

            entity.Property(e => e.SaleProductId).HasColumnName("saleProductID");
            entity.Property(e => e.CoastToProduct)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("coastToProduct");
            entity.Property(e => e.CountProductSale).HasColumnName("countProductSale");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.SaleId).HasColumnName("saleId");

            entity.HasOne(d => d.Product).WithMany(p => p.SaleProducts)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_SaleProduct_Products");

            entity.HasOne(d => d.Sale).WithMany(p => p.SaleProducts)
                .HasForeignKey(d => d.SaleId)
                .HasConstraintName("FK_SaleProduct_Sales");
        });

        modelBuilder.Entity<SalesProduction>(entity =>
        {
            entity.HasKey(e => e.SaleProductionId);

            entity.Property(e => e.SaleProductionId).HasColumnName("saleProductionID");
            entity.Property(e => e.ProductionId).HasColumnName("productionId");
            entity.Property(e => e.SaleId).HasColumnName("saleId");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("Status");

            entity.Property(e => e.StatusId).HasColumnName("statusID");
            entity.Property(e => e.StatusName)
                .HasMaxLength(150)
                .HasColumnName("statusName");
        });

        modelBuilder.Entity<SupplyRequest>(entity =>
        {
            entity.HasKey(e => e.SupplyRequestId).HasName("PK_SypplyRequests");

            entity.Property(e => e.SupplyRequestId).HasColumnName("supplyRequestID");
            entity.Property(e => e.StatusId).HasColumnName("statusId");
            entity.Property(e => e.SupplyRequestDate)
                .HasColumnType("datetime")
                .HasColumnName("supplyRequestDate");
            entity.Property(e => e.TotalSalary)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("totalSalary");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.Status).WithMany(p => p.SupplyRequests)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplyRequests_Status");

            entity.HasOne(d => d.User).WithMany(p => p.SupplyRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SypplyRequests_Users");
        });

        modelBuilder.Entity<SupplyRequestsRawMaterial>(entity =>
        {
            entity.HasKey(e => e.SupplyRequestWarehouseRawMaterialId).HasName("PK_SupplyRequestsRawMaterials_1");

            entity.Property(e => e.SupplyRequestWarehouseRawMaterialId).HasColumnName("SupplyRequestWarehouseRawMaterialID");
            entity.Property(e => e.CountRawMaterial).HasColumnName("countRawMaterial");
            entity.Property(e => e.RawMaterialId).HasColumnName("rawMaterialId");
            entity.Property(e => e.SupplyCoastToMaterial)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("supplyCoastToMaterial");
            entity.Property(e => e.SupplyRequestId).HasColumnName("supplyRequestId");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouseId");

            entity.HasOne(d => d.RawMaterial).WithMany(p => p.SupplyRequestsRawMaterials)
                .HasForeignKey(d => d.RawMaterialId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SupplyRequestsRawMaterials_RawMaterials1");

            entity.HasOne(d => d.SupplyRequest).WithMany(p => p.SupplyRequestsRawMaterials)
                .HasForeignKey(d => d.SupplyRequestId)
                .HasConstraintName("FK_SupplyRequestsRawMaterials_SupplyRequests1");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.SupplyRequestsRawMaterials)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplyRequestsRawMaterials_Warehouses");
        });

        modelBuilder.Entity<TypesProduct>(entity =>
        {
            entity.HasKey(e => e.TypeProductId);

            entity.ToTable("TypesProduct");

            entity.Property(e => e.TypeProductId).HasColumnName("typeProductID");
            entity.Property(e => e.TypeProductName)
                .HasMaxLength(200)
                .HasColumnName("typeProductName");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.UserHashPassword)
                .HasMaxLength(64)
                .HasColumnName("userHashPassword");
            entity.Property(e => e.UserLogin)
                .HasMaxLength(100)
                .HasColumnName("userLogin");
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .HasColumnName("userName");
            entity.Property(e => e.UserRoleId).HasColumnName("userRoleId");
            entity.Property(e => e.UserSurname)
                .HasMaxLength(100)
                .HasColumnName("userSurname");

            entity.HasOne(d => d.UserRole).WithMany(p => p.Users)
                .HasForeignKey(d => d.UserRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_UserRole");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRole");

            entity.Property(e => e.UserRoleId).HasColumnName("userRoleID");
            entity.Property(e => e.UserRoleName)
                .HasMaxLength(100)
                .HasColumnName("userRoleName");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.Property(e => e.WarehouseId).HasColumnName("warehouseID");
            entity.Property(e => e.WarehouseName)
                .HasMaxLength(125)
                .HasColumnName("warehouseName");
            entity.Property(e => e.WarehouseType)
                .HasMaxLength(75)
                .HasColumnName("warehouseType");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
