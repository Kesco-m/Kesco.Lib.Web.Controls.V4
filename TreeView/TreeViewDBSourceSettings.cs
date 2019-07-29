using System.Collections.Generic;

namespace Kesco.Lib.Web.Controls.V4.TreeView
{
    /// <summary>
    ///     Класс для хранения настроек источника данных контрола TreeView
    /// </summary>
    public class TreeViewDbSourceSettings
    {
        /// <summary>
        ///     Параметры SqlQuery
        /// </summary>
        public Dictionary<string, object> AddSqlParams;

        /// <summary>
        ///     Строка подключения
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Название талицы
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        ///     Название представления
        /// </summary>
        public string ViewName { get; set; }

        /// <summary>
        ///     Название идентификатора поля первичного ключа
        /// </summary>
        public string PkField { get; set; }

        /// <summary>
        ///     Название отображаемого поля
        /// </summary>
        public string NameField { get; set; }

        /// <summary>
        ///     Название поля с полным путем
        /// </summary>
        public string PathField { get; set; }

        /// <summary>
        ///     Название SQL-функции для получения полного пути к узлу
        /// </summary>
        public string TreePathFuncName { get; set; }

        /// <summary>
        ///     Тип полного пути к узлу, получаемого SQL-функцией
        /// </summary>
        public byte TreePathType { get; set; }

        /// <summary>
        ///     Название SQL-поля Изменил
        /// </summary>
        public string ModifyUserField { get; set; }

        /// <summary>
        ///     Название SQL-поля Изменено
        /// </summary>
        public string ModifyDateField { get; set; }

        public string AddSqlQuery { get; set; }

        /// <summary>
        ///     Название корневого узла
        /// </summary>
        public string RootName { get; set; }
    }
}