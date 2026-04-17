using JobSearch.Api.Services;
using JobSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Services
builder.Services.AddControllers();
builder.Services.AddDbContext<JobSearchDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<RecommendationQueryService>();

var app = builder.Build();

// Ensure Database is Created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<JobSearchDbContext>();
    context.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
