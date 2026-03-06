using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ChatApp.Mvc.Models.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Mvc.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AccountController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory.CreateClient("ChatApi");

        var payload = JsonSerializer.Serialize(new
        {
            username = model.Username,
            password = model.Password
        });

        var response = await client.PostAsync("api/auth/login",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            model.Error = "Invalid username or password.";
            return View(model);
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var token = root.GetProperty("token").GetString();

        if (token == null)
        {
            model.Error = "Failed to login.";
            return View(model);
        }

        Response.Cookies.Append("chat_jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });

        return RedirectToAction("Index", "Chat");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory.CreateClient("ChatApi");

        var payload = JsonSerializer.Serialize(new
        {
            username = model.Username,
            password = model.Password,
            email = model.Email,
            displayName = model.DisplayName
        });

        var response = await client.PostAsync("api/auth/register",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            model.Error = "Registration failed.";
            return View(model);
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var token = root.GetProperty("token").GetString();

        if (token == null)
        {
            model.Error = "Registration failed.";
            return View(model);
        }

        Response.Cookies.Append("chat_jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });

        return RedirectToAction("Index", "Chat");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("chat_jwt");
        return RedirectToAction("Login");
    }
}

