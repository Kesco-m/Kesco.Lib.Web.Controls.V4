using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private List<string> _sortingColumns;

        public int GridCmdListnerIndex { get; private set; }
        public string GridId { get; private set; }
        public bool IsPrintVersion { get; set; }
        public bool IsGroupEnable { get; set; }
        public int GroupingExpandIndex { get; set; }

        /// <summary>
        /// Признак разрешена ли сортировка
        /// </summary>
        public bool IsSortingEnable
        {
            get { return _sortingColumns.Count > 0; }
        }

        /// <summary>
        /// Список названий колонок, по которым возможна сортировка
        /// </summary>
        public ReadOnlyCollection<string> SortingColumns
        {
            get { return _sortingColumns.AsReadOnly(); }
        }

        /// <summary>
        /// Признак разрешена ли фильтрация
        /// </summary>
        public bool IsFilterEnable { get; set; }

        public DataTable DT;
        public List<GridColumn> TableColumns;
        public List<GridColumn> GroupingColumns;
        public List<GridColumn> ColumnsDisplayOrder;


        public new Page V4Page
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
            _sortingColumns = TableColumns.Select(x => x.FieldName).ToList();
        }
        
        /// <summary>
        /// Формирование колонок из DataTable
        /// </summary>
        private void FillTableColumns()
        {
            TableColumns = new List<GridColumn>();
            
            var inx = 0;
            if (DT == null) return;
            foreach (DataColumn clmn in DT.Columns)
            {
                var x = new GridColumn(this)
                {
                    Id = Guid.NewGuid().ToString(),
                    Alias = clmn.ColumnName,
                    FieldName = clmn.ColumnName,
                    DisplayVisible = true,
                    DisplayOrder = inx
                };
                var typeCode = Type.GetTypeCode(clmn.DataType);
                x.ColumnType = GetColumnType(typeCode);
                x.UniqueValuesOriginal = GetUniqueValues(clmn);
                TableColumns.Add(x);
            }

            if (ColumnsDisplayOrder != null && ColumnsDisplayOrder.Count > 0)
            {
                TableColumns.Where(x => ColumnsDisplayOrder.Any(y => y.FieldName == x.FieldName)).ToList().ForEach(
                    delegate(GridColumn clmn)
                    {
                        var _clmn = ColumnsDisplayOrder.FirstOrDefault(y => y.FieldName == clmn.FieldName);
                        if (_clmn != null)
                        {
                            clmn.DisplayOrder = _clmn.DisplayOrder;
                            if (inx < clmn.DisplayOrder) inx = clmn.DisplayOrder;
                        }
                    });
            }

            TableColumns.Where(x => x.DisplayOrder==0).ToList().ForEach(delegate(GridColumn clmn)
            {
                clmn.DisplayOrder = inx++;
            });

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
                case TypeCode.Double:
                    myType = GridColumnTypeEnum.Double;
                    break;
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
        /// <param name="fieldName">Название колонки</param>
        /// <param name="text">Текст</param>
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
        /// <param name="fields">Словарь названий колонок с алиасами</param>
        public void SetColumnHeaderAlias(Dictionary<string, string> fields)
        {
            foreach (KeyValuePair<string, string> field in fields)
            {
                SetColumnHeaderAlias(field.Key, field.Value);
            }
        }

        /// <summary>
        /// Установка алиасов названий колонок
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="alias">Псевдоним</param>
        /// <param name="headerTitle">Всплывающая подсказка</param>
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
        /// Установка видимости колонок
        /// </summary>
        /// <param name="fields">Список названий колонок</param>
        /// <param name="display">Видимость</param>
        public void SetColumnDisplayVisible(List<string> fields, bool display)
        {
            fields.ForEach(delegate(string fieldName)
            {
                SetColumnDisplayVisible(fieldName, display);
            });
        }

        /// <summary>
        /// Установка видимости колонки
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="display">Видимость</param>
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
        /// <param name="fieldName">Название колонки</param>
        /// <param name="color">Цвет фона</param>
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
        /// <param name="fieldName">Название колонки</param>
        /// <param name="title">Всплывающая подсказка</param>
        public void SetColumnTitle(string fieldName, string title)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.Title = title;
        }

        /// <summary>
        /// Делает текст в ячейке не переносимым
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        public void SetColumnNoWrapText(string fieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.IsNoWrap = true;
        }
        
        #region SetColumnHref

        /// <summary>
        /// Установка параметров для генерации ссылки на сущность по данным текущей записи
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="hrefIdFieldName">Название колонки, где брать идентификатор сущности</param>
        /// <param name="uri">Ссылка на форму редактирования сущности</param>
        public void SetColumnHref(string fieldName, string hrefIdFieldName, string uri)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
            {
                clmn.HrefIdFieldName = hrefIdFieldName;
                clmn.HrefUri = uri;
            }
        }

        /// <summary>
        /// Установка параметров для генерации ссылки на документ по данным текущей записи
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="hrefIdFieldName">Название колонки, где брать идентификатор сущности</param>
        public void SetColumnHrefDocument(string fieldName, string hrefIdFieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn == null) return;
            clmn.HrefIdFieldName = hrefIdFieldName;
            clmn.HrefIsDocument = true;
        }
       
        /// <summary>
        /// Установка параметров для генерации ссылки на сотрудника по данным текущей записи
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="hrefIdFieldName">Название колонки, где брать идентификатор сущности</param>
        public void SetColumnHrefEmployee(string fieldName, string hrefIdFieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn == null) return;
            clmn.HrefIdFieldName = hrefIdFieldName;
            clmn.HrefIsEmployee = true;
        }

        /// <summary>
        /// Установка параметров для генерации ссылки на форму сушности по условиям
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="clauses">Словарь с условиями key = FieldWithId, Value={FieldClause, Href}</param>
        public void SetColumnHrefByClause(string fieldName, Dictionary<string, Dictionary<string, string>> clauses)
        {
            
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn == null) return;
            clmn.HrefIsClause = true;
            clmn.HrefClauses = clauses;
        }

        #endregion

        
        #region SetColumnTextAlign

        /// <summary>
        /// Установить выравнивание текста в колонках
        /// </summary>
        /// <param name="fields">Список колонок</param>
        /// <param name="align">Как выравниваем</param>
        public void SetColumnTextAlign(List<string> fields, string align)
        {
            fields.ForEach(delegate(string fieldName)
            {
                SetColumnTextAlign(fieldName, align);
            });
        }

        /// <summary>
        /// Установить выравнивание текста в колонке
        /// </summary>
        /// <param name="fieldName">Список колонок</param>
        /// <param name="align">Как выравниваем</param>
        public void SetColumnTextAlign(string fieldName, string align)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.TextAlign = align;
        }
      
        #endregion

       
        #region SetColumnBitFormat

        /// <summary>
        /// Выводит иконку Ok.gif, если значение поля = 0
        /// </summary>
        /// <param name="fields">Список название колонки</param>
        public void SetColumnBitFormat(List<string> fields)
        {
            fields.ForEach(SetColumnBitFormat);
        }

        /// <summary>
        /// Выводит иконку Ok.gif, если значение поля = 0
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        public void SetColumnBitFormat(string fieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.IsBit = true;
        }

        #endregion


        #region SetColumnFormat

        /// <summary>
        /// Установка формата выводимых значений c указанной в другой колонке точночтью
        /// </summary>
        /// <param name="fieldName">Поле, которое надо выводить с указанной точностью</param>
        /// <param name="scaleFieldName">Поле откуда брать значение точности</param>
        /// /// <param name="defaultScale">Значение точности по-умолчанию</param>
        public void SetColumnFormatByColumnScale(string fieldName, string scaleFieldName, int defaultScale)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
            {
                clmn.ScaleFieldName = scaleFieldName;
                clmn.DefaultScale = defaultScale;
            }
        }

        /// <summary>
        /// Установка формата выводимых значений c точностью сохраненного значения
        /// </summary>
        /// <param name="fieldName">Поле, которое надо выводить с указанной точностью</param>
        /// <param name="maxScale">Максимальное количество знаков после запятой</param>
        /// <param name="defaultScale">Количество знаков после запятой по-умолчанию</param>
        public void SetColumnFormatByValueScale(string fieldName, int maxScale, int defaultScale)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
            {
                clmn.IsScaleByValue = true;
                clmn.MaxScale = maxScale;
                clmn.DefaultScale = defaultScale;
            }
        }

        /// <summary>
        /// Установка точности по-умолчанию для переданных  колонок
        /// </summary>
        /// <param name="fields">Словарь названий колонок с указанной точностью</param>
        public void SetColumnFormatDefaultScale(Dictionary<string, int> fields)
        {
            foreach (KeyValuePair<string, int> field in fields)
                SetColumnFormatDefaultScale(field.Key, field.Value);

        }

        /// <summary>
        /// Установка точности по-умолчанию указанной колонке
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="defaultScale">Точность</param>
        public void SetColumnFormatDefaultScale(string fieldName, int defaultScale)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.DefaultScale = defaultScale;
        }

        /// <summary>
        /// Установка формата выводимых значений
        /// </summary>
        /// <param name="fields">Список названий колонок</param>
        /// <param name="formatString">Строка формата</param>
        public void SetColumnFormat(List<string> fields, string formatString)
        {
            fields.ForEach(delegate(string fieldName)
            {
                SetColumnFormat(fieldName, formatString);
            });
        }

        /// <summary>
        /// Установка формата выводимых значений
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="formatString">Строка формата</param>
        public void SetColumnFormat(string fieldName, string formatString)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.FormatString = formatString;
        }

        /// <summary>
        /// Установить преобразование времени UTC в локальное время
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        public void SetColumnLocalTime(string fieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.IsLocalTime = true;
        }

        /// <summary>
        /// Установить список названий колонок, по которым возможна сортировка
        /// </summary>
        /// <param name="fields">Список названий колонок</param>
        public void SetSortingColumns(params string[] fields)
        {
            _sortingColumns.Clear();
            _sortingColumns.AddRange(fields);
        }

        #endregion

    }
}