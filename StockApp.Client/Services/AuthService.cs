using Microsoft.JSInterop;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;

namespace StockApp.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    //guarda el token en el local storage
    public async Task GuardarToken(string token)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", "token", token);
        _http.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);
    }

    //obtiene el token del localStorage
    public async Task<string?> ObtenerToken()
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", "token");
    }

    //elimina el token (logout)
    public async Task CerrarSesion()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "token");
        _http.DefaultRequestHeaders.Authorization = null;
    }

    //verifica si hay un token guardado
    public async Task<bool> EstaLogueado()
    {
        var token = await ObtenerToken();
        return !string.IsNullOrEmpty(token);
    }

    //carga el tokenn al iniciar la app
    public async Task CargarToken()
    {
        var token = await ObtenerToken();
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        }
    }

    //obtiene el rol del usuario desde el token
    public async Task<string?> ObtenerRol()
    {
        var token = await ObtenerToken();
        if(string.IsNullOrEmpty(token)) return null;

        //decodificamos el payload del JWT
        var partes = token.Split('.');
        if(partes.Length != 3) return null;

        var payload = partes[1];
        //agregamos padding si es necesario
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        var bytes = Convert.FromBase64String(payload);
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        var datos = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        var rolKey = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        if(datos != null && datos.ContainsKey(rolKey))
        return datos[rolKey].GetString();

        return null;
    }
}