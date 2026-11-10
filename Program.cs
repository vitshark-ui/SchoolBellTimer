using System;
using System.Threading;
using System.Windows.Forms;

namespace SchoolBellTimer
{
    /// <summary>
    /// Главная точка входа для приложения школьного таймера звонков SchoolBellTimer.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Главный метод запуска программы.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Включение поддержки перехвата необработанных исключений потока интерфейса
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // Настройка стандартных визуальных стилей элементов Windows Forms
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Запуск главного окна приложения
            Application.Run(new MainForm());
        }

        /// <summary>
        /// Обработчик необработанных исключений в потоке интерфейса Windows Forms.
        /// </summary>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowErrorMessage(e.Exception);
        }

        /// <summary>
        /// Обработчик критических необработанных исключений домена приложения.
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            ShowErrorMessage(ex);
        }

        /// <summary>
        /// Отображение дружелюбного сообщения об ошибке для пользователя.
        /// </summary>
        /// <param name="ex">Объект исключения.</param>
        private static void ShowErrorMessage(Exception ex)
        {
            string errorDetails = (ex != null) ? ex.Message : "Неизвестная ошибка";
            MessageBox.Show(
                string.Format("Произошла непредвиденная ошибка в работе программы:\n\n{0}\n\nПриложение продолжит работу, если это возможно.", errorDetails),
                "SchoolBellTimer — Внимание",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
