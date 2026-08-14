using Raphael;
using Raphael.Effects;
using Raphael.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRaphael(builder.Configuration);

// Add services to the container.

var app = builder.Build();

// View discovered effects
var processor = app.Services.GetRequiredService<IEffectProcessor>();
var effectInfo = processor.GetEffectInfo();

foreach (var effect in effectInfo)
{
    Console.WriteLine($"Effect: {effect.Key}");
    Console.WriteLine($"  Type: {effect.Value.TypeName}");
    Console.WriteLine($"  Description: {effect.Value.Description}");
    Console.WriteLine($"  Properties: {string.Join(", ", effect.Value.Properties.Keys)}");
    Console.WriteLine($"  Order: {effect.Value.Order}");
    Console.WriteLine($"  Enabled: {effect.Value.Enabled}");
    Console.WriteLine();
}

app.UseHttpsRedirection();

app.AddRaphaelRoutes();

app.Run();

