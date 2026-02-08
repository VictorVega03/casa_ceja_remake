using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    /// Servicio para gestión de carritos de compra
    /// Soporta múltiples colecciones (A, B, C, D) para manejar ventas simultáneas

    public class CartService
    {
        private readonly Dictionary<char, Collection> _collections;
        private char _currentCollection = 'A';

        public CartService()
        {
            _collections = new Dictionary<char, Collection>
            {
                { 'A', new Collection { Identifier = 'A' } },
                { 'B', new Collection { Identifier = 'B' } },
                { 'C', new Collection { Identifier = 'C' } },
                { 'D', new Collection { Identifier = 'D' } }
            };
        }

        public char CurrentCollectionId => _currentCollection;

        public Collection CurrentCollection => _collections[_currentCollection];

        public ObservableCollection<CartItem> Items => CurrentCollection.Items;

        public decimal Total => CurrentCollection.Total;

        public decimal TotalDiscount => CurrentCollection.TotalDiscount;

        public int TotalItems => CurrentCollection.TotalItems;

        public bool IsEmpty => CurrentCollection.IsEmpty;

        // ========== DESCUENTO GENERAL SOBRE LA VENTA ==========
        // NOTA: Ahora cada colección mantiene su propio descuento general
        
        /// <summary>Porcentaje de descuento general (si aplica)</summary>
        public decimal GeneralDiscountPercent => CurrentCollection.GeneralDiscountPercent;

        /// <summary>Monto fijo de descuento general (si aplica)</summary>
        public decimal GeneralDiscountAmount => CurrentCollection.GeneralDiscountAmount;

        /// <summary>Indica si el descuento es porcentaje (true) o cantidad fija (false)</summary>
        public bool IsGeneralDiscountPercentage => CurrentCollection.IsGeneralDiscountPercentage;

        /// <summary>Indica si hay un descuento general aplicado</summary>
        public bool HasGeneralDiscount => CurrentCollection.HasGeneralDiscount;

        /// <summary>Descuento general calculado</summary>
        public decimal CalculatedGeneralDiscount => CurrentCollection.CalculatedGeneralDiscount;

        /// <summary>Total final después de descuento general</summary>
        public decimal FinalTotal => CurrentCollection.FinalTotal;

        /// <summary>
        /// Aplica un descuento general sobre el total de la venta
        /// </summary>
        /// <param name="value">Valor del descuento (porcentaje o monto fijo)</param>
        /// <param name="isPercentage">true = porcentaje, false = monto fijo</param>
        public void ApplyGeneralDiscount(decimal value, bool isPercentage)
        {
            CurrentCollection.IsGeneralDiscountPercentage = isPercentage;
            if (isPercentage)
            {
                CurrentCollection.GeneralDiscountPercent = Math.Max(0, Math.Min(100, value)); // Limitar entre 0 y 100
                CurrentCollection.GeneralDiscountAmount = 0;
            }
            else
            {
                CurrentCollection.GeneralDiscountAmount = Math.Max(0, Math.Min(value, CurrentCollection.Total)); // No exceder total
                CurrentCollection.GeneralDiscountPercent = 0;
            }
            CartChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Limpia el descuento general
        /// </summary>
        public void ClearGeneralDiscount()
        {
            CurrentCollection.GeneralDiscountPercent = 0;
            CurrentCollection.GeneralDiscountAmount = 0;
            CurrentCollection.IsGeneralDiscountPercentage = true;
            CartChanged?.Invoke(this, EventArgs.Empty);
        }

        // ========== FIN DESCUENTO GENERAL ==========

        public event EventHandler? CartChanged;

        public event EventHandler<char>? CollectionChanged;

        /// <summary>
        /// Notifica que el carrito ha cambiado (para uso externo)
        /// </summary>
        public void NotifyCartChanged()
        {
            CartChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Avanza a la siguiente cobranza (A→B→C→D→A)
        /// </summary>
        public void ChangeCollection()
        {
            _currentCollection = _currentCollection switch
            {
                'A' => 'B',
                'B' => 'C',
                'C' => 'D',
                'D' => 'A',
                _ => 'A'
            };
            CollectionChanged?.Invoke(this, _currentCollection);
            CartChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Retrocede a la cobranza anterior (A←B←C←D←A)
        /// </summary>
        public void ChangeCollectionPrevious()
        {
            _currentCollection = _currentCollection switch
            {
                'A' => 'D',
                'B' => 'A',
                'C' => 'B',
                'D' => 'C',
                _ => 'A'
            };
            CollectionChanged?.Invoke(this, _currentCollection);
            CartChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Cambia a una cobranza específica
        /// </summary>
        public void ChangeCollection(char identifier)
        {
            if (_collections.ContainsKey(identifier))
            {
                _currentCollection = identifier;
                CollectionChanged?.Invoke(this, _currentCollection);
                CartChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void AddProduct(CartItem item)
        {
            if (item == null) return;

            var existingItem = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                Items.Add(item);
            }
            CartChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool ModifyQuantity(int productId, int newQuantity)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return false;

            if (newQuantity <= 0)
            {
                Items.Remove(item);
            }
            else
            {
                item.Quantity = newQuantity;
            }
            CartChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool ModifyQuantityByIndex(int index, int newQuantity)
        {
            if (index < 0 || index >= Items.Count) return false;

            if (newQuantity <= 0)
            {
                Items.RemoveAt(index);
            }
            else
            {
                Items[index].Quantity = newQuantity;
            }
            CartChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool RemoveProduct(int productId)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return false;

            Items.Remove(item);
            CartChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool RemoveProductByIndex(int index)
        {
            if (index < 0 || index >= Items.Count) return false;

            Items.RemoveAt(index);
            CartChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void ClearCart()
        {
            Items.Clear();
            ClearGeneralDiscount(); // También limpia el descuento general
        }

        public void ClearAllCollections()
        {
            foreach (var collection in _collections.Values)
            {
                collection.Items.Clear();
            }
            CartChanged?.Invoke(this, EventArgs.Empty);
        }

        public List<(char Id, int Items, decimal Total)> GetCollectionsSummary()
        {
            return _collections.Values
                .Where(c => !c.IsEmpty)
                .Select(c => (c.Identifier, c.TotalItems, c.Total))
                .ToList();
        }

        /// <summary>
        /// Obtiene información de todas las cobranzas (vacías o no)
        /// </summary>
        public List<(char Id, int Items, decimal Total, bool IsActive)> GetAllCollectionsInfo()
        {
            return _collections.Values
                .Select(c => (c.Identifier, c.TotalItems, c.Total, c.Identifier == _currentCollection))
                .ToList();
        }

        /// <summary>
        /// Verifica si una cobranza específica tiene items
        /// </summary>
        public bool CollectionHasItems(char identifier)
        {
            return _collections.ContainsKey(identifier) && !_collections[identifier].IsEmpty;
        }

        public bool HasPendingCollections()
        {
            return _collections.Values.Any(c => !c.IsEmpty);
        }

        public List<CartItem> GetItemsForSale()
        {
            return Items.ToList();
        }
    }
}