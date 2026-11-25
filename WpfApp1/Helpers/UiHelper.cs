using System.Windows;
using System.Windows.Xps;
using WpfApp1.Pages;
using WpfApp1.Services;

namespace WpfApp1.Helpers
{
    /// <summary>Общие диалоги и безопасный запуск асинхронных действий.</summary>
    public static class Ui
    {
        public static void Error(string message)
            => MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

        public static void Info(string message)
            => MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

        public static bool Confirm(string message)
            => MessageBox.Show(message, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

        /// <summary>Короткое всплывающее уведомление внизу окна.</summary>
        public static void Toast(string message) => MainWindow.Instance.ShowToast(message);

        /// <summary>
        /// Выполняет действие и превращает ошибки API в понятные сообщения.
        /// Если сессия истекла — отправляет пользователя на страницу входа.
        /// </summary>
        public static async Task RunAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (ApiException ex)
            {
                Error(ex.Message);
                if (ex.StatusCode == 401) Nav.Reset(new AuthPage());
            }
            catch (Exception ex)
            {
                Error("Непредвиденная ошибка: " + ex.Message);
            }
        }

        /// <summary>Для гостей: предлагает войти. Возвращает true, если пользователь уже авторизован.</summary>
        public static bool RequireAuth()
        {
            if (ApiClient.IsAuthorized) return true;
            if (Confirm("Это действие доступно только авторизованным пользователям.\nПерейти ко входу?"))
                Nav.To(new AuthPage());
            return false;
        }
    }
}