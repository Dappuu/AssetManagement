﻿using AssetManagement.Domain.Constants;
using AssetManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Data.Data.MappingConfigurations
{
    internal class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
    {
        public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
        {
            builder.HasMany<AppUser>()
            .WithMany();

            builder.HasData(
                new IdentityRole<Guid>
                {
                    Id = new Guid("5fc71af5-0216-402b-a5cb-ba17701e2fa3"),
                    Name = RoleConstant.AdminRole,
                    NormalizedName = RoleConstant.AdminRole.ToUpper()
                },
            new IdentityRole<Guid>()
            {
                Id = new Guid("8bbf66a4-da08-4b87-bdb2-1502e38562f3"),
                Name = RoleConstant.StaffRole,
                NormalizedName = RoleConstant.StaffRole.ToUpper()
            });

        }
    }
}