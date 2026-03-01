using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Servicio centralizado para generación de folios únicos.
    /// Thread-safe y libre de condiciones de carrera.
    /// Formato: SSCCDMMYYYYT#### (17 caracteres)
    /// SS = Sucursal (2 dígitos)
    /// CC = Caja (2 dígitos)
    /// DD = Día (2 dígitos)
    /// MM = Mes (2 dígitos)
    /// YYYY = Año (4 dígitos)
    /// T = Tipo (V=Venta, A=Apartado, C=Crédito, P=Pago/Abono, X=Corte)
    /// #### = Secuencial (4 dígitos)
    ///
    /// REGLA CRÍTICA: Secuencial de cortes (X) es GLOBAL y nunca reinicia.
    /// Otros tipos (V, A, C, P) reinician secuencial cada día.
    /// </summary>
    public class FolioService
    {
        private readonly CashCloseRepository _cashCloseRepository;
        private readonly SaleRepository _saleRepository;
        private readonly CreditRepository _creditRepository;
        private readonly LayawayRepository _layawayRepository;
        private readonly BaseRepository<CreditPayment> _creditPaymentRepository;
        private readonly BaseRepository<LayawayPayment> _layawayPaymentRepository;

        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public FolioService(
            CashCloseRepository cashCloseRepository,
            SaleRepository saleRepository,
            CreditRepository creditRepository,
            LayawayRepository layawayRepository,
            BaseRepository<CreditPayment> creditPaymentRepository,
            BaseRepository<LayawayPayment> layawayPaymentRepository)
        {
            _cashCloseRepository = cashCloseRepository ?? throw new ArgumentNullException(nameof(cashCloseRepository));
            _saleRepository = saleRepository ?? throw new ArgumentNullException(nameof(saleRepository));
            _creditRepository = creditRepository ?? throw new ArgumentNullException(nameof(creditRepository));
            _layawayRepository = layawayRepository ?? throw new ArgumentNullException(nameof(layawayRepository));
            _creditPaymentRepository = creditPaymentRepository ?? throw new ArgumentNullException(nameof(creditPaymentRepository));
            _layawayPaymentRepository = layawayPaymentRepository ?? throw new ArgumentNullException(nameof(layawayPaymentRepository));
        }

        /// <summary>Genera un folio único para venta.</summary>
        public async Task<string> GenerarFolioVentaAsync(int sucursalId, int cajaId)
        {
            return await GenerarFolioAsync(sucursalId, cajaId, 'V');
        }

        /// <summary>Genera un folio único para apartado.</summary>
        public async Task<string> GenerarFolioApartadoAsync(int sucursalId, int cajaId)
        {
            return await GenerarFolioAsync(sucursalId, cajaId, 'A');
        }

        /// <summary>Genera un folio único para crédito.</summary>
        public async Task<string> GenerarFolioCreditoAsync(int sucursalId, int cajaId)
        {
            return await GenerarFolioAsync(sucursalId, cajaId, 'C');
        }

        /// <summary>Genera un folio único para pago/abono.</summary>
        public async Task<string> GenerarFolioPagoAsync(int sucursalId, int cajaId)
        {
            return await GenerarFolioAsync(sucursalId, cajaId, 'P');
        }

        /// <summary>
        /// Genera un folio único para corte de caja.
        /// REGLA: El secuencial de cortes es GLOBAL y nunca reinicia.
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

        /// <summary>
        /// Método principal de generación de folios. Thread-safe mediante SemaphoreSlim.
        /// </summary>
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

        /// <summary>
        /// Obtiene el último secuencial usado para la combinación sucursal/caja/tipo/fecha.
        /// </summary>
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
                    var allCreditPayments = await _creditPaymentRepository.FindAsync(p =>
                        p.PaymentDate >= fechaInicio && p.PaymentDate < fechaFin);
                    var allLayawayPayments = await _layawayPaymentRepository.FindAsync(p =>
                        p.PaymentDate >= fechaInicio && p.PaymentDate < fechaFin);

                    foliosEncontrados.AddRange(allCreditPayments
                        .Where(p => p.Folio.StartsWith(prefijo) && p.Folio.Length == 17)
                        .Select(p => p.Folio));
                    foliosEncontrados.AddRange(allLayawayPayments
                        .Where(p => p.Folio.StartsWith(prefijo) && p.Folio.Length == 17)
                        .Select(p => p.Folio));
                }
                else if (tipo == 'V')
                {
                    var items = await _saleRepository.GetByBranchSinceDateAsync(0, fechaInicio);
                    // GetByBranchSinceDateAsync with branchId=0 won't filter by branch,
                    // but FolioService needs all branches — fall back to FindAsync
                    var all = await _saleRepository.FindAsync(s =>
                        s.SaleDate >= fechaInicio && s.SaleDate < fechaFin);
                    foliosEncontrados.AddRange(all
                        .Where(s => s.Folio.StartsWith(prefijo) && s.Folio.Length == 17)
                        .Select(s => s.Folio));
                }
                else if (tipo == 'A')
                {
                    var all = await _layawayRepository.FindAsync(l =>
                        l.LayawayDate >= fechaInicio && l.LayawayDate < fechaFin);
                    foliosEncontrados.AddRange(all
                        .Where(l => l.Folio.StartsWith(prefijo) && l.Folio.Length == 17)
                        .Select(l => l.Folio));
                }
                else if (tipo == 'C')
                {
                    var all = await _creditRepository.GetCreatedSinceAsync(fechaInicio);
                    foliosEncontrados.AddRange(all
                        .Where(c => c.CreditDate < fechaFin && c.Folio.StartsWith(prefijo) && c.Folio.Length == 17)
                        .Select(c => c.Folio));
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
        /// Verifica si un folio ya existe en alguna de las tablas usando FindAsync.
        /// </summary>
        private async Task<bool> ExisteFolioAsync(string folio)
        {
            try
            {
                var inSales = await _saleRepository.FindAsync(s => s.Folio == folio);
                if (inSales.Any()) return true;

                var inLayaways = await _layawayRepository.FindAsync(l => l.Folio == folio);
                if (inLayaways.Any()) return true;

                var inCredits = await _creditRepository.FindAsync(c => c.Folio == folio);
                if (inCredits.Any()) return true;

                var inCreditPayments = await _creditPaymentRepository.FindAsync(p => p.Folio == folio);
                if (inCreditPayments.Any()) return true;

                var inLayawayPayments = await _layawayPaymentRepository.FindAsync(p => p.Folio == folio);
                if (inLayawayPayments.Any()) return true;

                var inCashCloses = await _cashCloseRepository.FindAsync(c => c.Folio == folio);
                if (inCashCloses.Any()) return true;

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FolioService] Error verificando existencia de folio: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parsea un folio y extrae sus componentes.
        /// Formato: SSCCDMMYYYYT#### (17 caracteres)
        /// </summary>
        public (int sucursalId, int cajaId, DateTime fecha, char tipo, int secuencial) ParsearFolio(string folio)
        {
            if (string.IsNullOrEmpty(folio) || folio.Length != 17)
            {
                throw new ArgumentException("Folio inválido. Debe tener 17 caracteres.");
            }

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
