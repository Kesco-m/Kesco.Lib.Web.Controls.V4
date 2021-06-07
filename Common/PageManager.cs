using System.Diagnostics;
using System.Timers;
using Kesco.Lib.Web.SignalR;

namespace Kesco.Lib.Web.Controls.V4.Common
{
    /// <summary>
    ///     Класс управления страницей (основной функционал - "сборщик мусора", то есть удаление просроченных страниц из
    ///     KescoHub)
    /// </summary>
    public class PageManager
    {
        private static Timer _timer;


        /// <summary>
        ///     Метод вызывается при старте приложения, в нем устанавливается таймер проверки страниц на неактуальность - 2 минуты
        ///     То есть, GC запускается каждую 2 минуты
        /// </summary>
        public static void Start()
        {
            //Debug.WriteLine("Таймер очистки запущен");

            _timer?.Dispose();

            _timer = new Timer(120000);
            _timer.Elapsed += TimerElapsed;
            _timer.Enabled = true;
        }

        public static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            KescoHub.RemovePagesWithOutConnection();
        }

        
    }
}