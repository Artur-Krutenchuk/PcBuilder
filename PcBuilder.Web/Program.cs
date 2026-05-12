using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PcBuilder.Web.Data.ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<PcBuilder.Web.Models.Auth.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<PcBuilder.Web.Data.ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<PcBuilder.Web.Repositories.IComponentRepository, PcBuilder.Web.Repositories.JsonComponentRepository>();
builder.Services.AddScoped<PcBuilder.Web.Repositories.ISavedBuildRepository, PcBuilder.Web.Repositories.JsonSavedBuildRepository>();
builder.Services.AddScoped<PcBuilder.Web.Services.IComponentService, PcBuilder.Web.Services.JsonComponentService>();
builder.Services.AddScoped<PcBuilder.Web.Services.ICompatibilityService, PcBuilder.Web.Services.CompatibilityService>();
builder.Services.AddScoped<PcBuilder.Web.Services.ISavedBuildService, PcBuilder.Web.Services.JsonSavedBuildService>();
builder.Services.AddScoped<PcBuilder.Web.Services.IAdminDashboardService, PcBuilder.Web.Services.AdminDashboardService>();
builder.Services.AddScoped<PcBuilder.Web.Services.IBuildService, PcBuilder.Web.Services.BuildService>();
builder.Services.AddScoped<PcBuilder.Web.Services.IGalleryService, PcBuilder.Web.Services.GalleryService>();
builder.Services.AddScoped<PcBuilder.Web.Services.IProfileService, PcBuilder.Web.Services.ProfileService>();
builder.Services.AddScoped<PcBuilder.Web.Services.IHomeService, PcBuilder.Web.Services.HomeService>();
builder.Services.AddScoped<PcBuilder.Web.Services.IAdminCatalogService, PcBuilder.Web.Services.AdminCatalogService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PcBuilder.Web.Data.ApplicationDbContext>();
    db.Database.EnsureCreated();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
}


app.Run();
