using MiWebAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<EmpleadoData>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("NuevaPolitica", app =>
    {
        app.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 1. IMPORTANTE: Habilita CORS antes de los archivos estáticos y controladores
app.UseCors("NuevaPolitica");

// 2. Configura los archivos estáticos (HTML, CSS, JS desde wwwroot)
app.UseStaticFiles();

// 3. Configura el archivo inicial (Splash Screen) si deseas que cargue primero
var optionsFiles = new DefaultFilesOptions();
optionsFiles.DefaultFileNames.Clear();
optionsFiles.DefaultFileNames.Add("splash.html");
app.UseDefaultFiles(optionsFiles);

app.UseAuthorization();

// 4. Mapea los controladores de la API (/api/...)
app.MapControllers();

app.Run();