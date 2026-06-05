using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    public class SupplierService
    {
        private readonly BaseRepository<Supplier> _supplierRepo;
        private readonly ApiClient _apiClient;

        public SupplierService(BaseRepository<Supplier> supplierRepo, ApiClient apiClient)
        {
            _supplierRepo = supplierRepo ?? throw new ArgumentNullException(nameof(supplierRepo));
            _apiClient    = apiClient    ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public ApiClient ApiClient => _apiClient;

        public async Task<List<Supplier>> GetAllAsync() =>
            await _supplierRepo.FindAsync(s => s.Active);

        public async Task<(bool Success, string Message, Supplier? Data)> CreateAsync(Supplier supplier)
        {
            var payload = new
            {
                name    = supplier.Name,
                phone   = supplier.Phone,
                email   = supplier.Email,
                address = supplier.Address,
            };

            try
            {
                var response = await _apiClient.PostAsync<Supplier>("/api/v1/admin/suppliers", payload);

                if (response.IsNetworkError)
                    return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.", null);

                if (response.IsServerError)
                    return (false, response.ServerMessage ?? "No se pudo crear el proveedor en el servidor.", null);

                if (!response.IsSuccess || response.Data == null)
                    return (false, response.ServerMessage ?? "No se pudo crear el proveedor en el servidor.", null);

                supplier.Id         = response.Data.Id;
                supplier.Active     = true;
                supplier.CreatedAt  = DateTime.Now;
                supplier.UpdatedAt  = DateTime.Now;
                supplier.SyncStatus = 2;
                supplier.LastSync   = DateTime.Now;

                await _supplierRepo.UpsertAsync(supplier);
                Console.WriteLine($"[SupplierService] Proveedor creado: {supplier.Name} (Id: {supplier.Id})");
                return (true, "Proveedor creado exitosamente.", supplier);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupplierService] Error creando proveedor: {ex.Message}");
                return (false, $"Error al crear proveedor: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Supplier supplier)
        {
            var payload = new
            {
                name    = supplier.Name,
                phone   = supplier.Phone,
                email   = supplier.Email,
                address = supplier.Address,
                active  = supplier.Active,
            };

            try
            {
                var response = await _apiClient.PutAsync<Supplier>($"/api/v1/admin/suppliers/{supplier.Id}", payload);

                if (response.IsNetworkError)
                    return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

                if (response.IsServerError)
                    return (false, response.ServerMessage ?? "No se pudo actualizar el proveedor en el servidor.");

                if (!response.IsSuccess)
                    return (false, response.ServerMessage ?? "No se pudo actualizar el proveedor en el servidor.");

                supplier.UpdatedAt  = DateTime.Now;
                supplier.SyncStatus = 2;
                supplier.LastSync   = DateTime.Now;

                await _supplierRepo.UpdateAsync(supplier);
                Console.WriteLine($"[SupplierService] Proveedor actualizado: {supplier.Name}");
                return (true, "Proveedor actualizado exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupplierService] Error actualizando proveedor: {ex.Message}");
                return (false, $"Error al actualizar proveedor: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeactivateAsync(int supplierId)
        {
            var supplier = await _supplierRepo.GetByIdAsync(supplierId);
            if (supplier == null)
                return (false, "Proveedor no encontrado.");

            try
            {
                var payload = new
                {
                    name    = supplier.Name,
                    phone   = supplier.Phone,
                    email   = supplier.Email,
                    address = supplier.Address,
                    active  = false,
                };

                var response = await _apiClient.PutAsync<Supplier>($"/api/v1/admin/suppliers/{supplierId}", payload);

                if (response.IsNetworkError)
                    return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

                if (response.IsServerError || !response.IsSuccess)
                    return (false, response.ServerMessage ?? "No se pudo dar de baja el proveedor en el servidor.");

                supplier.Active     = false;
                supplier.UpdatedAt  = DateTime.Now;
                supplier.SyncStatus = 2;
                supplier.LastSync   = DateTime.Now;

                await _supplierRepo.UpdateAsync(supplier);
                Console.WriteLine($"[SupplierService] Proveedor desactivado: {supplier.Name}");
                return (true, $"Proveedor '{supplier.Name}' dado de baja exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupplierService] Error desactivando proveedor: {ex.Message}");
                return (false, $"Error al dar de baja proveedor: {ex.Message}");
            }
        }
    }
}
