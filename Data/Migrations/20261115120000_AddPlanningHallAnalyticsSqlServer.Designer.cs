using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ca_3.Data.Migrations;

[DbContext(typeof(web_ca_3.Data.ApplicationDbContext))]
[Migration("20261115120000_AddPlanningHallAnalyticsSqlServer")]
partial class AddPlanningHallAnalyticsSqlServer
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12");
    }
}
