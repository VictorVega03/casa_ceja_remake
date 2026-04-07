using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CasaCejaRemake.Data;
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
        private readonly DatabaseService _databaseService;

        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public FolioService(
            CashCloseRepository cashCloseRepository,
            SaleRepository saleRepository,
            CreditRepository creditRepository,
            LayawayRepository layawayRepository,
            BaseRepository<CreditPayment> creditPaymentRepository,
            BaseRepository<LayawayPayment> layawayPaymentRepository,
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

                // Sin recursión — incrementar hasta encontrar uno libre
                while (await ExisteFolioAsync(folio))
                {
                    Console.WriteLine($"[FolioService] Folio duplicado detectado: {folio}. Incrementando...");
                    nuevoSecuencial++;
                    folio = $"{sucursalId:D2}{cajaId:D2}{ahora:ddMMyyyy}{tipo}{nuevoSecuencial:D4}";
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

                string tabla = tipo switch
                {
                    'V' => "sales",
                    'A' => "layaways",
                    'C' => "credits",
                    'X' => "cash_closes",
                    _   => null!
                };

                int max = 0;

                if (tipo == 'P')
                {
                    // Abonos están en dos tablas — tomar el MAX de ambas
                    var sql = $"SELECT MAX(CAST(SUBSTR(folio, 14, 4) AS INTEGER)) FROM credit_payments WHERE folio LIKE ?";
                    var maxCredits = await _databaseService.ExecuteScalarAsync<int>(sql, $"{prefijo}%");

                    sql = $"SELECT MAX(CAST(SUBSTR(folio, 14, 4) AS INTEGER)) FROM layaway_payments WHERE folio LIKE ?";
                    var maxLayaways = await _databaseService.ExecuteScalarAsync<int>(sql, $"{prefijo}%");

                    max = Math.Max(maxCredits, maxLayaways);
                }
                else
                {
                    var sql = $"SELECT MAX(CAST(SUBSTR(folio, 14, 4) AS INTEGER)) FROM {tabla} WHERE folio LIKE ?";
                    max = await _databaseService.ExecuteScalarAsync<int>(sql, $"{prefijo}%");
                }

                Console.WriteLine($"[FolioService] Último secuencial encontrado: {max}");
                return max;
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
