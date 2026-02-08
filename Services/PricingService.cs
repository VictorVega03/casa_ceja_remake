using System;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Tipos de precio disponibles para productos
    /// </summary>
    public enum PriceType
    {
        Retail,      // Menudeo (precio base)
        Wholesale,   // Mayoreo (por cantidad)
        Special,     // Especial (promoción, aislado)
        Dealer       // Vendedor (precio para vendedores, aislado)
    }

    /// <summary>
    /// Resultado del cálculo de precio para un producto
    /// </summary>
    public class PriceCalculation
    {
        /// <summary>Precio de lista original (menudeo)</summary>
        public decimal ListPrice { get; set; }

        /// <summary>Precio base antes de descuento de categoría (mayoreo o menudeo)</summary>
        public decimal BasePrice { get; set; }

        /// <summary>Porcentaje de descuento de categoría aplicado</summary>
        public decimal CategoryDiscountPercent { get; set; }

        /// <summary>Monto de descuento de categoría por unidad</summary>
        public decimal CategoryDiscountAmount { get; set; }

        /// <summary>Precio final por unidad</summary>
        public decimal FinalPrice { get; set; }

        /// <summary>Tipo de precio aplicado</summary>
        public PriceType AppliedPriceType { get; set; }

        /// <summary>Indica si es precio aislado (no combina con otros descuentos)</summary>
        public bool IsIsolatedPrice => AppliedPriceType == PriceType.Special || AppliedPriceType == PriceType.Dealer;

        /// <summary>Información descriptiva del precio/descuento aplicado</summary>
        public string DiscountInfo { get; set; } = string.Empty;

        /// <summary>Descuento total por unidad (ListPrice - FinalPrice)</summary>
        public decimal TotalDiscount => ListPrice - FinalPrice;
    }

    /// <summary>
    /// Servicio centralizado para cálculo de precios y descuentos.
    /// Implementa las reglas de negocio definidas en descuentos_casaceja.md
    /// </summary>
    public class PricingService
    {
        /// <summary>
        /// Calcula el precio para un producto siguiendo las reglas de negocio:
        /// 1. Determina precio base (mayoreo si califica, sino menudeo)
        /// 2. Aplica descuento de categoría si existe y no es precio aislado
        /// </summary>
        /// <param name="product">Producto a calcular</param>
        /// <param name="quantity">Cantidad solicitada</param>
        /// <param name="category">Categoría del producto (puede ser null)</param>
        /// <param name="forcePriceType">Tipo de precio forzado (para F2/F3)</param>
        /// <returns>Resultado del cálculo de precio</returns>
        public PriceCalculation CalculatePrice(
            Product product,
            int quantity,
            Category? category = null,
            PriceType? forcePriceType = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var result = new PriceCalculation
            {
                ListPrice = product.PriceRetail,
                BasePrice = product.PriceRetail,
                AppliedPriceType = PriceType.Retail
            };

            // Si se fuerza un tipo de precio (F2 o F3)
            if (forcePriceType.HasValue)
            {
                return CalculateIsolatedPrice(product, forcePriceType.Value, result);
            }

            // Paso 1: Determinar precio base (mayoreo o menudeo)
            var discountInfoParts = new System.Collections.Generic.List<string>();

            if (product.QualifiesForWholesale(quantity))
            {
                result.BasePrice = product.PriceWholesale;
                result.AppliedPriceType = PriceType.Wholesale;
                discountInfoParts.Add($"Mayoreo ({product.WholesaleQuantity}+ pzas)");
            }

            result.FinalPrice = result.BasePrice;

            // Paso 2: Aplicar descuento de categoría si existe
            if (category != null && category.HasDiscount && category.Discount > 0)
            {
                result.CategoryDiscountPercent = category.Discount;
                result.CategoryDiscountAmount = result.BasePrice * (category.Discount / 100m);
                result.FinalPrice = result.BasePrice - result.CategoryDiscountAmount;
                discountInfoParts.Add($"{category.Discount}% desc. {category.Name}");
            }

            result.DiscountInfo = string.Join(" + ", discountInfoParts);

            return result;
        }

        /// <summary>
        /// Calcula un precio aislado (especial o vendedor).
        /// Los precios aislados NO se combinan con otros descuentos.
        /// </summary>
        private PriceCalculation CalculateIsolatedPrice(Product product, PriceType priceType, PriceCalculation result)
        {
            switch (priceType)
            {
                case PriceType.Special:
                    if (product.HasSpecialPrice)
                    {
                        result.BasePrice = product.PriceSpecial;
                        result.FinalPrice = product.PriceSpecial;
                        result.AppliedPriceType = PriceType.Special;
                        result.DiscountInfo = "Precio Especial";
                    }
                    break;

                case PriceType.Dealer:
                    if (product.HasDealerPrice)
                    {
                        result.BasePrice = product.PriceDealer;
                        result.FinalPrice = product.PriceDealer;
                        result.AppliedPriceType = PriceType.Dealer;
                        result.DiscountInfo = "Precio Vendedor";
                    }
                    break;

                default:
                    result.FinalPrice = result.BasePrice;
                    break;
            }

            // Los precios aislados no tienen descuento de categoría
            result.CategoryDiscountPercent = 0;
            result.CategoryDiscountAmount = 0;

            return result;
        }

        /// <summary>
        /// Verifica si se puede aplicar precio especial a un item.
        /// Se bloquea si el item ya tiene descuentos aplicados.
        /// </summary>
        /// <param name="item">Item del carrito</param>
        /// <param name="product">Producto (para obtener precio especial)</param>
        /// <param name="blockedReason">Razón del bloqueo si no se puede aplicar</param>
        /// <returns>true si se puede aplicar, false si está bloqueado</returns>
        public bool CanApplySpecialPrice(CartItem item, Product product, out string blockedReason)
        {
            blockedReason = string.Empty;

            // Verificar si el producto tiene precio especial
            if (!product.HasSpecialPrice || product.PriceSpecial <= 0)
            {
                blockedReason = $"El producto \"{item.ProductName}\" no tiene precio especial configurado.";
                return false;
            }

            // Verificar si ya tiene precio aislado
            if (item.PriceType == "special" || item.PriceType == "dealer")
            {
                blockedReason = $"El producto ya tiene {(item.PriceType == "special" ? "precio especial" : "precio vendedor")} aplicado.";
                return false;
            }

            // Verificar si ya tiene descuentos aplicados (mayoreo o categoría)
            if (item.TotalDiscount > 0 || item.PriceType == "wholesale")
            {
                var currentPrice = item.FinalUnitPrice;
                var specialPrice = product.PriceSpecial;
                var difference = currentPrice - specialPrice;

                blockedReason = $"No se puede aplicar precio especial.\n\n" +
                    $"El producto ya tiene descuentos aplicados:\n" +
                    $"• {item.DiscountInfo}\n\n" +
                    $"Precio actual: ${currentPrice:N2}\n" +
                    $"Precio especial: ${specialPrice:N2}\n" +
                    $"Diferencia: ${Math.Abs(difference):N2} {(difference > 0 ? "menos" : "más")}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifica si se puede aplicar precio vendedor a un item.
        /// Se bloquea si el item ya tiene descuentos aplicados.
        /// </summary>
        public bool CanApplyDealerPrice(CartItem item, Product product, out string blockedReason)
        {
            blockedReason = string.Empty;

            // Verificar si el producto tiene precio vendedor
            if (!product.HasDealerPrice || product.PriceDealer <= 0)
            {
                blockedReason = $"El producto \"{item.ProductName}\" no tiene precio vendedor configurado.";
                return false;
            }

            // Verificar si ya tiene precio aislado
            if (item.PriceType == "special" || item.PriceType == "dealer")
            {
                blockedReason = $"El producto ya tiene {(item.PriceType == "special" ? "precio especial" : "precio vendedor")} aplicado.";
                return false;
            }

            // Verificar si ya tiene descuentos aplicados
            if (item.TotalDiscount > 0 || item.PriceType == "wholesale")
            {
                var currentPrice = item.FinalUnitPrice;
                var dealerPrice = product.PriceDealer;
                var difference = currentPrice - dealerPrice;

                blockedReason = $"No se puede aplicar precio vendedor.\n\n" +
                    $"El producto ya tiene descuentos aplicados:\n" +
                    $"• {item.DiscountInfo}\n\n" +
                    $"Precio actual: ${currentPrice:N2}\n" +
                    $"Precio vendedor: ${dealerPrice:N2}\n" +
                    $"Diferencia: ${Math.Abs(difference):N2} {(difference > 0 ? "menos" : "más")}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Aplica precio especial a un item del carrito.
        /// </summary>
        /// <returns>Mensaje de confirmación o error</returns>
        public (bool Success, string Message) ApplySpecialPrice(CartItem item, Product product)
        {
            if (!CanApplySpecialPrice(item, product, out string blockedReason))
            {
                return (false, blockedReason);
            }

            var oldPrice = item.FinalUnitPrice;
            var newPrice = product.PriceSpecial;
            var savings = (oldPrice - newPrice) * item.Quantity;

            // Actualizar el item
            item.FinalUnitPrice = newPrice;
            item.TotalDiscount = item.ListPrice - newPrice;
            item.PriceType = "special";
            item.DiscountInfo = "Precio Especial";

            return (true, $"✓ Precio especial aplicado a \"{item.ProductName}\"\n\n" +
                $"Precio anterior: ${oldPrice:N2}\n" +
                $"Precio especial: ${newPrice:N2}\n" +
                $"Ahorro total: ${savings:N2}");
        }

        /// <summary>
        /// Aplica precio vendedor a un item del carrito.
        /// </summary>
        public (bool Success, string Message) ApplyDealerPrice(CartItem item, Product product)
        {
            if (!CanApplyDealerPrice(item, product, out string blockedReason))
            {
                return (false, blockedReason);
            }

            var oldPrice = item.FinalUnitPrice;
            var newPrice = product.PriceDealer;
            var savings = (oldPrice - newPrice) * item.Quantity;

            // Actualizar el item
            item.FinalUnitPrice = newPrice;
            item.TotalDiscount = item.ListPrice - newPrice;
            item.PriceType = "dealer";
            item.DiscountInfo = "Precio Vendedor";

            return (true, $"✓ Precio vendedor aplicado a \"{item.ProductName}\"\n\n" +
                $"Precio anterior: ${oldPrice:N2}\n" +
                $"Precio vendedor: ${newPrice:N2}\n" +
                $"Ahorro total: ${savings:N2}");
        }

        /// <summary>
        /// Obtiene el nombre del tipo de precio para display
        /// </summary>
        public static string GetPriceTypeName(string priceType)
        {
            return priceType?.ToLower() switch
            {
                "retail" => "Menudeo",
                "wholesale" => "Mayoreo",
                "special" => "Especial",
                "dealer" => "Vendedor",
                _ => priceType ?? "Menudeo"
            };
        }

        /// <summary>
        /// Determina si un tipo de precio es aislado (no combina con otros descuentos)
        /// </summary>
        public static bool IsIsolatedPriceType(string priceType)
        {
            return priceType?.ToLower() == "special" || priceType?.ToLower() == "dealer";
        }

        /// <summary>
        /// Revierte un item a su precio original de menudeo.
        /// Se usa cuando el usuario quiere quitar un descuento especial/vendedor.
        /// </summary>
        /// <param name="item">Item del carrito</param>
        /// <param name="product">Producto (para obtener precio original)</param>
        /// <returns>Mensaje de confirmación</returns>
        public (bool Success, string Message) RevertToRetailPrice(CartItem item, Product product)
        {
            if (item.PriceType != "special" && item.PriceType != "dealer")
            {
                return (false, "El producto no tiene un precio especial o vendedor aplicado.");
            }

            var oldPrice = item.FinalUnitPrice;
            var oldPriceType = item.PriceType == "special" ? "especial" : "vendedor";
            var newPrice = product.PriceRetail;

            // Revertir el item al precio de menudeo
            item.FinalUnitPrice = newPrice;
            item.TotalDiscount = 0;
            item.PriceType = "retail";
            item.DiscountInfo = string.Empty;

            return (true, $"✓ Precio {oldPriceType} removido de \"{item.ProductName}\"\n\n" +
                $"Precio anterior: ${oldPrice:N2}\n" +
                $"Precio actual: ${newPrice:N2}");
        }
    }
}
