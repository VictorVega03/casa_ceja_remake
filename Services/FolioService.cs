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
        private readonly DatabaseService _databaseService;
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public FolioService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Genera un folio único para venta.
        /// </summary>
        public async Task<string> GenerarFolioVentaAsync(int sucursalId, int cajaId)
        {
            return await GenerarFolioAsync(sucursalId, cajaId, 'V');
        }

        /// <summary>
        /// Genera un folio único para apartado.
        /// </summary>
        public async Task<string> GenerarFolioApartadoAsync(int sucursalId, int cajaId)
        {
            return await GenerarFolioAsync(sucursalId, cajaId, 'A');
        }

        /// <summary>
        /// Genera un folio único para crédito.
        /// </summary>
        public async Task<string> GenerarFolioCreditoAsync(int sucursalId, int cajaId)
        {
            return await GenerarFolioAsync(sucursalId, cajaId, 'C');
        }

        /// <summary>
        /// Genera un folio único para pago/abono.
        /// </summary>
        public async Task<string> GenerarFolioPagoAsync(int sucursalId, int cajaId)
        {
            return await GenerarFolioAsync(sucursalId, cajaId, 'P');
        }

        /// <summary>
        /// Genera un folio único para corte de caja.
        /// REGLA: El secuencial de cortes es GLOBAL y nunca reinicia.
        /// Simplemente obtiene el último corte y suma 1 al secuencial.
        /// </summary>
        public async Task<string> GenerarFolioCorteAsync(int sucursalId, int cajaId)
        {
            await _lock.WaitAsync();
            try
            {
                var ahora = DateTime.Now;
                
                // Obtener el último corte global (mayor secuencial en todos los registros)
                var repository = new BaseRepository<CashClose>(_databaseService);
                var todosLosCortes = await repository.GetAllAsync();
                
                int nuevoSecuencial = 1;
                
                if (todosLosCortes.Any())
                {
                    // Buscar el máximo secuencial entre TODOS los cortes (sin filtro)
                    var maxSecuencial = todosLosCortes
                        .Where(c => c.Folio.Length == 17) // Validar formato correcto
                        .Select(c =>
                        {
                            var secStr = c.Folio.Substring(13, 4); // Últimos 4 dígitos
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
                
                // Construir el nuevo folio (17 caracteres)
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
        /// Método principal de generación de folios.
        /// Thread-safe mediante SemaphoreSlim.
        /// </summary>
        private async Task<string> GenerarFolioAsync(int sucursalId, int cajaId, char tipo)
        {
            await _lock.WaitAsync();
            try
            {
                var ahora = DateTime.Now;
                var inicioDelDia = ahora.Date;
                var finDelDia = inicioDelDia.AddDays(1);

                // Obtener el último secuencial usado hoy
                var ultimoSecuencial = await ObtenerUltimoSecuencialAsync(
                    sucursalId, cajaId, tipo, inicioDelDia, finDelDia);

                // Incrementar el secuencial
                var nuevoSecuencial = ultimoSecuencial + 1;

                // Construir el folio
                var folio = $"{sucursalId:D2}{cajaId:D2}{ahora:ddMMyyyy}{tipo}{nuevoSecuencial:D4}";

                // Validar que no existe (por seguridad adicional)
                if (await ExisteFolioAsync(folio))
                {
                    // Si existe (caso extremadamente raro), reintentar
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
                // Construir el prefijo del folio para buscar (13 caracteres: SSCCDMMYYYYT)
                var prefijo = $"{sucursalId:D2}{cajaId:D2}{fechaInicio:ddMMyyyy}{tipo}";
                
                Console.WriteLine($"[FolioService] Buscando último secuencial con prefijo: {prefijo}");

                // Determinar la tabla según el tipo
                List<string> foliosEncontrados = new List<string>();
                
                if (tipo == 'P')
                {
                    // Para pagos, buscar en ambas tablas usando GetAllAsync y filtrar en memoria
                    var creditPaymentRepo = new BaseRepository<CreditPayment>(_databaseService);
                    var layawayPaymentRepo = new BaseRepository<LayawayPayment>(_databaseService);
                    
                    var allCreditPayments = await creditPaymentRepo.GetAllAsync();
                    var allLayawayPayments = await layawayPaymentRepo.GetAllAsync();
                    
                    // Filtrar en memoria
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
                    // Para otros tipos, buscar en su tabla correspondiente
                    if (tipo == 'V')
                    {
                        var repo = new BaseRepository<Sale>(_databaseService);
                        var all = await repo.GetAllAsync();
                        var items = all.Where(s => 
                            s.SaleDate >= fechaInicio && 
                            s.SaleDate < fechaFin && 
                            s.Folio.StartsWith(prefijo) && 
                            s.Folio.Length == 17).ToList();
                        foliosEncontrados.AddRange(items.Select(s => s.Folio));
                    }
                    else if (tipo == 'A')
                    {
                        var repo = new BaseRepository<Layaway>(_databaseService);
                        var all = await repo.GetAllAsync();
                        var items = all.Where(l => 
                            l.LayawayDate >= fechaInicio && 
                            l.LayawayDate < fechaFin && 
                            l.Folio.StartsWith(prefijo) && 
                            l.Folio.Length == 17).ToList();
                        foliosEncontrados.AddRange(items.Select(l => l.Folio));
                    }
                    else if (tipo == 'C')
                    {
                        var repo = new BaseRepository<Credit>(_databaseService);
                        var all = await repo.GetAllAsync();
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
                    return 0; // No hay folios previos hoy
                }

                // Extraer los secuenciales y encontrar el máximo
                var maxSecuencial = foliosEncontrados
                    .Select(folio =>
                    {
                        var secStr = folio.Substring(13, 4); // Posición 13-16
                        return int.TryParse(secStr, out var sec) ? sec : 0;
                    })
                    .Max();

                Console.WriteLine($"[FolioService] Último secuencial encontrado: {maxSecuencial} (de {foliosEncontrados.Count} folios)");
                return maxSecuencial;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FolioService] Error obteniendo último secuencial: {ex.Message}");
                return 0; // En caso de error, empezar desde 0
            }
        }

        /// <summary>
        /// Verifica si un folio ya existe en alguna de las tablas.
        /// </summary>
        private async Task<bool> ExisteFolioAsync(string folio)
        {
            try
            {
                // Buscar en todas las tablas que tienen folios
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
                    {
                        return true;
                    }
                }

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
                var sucursalId = int.Parse(folio.Substring(0, 2));    // Posición 0-1
                var cajaId = int.Parse(folio.Substring(2, 2));         // Posición 2-3
                var dia = int.Parse(folio.Substring(4, 2));            // Posición 4-5
                var mes = int.Parse(folio.Substring(6, 2));            // Posición 6-7
                var anio = int.Parse(folio.Substring(8, 4));           // Posición 8-11
                var tipo = folio[12];                                   // Posición 12
                var secuencial = int.Parse(folio.Substring(13, 4));    // Posición 13-16

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
