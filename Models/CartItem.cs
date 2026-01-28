using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CasaCejaRemake.Models
{   
    /// Item del carrito de compras (modelo temporal, no persistido en BD)   
    public partial class CartItem : ObservableObject
    {       
        /// ID del producto en la base de datos       
        [ObservableProperty]
        private int _productId;
       
        /// Código de barras del producto       
        [ObservableProperty]
        private string _barcode = string.Empty;
       
        /// Nombre del producto    
        [ObservableProperty]
        private string _productName = string.Empty;
       
        /// Nombre de la categoría (para display)       
        [ObservableProperty]
        private string _categoryName = string.Empty;
       
        /// Nombre de la unidad de medida (para display)       
        [ObservableProperty]
        private string _unitName = string.Empty;
       
        /// Cantidad de productos en el carrito       
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LineTotal))]
        private int _quantity;
       
        /// Precio de lista original (sin descuentos)       
        [ObservableProperty]
        private decimal _listPrice;
       
        /// Precio final por unidad (con descuentos aplicados)       
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LineTotal))]
        private decimal _finalUnitPrice;
       
        /// Total de la línea (FinalUnitPrice * Quantity)       
        public decimal LineTotal => FinalUnitPrice * Quantity;
       
        /// Descuento por unidad       
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasDiscount))]
        private decimal _totalDiscount;
       
        /// Tipo de precio aplicado: "retail", "wholesale", "special", "dealer"       
        [ObservableProperty]
        private string _priceType = "retail";
       
        /// Información descriptiva del descuento aplicado       
        [ObservableProperty]
        private string _discountInfo = string.Empty;
       
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
