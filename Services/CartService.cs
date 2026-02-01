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

        public event EventHandler? CartChanged;

        public event EventHandler<char>? CollectionChanged;

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
            CartChanged?.Invoke(this, EventArgs.Empty);
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