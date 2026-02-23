using ApiOptimizerService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiOptimizerService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }

        public DbSet<ApiMetric> ApiMetrics { get; set; }
        public DbSet<OptimizationReport> OptimizationReports { get; set; }

    }
}
