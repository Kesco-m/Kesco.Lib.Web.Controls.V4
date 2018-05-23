using System.Collections.Specialized;

namespace Kesco.Lib.Web.Controls.V4.Common
{
    /// <summary>
    ///     Класс управление событиями на странице
    /// </summary>
    public class PageEventManager
    {
        /// <summary>
        ///     Делегат отправки события
        /// </summary>
        /// <param name="sender">Инициатор</param>
        /// <param name="eventName">Наименование события</param>
        /// <param name="eventArgs">Аргументы события</param>
        public delegate void SendEventHandler(object sender, string eventName, NameValueCollection eventArgs);

        private static PageEventManager _p;

        /// <summary>
        ///     Получение экземпляра менеджера
        /// </summary>
        /// <returns>менеджер</returns>
        public static PageEventManager GetInstatnce()
        {
            return _p ?? (_p = new PageEventManager());
        }

        /// <summary>
        ///     Событие страницы
        /// </summary>
        public event SendEventHandler PageEvent;

        /// <summary>
        ///     Отправка события
        /// </summary>
        /// <param name="sender">Инициатор</param>
        /// <param name="eventName">Наименование события</param>
        /// <param name="eventArgs">Аргументы события</param>
        public void SendEvent(object sender, string eventName, NameValueCollection eventArgs)
        {
            if (PageEvent != null)
                PageEvent(sender, eventName, eventArgs);
        }
    }
}