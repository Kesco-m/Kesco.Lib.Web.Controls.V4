using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Kesco.Lib.DALC;
using Kesco.Lib.Web.Controls.V4.Common;

namespace Kesco.Lib.Web.Controls.V4.TreeView
{
    /// <summary>
    ///     Класс настроек конторола TreeView
    /// </summary>
    public class TreeViewSettings : V4Control
    {
        private readonly List<string> _filteredColumns;

        public DataTable DT;
        public List<DataTable> AddDT;
        public List<TreeViewColumn> TableColumns;
        public List<TreeViewColumn> AddTableColumns;
        public string FilterClause;
        public string FilterClauseOriginal;

        protected TreeViewDbSourceSettings dbSource;

        /// <summary>
        ///     Конструктор
        /// </summary>
        public TreeViewSettings()
        {
        }

        public TreeViewSettings(DataTable dt, string treeViewId, int treeViewCmdListnerIndex, Page page)
        {
            V4Page = page;
            var treeView = (TreeView)V4Page.V4Controls[treeViewId];

            DT = dt;
            TreeViewCmdListnerIndex = treeViewCmdListnerIndex;
            TreeViewId = treeViewId;
            dbSource = treeView.DbSourceSettings;

            if (treeView.ColumnNotNullSearchList == null) treeView.ColumnNotNullSearchList = new List<string>();
            if (treeView.ColumnsDefaultValues == null) treeView.ColumnsDefaultValues = new Dictionary<string, object>();

            FillTableColumns(treeView.ColumnNotNullSearchList, treeView.ColumnsType, treeView.ColumnsDefaultValues);
            _filteredColumns = TableColumns.Select(x => x.FieldName).ToList();

            AddDT = new List<DataTable>();
            if (treeView.DbSourceSettings.AdditionalTable != null)
            {
                foreach (var table in treeView.DbSourceSettings.AdditionalTable)
                {
                    var sql = "";
                    var addField = "";
                    foreach (var field in table.Value)
                    {
                        addField = addField != "" ? addField + "," + field : field;
                    }

                    sql = "SELECT TOP 1 " + addField + " FROM " + table.Key;


                    var dtAdd = DBManager.GetData(sql, dbSource.ConnectionString);
                    AddDT.Add(dtAdd);
                }

                FillAddTableColumns(treeView.ColumnNotNullSearchList, treeView.ColumnsType, treeView.ColumnsDefaultValues);
                _filteredColumns.AddRange(AddTableColumns.Select(x => x.FieldName).ToList());
            }

        }

        public int TreeViewCmdListnerIndex { get; }
        public string TreeViewId { get; }

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
        private void FillTableColumns(List<string> columnNotNullSearchList, Dictionary<string, TypeCode> columnsType = null, Dictionary<string, object> columnsDefaulValues = null)
        {
            TableColumns = new List<TreeViewColumn>();
                        
            if (DT == null) return;
            foreach (DataColumn clmn in DT.Columns)
            {
                var x = new TreeViewColumn(this)
                {
                    Id = Guid.NewGuid().ToString(),
                    Alias = clmn.ColumnName,
                    FieldName = clmn.ColumnName,
                    DisplayVisible = true,
                    IsAllowNull = !columnNotNullSearchList.Contains(clmn.ColumnName),
                    IsFilteredListColumn = dbSource.FieldValuesList != null && dbSource.FieldValuesList.ContainsKey(clmn.ColumnName)
                };

                var typeCode = Type.GetTypeCode(clmn.DataType);
                if (columnsType != null && columnsType.ContainsKey(clmn.ColumnName))
                {
                    typeCode = columnsType[clmn.ColumnName];
                }

                x.ColumnType = GetColumnType(typeCode);

                if (columnsDefaulValues != null && columnsDefaulValues.Count != 0)
                {
                    var dv = columnsDefaulValues.FirstOrDefault(d => d.Key == x.FieldName);

                    if (dv.Key != null)
                    {
                        var filterType = TreeViewColumnUserFilterEnum.Равно;
                        var filterValue = dv.Value;

                        if (x.ColumnType == TreeViewColumnTypeEnum.Boolean)
                        {
                            filterValue = null;
                            filterType = (int) dv.Value == 0
                                ? TreeViewColumnUserFilterEnum.Нет
                                : TreeViewColumnUserFilterEnum.Да;
                        }

                        x.FilterUser = new TreeViewColumnUserFilter
                        {
                            FilterType = filterType,
                            FilterValue1 = filterValue,
                            FilterValue2 = null
                        };
                        x.FilterUserOriginal = x.FilterUser;
                        x.DefaultValue = dv.Value;
                    }
                }

                TableColumns.Add(x);
            }

        }

        private void FillAddTableColumns(List<string> columnNotNullSearchList, Dictionary<string, TypeCode> columnsType = null, Dictionary<string, object> columnsDefaulValues = null)
        {
            AddTableColumns = new List<TreeViewColumn>();

            foreach (var dt in AddDT)
            {
                foreach (DataColumn clmn in dt.Columns)
                {
                    var x = new TreeViewColumn(this)
                    {
                        Id = Guid.NewGuid().ToString(),
                        Alias = clmn.ColumnName,
                        FieldName = clmn.ColumnName,
                        DisplayVisible = true,
                        IsAllowNull = !columnNotNullSearchList.Contains(clmn.ColumnName),
                        IsFilteredListColumn = dbSource.FieldValuesList != null && dbSource.FieldValuesList.ContainsKey(clmn.ColumnName)
                    };

                    var typeCode = Type.GetTypeCode(clmn.DataType);
                    if (columnsType != null && columnsType.ContainsKey(clmn.ColumnName))
                    {
                        typeCode = columnsType[clmn.ColumnName];
                    }

                    x.ColumnType = GetColumnType(typeCode);

                    if (columnsDefaulValues != null && columnsDefaulValues.Count != 0)
                    {
                        var dv = columnsDefaulValues.FirstOrDefault(d => d.Key == x.FieldName);

                        if (dv.Key != null)
                        {
                            var filterType = TreeViewColumnUserFilterEnum.Равно;
                            var filterValue = dv.Value;

                            if (x.ColumnType == TreeViewColumnTypeEnum.Boolean)
                            {
                                filterValue = null;
                                filterType = (int)dv.Value == 0
                                    ? TreeViewColumnUserFilterEnum.Нет
                                    : TreeViewColumnUserFilterEnum.Да;
                            }

                            x.FilterUser = new TreeViewColumnUserFilter
                            {
                                FilterType = filterType,
                                FilterValue1 = filterValue,
                                FilterValue2 = null
                            };
                            x.FilterUserOriginal = x.FilterUser;
                            x.DefaultValue = dv.Value;
                        }
                    }

                    AddTableColumns.Add(x);
                }
            }

        }

        /// <summary>
        ///     Получение типа значений для колонки
        /// </summary>
        /// <param name="typeCode"></param>
        /// <returns></returns>
        private TreeViewColumnTypeEnum GetColumnType(TypeCode typeCode)
        {
            TreeViewColumnTypeEnum myType;
            switch (typeCode)
            {
                case TypeCode.Double:
                    myType = TreeViewColumnTypeEnum.Double;
                    break;
                case TypeCode.Decimal:
                    myType = TreeViewColumnTypeEnum.Decimal;
                    break;
                case TypeCode.Int32:
                    myType = TreeViewColumnTypeEnum.Int;
                    break;
                case TypeCode.Byte:
                case TypeCode.Boolean:
                    myType = TreeViewColumnTypeEnum.Boolean;
                    break;
                case TypeCode.DateTime:
                    myType = TreeViewColumnTypeEnum.Date;
                    break;
                default:
                    myType = TreeViewColumnTypeEnum.String;
                    break;
            }

            return myType;
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
            else
            {
                clmn = AddTableColumns.FirstOrDefault(x => x.FieldName == fieldName);
                if (clmn != null)
                {
                    clmn.Alias = alias;
                    if (!string.IsNullOrEmpty(headerTitle))
                        clmn.HeaderTitle = headerTitle;
                }
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

        #region SetColumnFormat

        /// <summary>
        ///     Сохранить установленные фильтры
        /// </summary>
        public void SaveOriginalFilter()
        {
            foreach (var clmn in TableColumns)
                clmn.FilterUserOriginal = clmn.FilterUser;

            if (AddTableColumns != null)
            foreach (var clmn in AddTableColumns)
                clmn.FilterUserOriginal = clmn.FilterUser;

            FilterClauseOriginal = FilterClause;

        }

        /// <summary>
        ///     Востановить установленные фильтры
        /// </summary>
        public void RestoreOriginalFilter()
        {
            foreach (var clmn in TableColumns)
                clmn.FilterUser = clmn.FilterUserOriginal;

            if (AddTableColumns != null)
            foreach (var clmn in AddTableColumns)
                clmn.FilterUser = clmn.FilterUserOriginal;

            FilterClause = FilterClauseOriginal;

        }

        #endregion


    }
}