using System;
using System.IO;
using System.Net.Mime;
using System.Timers;
using System.Web;
using Kesco.Lib.Web.Comet;

namespace Kesco.Lib.Web.Controls.V4.Common
{
    /// <summary>
    ///     Класс управления страницей (основной функционал - "сборщик мусора", то есть удаление просроченных страниц из
    ///     Application)
    /// </summary>
    public class PageManager
    {
        /// <summary>
        ///     Объект Application
        /// </summary>
        public static HttpApplicationState Application;

        /// <summary>
        ///     Метод вызывается при старте приложения, в нем устанавливается таймер проверки страниц на неактуальность - 1 минута
        ///     То есть, GC запускается каждую минуту
        /// </summary>
        /// <param name="application">Объект Application приложения</param>
        public static void Start(HttpApplicationState application)
        {
            Application = application;
            var t = new Timer(120000);
            t.Elapsed += TimerElapsed;
            t.Enabled = true;
        }

        public static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            CometServer.WriteLog("Start TimerElapsed");
            DeleteOldPagesFromApplication(sender, e);
        }

        /// <summary>
        ///     Удаление неактуальных страниц и соединений
        /// </summary>
        /// <param name="sender">таймер</param>
        /// <param name="e">аргумент таймера</param>
        public static void DeleteOldPagesFromApplication(object sender, ElapsedEventArgs e)
        {
            
            CometServer.WriteLog("Start DeleteOldPagesFromApplication");

            CometServer.ClearExpiredConnections();
            Application.Lock();
            for (var i = Application.Keys.Count - 1; i >= 0; i--)
            {
                var key = Application.GetKey(i);
                var p = Application[key] as Page;

                //null объекты не нужны
                if (Application[key] == null)
                {
                    Application.Remove(key);
                    CometServer.WriteLog("Application.Remove DeleteOldPages -> " + key);
                    continue;
                }
                if (p == null) continue;

                if (!CometServer.Connections.Exists(s => s.ClientGuid == key))
                {
                    p.V4Dispose();

                    CometServer.WriteLog("V4Dispose DeleteOldPages -> " + key);
                }
            }

            Application.UnLock();
          
        }
    }
}