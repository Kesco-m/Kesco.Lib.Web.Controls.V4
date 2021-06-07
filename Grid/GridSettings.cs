using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities.Grid;
using Kesco.Lib.Web.Controls.V4.Common;
using Kesco.Lib.Web.Settings;
using Microsoft.AspNet.SignalR.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    /// <summary>
    ///     Класс настроек конторола Grid
    /// </summary>
    public class GridSettings : V4Control
    {
        private readonly int maxUniqueValues = 200;
        private readonly List<string> _sortingColumns;
        private readonly List<string> _filteredColumns;
        public List<GridColumn> ColumnsDisplayOrder;

        public DataTable DT;
        public List<GridColumn> GroupingColumns;
        public List<GridColumn> TableColumns;

        private List<QueryColumn> QueryColumnsList;
                
        public List<string> ColumnDisplayVisibleIfHasValue;

        /// <summary>
        ///     Видимость колонки из запроса по умолчанию
        /// </summary>
        public bool DefaultVisibleValue { get; set; }

        /// <summary>
        ///     Конструктор
        /// </summary>
        public GridSettings()
        {
        }

        public GridSettings(DataTable dt, string gridId, int gridCmdListnerIndex, Page page, bool isFilterUniqueEnable, List<QueryColumn> queryColumnsList)
        {
            DT = dt;
            GridCmdListnerIndex = gridCmdListnerIndex;
            GridId = gridId;
            V4Page = page;
            IsFilterUniqueEnable = isFilterUniqueEnable;
            DefaultVisibleValue = true;
            QueryColumnsList = queryColumnsList;

            FillTableColumns();
            _sortingColumns = TableColumns.Select(x => x.FieldName).ToList();
            _filteredColumns = TableColumns.Select(x => x.FieldName).ToList();
        }

        public int GridCmdListnerIndex { get; }
        public string GridId { get; }
        public bool IsPrintVersion { get; set; }
        public bool IsGroupEnable { get; set; }

        public int GroupingExpandIndex { get; set; }

        public bool IsFilterUniqueEnable { get; set; }

        /// <summary>
        ///     Признак разрешена ли сортировка
        /// </summary>
        public bool IsSortingEnable => _sortingColumns.Count > 0;

        /// <summary>
        ///     Список названий колонок, по которым возможна сортировка
        /// </summary>
        public ReadOnlyCollection<string> SortingColumns => _sortingColumns.AsReadOnly();


        /// <summary>
        ///     Список названий колонок, по которым возможна фильтрация
        /// </summary>
        public ReadOnlyCollection<string> FilteredColumns => _filteredColumns.AsReadOnly();

        /// <summary>
        ///     Признак разрешена ли фильтрация
        /// </summary>
        public bool IsFilterEnable { get; set; }


        public new Page V4Page
        {
            get { return Page as Page; }
            set { Page = value; }
        }

        /// <summary>
        ///     Формирование колонок из DataTable
        /// </summary>
        private void FillTableColumns()
        {
            TableColumns = new List<GridColumn>();

            var inx = 0;
            if (DT == null) return;
            foreach (DataColumn clmn in DT.Columns)
            {
                inx++;
                var x = new GridColumn(this)
                {
                    Id = Guid.NewGuid().ToString(),
                    FieldName = clmn.ColumnName,
                    DisplayVisible = DefaultVisibleValue,
                    DisplayVisibleDefault = DefaultVisibleValue
                };

                if (Regex.IsMatch(clmn.ColumnName, "@\\d+$"))
                    x.IsDublicateColumn = true;
                else
                    x.IsDublicateColumn = false;

                var colQueryTable = new QueryColumn();

                int columnId = 0;
                foreach (DictionaryEntry item in clmn.ExtendedProperties)
                {
                    if (item.Key.ToString() == "ColumnId")
                    {
                        columnId = (int)item.Value;
                    }
                }

                if (QueryColumnsList != null) colQueryTable = QueryColumnsList.Find(c => c.ColumnId == columnId);

                var listTypeFieldList = new List<KeyValuePair<int, string>>();
                if (QueryColumnsList == null || colQueryTable == null)
                {

                    x.Alias = clmn.ColumnName;
                    x.DisplayOrder = inx;
                    x.DisplayOrderDefault = inx;
                    
                    var typeCode = Type.GetTypeCode(clmn.DataType);
                    x.ColumnType = GetColumnType(typeCode);
                }
                else
                {
                    x.Alias = colQueryTable.ColumnHeader;
                    if (colQueryTable.Order != null)
                    {
                        x.DisplayOrder = (int) colQueryTable.Order;
                        x.DisplayOrderDefault = (int) colQueryTable.Order;
                    }
                    else
                    {
                        x.DisplayVisible = false;
                        x.DisplayVisibleDefault = false;

                        x.DisplayOrder = 0;
                        x.DisplayOrderDefault = 0;
                    }

                    char act = '0'; //Определяет операцию в SQL выражении
                    object paramValue = null;

                    if (!string.IsNullOrEmpty(colQueryTable.FilterValue))
                    {
                        if (colQueryTable.Type == 1 || colQueryTable.Type == 3 || colQueryTable.Type == 10 || colQueryTable.Type == 7)
                        {
                            if (colQueryTable.Type == 7)
                            {
                                act = '1';
                                paramValue = colQueryTable.FilterValue;
                            }
                            else
                            {
                                
                                if (colQueryTable.Type == 1 && colQueryTable.FilterValue == "EX") act = '6';
                                else if (colQueryTable.Type == 1 && colQueryTable.FilterValue == "NEX") act = '7';
                                else
                                {
                                    var valkey = colQueryTable.FilterValue.ToUpper();
                                    if (valkey.IndexOf(";") != -1)
                                    {
                                        if (valkey[0] == ';')
                                        {
                                            if (valkey[valkey.Length - 1] == 'E')
                                                valkey = valkey.Replace(";", "4");
                                            else
                                                valkey = valkey.Replace(";", "5");
                                        }
                                        else if (valkey[valkey.Length - 1] == ';')
                                        {
                                            if (valkey[valkey.Length - 2] == 'E')
                                                valkey = "2" + valkey.Replace("E;", "");
                                            else
                                                valkey = "3" + valkey.Replace(";", "");
                                        }
                                        else
                                        {
                                            if (valkey[valkey.Length - 1] == 'E')
                                                valkey = valkey.Replace(";", ";4");
                                            else
                                                valkey = valkey.Replace(";", ";5");

                                            int ind = valkey.IndexOf(";");

                                            if (valkey[ind - 1] == 'E')
                                                valkey = "2" + valkey;
                                            else
                                                valkey = "3" + valkey;
                                        }
                                    }
                                    else
                                    {
                                        if (valkey.IndexOf("E") > 0)
                                            valkey = "1" + valkey;
                                        else if (valkey.IndexOf("N") > 0)
                                        {
                                            switch (colQueryTable.Type)
                                            {
                                                case 1: //число
                                                    valkey = "8" + valkey;
                                                    break;
                                                case 3: //дата
                                                case 10: //дата локализованная
                                                    valkey = "D" + valkey;
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            if (valkey.Equals("-1"))
                                                valkey = "6" + valkey;
                                            else if (valkey.Equals("-2"))
                                                valkey = "Y" + valkey;
                                            else
                                                valkey = "1" + valkey;
                                        }

                                    }

                                    valkey = valkey.Replace("E", "");
                                    valkey = valkey.Replace("N", "");

                                    act = valkey[0];
                                    paramValue = valkey.Substring(1, valkey.Length - 1);

                                }
                                
                            }
                        }
                        else
                        {
                            act = colQueryTable.FilterValue[0]; //первый символ параметра определяет действие, далее сам параметр
                            paramValue = colQueryTable.FilterValue.Substring(1, colQueryTable.FilterValue.Length - 1);
                        }
                    }

                    switch (colQueryTable.Type)
                    {
                        case 1: // число
                            x.ColumnType = GetColumnType(Type.GetTypeCode(clmn.DataType));
                            x.TextAlign = "right";
                            if (paramValue != null)
                            {
                                try
                                {
                                    paramValue = int.Parse(paramValue.ToString());
                                }
                                catch
                                {
                                    paramValue = ConvertExtention.Convert.Str2Decimal(paramValue.ToString());
                                }

                                var filterType = GetAction(act, colQueryTable.Type);

                                x.FilterUser = new GridColumnUserFilter
                                {
                                    FilterType = filterType,
                                    FilterValue1 = paramValue,
                                    FilterValue2 = null
                                };
                                x.FilterRequired = colQueryTable.FilterRequired;
                            }
                            break;
                        case 3: // дата
                            x.ColumnType = GridColumnTypeEnum.Date;
                            x.FormatString = colQueryTable.Format;
                            x.IsNoWrap = true;
                            if (paramValue != null)
                            {
                                var filterType = GetAction(act, colQueryTable.Type);
                                x.FilterUser = new GridColumnUserFilterDate
                                {
                                    IsCurrentDate = false,
                                    FilterType = filterType,
                                    FilterValue1 = Convert.ToDateTime(paramValue.ToString()),
                                    FilterValue2 = null
                                };
                                x.FilterRequired = colQueryTable.FilterRequired;
                            }
                            break;
                        case 6: // булевое (условие)
                        {
                            x.ColumnType = GridColumnTypeEnum.Boolean;
                            x.IsCondition = true;
                            x.IsBit = true;
                            x.FormatString = colQueryTable.Format;
                            x.ValueBooleanCaption = new Dictionary<string, string> {{"0", "Нет"}, {"1", "Да"}};
                            x.TextAlign = "center";
                            if (paramValue != null)
                            {
                                if (paramValue.ToString().ToLower() == "0" ||
                                    paramValue.ToString().ToLower() == "false")
                                    x.FilterUniqueValues = new Dictionary<object, object> { { 1, 0 } };
                                else
                                    x.FilterUniqueValues = new Dictionary<object, object> { { 2, 1 } };
                                x.FilterEqual = GridColumnFilterEqualEnum.In;
                                x.FilterRequired = colQueryTable.FilterRequired;
                            }
                        }
                            break;
                        case 7: // булевое
                        {
                            x.ColumnType = GridColumnTypeEnum.Boolean;
                            x.IsCondition = false;
                            x.IsBit = true;
                            x.FormatString = colQueryTable.Format;
                            x.ValueBooleanCaption = new Dictionary<string, string> {{"0", "Нет"}, {"1", "Да"}};
                            x.TextAlign = "center";

                            if (paramValue != null)
                            {
                                if (paramValue.ToString().ToLower() == "0" ||
                                    paramValue.ToString().ToLower() == "false")
                                    x.FilterUniqueValues = new Dictionary<object, object> { { 1, 0 } };
                                else
                                    x.FilterUniqueValues = new Dictionary<object, object> { { 2, 1 } };
                                x.FilterEqual = GridColumnFilterEqualEnum.In;
                                x.FilterRequired = colQueryTable.FilterRequired;
                            }

                        }
                            break;
                        case 10: // дата + время (локальная дата)
                            x.ColumnType = GridColumnTypeEnum.Date;
                            x.FormatString = "dd.MM.yyyy hh:mm";
                            x.IsNoWrap = true;
                            x.IsLocalTime = true;
                            if (paramValue != null)
                            {
                                var filterType = GetAction(act, colQueryTable.Type);
                                x.FilterUser = new GridColumnUserFilter
                                {
                                    FilterType = filterType,
                                    FilterValue1 = Convert.ToDateTime(paramValue.ToString()),
                                    FilterValue2 = null
                                };
                                x.FilterRequired = colQueryTable.FilterRequired;
                            }
                            break;
                        case 5: // список
                            x.ColumnType = GridColumnTypeEnum.List;
                            var dt = DBManager.GetData(colQueryTable.Format, Config.DS_user, CommandType.Text, new Dictionary<string, object>());

                            if (dt.Rows.Count > 0)
                            {
                                foreach (DataRow row in dt.Rows)
                                {
                                    listTypeFieldList.Add(new KeyValuePair<int, string>(Convert.ToInt32(row[0].ToString()), row[1].ToString()));
                                }
                                var sw = new StringWriter();
                                var js = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
                                js.Serialize(listTypeFieldList, sw);
                                x.FormatString = sw.ToString();
                                x.ValueBooleanCaption = listTypeFieldList.ToDictionary(v => v.Key.ToString(), v => v.Value);
                                if (paramValue != null)
                                {
                                    x.FilterUniqueValues = new Dictionary<object, object>
                                    {
                                        {colQueryTable.FilterValue, colQueryTable.FilterValue}
                                    };
                                    x.FilterEqual = GridColumnFilterEqualEnum.In;
                                    x.FilterRequired = colQueryTable.FilterRequired;
                                }
                            }
                            break;
                        default:
                            x.ColumnType = GetColumnType(Type.GetTypeCode(clmn.DataType));
                            if (paramValue != null)
                            {
                                var filterType = GetAction(act, colQueryTable.Type);
                                x.FilterUser = new GridColumnUserFilter
                                {
                                    FilterType = filterType,
                                    FilterValue1 = Convert.ToString(paramValue.ToString()),
                                    FilterValue2 = null
                                };
                                x.FilterRequired = colQueryTable.FilterRequired;
                            }
                            break;
                    }
                }

                if (x.ColumnType == GridColumnTypeEnum.List)
                {
                    x.UniqueValuesOriginal = GetUniqueValues(clmn, listTypeFieldList);
                }
                else if (IsFilterUniqueEnable || x.ColumnType == GridColumnTypeEnum.Boolean)
                {
                    x.UniqueValuesOriginal = GetUniqueValues(clmn, x.ColumnType);
                }

                TableColumns.Add(x);
            }

            if (ColumnsDisplayOrder != null && ColumnsDisplayOrder.Count > 0)
                TableColumns.Where(x => ColumnsDisplayOrder.Any(y => y.FieldName == x.FieldName)).ToList().ForEach(
                    delegate(GridColumn clmn)
                    {
                        var _clmn = ColumnsDisplayOrder.FirstOrDefault(y => y.FieldName == clmn.FieldName);
                        if (_clmn != null)
                        {
                            clmn.DisplayOrder = _clmn.DisplayOrder;
                            clmn.DisplayOrderDefault = clmn.DisplayOrder;
                            if (inx < clmn.DisplayOrder) inx = clmn.DisplayOrder;
                        }
                    });

            TableColumns.Where(x => x.DisplayOrder == 0).ToList().ForEach(delegate(GridColumn clmn)
            {
                clmn.DisplayOrder = inx++;
                clmn.DisplayOrderDefault = clmn.DisplayOrder;
            });
        }

        /// <summary>
        ///     Получение типа значений для колонки
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
                case TypeCode.Int64:
                    myType = GridColumnTypeEnum.Long;
                    break;
                case TypeCode.Int16:
                    myType = GridColumnTypeEnum.Short;
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
        ///     Получение уникальных значений колонки
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public Dictionary<object, object> GetUniqueValues(DataColumn column, GridColumnTypeEnum columnType)
        {
            if (Type.GetTypeCode(column.DataType) == TypeCode.Byte || Type.GetTypeCode(column.DataType) == TypeCode.Boolean)
            {
                return new Dictionary<object, object> { { 1, (byte)0 }, { 2, (byte)1 } };
            }

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

        public Dictionary<object, object> GetUniqueValues(DataColumn column, List<KeyValuePair<int, string>> keyValuePair)
        {
            return keyValuePair.ToDictionary(v => (object)v.Key, v => (object)(byte)v.Key);
        }

        /// <summary>
        ///     Установка формата колонки в виде HH:MM:SS
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
        ///     Установка условия для вывода суммы в итоговую строку
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
        ///     Установка условия для вывода текста в итоговую строку
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
        ///     Установка алиасов названий колонок
        /// </summary>
        /// <param name="fields">Словарь названий колонок с алиасами</param>
        public void SetColumnHeaderAlias(Dictionary<string, string> fields)
        {
            foreach (var field in fields) SetColumnHeaderAlias(field.Key, field.Value);
        }

        /// <summary>
        ///     Установка алиасов названий колонок
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

        public void SetColumnRender(string fieldName, GridColumn.RenderColumnDelegate action)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.renderColumnDelegate = action;
        }

        /// <summary>
        ///     Установка видимости колонок
        /// </summary>
        /// <param name="fields">Список названий колонок</param>
        /// <param name="display">Видимость</param>
        public void SetColumnDisplayVisible(List<string> fields, bool display)
        {
            fields.ForEach(delegate(string fieldName) { SetColumnDisplayVisible(fieldName, display); });
        }

        /// <summary>
        ///     Установка видимости колонки
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="display">Видимость</param>
        public void SetColumnDisplayVisible(string fieldName, bool display)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
            {
                clmn.DisplayVisible = display;
                clmn.DisplayVisibleDefault = display;
            }
        }

        /// <summary>
        ///     Установка порядкового номера колонки и номера по умполчанию
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="ndx">Порядковый номер</param>
        public void SetColumnOrder(string fieldName, int ndx)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
            {
                clmn.DisplayOrder = ndx;
                clmn.DisplayOrderDefault = ndx;
            }
        }

        /// <summary>
        ///     Установка сортировки по значениям null
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="firstNull">Значения null первые</param>
        public void SetColumnOrderNullValue(string fieldName, bool firstNull)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.SortNullFirst = firstNull;
        }

        /// <summary>
        ///     Установка цвета фона
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
        ///     Установка подсказки для названия колонки
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
        ///     Делает текст в ячейке не переносимым
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        public void SetColumnNoWrapText(string fieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.IsNoWrap = true;
        }

        /// <summary>
        ///     Обновление уникальных значений
        /// </summary>
        public void ReloadUniqueValues()
        {
            foreach (DataColumn clmn in DT.Columns)
            {
                var tc = TableColumns.Find(cl => cl.Alias == clmn.ColumnName);
                if (tc != null)
                {
                    //tc.UniqueValuesOriginal = GetUniqueValues(clmn);
                }
            }
        }
        #region SetColumnHref

        /// <summary>
        ///     Установка параметров для генерации ссылки на сущность по данным текущей записи
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="hrefIdFieldName">Название колонки, где брать идентификатор сущности</param>
        /// <param name="uri">Ссылка на форму редактирования сущности</param>
        public void SetColumnHref(string fieldName, string hrefIdFieldName, string uri, bool hrefUriFieldName = false)
        {
            SetColumnHref(fieldName, "id", hrefIdFieldName, uri, hrefUriFieldName);
        }

        /// <summary>
        ///     Установка параметров для генерации ссылки на сущность по данным текущей записи
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="hrefId">Название параметра идентификатора</param>
        /// <param name="hrefIdFieldName">Название колонки, где брать идентификатор сущности</param>
        /// <param name="uri">Ссылка на форму редактирования сущности</param>
        public void SetColumnHref(string fieldName, string hrefId, string hrefIdFieldName, string uri, bool hrefUriFieldName = false)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
            {
                clmn.HrefId = hrefId;
                clmn.HrefIdFieldName = hrefIdFieldName;
                clmn.HrefUri = uri;
                clmn.HrefUriFieldName = hrefUriFieldName;
            }
        }

        /// <summary>
        ///     Установка параметров для генерации ссылки на документ по данным текущей записи
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="hrefIdFieldName">Название колонки, где брать идентификатор сущности</param>
        public void SetColumnHrefDocument(string fieldName, string hrefIdFieldName, bool isGetDocumentNameByEachRow = false)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn == null) return;
            clmn.HrefIdFieldName = hrefIdFieldName;
            clmn.HrefIsDocument = true;
            clmn.IsGetDocumentNameByEachRow = isGetDocumentNameByEachRow;
        }

        /// <summary>
        ///     Установка параметров для генерации ссылки на сотрудника по данным текущей записи
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
        ///     Установка параметров для генерации ссылки на лицо по данным текущей записи
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="hrefIdFieldName">Название колонки, где брать идентификатор сущности</param>
        public void SetColumnHrefPerson(string fieldName, string hrefIdFieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn == null) return;
            clmn.HrefIdFieldName = hrefIdFieldName;
            clmn.HrefIsPerson = true;
        }

        /// <summary>
        ///     Установка параметров для генерации ссылки на оборудование по данным текущей записи
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="hrefIdFieldName">Название колонки, где брать идентификатор сущности</param>
        public void SetColumnHrefEquipment(string fieldName, string hrefIdFieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn == null) return;
            clmn.HrefIdFieldName = hrefIdFieldName;
            clmn.HrefIsEquipment = true;
        }

        /// <summary>
        ///     Установка параметров для сортировки
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="sortFieldName">Название колонки, по которой осуществлять сортировку</param>
        public void SetColumnSortName(string fieldName, string sortFieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn == null) return;
            var sortclmn = TableColumns.FirstOrDefault(x => x.FieldName == sortFieldName);
            if (sortclmn == null) return;

            clmn.SortFieldName = sortFieldName;
        }

        /// <summary>
        ///     Установка параметров для фильтрации
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="filterFieldName">Название колонки, по которой осуществлять фильтрацию</param>
        public void SetColumnFilterName(string fieldName, string filterFieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn == null) return;
            var sortclmn = TableColumns.FirstOrDefault(x => x.FieldName == filterFieldName);
            if (sortclmn == null) return;

            clmn.FilterFieldName = filterFieldName;
        }


        /// <summary>
        ///     Установка параметров для вывода булевских значений фильтрации
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="gridColumnValueBoolean">Значения</param>
        public void SetColumnFilterValueBoolean(string fieldName, Dictionary<string, string> gridColumnValueBoolean)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn == null) return;
            clmn.ValueBooleanCaption = gridColumnValueBoolean;
        }

        /// <summary>
        ///     Установка параметров для вывода булевских значений фильтрации
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="messageConfirm">Сообщение при клике</param>
        /// <param name="clientFuncName">Клиентская функция, которая будет вызываться при нажатии на иконку удаления</param>
        /// <param name="pkFieldsName">Параметры клиентской функции</param>
        /// <param name="title">Всплывающая подсказка</param>
        public void SetColumnClick(string fieldName, string messageConfirm, string clientFuncName, List<string> pkFieldsName, List<string> messageFieldsName, string title = "")
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn == null) return;
            clmn.ClickMessageConfirm = messageConfirm;
            clmn.ClickClientFuncName = clientFuncName;
            clmn.ClickPkFieldsName = pkFieldsName;
            clmn.ClickMessageFieldsName = messageFieldsName;
            clmn.ClickTitle = title;
        }

        /// <summary>
        ///     Установка параметров для генерации ссылки на форму сушности по условиям
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

        /// <summary>
        ///     Установить тип данных колонки
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        /// <param name="type">Значение из перечисления возможных типов колонки</param>
        public void SetColumnDataType(string fieldName, GridColumnTypeEnum type)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.ColumnType = type;
        }

        #endregion


        #region SetColumnTextAlign

        /// <summary>
        ///     Установить выравнивание текста в колонках
        /// </summary>
        /// <param name="fields">Список колонок</param>
        /// <param name="align">Как выравниваем</param>
        public void SetColumnTextAlign(List<string> fields, string align)
        {
            fields.ForEach(delegate(string fieldName) { SetColumnTextAlign(fieldName, align); });
        }

        /// <summary>
        ///     Установить выравнивание текста в колонке
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
        ///     Выводит иконку Ok.gif, если значение поля = 0
        /// </summary>
        /// <param name="fields">Список название колонки</param>
        public void SetColumnBitFormat(List<string> fields)
        {
            fields.ForEach(SetColumnBitFormat);
        }

        /// <summary>
        ///     Выводит иконку Ok.gif, если значение поля = 0
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        public void SetColumnBitFormat(string fieldName)
        {
            if (TableColumns == null) return;
            var clmn = TableColumns.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.IsBit = true;
        }

        /// <summary>
        ///     Устанавливает колонкам свойство сохранения состояния
        /// </summary>
        /// <param name="fields">Список названий колонок</param>
        public void SetColumnSaveSettings(List<string> fields, bool state)
        {
            foreach (var item in fields)
            {
                SetColumnSaveSettings(item, state);
            }
        }

        /// <summary>
        ///     Устанавливает колонке свойство сохранения состояния
        /// </summary>
        /// <param name="fieldName">Название колонки</param>
        public void SetColumnSaveSettings(string fieldName, bool state)
        {
            var clmn = TableColumns?.FirstOrDefault(x => x.FieldName == fieldName);
            if (clmn != null)
                clmn.IsSaveSettings = state;
        }

        #endregion


        #region SetColumnFormat

        /// <summary>
        ///     Установка формата выводимых значений c указанной в другой колонке точночтью
        /// </summary>
        /// <param name="fieldName">Поле, которое надо выводить с указанной точностью</param>
        /// <param name="scaleFieldName">Поле откуда брать значение точности</param>
        /// ///
        /// <param name="defaultScale">Значение точности по-умолчанию</param>
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
        ///     Установка формата выводимых значений c точностью сохраненного значения
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
        ///     Установка точности по-умолчанию для переданных  колонок
        /// </summary>
        /// <param name="fields">Словарь названий колонок с указанной точностью</param>
        public void SetColumnFormatDefaultScale(Dictionary<string, int> fields)
        {
            foreach (var field in fields)
                SetColumnFormatDefaultScale(field.Key, field.Value);
        }

        /// <summary>
        ///     Установка точности по-умолчанию указанной колонке
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
        ///     Установка формата выводимых значений
        /// </summary>
        /// <param name="fields">Список названий колонок</param>
        /// <param name="formatString">Строка формата</param>
        public void SetColumnFormat(List<string> fields, string formatString)
        {
            fields.ForEach(delegate(string fieldName) { SetColumnFormat(fieldName, formatString); });
        }

        /// <summary>
        ///     Установка формата выводимых значений
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

        //gridSettingsSortList = JsonConvert.DeserializeObject<List<GridSettingsSort>>(appParam.Value);

            
        /// <summary>
        ///     Установить преобразование времени UTC в локальное время
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
        ///     Установить список названий колонок, по которым возможна сортировка
        /// </summary>
        /// <param name="fields">Список названий колонок</param>
        public void SetSortingColumns(params string[] fields)
        {
            _sortingColumns.Clear();
            _sortingColumns.AddRange(fields);
        }

        /// <summary>
        ///     Установить список названий колонок, по которым возможна фильтрация
        /// </summary>
        /// <param name="fields">Список названий колонок</param>
        public void SetFilteredColumns(params string[] fields)
        {
            _filteredColumns.Clear();
            _filteredColumns.AddRange(fields);
        }

        /// <summary>
        /// Чтение параметров, совместимых с V3
        /// </summary>
        /// <param name="code"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private GridColumnUserFilterEnum GetAction(char code, int type)
        {
            switch (type)
            {
                case 1: // число
                {
                    switch (code)
                    {
                        case '0':
                            return GridColumnUserFilterEnum.Равно;
                        case '1':
                            return GridColumnUserFilterEnum.Равно;
                        case '8':
                            return GridColumnUserFilterEnum.НеРавно;
                        case '2':
                            return GridColumnUserFilterEnum.Больше;
                        case '3':
                            return GridColumnUserFilterEnum.БольшеИлиРавно;
                        case '4':
                            return GridColumnUserFilterEnum.Меньше;
                        case '5':
                            return GridColumnUserFilterEnum.МеньшеИлиРавно;
                        case '6':
                            return GridColumnUserFilterEnum.Указано;
                        case '7':
                            return GridColumnUserFilterEnum.НеУказано;
                    }
                }
                    break;
                case 3: // дата
                case 10: // дата локализованная
                    switch (code)
                    {
                        case '0':
                            return GridColumnUserFilterEnum.Равно;
                        case '1':
                            return GridColumnUserFilterEnum.Равно;
                        case '2':
                            return GridColumnUserFilterEnum.Больше;
                        case '3':
                            return GridColumnUserFilterEnum.БольшеИлиРавно;
                        case '4':
                            return GridColumnUserFilterEnum.Меньше;
                        case '5':
                            return GridColumnUserFilterEnum.МеньшеИлиРавно;
                        case '6':
                            return GridColumnUserFilterEnum.НеУказано;
                        case '7':
                            return GridColumnUserFilterEnum.Равно;
                        case '8':
                            return GridColumnUserFilterEnum.Равно;
                        case '9':
                            return GridColumnUserFilterEnum.Больше;
                        case 'A':
                            return GridColumnUserFilterEnum.БольшеИлиРавно;
                        case 'B':
                            return GridColumnUserFilterEnum.Меньше;
                        case 'C':
                            return GridColumnUserFilterEnum.МеньшеИлиРавно;
                        case 'D':
                            return GridColumnUserFilterEnum.НеРавно;
                        case 'Y':
                            return GridColumnUserFilterEnum.Указано;
                    }
                    break;
                case 2: // строка
                case 8: // html строка
                default:
                    switch (code)
                    {
                        case '0':
                            return GridColumnUserFilterEnum.Содержит;
                        case '1':
                            return GridColumnUserFilterEnum.НачинаетсяС;
                        case '2':
                            return GridColumnUserFilterEnum.Равно;
                        case '3':
                            return GridColumnUserFilterEnum.НеСодержит;
                        case '4':
                            return GridColumnUserFilterEnum.НеНачинаетсяС;
                        case '5':
                            return GridColumnUserFilterEnum.НеРавно;
                        case '6':
                            return GridColumnUserFilterEnum.Содержит;
                        case '7':
                            return GridColumnUserFilterEnum.НеСодержит;
                        case '8':
                            return GridColumnUserFilterEnum.ЗаканчиваетсяНа;
                        case '9':
                            return GridColumnUserFilterEnum.НеЗаканчиваетсяНа;
                        case 'A':
                            return GridColumnUserFilterEnum.НеУказано;
                        case 'B':
                            return GridColumnUserFilterEnum.Указано;
                    }

                    break;
            }
            return GridColumnUserFilterEnum.Равно;
        }

        #endregion
    }
}