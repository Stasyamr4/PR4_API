using System.Windows.Controls;
using WpfApp1.Pages;

namespace WpfApp1.Helpers
{
    /// <summary>Навигация между страницами внутри Frame главного окна.</summary>
    public static class Nav
    {
        private static readonly Stack<Page> History = new();

        private static Frame Frame => MainWindow.Instance.MainFrame;

        /// <summary>Перейти на страницу, запомнив текущую (чтобы вернуться кнопкой «Назад»).</summary>
        public static void To(Page page)
        {
            if (Frame.Content is Page current) History.Push(current);
            Go(page);
        }

        /// <summary>Перейти на страницу и очистить историю (вход, выход, главная).</summary>
        public static void Reset(Page page)
        {
            History.Clear();
            Go(page);
        }

        /// <summary>Вернуться на предыдущую страницу (с сохранёнными фильтрами и т.п.).</summary>
        public static void Back()
        {
            if (History.Count > 0) Go(History.Pop());
            //else Reset(new CatalogPage());
        }

        private static void Go(Page page)
        {
            Frame.Navigate(page);
            // Журнал Frame нам не нужен — историю ведём сами
            while (Frame.CanGoBack) Frame.RemoveBackEntry();
        }
    }
}
