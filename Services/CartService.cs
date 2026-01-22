using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CasaCejaRemake.Services
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal ListPrice { get; set; }
        public decimal FinalUnitPrice { get; set; }
        public decimal LineTotal => FinalUnitPrice * Quantity;
        public decimal TotalDiscount { get; set; }
        public string PriceType { get; set; } = "retail";
        public string DiscountInfo { get; set; } = string.Empty;
        public bool HasDiscount => TotalDiscount > 0;
        public byte[]? PricingData { get; set; }
    }

    public class Collection
    {
        public char Identifier { get; set; }
        public ObservableCollection<CartItem> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.LineTotal);
        public decimal TotalDiscount => Items.Sum(i => i.TotalDiscount * i.Quantity);
        public int TotalItems => Items.Sum(i => i.Quantity);
        public bool IsEmpty => Items.Count == 0;
    }

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