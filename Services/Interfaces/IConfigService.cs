using System;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de configuración.
    /// </summary>
    public interface IConfigService
    {
        // ─── Propiedades ───────────────────────────────────────────────────────────
        AppConfig AppConfig { get; }
        PosTerminalConfig PosTerminalConfig { get; }

        // ─── Eventos ──────────────────────────────────────────────────────────────
        event EventHandler? AppConfigChanged;
        event EventHandler? PosTerminalConfigChanged;

        // ─── Métodos ──────────────────────────────────────────────────────────────
        Task LoadAsync();
        Task SaveAppConfigAsync();
        Task SavePosTerminalConfigAsync();
        Task UpdateAppConfigAsync(Action<AppConfig> updateAction);
        Task UpdatePosTerminalConfigAsync(Action<PosTerminalConfig> updateAction);
    }
}
