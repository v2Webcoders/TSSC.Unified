using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using AspNetCore.ReportingServices.ReportProcessing.ReportObjectModel;
using Microsoft.AspNetCore.Builder;
using QUIZAPP.Services;
using QUIZAPP;
using QUIZAPP.Models;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;


var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddDistributedMemoryCache();  // Required for session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4); // Set long enough session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddMvc();
var connString = builder.Configuration.GetConnectionString("DefaultConnectionString");
builder.Services.AddDbContext<AppdbContext>(options => options.UseSqlServer(connString));
builder.Services.AddSingleton<IFileProvider>(
                  new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
                  );
//builder.Services.AddIdentityApiEndpoints<AppUser>();

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 3;
    options.Password.RequiredUniqueChars = 0;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
}).AddDefaultTokenProviders().AddEntityFrameworkStores<AppdbContext>();

// Register repository
//builder.Services.AddScoped<ILookupRepository, Repository>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<UtilityService>();

var app = builder.Build();


//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var userManager = services.GetRequiredService<UserManager<AppUser>>();
//    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

//    //Seed Roles
//    string[] roles = new[] { "HR", "Employee" };
//    foreach (var role in roles)
//    {
//        if (!await roleManager.RoleExistsAsync(role))
//        {
//            await roleManager.CreateAsync(new IdentityRole(role));
//        }
//    }

//    //// Delete user if needed
//    var existing = await userManager.FindByNameAsync("hrmanager@tssc.local");
//    if (existing != null)
//    {
//        await userManager.DeleteAsync(existing);
//    }

//    // Seed default admin
//    var adminEmail = "hrmanager@tssc.local";
//    var admin = await userManager.FindByEmailAsync(adminEmail);

//    if (admin == null)
//    {
//        var newAdmin = new AppUser
//        {
//            UserName = "hrmanager@tssc.local",
//            Email = adminEmail
//        };

//        var result = await userManager.CreateAsync(newAdmin, "Localhr@123"); // password must meet Identity rules

//        if (result.Succeeded)
//        {
//            await userManager.AddToRoleAsync(newAdmin, "HR");
//        }
//        else
//        {
//            Console.WriteLine(string.Join("\n", result.Errors.Select(e => e.Description)));
//        }
//    }
//}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
  );
    //endpoints.MapControllerRoute(
    //name: "default",
    //pattern: "{controller=Home}/{action=Index}/{id?}");
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Login}/{id?}");
});
app.Run();
