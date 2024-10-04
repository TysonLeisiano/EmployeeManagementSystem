﻿using BaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerLibrary.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Employee> Employees { get; set; }
        public DbSet<GeneralDepartment> GeneralDepartments { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Branch> Braches { get; set; }
        public DbSet<Town> Towns { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<SystemRoles> SystemRoles { get; set; }
        public DbSet<UserRoles> UserRoles { get; set; }
        public DbSet<RefreshTokenInfo> RefreshTokenInfo { get; set; }

    }
}
