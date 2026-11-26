using MiWebAPI.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<EmpleadoData>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("NuevaPolitica", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("NuevaPolitica");

app.UseStaticFiles();

var optionsFiles = new DefaultFilesOptions();
optionsFiles.DefaultFileNames.Clear();
optionsFiles.DefaultFileNames.Add("splash.html");
app.UseDefaultFiles(optionsFiles);

app.UseAuthorization();

app.MapControllers();

app.Run();