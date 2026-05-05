using Microsoft.AspNetCore.ResponseCompression;
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost",
                                              "https://localhost",
                                              "http://localhost:5021",
                                              "https://localhost:7122")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();
app.Use(async (context, next) => {
    context.Response.OnStarting(() => {
        context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
        context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
        return Task.CompletedTask;
    });

    await next();
});
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
