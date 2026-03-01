using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de carrito de compras.
    /// Soporta múltiples colecciones (A, B, C, D) para ventas simultáneas.
    /// </summary>
    public interface ICartService
    {
        // ─── Propiedades de la colección actual ───────────────────────────────────
        char CurrentCollectionId { get; }
        Collection CurrentCollection { get; }
        ObservableCollection<CartItem> Items { get; }
        decimal Total { get; }
        decimal TotalDiscount { get; }
        int TotalItems { get; }
        bool IsEmpty { get; }

        // ─── Descuento general ────────────────────────────────────────────────────
        decimal GeneralDiscountPercent { get; }
        decimal GeneralDiscountAmount { get; }
        bool IsGeneralDiscountPercentage { get; }
        bool HasGeneralDiscount { get; }
        decimal CalculatedGeneralDiscount { get; }
        decimal FinalTotal { get; }

        // ─── Eventos ──────────────────────────────────────────────────────────────
        event EventHandler? CartChanged;
        event EventHandler<char>? CollectionChanged;

        // ─── Métodos de descuento ─────────────────────────────────────────────────
        void ApplyGeneralDiscount(decimal value, bool isPercentage);
        void ClearGeneralDiscount();
        void NotifyCartChanged();

        // ─── Métodos de colección ─────────────────────────────────────────────────
        void ChangeCollection();
        void ChangeCollectionPrevious();
        void ChangeCollection(char identifier);

        // ─── Métodos de productos ─────────────────────────────────────────────────
        void AddProduct(CartItem item);
        bool ModifyQuantity(int productId, int newQuantity);
        bool ModifyQuantityByIndex(int index, int newQuantity);
        bool RemoveProduct(int productId);
        bool RemoveProductByIndex(int index);
        void ClearCart();
        void ClearAllCollections();

        // ─── Utilidades ───────────────────────────────────────────────────────────
        List<(char Id, int Items, decimal Total)> GetCollectionsSummary();
        List<(char Id, int Items, decimal Total, bool IsActive)> GetAllCollectionsInfo();
        bool CollectionHasItems(char identifier);
        bool HasPendingCollections();
        List<CartItem> GetItemsForSale();
    }
}
