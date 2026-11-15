using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ca_3.Data.Migrations;

[DbContext(typeof(web_ca_3.Data.ApplicationDbContext))]
[Migration("202611150001_AddDomainModel")]
partial class AddDomainModel
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12");
    }
}
