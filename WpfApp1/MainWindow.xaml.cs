using System.Windows;
using System.Windows.Threading;
using WpfApp1.Helpers;
using WpfApp1.Pages;
using WpfApp1.Services;
using WpfApp1.Pages.Admin;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; } = null!;

        private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(2.5) };

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            _toastTimer.Tick += (_, _) =>
            {
                _toastTimer.Stop();
                ToastBox.Visibility = Visibility.Collapsed;
            };

            ApiClient.SessionChanged += RefreshHeader;
            RefreshHeader();

            // Каталог открыт для всех, в том числе для гостей
            //Nav.Reset(new CatalogPage());
        }

        public void ShowToast(string message)
        {
            ToastText.Text = message;
            ToastBox.Visibility = Visibility.Visible;
            _toastTimer.Stop();
            _toastTimer.Start();
        }

        /// <summary>Показывает в шапке только то, что доступно текущей роли: гость / пользователь / администратор.</summary>
        private void RefreshHeader()
        {
            bool auth = ApiClient.IsAuthorized;
            static Visibility Vis(bool v) => v ? Visibility.Visible : Visibility.Collapsed;

            FavoritesBtn.Visibility = Vis(auth);
            BasketBtn.Visibility = Vis(auth);
            OrdersBtn.Visibility = Vis(auth);
            AdminBtn.Visibility = Vis(ApiClient.IsAdmin);

            LoginBtn.Visibility = Vis(!auth);
            RegisterBtn.Visibility = Vis(!auth);
            LogoutBtn.Visibility = Vis(auth);
            UserText.Visibility = Vis(auth);
            UserText.Text = auth
                ? ApiClient.CurrentUser!.Name + (ApiClient.IsAdmin ? " (администратор)" : "")
                : "";
        }

        private void Catalog_Click(object sender, RoutedEventArgs e) => Nav.Reset(new CatalogPage());
        private void Favorites_Click(object sender, RoutedEventArgs e) => Nav.Reset(new FavoritesPage());
        private void Basket_Click(object sender, RoutedEventArgs e) => Nav.Reset(new BasketPage());
        private void Orders_Click(object sender, RoutedEventArgs e) => Nav.Reset(new OrdersPage());
        private void Admin_Click(object sender, RoutedEventArgs e) => Nav.Reset(new AdminPage());
        private void Login_Click(object sender, RoutedEventArgs e) => Nav.Reset(new AuthPage());
        private void Register_Click(object sender, RoutedEventArgs e) => Nav.Reset(new RegisterPage());

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            ApiClient.Logout();
            Nav.Reset(new CatalogPage());
        }
    }
}
