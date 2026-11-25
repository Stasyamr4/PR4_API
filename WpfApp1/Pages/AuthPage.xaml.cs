using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Xps;
using WpfApp1.Helpers;
using WpfApp1.Services;

namespace WpfApp1.Pages
{
    public partial class AuthPage : Page
    {
        public AuthPage()
        {
            InitializeComponent();
            Loaded += (_, _) => LoginInput.Focus();
        }

        private async void Enter_Click(object sender, RoutedEventArgs e) => await SignInAsync();

        private async void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) await SignInAsync();
        }

        private async Task SignInAsync()
        {
            if (!EnterBtn.IsEnabled) return;
            ErrorText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(LoginInput.Text) || string.IsNullOrEmpty(PasswordInput.Password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            EnterBtn.IsEnabled = false;
            try
            {
                await ApiClient.LoginAsync(LoginInput.Text, PasswordInput.Password);
                Ui.Toast($"Добро пожаловать, {ApiClient.CurrentUser!.Name}!");
                //Nav.Reset(new CatalogPage());
            }
            catch (ApiException ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                EnterBtn.IsEnabled = true;
            }
        }

        private void ShowError(string text)
        {
            ErrorText.Text = text;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void Register_Click(object sender, RoutedEventArgs e) => Nav.Reset(new RegisterPage());
        private void Guest_Click(object sender, RoutedEventArgs e) => Nav.Reset(new CatalogPage());
    }
}
