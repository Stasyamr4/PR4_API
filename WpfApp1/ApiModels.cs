namespace PR4_API.DTOs;

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class BasketItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CommentRequest
{
    public int Stars { get; set; }
    public string Response { get; set; } = string.Empty;
}

public class CheckoutRequest
{
    public bool UseBasket { get; set; }
    public int? ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public int? AddressId { get; set; }
    public int? PvzId { get; set; }
}

public class ProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Count { get; set; }
    public int CountInProduct { get; set; } = 1;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public bool Available { get; set; }
}

public class NameRequest
{
    public string Name { get; set; } = string.Empty;
}

public class StatusRequest
{
    public int StatusId { get; set; }
}
