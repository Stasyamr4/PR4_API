namespace PR4_API.Models;
public class Order { public int ID { get; set; } public DateTime DateOrder { get; set; } public int UserID { get; set; } public int StatusID { get; set; } public int? AddressPVZID { get; set; } public int? AddressID { get; set; } public decimal TotalPrice { get; set; } }
