using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    public class CustomerService
    {
        private readonly DatabaseService _databaseService;
        private readonly BaseRepository<Customer> _customerRepository;

        public CustomerService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _customerRepository = new BaseRepository<Customer>(databaseService);
        }

        public async Task<List<Customer>> SearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                // Retornar todos los clientes activos (limitado a 100)
                var allCustomers = await _customerRepository.FindAsync(c => c.Active);
                return allCustomers.Take(100).OrderBy(c => c.Name).ToList();
            }

            var searchTerm = term.Trim().ToLower();
            var customers = await _customerRepository.FindAsync(c => c.Active);

            return customers
                .Where(c => 
                    (c.Name?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.Phone?.Contains(searchTerm) ?? false) ||
                    (c.Email?.ToLower().Contains(searchTerm) ?? false))
                .OrderBy(c => c.Name)
                .Take(100)
                .ToList();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _customerRepository.GetByIdAsync(id);
        }

        public async Task<Customer?> GetByPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;

            var customers = await _customerRepository.FindAsync(c => 
                c.Phone == phone && c.Active);
            
            return customers.FirstOrDefault();
        }

        public async Task<bool> ExistsByPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;

            var customers = await _customerRepository.FindAsync(c => 
                c.Phone == phone && c.Active);
            
            return customers.Any();
        }

        public async Task<int> CreateAsync(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (string.IsNullOrWhiteSpace(customer.Name))
                throw new ArgumentException("El nombre del cliente es requerido.");

            if (string.IsNullOrWhiteSpace(customer.Phone))
                throw new ArgumentException("El telefono del cliente es requerido.");

            // Verificar si ya existe un cliente con el mismo telefono
            if (await ExistsByPhoneAsync(customer.Phone))
                throw new InvalidOperationException($"Ya existe un cliente con el telefono {customer.Phone}.");

            customer.Active = true;
            customer.SyncStatus = 1; // Pending
            customer.CreatedAt = DateTime.Now;
            customer.UpdatedAt = DateTime.Now;

            return await _customerRepository.AddAsync(customer);
        }

        public async Task UpdateAsync(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var existing = await _customerRepository.GetByIdAsync(customer.Id);
            if (existing == null)
                throw new InvalidOperationException($"Cliente con ID {customer.Id} no encontrado.");

            // Verificar si el nuevo telefono ya existe en otro cliente
            if (existing.Phone != customer.Phone)
            {
                var otherWithPhone = await _customerRepository.FindAsync(c => 
                    c.Phone == customer.Phone && c.Active && c.Id != customer.Id);
                
                if (otherWithPhone.Any())
                    throw new InvalidOperationException($"Ya existe otro cliente con el telefono {customer.Phone}.");
            }

            customer.UpdatedAt = DateTime.Now;
            customer.SyncStatus = 1; // Pending

            await _customerRepository.UpdateAsync(customer);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null) return false;

            customer.Active = false;
            customer.UpdatedAt = DateTime.Now;
            customer.SyncStatus = 1; // Pending

            await _customerRepository.UpdateAsync(customer);
            return true;
        }

        public async Task<List<Customer>> GetAllActiveAsync()
        {
            var customers = await _customerRepository.FindAsync(c => c.Active);
            return customers.OrderBy(c => c.Name).ToList();
        }

        public async Task<int> CountActiveAsync()
        {
            var customers = await _customerRepository.FindAsync(c => c.Active);
            return customers.Count;
        }
    }
}
