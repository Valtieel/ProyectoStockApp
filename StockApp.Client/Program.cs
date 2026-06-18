using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StockApp.Client;
using StockApp.Client.Services;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configuramos la cultura en español (Argentina) para que fechas, números
// y moneda se muestren en formato local en toda la app.
var culture = new CultureInfo("es-AR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Configuramos el HttpClient para que apunte a nuestra API
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5181/api/")
});

// Registramos el servicio de autenticación
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();