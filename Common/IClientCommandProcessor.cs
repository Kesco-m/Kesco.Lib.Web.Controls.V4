using System.Collections.Specialized;

namespace Kesco.Lib.Web.Controls.V4.Common
{
    /// <summary>
    ///     Интерфейс обработки клиентских команд
    /// </summary>
    public interface IClientCommandProcessor
    {
        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="name">Коллекция параметров</param>
        void ProcessClientCommand(NameValueCollection name);
    }
}