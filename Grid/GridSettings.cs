using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Page = Kesco.Lib.Web.Controls.V4.Common.Page;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    /// <summary>
    ///     Класс настроек конторола Grid
    /// </summary>
    public class GridSettings : V4Control
    {
        private readonly int maxUniqueValues = 200;

        public int GridCmdListnerIndex { get; private set; }
        public string GridId { get; private set; }
        public bool IsPrintVersion { get; set; }

        public DataTable DT;
        public List<GridColumn> TableColumns;

        public Page V4Page
        {
            get { return Page as Page; }
            set { Page = value; }
        }

        /// <summary>
        ///     Конструктор
        /// </summary>
        public GridSettings()
        {

        }
        
        public GridSettings(DataTable dt, string gridId, int gridCmdListnerIndex, Page page)
        {
            DT = dt;
            GridCmdListnerIndex = gridCmdListnerIndex;
            GridId = gridId;
            V4Page = page;

            FillTableColumns();
        }
        
        /// <summary>
        /// Формирование колонок из DataTable
        /// </summary>
        private void FillTableColumns()
        {
            TableColumns = new List<GridColumn>();
            var inx = 0;
            foreach (DataColumn clmn in DT.Columns)
            {
                var x = new GridColumn(this)
                {
                    Id = Guid.NewGuid().ToString(),
                    Alias = clmn.ColumnName,
                    FieldName = clmn.ColumnName,
                    DisplayVisible = true,
                    DisplayOrder = inx++
                };
                var typeCode = Type.GetTypeCode(clmn.DataType);
                x.ColumnType = GetColumnType(typeCode);
                x.UniqueValuesOriginal = GetUniqueValues(clmn);
                TableColumns.Add(x);
            }
        }

        /// <summary>
        /// Получение типа значений для колонки
        /// </summary>
        /// <param name="typeCode"></param>
        /// <returns></returns>
        private GridColumnTypeEnum GetColumnType(TypeCode typeCode)
        {
            GridColumnTypeEnum myType;
            switch (typeCode)
            {
                case TypeCode.Decimal:
                    myType = GridColumnTypeEnum.Decimal;
                    break;
                case TypeCode.Int32:
                    myType = GridColumnTypeEnum.Int;
                    break;
                case TypeCode.Byte:
                case TypeCode.Boolean:
                    myType = GridColumnTypeEnum.Boolean;
                    break;
                case TypeCode.DateTime:
                    myType = GridColumnTypeEnum.Date;
                    break;
                default:
                    myType = GridColumnTypeEnum.String;
                    break;
            }

            return myType;
        }

        /// <summary>
        /// Получение уникальных значений колонки 
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private Dictionary<object, object> GetUniqueValues(DataColumn column)
        {
            var results = from r in DT.AsEnumerable()
                group r by new {MyValue = r.Field<object>(column.ColumnName)}
                into myGroup
                orderby myGroup.Key.MyValue
                select new {myGroup.Key.MyValue};

            if (results.ToList().Count > maxUniqueValues) return null;

            var inx = 1;
            var dict = results.Where(x => x.MyValue != null && x.MyValue.ToString().Length > 0)
                .ToDictionary(x => (object) inx++, x => x.MyValue);

            if (results.Any(x => x.MyValue == null || x.MyValue != null && x.MyValue.ToString().Length == 0))
                dict.Add(0, "");

            return dict;
        }

        /// <summary>
        /// Установка формата выводимых значений
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="formatString"></param>
        public void SetColumnFormat(string fieldName, string formatString)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.FormatString = formatString;
        }

        /// <summary>
        /// Установка формата колонки в виде HH:MM:SS
        /// </summary>
        /// <param name="fieldName"></param>
        public void SetColumnIsTimeSecond(string fieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.IsTimeSecond = true;
        }

        /// <summary>
        /// Установка условия для вывода суммы в итоговую строку
        /// </summary>
        /// <param name="fieldName"></param>
        public void SetColumnIsSumValues(string fieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.IsSumValues = true;
        }

        /// <summary>
        /// Установка условия для вывода текста в итоговую строку
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="text"></param>
        public void SetColumnSumValuesText(string fieldName, string text)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.SumValuesText = text;
        }

        /// <summary>
        /// Установка алиасов названий колонок
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="alias"></param>
        /// <param name="headerTitle"></param>
        public void SetColumnHeaderAlias(string fieldName, string alias, string headerTitle = "")
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
            {
                clmn.Alias = alias;
                if (!string.IsNullOrEmpty(headerTitle))
                    clmn.HeaderTitle = headerTitle;
            }
        }

        /// <summary>
        /// Установка видимости колонки
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="display"></param>
        public void SetColumnDisplayVisible(string fieldName, bool display)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.DisplayVisible = display;
        }

        /// <summary>
        /// Установка цвета фона
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="color"></param>
        public void SetColumnBackGroundColor(string fieldName, string color)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.BackGroundColor = color;
        }

        /// <summary>
        /// Установка подсказки для названия колонки
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="title"></param>
        public void SetColumnTitle(string fieldName, string title)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.Title = title;
        }

        
    }
}