using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

var builder = WebApplication.CreateBuilder(args);

// Swagger servislerini ekle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// DİKKAT: Normalde Swagger sadece Development'ta açılır. 
// K8s ortamında Production sayılacağı için, her zaman açılmasını sağlıyoruz.
app.UseSwagger();
app.UseSwaggerUI();

// Test Endpoint'imiz
app.MapGet("/api/durum", () => new { 
    Mesaj = "Adana'dan K3s uzerinde calisan .NET 10 API'sine Selamlar!", 
    Tarih = DateTime.Now,
    SunucuAdresi = Environment.MachineName 
});

app.Run();