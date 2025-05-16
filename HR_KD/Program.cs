using HR_KD.Authorization;
using HR_KD.Data;
using HR_KD.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuestPDF.Infrastructure;

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
builder.Services.AddScoped<PayrollReportService>();

// Cấu hình Authentication 
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EMPLOYEE", policy => policy.RequireRole("EMPLOYEE"));
    options.AddPolicy("EMPLOYEE_MANAGER", policy => policy.RequireRole("EMPLOYEE_MANAGER"));
    options.AddPolicy("LINE_MANAGER", policy => policy.RequireRole("LINE_MANAGER"));
    options.AddPolicy("DIRECTOR", policy => policy.RequireRole("DIRECTOR"));

    options.AddPolicy("EmployeeView", policy => policy.Requirements.Add(new ManageSubordinateRequirement("employees", "view")));
    options.AddPolicy("EmployeeCreate", policy => policy.Requirements.Add(new ManageSubordinateRequirement("employees", "create")));
    options.AddPolicy("RoleManage", policy => policy.Requirements.Add(new ManageSubordinateRequirement("employees", "assign_roles")));
    options.AddPolicy("AttendanceManage", policy => policy.Requirements.Add(new ManageSubordinateRequirement("attendance", "manage")));
    options.AddPolicy("HolidayCreate", policy => policy.Requirements.Add(new ManageSubordinateRequirement("holidays", "create")));
    options.AddPolicy("HolidayApprove", policy => policy.Requirements.Add(new ManageSubordinateRequirement("holidays", "approve")));
    options.AddPolicy("PayrollCreate", policy => policy.Requirements.Add(new ManageSubordinateRequirement("payroll", "create")));
    options.AddPolicy("PayrollApproveBL2", policy => policy.Requirements.Add(new ManageSubordinateRequirement("payroll", "approve_bl2_to_bl3")));
    options.AddPolicy("PayrollApproveBL3", policy => policy.Requirements.Add(new ManageSubordinateRequirement("payroll", "approve_bl3_to_bl4")));


});

// Cấu hình Entity Framework
builder.Services.AddDbContext<HrDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Db_HR")));
builder.Services.AddScoped<PhepNamService>(); // Đăng ký dịch vụ với vòng đời Scoped

builder.Services.AddScoped<YearlyTaskService>(); // Đổi thành Scoped
builder.Services.AddHostedService<YearlyTaskService>(); // Đảm bảo đăng ký như Hosted Service

// Đăng ký HttpClient và HolidayNotificationService
builder.Services.AddHttpClient();
builder.Services.AddHostedService<HolidayNotificationService>();

// Thêm Controllers và Views
builder.Services.AddControllersWithViews();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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
