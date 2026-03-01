using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Servicio centralizado para generación de folios únicos.
    /// Thread-safe y libre de condiciones de carrera.
    /// Formato: SSCCDMMYYYYT#### (17 caracteres)
    /// SS=Sucursal, CC=Caja, DD=Día, MM=Mes, YYYY=Año, T=Tipo, ####=Secuencial
    /// 
    /// REGLA CRÍTICA: Secuencial de cortes (X) es GLOBAL y nunca reinicia.
    /// Otros tipos (V, A, C, P) reinician secuencial cada día.
    /// </summary>
    public class FolioService : IFolioService
    {
        private readonly ICashCloseRepository _cashCloseRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly ICreditRepository _creditRepository;
        private readonly ILayawayRepository _layawayRepository;
        private readonly ICreditPaymentRepository _creditPaymentRepository;
        private readonly ILayawayPaymentRepository _layawayPaymentRepository;
        private readonly DatabaseService _databaseService; // Solo para ExisteFolioAsync (raw SQL)
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public FolioService(
            ICashCloseRepository cashCloseRepository,
            ISaleRepository saleRepository,
            ICreditRepository creditRepository,
            ILayawayRepository layawayRepository,
            ICreditPaymentRepository creditPaymentRepository,
            ILayawayPaymentRepository layawayPaymentRepository,
            DatabaseService databaseService)
        {
            _cashCloseRepository = cashCloseRepository ?? throw new ArgumentNullException(nameof(cashCloseRepository));
            _saleRepository = saleRepository ?? throw new ArgumentNullException(nameof(saleRepository));
            _creditRepository = creditRepository ?? throw new ArgumentNullException(nameof(creditRepository));
            _layawayRepository = layawayRepository ?? throw new ArgumentNullException(nameof(layawayRepository));
            _creditPaymentRepository = creditPaymentRepository ?? throw new ArgumentNullException(nameof(creditPaymentRepository));
            _layawayPaymentRepository = layawayPaymentRepository ?? throw new ArgumentNullException(nameof(layawayPaymentRepository));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        public async Task<string> GenerarFolioVentaAsync(int sucursalId, int cajaId)
            => await GenerarFolioAsync(sucursalId, cajaId, 'V');

        public async Task<string> GenerarFolioApartadoAsync(int sucursalId, int cajaId)
            => await GenerarFolioAsync(sucursalId, cajaId, 'A');

        public async Task<string> GenerarFolioCreditoAsync(int sucursalId, int cajaId)
            => await GenerarFolioAsync(sucursalId, cajaId, 'C');

        public async Task<string> GenerarFolioPagoAsync(int sucursalId, int cajaId)
            => await GenerarFolioAsync(sucursalId, cajaId, 'P');

        /// <summary>
        /// Genera un folio único para corte de caja.
        /// Secuencial GLOBAL — nunca reinicia.
        /// </summary>
        public async Task<string> GenerarFolioCorteAsync(int sucursalId, int cajaId)
        {
            await _lock.WaitAsync();
            try
            {
                var ahora = DateTime.Now;

                var todosLosCortes = await _cashCloseRepository.GetAllAsync();

                int nuevoSecuencial = 1;

                if (todosLosCortes.Any())
                {
                    var maxSecuencial = todosLosCortes
                        .Where(c => c.Folio.Length == 17)
                        .Select(c =>
                        {
                            var secStr = c.Folio.Substring(13, 4);
                            return int.TryParse(secStr, out var sec) ? sec : 0;
                        })
                        .DefaultIfEmpty(0)
                        .Max();

                    nuevoSecuencial = maxSecuencial + 1;
                    Console.WriteLine($"[FolioService] Último secuencial global de corte: {maxSecuencial}");
                }
                else
                {
                    Console.WriteLine($"[FolioService] No hay cortes previos en el sistema");
                }

                var folio = $"{sucursalId:D2}{cajaId:D2}{ahora:ddMMyyyy}X{nuevoSecuencial:D4}";
                Console.WriteLine($"[FolioService] Folio de corte generado: {folio} (secuencial global: {nuevoSecuencial})");
                return folio;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<string> GenerarFolioAsync(int sucursalId, int cajaId, char tipo)
        {
            await _lock.WaitAsync();
            try
            {
                var ahora = DateTime.Now;
                var inicioDelDia = ahora.Date;
                var finDelDia = inicioDelDia.AddDays(1);

                var ultimoSecuencial = await ObtenerUltimoSecuencialAsync(
                    sucursalId, cajaId, tipo, inicioDelDia, finDelDia);

                var nuevoSecuencial = ultimoSecuencial + 1;
                var folio = $"{sucursalId:D2}{cajaId:D2}{ahora:ddMMyyyy}{tipo}{nuevoSecuencial:D4}";

                if (await ExisteFolioAsync(folio))
                {
                    Console.WriteLine($"[FolioService] Folio duplicado detectado: {folio}. Reintentando...");
                    return await GenerarFolioAsync(sucursalId, cajaId, tipo);
                }

                Console.WriteLine($"[FolioService] Folio generado: {folio}");
                return folio;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<int> ObtenerUltimoSecuencialAsync(
            int sucursalId, int cajaId, char tipo, DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                var prefijo = $"{sucursalId:D2}{cajaId:D2}{fechaInicio:ddMMyyyy}{tipo}";
                Console.WriteLine($"[FolioService] Buscando último secuencial con prefijo: {prefijo}");

                List<string> foliosEncontrados = new List<string>();

                if (tipo == 'P')
                {
                    var allCreditPayments = await _creditPaymentRepository.GetAllAsync();
                    var allLayawayPayments = await _layawayPaymentRepository.GetAllAsync();

                    var creditPayments = allCreditPayments
                        .Where(p => p.PaymentDate >= fechaInicio &&
                                   p.PaymentDate < fechaFin &&
                                   p.Folio.StartsWith(prefijo) &&
                                   p.Folio.Length == 17)
                        .ToList();

                    var layawayPayments = allLayawayPayments
                        .Where(p => p.PaymentDate >= fechaInicio &&
                                   p.PaymentDate < fechaFin &&
                                   p.Folio.StartsWith(prefijo) &&
                                   p.Folio.Length == 17)
                        .ToList();

                    foliosEncontrados.AddRange(creditPayments.Select(p => p.Folio));
                    foliosEncontrados.AddRange(layawayPayments.Select(p => p.Folio));
                }
                else
                {
                    if (tipo == 'V')
                    {
                        var all = await _saleRepository.GetAllAsync();
                        var items = all.Where(s =>
                            s.SaleDate >= fechaInicio &&
                            s.SaleDate < fechaFin &&
                            s.Folio.StartsWith(prefijo) &&
                            s.Folio.Length == 17).ToList();
                        foliosEncontrados.AddRange(items.Select(s => s.Folio));
                    }
                    else if (tipo == 'A')
                    {
                        var all = await _layawayRepository.GetAllAsync();
                        var items = all.Where(l =>
                            l.LayawayDate >= fechaInicio &&
                            l.LayawayDate < fechaFin &&
                            l.Folio.StartsWith(prefijo) &&
                            l.Folio.Length == 17).ToList();
                        foliosEncontrados.AddRange(items.Select(l => l.Folio));
                    }
                    else if (tipo == 'C')
                    {
                        var all = await _creditRepository.GetAllAsync();
                        var items = all.Where(c =>
                            c.CreditDate >= fechaInicio &&
                            c.CreditDate < fechaFin &&
                            c.Folio.StartsWith(prefijo) &&
                            c.Folio.Length == 17).ToList();
                        foliosEncontrados.AddRange(items.Select(c => c.Folio));
                    }
                }

                if (!foliosEncontrados.Any())
                {
                    Console.WriteLine($"[FolioService] No se encontraron folios con prefijo {prefijo}");
                    return 0;
                }

                var maxSecuencial = foliosEncontrados
                    .Select(folio =>
                    {
                        var secStr = folio.Substring(13, 4);
                        return int.TryParse(secStr, out var sec) ? sec : 0;
                    })
                    .Max();

                Console.WriteLine($"[FolioService] Último secuencial encontrado: {maxSecuencial} (de {foliosEncontrados.Count} folios)");
                return maxSecuencial;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FolioService] Error obteniendo último secuencial: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Verifica si un folio ya existe en alguna de las tablas.
        /// Usa raw SQL por eficiencia (cross-table check con una sola conexión).
        /// </summary>
        private async Task<bool> ExisteFolioAsync(string folio)
        {
            try
            {
                var queries = new[]
                {
                    $"SELECT COUNT(*) FROM sales WHERE folio = '{folio}'",
                    $"SELECT COUNT(*) FROM layaways WHERE folio = '{folio}'",
                    $"SELECT COUNT(*) FROM credits WHERE folio = '{folio}'",
                    $"SELECT COUNT(*) FROM credit_payments WHERE folio = '{folio}'",
                    $"SELECT COUNT(*) FROM layaway_payments WHERE folio = '{folio}'",
                    $"SELECT COUNT(*) FROM cash_closes WHERE folio = '{folio}'"
                };

                foreach (var query in queries)
                {
                    var count = await _databaseService.ExecuteScalarAsync<int>(query);
                    if (count > 0)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FolioService] Error verificando existencia de folio: {ex.Message}");
                return false;
            }
        }

        public (int sucursalId, int cajaId, DateTime fecha, char tipo, int secuencial) ParsearFolio(string folio)
        {
            if (string.IsNullOrEmpty(folio) || folio.Length != 17)
                throw new ArgumentException("Folio inválido. Debe tener 17 caracteres.");

            try
            {
                var sucursalId = int.Parse(folio.Substring(0, 2));
                var cajaId = int.Parse(folio.Substring(2, 2));
                var dia = int.Parse(folio.Substring(4, 2));
                var mes = int.Parse(folio.Substring(6, 2));
                var anio = int.Parse(folio.Substring(8, 4));
                var tipo = folio[12];
                var secuencial = int.Parse(folio.Substring(13, 4));
                var fecha = new DateTime(anio, mes, dia);

                return (sucursalId, cajaId, fecha, tipo, secuencial);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parseando folio: {ex.Message}");
            }
        }
    }
}
