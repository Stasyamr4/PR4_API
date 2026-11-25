using PR4_API.DTOs;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WpfApp1.Dto;

namespace WpfApp1.Services
{
    /// <summary>Ошибка обращения к API с текстом, понятным пользователю.</summary>
    public class ApiException : Exception
    {
        public int StatusCode { get; }
        public ApiException(string message, int statusCode) : base(message) => StatusCode = statusCode;
    }

    /// <summary>
    /// Единственное место, где приложение общается с API: адрес, JWT-токен, разбор JSON и ошибок.
    /// Страницы вызывают только типизированные методы и ничего не знают про HTTP.
    /// </summary>
    public static class ApiClient
    {
        /// <summary>Адрес запущенного API (профиль "http" из launchSettings.json).</summary>
        public const string BaseUrl = "http://localhost:5125/";

        private static readonly HttpClient Http = new()
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        private static string? _token;

        // ===== Сессия =====
        public static UserInfo? CurrentUser { get; private set; }
        public static bool IsAuthorized => CurrentUser != null;
        public static bool IsAdmin => CurrentUser?.Role == "Admin";
        public static event Action? SessionChanged;

        public static void Logout()
        {
            _token = null;
            CurrentUser = null;
            SessionChanged?.Invoke();
        }

        // ===== Базовые методы =====
        private static async Task<string> SendRawAsync(HttpMethod method, string url, object? body = null)
        {
            using var request = new HttpRequestMessage(method, url);
            if (_token != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            if (body != null)
                request.Content = new StringContent(JsonSerializer.Serialize(body, body.GetType(), Json), Encoding.UTF8, "application/json");

            try
            {
                using var response = await Http.SendAsync(request);
                var text = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode) return text;

                int code = (int)response.StatusCode;
                if (response.StatusCode == HttpStatusCode.Unauthorized && _token != null)
                {
                    Logout(); // токен просрочен или недействителен
                    throw new ApiException("Сессия истекла. Войдите в аккаунт снова.", code);
                }
                throw new ApiException(ExtractError(text, response.StatusCode), code);
            }
            catch (HttpRequestException)
            {
                throw new ApiException($"Не удалось подключиться к серверу ({BaseUrl}). Проверьте, что API запущен.", 0);
            }
            catch (TaskCanceledException)
            {
                throw new ApiException("Сервер не отвечает. Попробуйте ещё раз.", 0);
            }
        }

        private static async Task<T> CallAsync<T>(HttpMethod method, string url, object? body = null)
        {
            var text = await SendRawAsync(method, url, body);
            try
            {
                return JsonSerializer.Deserialize<T>(text, Json)
                       ?? throw new ApiException("Сервер вернул пустой ответ.", 0);
            }
            catch (JsonException)
            {
                throw new ApiException("Не удалось разобрать ответ сервера.", 0);
            }
        }

        /// <summary>Запрос, где важно только сообщение сервера ("message") — оно показывается пользователю.</summary>
        private static async Task<string> ActionAsync(HttpMethod method, string url, object? body = null)
        {
            var text = await SendRawAsync(method, url, body);
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                    return m.GetString() ?? "";
            }
            catch (JsonException) { }
            return "";
        }

        private static string ExtractError(string text, HttpStatusCode status)
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                        return m.GetString() ?? "";
                    // Ошибки проверки модели ASP.NET: { errors: { Поле: ["текст"] } }
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                        foreach (var field in errors.EnumerateObject())
                            foreach (var item in field.Value.EnumerateArray())
                                if (item.ValueKind == JsonValueKind.String) return item.GetString() ?? "";
                    if (root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                        return t.GetString() ?? "";
                }
            }
            catch (JsonException) { }

            return status switch
            {
                HttpStatusCode.BadRequest => "Некорректный запрос.",
                HttpStatusCode.Unauthorized => "Войдите в аккаунт, чтобы выполнить это действие.",
                HttpStatusCode.Forbidden => "Недостаточно прав для этого действия.",
                HttpStatusCode.NotFound => "Запрошенные данные не найдены.",
                _ when (int)status >= 500 => "Ошибка на сервере. Попробуйте позже.",
                _ => $"Ошибка запроса ({(int)status})."
            };
        }

        private static string Inv(decimal v) => v.ToString(CultureInfo.InvariantCulture);

        // ===== Авторизация =====
        public static async Task LoginAsync(string login, string password)
        {
            var r = await CallAsync<LoginResponse>(HttpMethod.Post, "api/Auth/login",
                new LoginRequest { Login = login.Trim(), Password = password });
            _token = r.Token;
            CurrentUser = r.User;
            SessionChanged?.Invoke();
        }

        public static Task<string> RegisterAsync(string name, string login, string password)
            => ActionAsync(HttpMethod.Post, "api/Auth/register",
                new RegisterRequest { Name = name.Trim(), Login = login.Trim(), Password = password });

        // ===== Каталог (доступен гостям) =====
        public static Task<List<ProductDto>> GetProductsAsync(string? search, int? categoryId,
            decimal? minPrice, decimal? maxPrice, bool onlyAvailable)
        {
            var q = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) q.Add("search=" + Uri.EscapeDataString(search.Trim()));
            if (categoryId is > 0) q.Add("categoryId=" + categoryId);
            if (minPrice.HasValue) q.Add("minPrice=" + Inv(minPrice.Value));
            if (maxPrice.HasValue) q.Add("maxPrice=" + Inv(maxPrice.Value));
            if (onlyAvailable) q.Add("onlyAvailable=true");
            var url = "api/Products" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
            return CallAsync<List<ProductDto>>(HttpMethod.Get, url);
        }

        public static Task<ProductDto> GetProductAsync(int id)
            => CallAsync<ProductDto>(HttpMethod.Get, $"api/Products/{id}");

        public static Task<List<NameDto>> GetCategoriesAsync()
            => CallAsync<List<NameDto>>(HttpMethod.Get, "api/Categories");

        public static Task<List<NameDto>> GetPvzAsync()
            => CallAsync<List<NameDto>>(HttpMethod.Get, "api/Delivery/pvz");

        // ===== Отзывы =====
        public static Task<CommentsResponse> GetCommentsAsync(int productId)
            => CallAsync<CommentsResponse>(HttpMethod.Get, $"api/Comments/product/{productId}");

        public static Task<string> AddCommentAsync(int productId, int stars, string text)
            => ActionAsync(HttpMethod.Post, $"api/Comments/product/{productId}",
                new CommentRequest { Stars = stars, Response = text });

        // ===== Корзина =====
        public static Task<BasketResponse> GetBasketAsync()
            => CallAsync<BasketResponse>(HttpMethod.Get, "api/Basket");

        public static Task<string> AddToBasketAsync(int productId, int quantity)
            => ActionAsync(HttpMethod.Post, "api/Basket/items",
                new BasketItemRequest { ProductId = productId, Quantity = quantity });

        public static Task<string> SetBasketQuantityAsync(int productId, int quantity)
            => ActionAsync(HttpMethod.Put, $"api/Basket/items/{productId}",
                new BasketItemRequest { ProductId = productId, Quantity = quantity });

        public static Task<string> RemoveFromBasketAsync(int productId)
            => ActionAsync(HttpMethod.Delete, $"api/Basket/items/{productId}");

        public static Task<string> ClearBasketAsync()
            => ActionAsync(HttpMethod.Delete, "api/Basket/clear");

        // ===== Избранное =====
        public static Task<List<ProductDto>> GetFavoritesAsync()
            => CallAsync<List<ProductDto>>(HttpMethod.Get, "api/Favorites");

        public static Task<string> AddFavoriteAsync(int productId)
            => ActionAsync(HttpMethod.Post, $"api/Favorites/{productId}");

        public static Task<string> RemoveFavoriteAsync(int productId)
            => ActionAsync(HttpMethod.Delete, $"api/Favorites/{productId}");

        public static Task<string> MoveFavoriteToBasketAsync(int productId)
            => ActionAsync(HttpMethod.Post, $"api/Favorites/{productId}/to-basket");

        // ===== Заказы =====
        public static Task<NameDto> CreateAddressAsync(string address)
            => CallAsync<NameDto>(HttpMethod.Post, "api/Delivery/addresses", new NameRequest { Name = address });

        public static Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request)
            => CallAsync<CheckoutResponse>(HttpMethod.Post, "api/Orders/checkout", request);

        public static Task<List<OrderSummaryDto>> GetMyOrdersAsync()
            => CallAsync<List<OrderSummaryDto>>(HttpMethod.Get, "api/Orders");

        public static Task<OrderDetailsDto> GetOrderAsync(int id)
            => CallAsync<OrderDetailsDto>(HttpMethod.Get, $"api/Orders/{id}");

        // ===== Администратор =====
        public static Task<CreatedProductResponse> CreateProductAsync(ProductRequest request)
            => CallAsync<CreatedProductResponse>(HttpMethod.Post, "api/Admin/products", request);

        public static Task<string> UpdateProductAsync(int id, ProductRequest request)
            => ActionAsync(HttpMethod.Put, $"api/Admin/products/{id}", request);

        public static Task<string> DeleteProductAsync(int id)
            => ActionAsync(HttpMethod.Delete, $"api/Admin/products/{id}");

        public static Task<NameDto> CreateCategoryAsync(string name)
            => CallAsync<NameDto>(HttpMethod.Post, "api/Admin/categories", new NameRequest { Name = name });

        public static Task<string> UpdateCategoryAsync(int id, string name)
            => ActionAsync(HttpMethod.Put, $"api/Admin/categories/{id}", new NameRequest { Name = name });

        public static Task<string> DeleteCategoryAsync(int id)
            => ActionAsync(HttpMethod.Delete, $"api/Admin/categories/{id}");

        public static Task<NameDto> CreatePvzAsync(string name)
            => CallAsync<NameDto>(HttpMethod.Post, "api/Admin/pvz", new NameRequest { Name = name });

        public static Task<string> UpdatePvzAsync(int id, string name)
            => ActionAsync(HttpMethod.Put, $"api/Admin/pvz/{id}", new NameRequest { Name = name });

        public static Task<string> DeletePvzAsync(int id)
            => ActionAsync(HttpMethod.Delete, $"api/Admin/pvz/{id}");

        public static Task<List<OrderSummaryDto>> GetAllOrdersAsync()
            => CallAsync<List<OrderSummaryDto>>(HttpMethod.Get, "api/Admin/orders");

        public static Task<OrderDetailsDto> GetAnyOrderAsync(int id)
            => CallAsync<OrderDetailsDto>(HttpMethod.Get, $"api/Admin/orders/{id}");

        public static Task<List<NameDto>> GetStatusesAsync()
            => CallAsync<List<NameDto>>(HttpMethod.Get, "api/Admin/statuses");

        public static Task<string> UpdateOrderStatusAsync(int orderId, int statusId)
            => ActionAsync(HttpMethod.Put, $"api/Admin/orders/{orderId}/status", new StatusRequest { StatusId = statusId });

        public static Task<string> DeleteCommentAsync(int commentId)
            => ActionAsync(HttpMethod.Delete, $"api/Admin/comments/{commentId}");
    }
}
