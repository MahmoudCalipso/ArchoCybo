using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ArchoCybo.Infrastructure.Data;

#nullable disable

namespace ArchoCybo.Infrastructure.Migrations
{
    [DbContext(typeof(ArchoCyboDbContext))]
    partial class ArchoCyboDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "EFCore 10.0.0");

            modelBuilder.Entity("ArchoCybo.Domain.Entities.CodeGeneration.GeneratedProject", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
                b.Property<string>("Name").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("Description").HasColumnType("nvarchar(max)");
                b.Property<Guid>("OwnerUserId").HasColumnType("uniqueidentifier");
                b.Property<int>("DatabaseType").HasColumnType("int");
                b.Property<string>("DatabaseConnectionJson").HasColumnType("nvarchar(max)");
                b.Property<bool>("UseBaseRoles").HasColumnType("bit");
                b.Property<string>("RepositoryUrl").HasColumnType("nvarchar(max)");
                b.Property<string>("GenerationOptions").HasColumnType("nvarchar(max)");
                b.Property<int>("Status").HasColumnType("int");
                b.Property<DateTime?>("GeneratedAt").HasColumnType("datetime2");
                b.Property<Guid>("CreatedBy").HasColumnType("uniqueidentifier");
                b.Property<DateTime>("CreatedAt").HasColumnType("datetime2");
                b.Property<Guid?>("UpdatedBy").HasColumnType("uniqueidentifier");
                b.Property<DateTime?>("UpdatedAt").HasColumnType("datetime2");

                b.HasKey("Id");

                b.ToTable("GeneratedProjects");
            });
        }
    }
}
