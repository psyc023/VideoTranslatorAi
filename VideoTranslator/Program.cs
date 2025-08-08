using Microsoft.AspNetCore.Http.Features;
using VideoTranslator.Services;
using VideoTranslator.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Límite global en Kestrel (2 GB)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024; // 2 GB
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Límite para multipart/form-data
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024; // 2 GB
});

builder.Services.AddScoped<ISpeechToTextService, SpeechToTextService>();
builder.Services.AddScoped<IAudioConversionService, AudioConversionService>();

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
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=VideoTranslator}/{action=Index}/{id?}");

app.Run();
