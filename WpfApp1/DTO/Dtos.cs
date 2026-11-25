using System;
using System.DirectoryServices.ActiveDirectory;
using System.Windows;
using System.Windows.Media;
using WpfApp1.Helpers;

namespace WpfApp1.Dto
{
    // Классы описывают JSON, который возвращает API. Имена сопоставляются без учёта регистра
    // (id / ID, categoryID / CategoryId). Вычисляемые свойства готовят текст для интерфейса.

    public class UserInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Login { get; set; } = "";
        public int RoleId { get; set; }
        public string Role { get; set; } = "";
    }

    public class LoginResponse
    {
        public string Token { get; set; } = "";
        public DateTime Expires { get; set; }
        public UserInfo User { get; set; } = new();
    }

    /// <summary>Простая пара «ID — название»: категория, ПВЗ, статус заказа.</summary>
    public class NameDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    public class ProductDto : Observable
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Count { get; set; }
        public int CountInProduct { get; set; } = 1;
        public string Description { get; set; } = "";
        public string Image { get; set; } = "";
        public double Rating { get; set; }
        public int CategoryId { get; set; }
        public bool Available { get; set; }

        /// <summary>В JSON товара названия категории нет - подставляется на клиенте по CategoryId.</summary>
        public string CategoryName { get; set; } = "";

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (Set(ref _isFavorite, value))
                {
                    Raise(nameof(FavoriteGlyph));
                    Raise(nameof(FavoriteTip));
                }
            }
        }

        public bool CanBuy => Available && Count > 0;
        public string PriceText => Format.Money(Price);
        public ImageSource? ImageSource => ImageLoader.Load(Image);
        public string StockText => !Available ? "Недоступен для покупки" : Count <= 0 ? "Нет в наличии" : $"В наличии: {Count} шт.";
        public string RatingText => Rating > 0 ? $"{Format.Stars(Rating)}  {Rating:0.0}" : "Пока нет оценок";
        public string PackText => $"В упаковке: {CountInProduct} шт.";
        public Visibility PackVisibility => CountInProduct > 1 ? Visibility.Visible : Visibility.Collapsed;
        public string AvailableText => Available ? "Да" : "Нет";
        public string FavoriteGlyph => IsFavorite ? "♥" : "♡";
        public string FavoriteTip => IsFavorite ? "Убрать из избранного" : "Добавить в избранное";
    }

    public class BasketResponse
    {
        public int BasketId { get; set; }
        public List<BasketItemDto> Items { get; set; } = new();
        public decimal Total { get; set; }
    }

    public class BasketItemDto
    {
        public int Id { get; set; }            // ID товара
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string Image { get; set; } = "";
        public int Count { get; set; }         // остаток на складе
        public int Quantity { get; set; }      // количество в корзине
        public decimal Sum { get; set; }

        public string PriceText => Format.Money(Price);
        public string SumText => Format.Money(Sum);
        public ImageSource? ImageSource => ImageLoader.Load(Image);
        public string LineText => $"{Quantity} × {Format.Money(Price)} = {Format.Money(Sum)}";
        public string Warning => Quantity > Count ? (Count <= 0 ? "Товара нет в наличии" : $"Доступно только {Count} шт.") : "";
        public Visibility WarningVisibility => Warning.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Stars { get; set; } = "";
        public string Response { get; set; } = "";

        public string StarsText => Format.Stars(int.TryParse(Stars, out var n) ? n : 0);
        public Visibility DeleteVisibility => Services.ApiClient.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
    }

    public class CommentsResponse
    {
        public double AverageRating { get; set; }
        public List<CommentDto> Comments { get; set; } = new();
    }

    /// <summary>Строка списка заказов (у пользователя и у администратора).</summary>
    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public DateTime DateOrder { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public decimal TotalPrice { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; } = "";
        public int? AddressId { get; set; }
        public int? AddressPvzId { get; set; }

        public string Title => $"Заказ №{Id}";
        public string DateText => Format.Date(DateOrder);
        public string TotalText => Format.Money(TotalPrice);
        public string DeliveryText => AddressPvzId.HasValue ? "Самовывоз из ПВЗ" : AddressId.HasValue ? "Доставка по адресу" : "—";
    }

    public class OrderProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public decimal UnitPrice { get; set; }   // цена на момент покупки
        public decimal Sum { get; set; }

        public string UnitPriceText => Format.Money(UnitPrice);
        public string SumText => Format.Money(Sum);
        public string LineText => $"{Name} — {Count} × {Format.Money(UnitPrice)} = {Format.Money(Sum)}";
    }

    public class OrderDetailsDto
    {
        public int Id { get; set; }
        public DateTime DateOrder { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public decimal TotalPrice { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; } = "";
        public string? DeliveryAddress { get; set; }
        public string? Pvz { get; set; }
        public List<OrderProductDto> Products { get; set; } = new();

        public string Title => $"Заказ №{Id}";
        public string DateText => Format.Date(DateOrder);
        public string TotalText => Format.Money(TotalPrice);
        public string DeliveryText => !string.IsNullOrEmpty(Pvz) ? $"Самовывоз из ПВЗ: {Pvz}"
                                    : !string.IsNullOrEmpty(DeliveryAddress) ? $"Доставка по адресу: {DeliveryAddress}"
                                    : "Способ получения не указан";
    }

    public class CheckoutResponse
    {
        public string Message { get; set; } = "";
        public int OrderId { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "";
    }

    public class CreatedProductResponse
    {
        public string Message { get; set; } = "";
        public int ProductId { get; set; }
    }
}
