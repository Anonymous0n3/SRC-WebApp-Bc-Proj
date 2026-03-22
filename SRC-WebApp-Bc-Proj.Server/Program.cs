using SRC_WebApp_Bc_Proj.Server.AudioConvert;
using SRC_WebApp_Bc_Proj.Server.DataValidation;
using SRC_WebApp_Bc_Proj.Server.FileService;
using SRC_WebApp_Bc_Proj.Server.Speech_to_Text;
using SRC_WebApp_Bc_Proj.Server.TextUtils;
using System.Runtime.InteropServices;

// Vynucení hledání nativních knihoven
NativeLibrary.SetDllImportResolver(typeof(Vosk.Vosk).Assembly, (name, assembly, path) =>
{
    if (name == "libvosk")
    {
        string dockerPath = "/app/libvosk.so";
        if (File.Exists(dockerPath)) return NativeLibrary.Load(dockerPath);

        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvosk.so");
        if (File.Exists(localPath)) return NativeLibrary.Load(localPath);
    }
    return IntPtr.Zero;
});

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Vývojové URL
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAudioService, AudioService>();
builder.Services.AddScoped<IDataValidation, DataValidation>();
builder.Services.AddScoped<ITextUtils, TextUtils>();

// UPRAVENO: Bezpečná cesta k modelu
builder.Services.AddSingleton<ISpeechToTextConvert>(sp =>
{
    string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "vosk-model-en-us-0.22");
    return new VoskSpeechToTextConverter(modelPath);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Zakomentováno schválně - v Dockeru často řeší Reverse Proxy

app.UseRouting();
app.UseCors("AllowReactApp");
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();