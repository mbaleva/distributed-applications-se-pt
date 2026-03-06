using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Mvc.Controllers;

public class ChatController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var token = Request.Cookies["chat_jwt"];
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login", "Account");
        }

        var client = _httpClientFactory.CreateClient("ChatApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("api/conversations?page=1&pageSize=50");
        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = "Cannot load conversations.";
            return View(Array.Empty<JsonElement>());
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items").Clone();

        return View(items);
    }
}

