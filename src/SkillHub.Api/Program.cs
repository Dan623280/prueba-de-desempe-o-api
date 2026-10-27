using SkillHub.Api;
using SkillHub.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApi(builder.Configuration);
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    await response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = response.StatusCode,
        Title = response.StatusCode switch { 401 => "A valid JWT bearer token is required.", 403 => "You do not have permission to perform this operation.", _ => "The requested resource is unavailable." }
    });
});
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
// The local assessment uses HTTP on loopback. Configure HTTPS at the reverse proxy for a deployment.
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
if (app.Configuration.GetValue("Database:InitializeOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(
        Path.Combine(AppContext.BaseDirectory, "Data", "SkillHub_Data.xlsx"));
}
await app.RunAsync();
