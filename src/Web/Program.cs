using IceCreamM12.Application.Interfaces;
using IceCreamM12.Application.Services;
using IceCreamM12.Domain.Identity;
using IceCreamM12.Domain.Interfaces;
using IceCreamM12.Infrastructure.Data;
using IceCreamM12.Infrastructure.Identity;
using IceCreamM12.Infrastructure.Repositories;
using IceCreamM12.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = ResolveContentRootPath()
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "app.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));


builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IFlavorRepository, FlavorRepository>();
builder.Services.AddScoped<IFlavorService, FlavorService>();
builder.Services.AddScoped<IAuditService, IceCreamM12.Infrastructure.Services.AuditService>();
builder.Services.AddScoped<IInventoryService, IceCreamM12.Infrastructure.Services.InventoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, IceCreamM12.Infrastructure.Services.OrderService>();
builder.Services.AddScoped<IManagementService, ManagementService>();
builder.Services.AddScoped<IProductionService, IceCreamM12.Infrastructure.Services.ProductionService>();

var app = builder.Build();

var bgCulture = new CultureInfo("bg-BG");
bgCulture.NumberFormat.CurrencySymbol = "€";
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(bgCulture),
    SupportedCultures = [bgCulture],
    SupportedUICultures = [bgCulture]
};

app.UseRequestLocalization(localizationOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var httpsPort = builder.Configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT")
    ?? builder.Configuration.GetValue<int?>("HTTPS_PORT");

if (httpsPort.HasValue)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

var sharedImagesPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "images");
if (Directory.Exists(sharedImagesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(sharedImagesPath),
        RequestPath = "/images"
    });
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (IServiceScope scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await DbInitializer.SeedAsync(dbContext, userManager, roleManager, app.Configuration);
}

app.Run();

static string ResolveContentRootPath()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "Web.csproj")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return Directory.GetCurrentDirectory();
}
