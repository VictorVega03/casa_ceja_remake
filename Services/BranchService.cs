using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    public class BranchService
    {
        private readonly BaseRepository<Branch> _branchRepo;
        private readonly ApiClient _apiClient;

        public BranchService(BaseRepository<Branch> branchRepo, ApiClient apiClient)
        {
            _branchRepo = branchRepo ?? throw new ArgumentNullException(nameof(branchRepo));
            _apiClient  = apiClient  ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public ApiClient ApiClient => _apiClient;

        public async Task<List<Branch>> GetAllAsync() =>
            await _branchRepo.FindAsync(b => b.Active);

        public async Task<(bool Success, string Message, Branch? Data)> CreateAsync(Branch branch)
        {
            var payload = new
            {
                name         = branch.Name,
                address      = branch.Address,
                email        = branch.Email,
                razon_social = branch.RazonSocial,
            };

            try
            {
                var response = await _apiClient.PostAsync<Branch>("/api/v1/admin/branches", payload);

                if (response.IsNetworkError)
                    return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.", null);

                if (response.IsServerError)
                    return (false, response.ServerMessage ?? "No se pudo crear la sucursal en el servidor.", null);

                if (!response.IsSuccess || response.Data == null)
                    return (false, response.ServerMessage ?? "No se pudo crear la sucursal en el servidor.", null);

                branch.Id         = response.Data.Id;
                branch.Active     = true;
                branch.CreatedAt  = DateTime.Now;
                branch.UpdatedAt  = DateTime.Now;
                branch.SyncStatus = 2;
                branch.LastSync   = DateTime.Now;

                await _branchRepo.UpsertAsync(branch);
                Console.WriteLine($"[BranchService] Sucursal creada: {branch.Name} (Id: {branch.Id})");
                return (true, "Sucursal creada exitosamente.", branch);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BranchService] Error creando sucursal: {ex.Message}");
                return (false, $"Error al crear sucursal: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Branch branch)
        {
            var payload = new
            {
                name         = branch.Name,
                address      = branch.Address,
                email        = branch.Email,
                razon_social = branch.RazonSocial,
                active       = branch.Active,
            };

            try
            {
                var response = await _apiClient.PutAsync<Branch>($"/api/v1/admin/branches/{branch.Id}", payload);

                if (response.IsNetworkError)
                    return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

                if (response.IsServerError)
                    return (false, response.ServerMessage ?? "No se pudo actualizar la sucursal en el servidor.");

                if (!response.IsSuccess)
                    return (false, response.ServerMessage ?? "No se pudo actualizar la sucursal en el servidor.");

                branch.UpdatedAt  = DateTime.Now;
                branch.SyncStatus = 2;
                branch.LastSync   = DateTime.Now;

                await _branchRepo.UpdateAsync(branch);
                Console.WriteLine($"[BranchService] Sucursal actualizada: {branch.Name}");
                return (true, "Sucursal actualizada exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BranchService] Error actualizando sucursal: {ex.Message}");
                return (false, $"Error al actualizar sucursal: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeactivateAsync(int branchId)
        {
            var branch = await _branchRepo.GetByIdAsync(branchId);
            if (branch == null)
                return (false, "Sucursal no encontrada.");

            try
            {
                var payload = new
                {
                    name         = branch.Name,
                    address      = branch.Address,
                    email        = branch.Email,
                    razon_social = branch.RazonSocial,
                    active       = false,
                };

                var response = await _apiClient.PutAsync<Branch>($"/api/v1/admin/branches/{branchId}", payload);

                if (response.IsNetworkError)
                    return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

                if (response.IsServerError || !response.IsSuccess)
                    return (false, response.ServerMessage ?? "No se pudo dar de baja la sucursal en el servidor.");

                branch.Active     = false;
                branch.UpdatedAt  = DateTime.Now;
                branch.SyncStatus = 2;
                branch.LastSync   = DateTime.Now;

                await _branchRepo.UpdateAsync(branch);
                Console.WriteLine($"[BranchService] Sucursal desactivada: {branch.Name}");
                return (true, $"Sucursal '{branch.Name}' dada de baja exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BranchService] Error desactivando sucursal: {ex.Message}");
                return (false, $"Error al dar de baja sucursal: {ex.Message}");
            }
        }
    }
}
