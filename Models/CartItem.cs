using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
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
        [NotifyPropertyChangedFor(nameof(RowBackgroundColor))]
        private string _priceType = "retail";
       
        /// Información descriptiva del descuento aplicado       
        [ObservableProperty]
        private string _discountInfo = string.Empty;
       
        /// Indica si el item tiene descuento aplicado       
        public bool HasDiscount => TotalDiscount > 0;
        
        /// Color de fondo del row según tipo de precio aplicado
        /// - Retail: color por defecto del grid (gris oscuro)
        /// - Wholesale: Verde esmeralda (#00897B) - destaca ahorro
        /// - Category: Púrpura vibrante (#9C27B0) - categoría especial
        /// - Special: Azul brillante (#2196F3) - oferta/promoción
        /// - Dealer: Naranja profundo (#E65100) - precio vendedor
        public IBrush RowBackgroundColor => PriceType switch
        {
            "wholesale" => new SolidColorBrush(Color.Parse("#00897B")), // Verde esmeralda (teal)
            "category" => new SolidColorBrush(Color.Parse("#9C27B0")),  // Púrpura vibrante
            "special" => new SolidColorBrush(Color.Parse("#2196F3")),   // Azul brillante
            "dealer" => new SolidColorBrush(Color.Parse("#E65100")),    // Naranja profundo
            _ => new SolidColorBrush(Color.Parse("#2D2D2D"))            // Color por defecto
        };
       
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
        
        // ========== DESCUENTO GENERAL (ahora por colección) ==========
        
        /// <summary>Porcentaje de descuento general (si aplica)</summary>
        public decimal GeneralDiscountPercent { get; set; } = 0;
        
        /// <summary>Monto fijo de descuento general (si aplica)</summary>
        public decimal GeneralDiscountAmount { get; set; } = 0;
        
        /// <summary>Indica si el descuento es porcentaje (true) o cantidad fija (false)</summary>
        public bool IsGeneralDiscountPercentage { get; set; } = true;
        
        /// <summary>Indica si hay un descuento general aplicado</summary>
        public bool HasGeneralDiscount => (IsGeneralDiscountPercentage && GeneralDiscountPercent > 0) || 
                                          (!IsGeneralDiscountPercentage && GeneralDiscountAmount > 0);
        
        /// <summary>Descuento general calculado</summary>
        public decimal CalculatedGeneralDiscount => IsGeneralDiscountPercentage 
            ? Total * (GeneralDiscountPercent / 100m)
            : Math.Min(GeneralDiscountAmount, Total);
        
        /// <summary>Total final después de descuento general</summary>
        public decimal FinalTotal => Total - CalculatedGeneralDiscount;
    }
}
