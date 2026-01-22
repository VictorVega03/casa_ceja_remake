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
        public LoginView()
        {
            InitializeComponent();
            
            // Configurar eventos de la vista
            Loaded += OnLoaded;
        }

        /// <summary>
        /// Evento cuando la ventana se ha cargado
        /// </summary>
        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Enfocar el campo de usuario al cargar
            UsernameTextBox.Focus();

            // Configurar eventos del ViewModel si existe
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.LoginSuccess += OnLoginSuccess;
                viewModel.LoginCancelled += OnLoginCancelled;
            }
        }

        /// <summary>
        /// Evento cuando el login es exitoso
        /// </summary>
        private void OnLoginSuccess(object? sender, LoginSuccessEventArgs e)
        {
            Console.WriteLine($"Login exitoso en LoginView: {e.UserName} (Admin: {e.IsAdmin})");
            // Guardar el resultado en Tag antes de cerrar
            Tag = "success";
            // Cerrar la ventana
            Close();
        }

        /// <summary>
        /// Evento cuando se cancela el login
        /// </summary>
        private void OnLoginCancelled(object? sender, EventArgs e)
        {
            // Cerrar la aplicación
            Close(null);
        }

        /// <summary>
        /// Manejo de teclas especiales en los TextBox
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Enter en username: ir a password
            if (e.Source == UsernameTextBox && e.Key == Key.Enter)
            {
                PasswordTextBox.Focus();
                e.Handled = true;
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
            }
        }

        /// <summary>
        /// Cleanup al cerrar
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // Desuscribir eventos
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.LoginSuccess -= OnLoginSuccess;
                viewModel.LoginCancelled -= OnLoginCancelled;
            }

            base.OnClosed(e);
        }
    }
}