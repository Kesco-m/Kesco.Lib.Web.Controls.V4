using System.Collections.Generic;
using System.Data;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    /// <summary>
    ///     Класс для хранения настроек источника данных контрола Grid
    /// </summary>
    public class GridDbSourceSettings
    {
        /// <summary>
        ///     Параметры SqlQuery
        /// </summary>
        public Dictionary<string, object> SqlParams;

        /// <summary>
        ///     SQL-запрос или выражение
        /// </summary>
        public string SqlQuery { get; set; }

        /// <summary>
        ///     Тип запроса или выражения
        /// </summary>
        public CommandType SqlCommandType { get; set; }

        /// <summary>
        ///     Строка подключения
        /// </summary>
        public string ConnectionString { get; set; }
    }
}