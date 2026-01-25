using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CasaCejaRemake.Models
{   
    /// Item del carrito de compras (modelo temporal, no persistido en BD)   
    public class CartItem
    {       
        /// ID del producto en la base de datos       
        public int ProductId { get; set; }
       
        /// Código de barras del producto       
        public string Barcode { get; set; } = string.Empty;
       
        /// Nombre del producto    
        public string ProductName { get; set; } = string.Empty;
       
        /// Nombre de la categoría (para display)       
        public string CategoryName { get; set; } = string.Empty;
       
        /// Nombre de la unidad de medida (para display)       
        public string UnitName { get; set; } = string.Empty;
       
        /// Cantidad de productos en el carrito       
        public int Quantity { get; set; }
       
        /// Precio de lista original (sin descuentos)       
        public decimal ListPrice { get; set; }
       
        /// Precio final por unidad (con descuentos aplicados)       
        public decimal FinalUnitPrice { get; set; }
       
        /// Total de la línea (FinalUnitPrice * Quantity)       
        public decimal LineTotal => FinalUnitPrice * Quantity;
       
        /// Descuento por unidad       
        public decimal TotalDiscount { get; set; }
       
        /// Tipo de precio aplicado: "retail", "wholesale", "special", "dealer"       
        public string PriceType { get; set; } = "retail";
       
        /// Información descriptiva del descuento aplicado       
        public string DiscountInfo { get; set; } = string.Empty;
       
        /// Indica si el item tiene descuento aplicado       
        public bool HasDiscount => TotalDiscount > 0;
       
        /// Datos de pricing serializados y comprimidos (inmutables para auditoría)       
        public byte[]? PricingData { get; set; }
    }
   
    /// Colección de items del carrito (A, B, C, D)
    /// Permite manejar múltiples carritos simultáneos
   
    public class Collection
    {       
        /// Identificador de la colección (A, B, C, D)       
        public char Identifier { get; set; }
       
        /// Items en esta colección       
        public ObservableCollection<CartItem> Items { get; set; } = new();
       
        /// Total de la colección (suma de todos los LineTotal)       
        public decimal Total => Items.Sum(i => i.LineTotal);
       
        /// Total de descuentos en la colección       
        public decimal TotalDiscount => Items.Sum(i => i.TotalDiscount * i.Quantity);
       
        /// Total de items (suma de cantidades)       
        public int TotalItems => Items.Sum(i => i.Quantity);
       
        /// Indica si la colección está vacía
        public bool IsEmpty => Items.Count == 0;
    }
}
