using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddAntiforgery(options =>
{
    // pick a clear header name
    options.HeaderName = "X-CSRF-TOKEN";

    // optional: adjust cookie settings if you have cross-site issues
    options.Cookie.Name = ".AspNetCore.Antiforgery";
    options.Cookie.SameSite = SameSiteMode.Lax; // change to None only if cross-site requests required
    // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // ensure HTTPS if used
});
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CalendarMeeting API", Version = "v1" });
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();

//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(
//           Path.Combine(builder.Environment.ContentRootPath, "demo2")),
//    RequestPath = "/demo2"
//});
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAll");

using (var scope = app.Services.CreateScope())
{
    var Initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await Initializer.InitializeAsync();
}

//var defaultCulture = new System.Globalization.CultureInfo("en-SA"); // or "ar-SA"
//var localizationOptions = new RequestLocalizationOptions
//{
//    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
//    SupportedCultures = new[] { defaultCulture },
//    SupportedUICultures = new[] { defaultCulture }
//};

//app.UseRequestLocalization(localizationOptions);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI(c =>
    //{
    //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CalendarMeeting API v1");
    //});
}

app.Run();
