using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.Shared;
using System;

namespace CasaCejaRemake.Views.Shared
{
    /// <summary>
    /// Vista de Login - Code Behind
    /// </summary>
    public partial class LoginView : Window
    {
        private LoginViewModel? _viewModel;

        public LoginView()
        {
            InitializeComponent();
            
            // Suscribirse al cambio de DataContext para capturar el ViewModel
            DataContextChanged += OnDataContextChanged;
            
            // Configurar eventos de la vista
            Loaded += OnLoaded;
        }

        /// <summary>
        /// Evento cuando el DataContext cambia
        /// </summary>
        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            // Desuscribir del ViewModel anterior si existe
            if (_viewModel != null)
            {
                _viewModel.LoginSuccess -= OnLoginSuccess;
                _viewModel.LoginCancelled -= OnLoginCancelled;
            }

            // Suscribir al nuevo ViewModel
            if (DataContext is LoginViewModel viewModel)
            {
                _viewModel = viewModel;
                _viewModel.LoginSuccess += OnLoginSuccess;
                _viewModel.LoginCancelled += OnLoginCancelled;
                Console.WriteLine("[LoginView] ViewModel conectado correctamente");
            }
        }

        /// <summary>
        /// Evento cuando la ventana se ha cargado
        /// </summary>
        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Enfocar el campo de usuario al cargar
            UsernameTextBox.Focus();
        }

        /// <summary>
        /// Evento cuando el login es exitoso
        /// </summary>
        private void OnLoginSuccess(object? sender, LoginSuccessEventArgs e)
        {
            Console.WriteLine($"[LoginView] Login exitoso: {e.UserName} (Admin: {e.IsAdmin}) - Cerrando ventana...");
            // Guardar el resultado en Tag antes de cerrar
            Tag = "success";
            // Cerrar la ventana inmediatamente
            Close();
            Console.WriteLine("[LoginView] Close() ejecutado");
        }

        /// <summary>
        /// Evento cuando se cancela el login
        /// </summary>
        private void OnLoginCancelled(object? sender, EventArgs e)
        {
            Console.WriteLine("[LoginView] Login cancelado - Cerrando ventana...");
            // Cerrar la ventana
            Close();
        }

        /// <summary>
        /// Manejo de teclas especiales en los TextBox
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Enter en username: ir a password
            if (e.Source == UsernameTextBox && e.Key == Key.Enter)
            {
                PasswordTextBox.Focus();
                e.Handled = true;
                return;
            }
            // Enter en password: intentar login
            else if (e.Source == PasswordTextBox && e.Key == Key.Enter)
            {
                if (DataContext is LoginViewModel viewModel)
                {
                    if (viewModel.LoginCommand.CanExecute(null))
                    {
                        viewModel.LoginCommand.Execute(null);
                    }
                }
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        /// <summary>
        /// Cleanup al cerrar
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            Console.WriteLine("[LoginView] OnClosed ejecutado");
            
            // Desuscribir eventos
            if (_viewModel != null)
            {
                _viewModel.LoginSuccess -= OnLoginSuccess;
                _viewModel.LoginCancelled -= OnLoginCancelled;
                _viewModel = null;
            }

            base.OnClosed(e);
        }
    }
}