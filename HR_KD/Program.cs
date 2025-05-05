using HR_KD.Authorization;
using HR_KD.Data;
using HR_KD.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Thêm hỗ trợ IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Thêm Session vào hệ thống
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 30 phút hết hạn
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IAuthorizationHandler, ManageSubordinateHandler>();
builder.Services.AddScoped<ExcelTemplateService>();
builder.Services.AddScoped<UsernameGeneratorService>();
builder.Services.AddScoped<PayrollCalculator>();

// Cấu hình Authentication 
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageSubordinates", policy =>
        policy.Requirements.Add(new ManageSubordinateRequirement()));
    options.AddPolicy("EMPLOYEE", policy => policy.RequireRole("EMPLOYEE"));
    options.AddPolicy("EMPLOYEE_MANAGER", policy => policy.RequireRole("EMPLOYEE_MANAGER"));
    options.AddPolicy("LINE_MANAGER", policy => policy.RequireRole("LINE_MANAGER"));
    // giám đốc
    options.AddPolicy("DIRECTOR", policy => policy.RequireRole("DIRECTOR"));

});

// Cấu hình Entity Framework
builder.Services.AddDbContext<HrDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Db_HR")));
builder.Services.AddScoped<PhepNamService>(); // Đăng ký dịch vụ với vòng đời Scoped

builder.Services.AddScoped<YearlyTaskService>(); // Đổi thành Scoped
builder.Services.AddHostedService<YearlyTaskService>(); // Đảm bảo đăng ký như Hosted Service



// Thêm Controllers và Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
