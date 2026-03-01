using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using casa_ceja_remake.Helpers;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.ViewModels.Shared;
using System.Collections.Generic;

namespace CasaCejaRemake.Views.Shared
{
    public partial class UserManagementView : Window
    {
        private UserManagementViewModel? _viewModel;
        private bool _isDialogOpen;

        public UserManagementView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Activated += OnActivated;
        }
        
        private void OnActivated(object? sender, EventArgs e)
        {
            Focus();
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is UserManagementViewModel vm)
            {
                _viewModel = vm;
                
                // Suscribirse a eventos
                _viewModel.CloseRequested += OnCloseRequested;
                _viewModel.AddUserRequested += OnAddUserRequested;
                _viewModel.EditUserRequested += OnEditUserRequested;

                // Inicializar datos
                await _viewModel.InitializeAsync();

                // Establecer handler para Enter en el DataGrid
                var dataGrid = this.FindControl<DataGrid>("UsersGrid");
                if (dataGrid != null)
                {
                    dataGrid.AddHandler(KeyDownEvent, DataGrid_PreviewKeyDown, RoutingStrategies.Tunnel);
                }
            }
        }

        private void DataGrid_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel?.SelectedUser != null)
            {
                ShowUserDetails(_viewModel.SelectedUser);
                e.Handled = true;
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private async void OnAddUserRequested(object? sender, EventArgs e)
        {
            if (_viewModel == null) return;
            
            // Obtener UserService via DI container expuesto por App
            bool isAdminMode = _viewModel.IsAdminMode;
            var appUserService = App.Current is App app ? app.GetUserService() : null;
            
            if (appUserService == null)
            {
                await DialogHelper.ShowMessageDialog(this, "Error", "No se pudo obtener el servicio de usuarios.");
                return;
            }

            var formVm = new UserFormViewModel(appUserService, isAdminMode);
            var formView = new UserFormView
            {
                DataContext = formVm
            };
            
            // Suscribirse al evento de guardado para refrescar la lista
            formVm.SaveCompleted += async (s, args) => 
            {
                await _viewModel.RefreshAsync();
            };

            _isDialogOpen = true;
            await formView.ShowDialog(this);
            _isDialogOpen = false;
        }

        private async void OnEditUserRequested(object? sender, User? user)
        {
            if (_viewModel == null || user == null) return;

            bool isAdminMode = _viewModel.IsAdminMode;
            var appUserService = App.Current is App app ? app.GetUserService() : null;
            
            if (appUserService == null) return;

            var formVm = new UserFormViewModel(appUserService, isAdminMode, user);
            var formView = new UserFormView
            {
                DataContext = formVm
            };

            formVm.SaveCompleted += async (s, args) => 
            {
                await _viewModel.RefreshAsync();
            };

            _isDialogOpen = true;
            await formView.ShowDialog(this);
            _isDialogOpen = false;
        }

        private async void ShowUserDetails(User user)
        {
            if (_viewModel == null || user == null) return;
            
            var appUserService = App.Current is App app ? app.GetUserService() : null;
            if (appUserService == null) return;

            // Formulario en modo solo lectura con la nueva vista de detalles
            var formVm = new UserFormViewModel(appUserService, _viewModel.IsAdminMode, user, isReadOnly: true);
            var detailsView = new UserDetailsView
            {
                DataContext = formVm
            };
            
            _isDialogOpen = true;
            await detailsView.ShowDialog(this);
            _isDialogOpen = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // No procesar atajos si hay un dialog abierto
            if (_isDialogOpen)
            {
                return;
            }

            KeyboardShortcutHelper.HandleShortcut(e, new Dictionary<Key, Action>
            {
                { Key.Escape, Close },
                { Key.F2, () => {
                    if (_viewModel?.SelectedUser != null)
                    {
                        OnEditUserRequested(this, _viewModel.SelectedUser);
                    }
                }}
            });

            base.OnKeyDown(e);
        }
        
        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= OnCloseRequested;
                _viewModel.AddUserRequested -= OnAddUserRequested;
                _viewModel.EditUserRequested -= OnEditUserRequested;
            }
            base.OnClosed(e);
        }
    }
}
