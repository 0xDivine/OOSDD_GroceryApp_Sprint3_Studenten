using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Grocery.App.ViewModels;
using Grocery.App.Services;

namespace Grocery.App.Views
{
    public partial class LoginView : ContentPage
    {
        // Parameterless ctor for XAML tooling
        public LoginView()
        {
            InitializeComponent();
        }

        // Constructor used by DI in App
        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        // Safe runtime lookups for named XAML controls.
        private Entry UsernameEntryControl => this.FindByName<Entry>("UsernameEntry");
        private Entry PasswordEntryControl => this.FindByName<Entry>("PasswordEntry");

        // Register handler with robust error handling
        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            try
            {
                string username = (UsernameEntryControl?.Text ?? string.Empty).Trim();
                string password = PasswordEntryControl?.Text ?? string.Empty;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                {
                    await DisplayAlert("Error", "Please enter both username and password.", "OK");
                    return;
                }

                bool added = SimpleUserStore.Register(username, password);
                if (!added)
                {
                    await DisplayAlert("Info", "Username already exists.", "OK");
                    return;
                }

                await DisplayAlert("Success", "Registered successfully. You can now login.", "OK");

                if (UsernameEntryControl != null) UsernameEntryControl.Text = string.Empty;
                if (PasswordEntryControl != null) PasswordEntryControl.Text = string.Empty;
            }
            catch (Exception ex)
            {
                // Log to console and show friendly message
                Console.WriteLine($"Register error: {ex}");
                await DisplayAlert("Error", "An unexpected error occurred while registering. Please try again.", "OK");
            }
        }

        // Login handler: validate, set session and switch to AppShell safely
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                string username = (UsernameEntryControl?.Text ?? string.Empty).Trim();
                string password = PasswordEntryControl?.Text ?? string.Empty;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                {
                    await DisplayAlert("Error", "Please enter both username and password.", "OK");
                    return;
                }

                if (!SimpleUserStore.Validate(username, password))
                {
                    await DisplayAlert("Error", "Invalid username or password.", "OK");
                    return;
                }

                // Successful login: set session
                UserSession.CurrentUsername = username;

                // Show welcome alert first; after OK navigate to main app UI.
                await DisplayAlert("Welcome", $"Logged in as {username}", "OK");

                try
                {
                    // Ensure MainPage switch happens on UI thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Application.Current.MainPage = new AppShell();
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Navigation error: {ex}");
                    await DisplayAlert("Error", "Navigation to the main app failed. Please restart the app.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex}");
                await DisplayAlert("Error", "An unexpected error occurred while logging in. Please try again.", "OK");
            }
        }

        // Tiny in-memory store: keeps everything short and dependency-free.
        // This remains non-persistent by design (lifetime = app process).
        private static class SimpleUserStore
        {
            private static readonly Dictionary<string, string> _users = new(StringComparer.OrdinalIgnoreCase);

            public static bool Register(string username, string password)
            {
                if (string.IsNullOrWhiteSpace(username) || _users.ContainsKey(username))
                    return false;
                _users[username] = password;
                return true;
            }

            public static bool Validate(string username, string password)
            {
                if (string.IsNullOrWhiteSpace(username)) return false;
                return _users.TryGetValue(username, out var pw) && pw == password;
            }
        }
    }
}