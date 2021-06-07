using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.BaseExtention.Enums.Docs;
using Kesco.Lib.ConvertExtention;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities.Grid;
using Kesco.Lib.Web.Controls.V4.Common;
using Kesco.Lib.Web.Settings;
using Kesco.Lib.Web.Settings.Parameters;
using Kesco.Lib.Web.SignalR;
using Microsoft.AspNet.SignalR.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using Convert = System.Convert;
using Page = Kesco.Lib.Web.Controls.V4.Common.Page;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    /// <summary>
    ///     Контрол Grid (Таблица)
    /// </summary>
    public class Grid : V4Control, IClientCommandProcessor
    {
        private const string _constIdTag = "[CID]";
        private const string _constGroupingTag = "[GROUPING]";
        private const string _constFilteringTag = "[FILTERING]";
        private const string _constCtrlPagingBar = "[C_PAGINGBAR]";
        private const string _constMarginBottom = "[MARGIN_BOTTOM]";
        private const string _constGridHeight = "[GRID_HEIGHT]";
        private const string _constGroupingPanelEmptyText = "[GROUPINGPANELEMPTYTEXT]";
        private const string _constGridAutoSize = "[GRID_AUTOSIZE]";
        private const int MAX_RENDER_ROWS = 4000;

        private int QS_Id { get; set; }
        private int ReturnId { get; set; }
        private int Clid { get; set; }
        private bool Sticker { get; set; }
        private Uri RequestUrl { get; set; }
        public string SQLDelete { get; set; }

        private PagingBar.PagingBar _currentPagingBarCtrl;

        private GridDbSourceSettings _dbSourceSettings;

        public DataTable _dtLocal;
        private string _emptyDataString = "";
        private int? _maxPrintRenderRows;
        private string _modifyDateColumn;
        private string _modifyUserColumn;
        private bool _showModifyInfoTooltip;

        protected int GridCmdListnerIndex;
        private List<QueryColumn> QueryColumnsList;

        private List<string> AddFilterFieldList;

        /// <summary>
        ///     Настройки
        /// </summary>
        public GridSettings Settings { get; private set; }

        /// <summary>
        ///     Отступ от нижнего края страницы
        /// </summary>
        public int MarginBottom { get; set; } = 100;

        /// <summary>
        ///     Высота грида, если не указана, используется MarginBottom
        /// </summary>
        public int GridHeight { get; set; }

        /// <summary>
        ///     Параметр фильтрации в таблице Настройки
        /// </summary>
        public string FilterParamPrefix { get; set; }

        /// <summary>
        ///     Наименование колонки, возвращающей цвет строки
        /// </summary>
        public string RowColorFieldName { get; set; }

        /// <summary>
        ///     Параметр сортировки в таблице Настройки
        /// </summary>
        public string SortParamPrefix { get; set; }

        /// <summary>
        ///     Параметр количества строк на странице
        /// </summary>
        public string RowParamPrefix { get; set; }

        /// <summary>
        ///     Параметр видимости и порядка колонок в таблице Настройки
        /// </summary>
        public string FieldsParamPrefix { get; set; }
        
        /// <summary>
        ///     Автоматическое изменение высоты грида при изменениии размера окна
        /// </summary>
        public bool GridAutoSize { get; set; }

        /// <summary>
        ///     Страница редактирования сущности
        /// </summary>
        public string EditPage { get; set; }

        /// <summary>
        ///     Страница датализации сущности
        /// </summary>
        public string DetailPage { get; set; }

        /// <summary>
        ///     Редактировать каждую сущность в отдельном окне 
        /// </summary>
        public bool IsMultiWinEdit { get; set; }

        /// <summary>
        ///     Детализация каждой сущности в отдельном окне 
        /// </summary>
        public bool IsMultiWinDetail { get; set; }

        /// <summary>
        ///     Параметры для страницы редактирования сущности
        /// </summary>
        public string EditParam { get; set; }

        /// <summary>
        ///     Параметры для страницы детализации сущности
        /// </summary>
        public string DetailParam { get; set; }

        /// <summary>
        ///     Ширина окна редактирования сущности
        /// </summary>
        public int EditWidth { get; set; }

        /// <summary>
        ///     Ширина окна детализации сущности
        /// </summary>
        public int DetailWidth { get; set; }

        /// <summary>
        ///     Высота окна редактирования сущности
        /// </summary>
        public int EditHeight { get; set; }

        /// <summary>
        ///     Высота окна детализации сущности
        /// </summary>
        public int DetailHeight { get; set; }

        /// <summary>
        ///     Заголовок окна редактирования сущности
        /// </summary>
        public string EditTitle { get; set; }

        /// <summary>
        ///     Заголовок окна детализации сущности
        /// </summary>
        public string DetailTitle { get; set; }

        /// <summary>
        ///     Строка, которая выводится, если источник данных пустой
        /// </summary>
        public string EmptyDataString
        {
            get { return string.IsNullOrEmpty(_emptyDataString) ? Resx.GetString("lNoData") : _emptyDataString; }
            set { _emptyDataString = value; }
        }

        /// <summary>
        ///     Тип строки, которая выводится, если источник данных пустой
        /// </summary>
        public NtfStatus EmptyDataNtfStatus { get; set; } = NtfStatus.Empty;

        /// <summary>
        ///     Максимальное количество строк
        /// </summary>
        public int MaxPrintRenderRows
        {
            get { return _maxPrintRenderRows ?? MAX_RENDER_ROWS; }
            set { _maxPrintRenderRows = value; }
        }

        /// <summary>
        ///     Поддержка контролом грида группировки
        /// </summary>
        public bool ShowGroupPanel { get; set; }

        /// <summary>
        ///     Ширина 100%
        /// </summary>
        public bool IsWitdthFull { get; set; }
        

        /// <summary>
        ///     Поддержка контролом грида группировки
        /// </summary>
        public bool ShowFilterPanel { get; set; }

        /// <summary>
        ///     Поддержка контролом грида фильтрации
        /// </summary>
        public bool ShowFilterOptions { get; set; }

        /// <summary>
        ///     Видимость колонки из запроса по умолчанию
        /// </summary>
        public bool DefaultVisibleValue { get; set; } = true;

        /// <summary>
        /// Не выбодить общую панель с информацией о фильтре
        /// </summary>
        public bool HideFilterText { get; set; }

        /// <summary>
        ///     Поддержка контролом грида динамической работы с БД
        /// </summary>
        public bool AlwaysLoadDataFromDataBase { get; set; }

        /// <summary>
        ///     Грид работает с таблицей Запросы и АпросыКолонки
        /// </summary>
        public bool isUseQueryTable { get; set; }

        /// <summary>
        ///     Поддержка контролом грида работы с огромными источниками данных (невозможно получить общее количество записей и перейти на последнюю страницу)
        ///     Данный механизм работает только с подгрузкой данных из БД, т.е. когда AlwaysLoadDataFromDataBase = true
        /// </summary>
        public bool IsBigData { get; set; }

        /// <summary>
        ///     Отображать панель навигатора страниц
        /// </summary>
        public bool ShowPageBar { get; set; }

        /// <summary>
        ///     Свойство, указывающее сколько записей выводится на странице
        /// </summary>
        public int RowsPerPage { get; set; }

        /// <summary>
        ///     Свойство, указывающее сколько записей в источнике данных грида, иммет значение только если каждый запрос грида идет к БД
        /// </summary>
        public int RowsAllCount { get; set; }

        /// <summary>
        ///     Всегда отображать заголовок грида
        /// </summary>
        public bool AlwaysShowHeader { get; set; }

        /// <summary>
        ///     Сообщение при удалении записи
        /// </summary>
        public string MessageDeleteConfirm { get; set; }

        /// <summary>
        ///     Акцессор V4Page
        /// </summary>
        public new Page V4Page
        {
            get { return Page as Page; }
            set { Page = value; }
        }

        private List<GridColumn> TableColumnsFilter;

        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="param"></param>
        public void ProcessClientCommand(NameValueCollection param)
        {
            switch (param["cmdName"])
            {
                case "RefreshGridData":
                    if (_dbSourceSettings != null)
                        SetDataSource(_dbSourceSettings.SqlQuery, _dbSourceSettings.ConnectionString,
                            _dbSourceSettings.SqlCommandType, _dbSourceSettings.SqlParams, false);
                    RefreshGridData();
                    V4Page.JS.Write("setTimeout(function(){{$('#btnRefresh_{0}').show()}}, 1000);", ID);
                    break;
                // Добавление колонки в выбор фильтра
                case "AddFilterField":
                {
                    var p0 = TableColumnsFilter.FirstOrDefault(x => x.Id == param["ColumnId"]);

                    if (p0 != null)
                    {
                        AddFilterFieldList.Add(p0.Id);
                    }
                    else
                        V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"), MessageStatus.Error);

                    RenderColumnsFieldsFilter(V4Page);
                    
                }
                    break;
                // Удаление колонки из выбора фильтра
                case "DeleteFilterField":
                {
                    var p0 = TableColumnsFilter.FirstOrDefault(x => x.Id == param["ColumnId"]);

                    if (p0 != null)
                    {
                        AddFilterFieldList.RemoveAll(f => f == p0.Id);
                        if (p0.ColumnType == GridColumnTypeEnum.Boolean || p0.ColumnType == GridColumnTypeEnum.List)
                        {
                            p0.FilterUniqueValues = null;
                        }
                            else
                        {
                            p0.FilterUser = null;
                        }
                    }
                    else
                        V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"), MessageStatus.Error);

                    RenderColumnsFieldsFilter(V4Page);
                }
                    break;
                //Отрисовка настроек колонки
                case "RenderColumnSettings":
                {
                    var p = Settings.TableColumns.FirstOrDefault(x => x.FieldName == param["ColumnId"]);
                    var p1 = param["ChangeFilter"];

                    if (p1 == "False")
                        p = TableColumnsFilter.FirstOrDefault(x => x.FieldName == param["ColumnId"]);

                    if (p != null)
                        p.RenderColumnSettings(V4Page, p1 == null || p1 == "true", AlwaysLoadDataFromDataBase);
                    else
                        V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"),
                            MessageStatus.Error);
                    V4Page.RestoreCursor();
                }
                    break;
                //Пользовательский фильтр
                case "OpenUserFilterForm":
                {
                    var p0 = Settings.TableColumns.FirstOrDefault(x => x.Id == param["ColumnId"]);
                    var p1 = param["ChangeFilter"];

                    if (p1=="False")
                        p0 = TableColumnsFilter.FirstOrDefault(x => x.Id == param["ColumnId"]);

                    if (p0 != null)
                    {
                        if (p0.ColumnType == GridColumnTypeEnum.Boolean || p0.ColumnType == GridColumnTypeEnum.List)
                        {
                            p0.RenderColumnSettings(V4Page, p1 == null || p1 == "true", AlwaysLoadDataFromDataBase);
                        }
                        else
                        {
                            p0.RenderColumnUserFilterForm(V4Page, param["FilterId"], param["SetValue"],
                                p1 == null || p1 == "true");
                        }
                    }
                    else
                        V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"), MessageStatus.Error);
                }
                    break;
                case "RefreshUserFilterForm":
                {
                    var p0 = Settings.TableColumns.FirstOrDefault(x => x.Id == param["ColumnId"]);
                    var p1 = param["ChangeFilter"];
                    if (p0 != null)
                    {
                        p0.RefreshColumnUserFilterForm(V4Page, param["FilterId"], param["SetValue"], p1 == null || p1.ToLower() == "true", param["IsCurrentDate"]);

                        if (TableColumnsFilter != null)
                        {
                            var clmn = TableColumnsFilter.FirstOrDefault(x => x.Id == p0.Id);
                            clmn.IsCurrentDate = p0.IsCurrentDate;
                        }

                    }
                    else
                        V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"), MessageStatus.Error);
                }
                    break;
                case "SetFilterColumns":
                {
                    AddFilterFieldList.Clear();
                    SetFilterColumns();
                    SaveControlSettings();
                    RefreshGridData();
                }
                    break;
                case "RenderColumnsFieldsSettings":
                    RenderColumnsFieldsSettings(V4Page);
                    break;
                case "RenderColumnsFieldsFilter":
                    AddFilterFieldList?.Clear();
                    TableColumnsFilter = Clone(Settings.TableColumns);
                    RenderColumnsFieldsFilter(V4Page);
                    break;
                case "SetFilterColumnByUser":
                    if (param["ChangeFilter"] == "False")
                    {
                        SetFilterColumnByUser(false, param["ColumnId"], param["FilterId"]);
                        AddFilterFieldList.RemoveAll(f => f == param["ColumnId"]);
                        RenderColumnsFieldsFilter(V4Page);
                        break;
                    }
                    
                    SetFilterColumnByUser(true, param["ColumnId"], param["FilterId"]);
                    SaveControlSettings();
                    _currentPagingBarCtrl.CurrentPageNumber = 1;
                    RefreshGridData();
                    break;
                //Фильтр по выбранным значениям
                case "SetFilterByColumnValues":
                {
                    if (param["ChangeFilter"] == "False")
                    {
                        SetFilterByColumnValues(false, param["ColumnId"], param["Data"], param["Equals"]);
                        AddFilterFieldList.RemoveAll(f => f == param["ColumnId"]);
                        RenderColumnsFieldsFilter(V4Page);
                        break;
                    }

                    SetFilterByColumnValues(true, param["ColumnId"], param["Data"], param["Equals"]);
                    if (!string.IsNullOrEmpty(param["Direction"]))
                    {
                        SetOrderByColumnValues(param["ColumnId"], param["Direction"] == "0"
                            ? GridColumnOrderByDirectionEnum.Asc
                            : GridColumnOrderByDirectionEnum.Desc);
                    }

                    SaveControlSettings();
                    _currentPagingBarCtrl.CurrentPageNumber = 1;

                    var clmn = Settings.TableColumns.Find(x => x.Id == param["ColumnId"]);
                    var clmnNdx = Settings.TableColumns.FindIndex(x => x.Id == param["ColumnId"]);
                    RefreshGridData("#imgSettings" + clmnNdx + "_" + clmn.DisplayOrder);
                }
                    break;
                //Очистка фильтров
                case "ClearDataFilter":
                    ClearDataFilter(true);
                    SaveControlSettings();
                    break;
                case "ClearFilterColumnValues":
                    ClearFilterColumnValues(param["ColumnId"]);
                    _currentPagingBarCtrl.CurrentPageNumber = 1;
                    SaveControlSettings();
                    RefreshGridData();
                    break;
                case "ClearFilterAllValues":
                    ClearFilterAllValues();
                    _currentPagingBarCtrl.CurrentPageNumber = 1;
                    SaveControlSettings();
                    RefreshGridData();
                    break;
                //Сортировка
                case "SetOrderByColumnValues":
                {
                    var direction = param["Direction"] != null && param["Direction"] == "0"
                        ? GridColumnOrderByDirectionEnum.Asc
                        : GridColumnOrderByDirectionEnum.Desc;
                    SetOrderByColumnValues(param["ColumnId"], direction);
                    SaveControlSettings();

                    var clmn = Settings.TableColumns.Find(x => x.Id == param["ColumnId"]);
                    var clmnNdx = Settings.TableColumns.FindIndex(x => x.Id == param["ColumnId"]);
                    RefreshGridData("#imgOrders" + clmnNdx + "_" + clmn.DisplayOrder);
                }
                    break;
                case "ClearOrderByColumnValues":
                    ClearOrderByColumnValues(param["ColumnId"]);
                    SaveControlSettings();
                    RefreshGridData();
                    break;
                //Группировка
                case "GroupingGridData":
                    SetGroupingColumns(param["ColumnIds"]);
                    GroupingExpandColumn();
                    RefreshGridData(true);
                    break;
                case "GroupingRemoveColumn":
                    RemoveGroupingColumn(param["ColumnId"]);
                    GroupingExpandColumn();
                    RefreshGridData(true);
                    break;
                //Свернуть/развернуть группу
                case "GroupingExpandColumn":
                    GroupingExpandColumn(param["ColumnId"]);
                    V4Page.RestoreCursor();
                    break;
                //Порядок и видимость колонок
                case "ReorderColumns":
                    SetColumnsOrder(param["ColumnIds"]);
                    SetColumnsVisible(param["ColumnVisibleIds"]);
                    ClearOrderAllColumn();
                    SetOrderColumns(param["ColumnSortIds"]);
                    SaveVisibleAndOrderParam();
                    SaveControlSettings();
                    RefreshGridData();
                    break;
                //Перемещение колонки
                case "MoveColumn":
                    if (MoveColumn(param["DropColumn"], param["TargetColumn"])) {
                        SaveVisibleAndOrderParam();
                        SaveControlSettings();
                    }

                    RefreshGridData();
                    break;
                //Восстановление порядка и видимости колонок по умолчанию
                case "SetDefaultOrderAndVisible":
                    SetDefaultColumnsOrder();
                    SetDefaultColumnsVisible();
                    SaveVisibleAndOrderParam(true);
                    RefreshGridData();
                    break;
                // Удаление записи (для таблицы Запросы)
                case "Delete":
                    var RecId = Convert.ToInt32(param["recid"]);
                    var sqlParams = new Dictionary<string, object> { { "@id", new object[] { RecId, DBManager.ParameterTypes.Int32 } } };
                    DBManager.ExecuteNonQuery(SQLDelete, CommandType.Text, Config.DS_user, sqlParams);
                    RefreshGridData();
                    break;
                case "ClearFormParams":                    
                    ClearFilterAllValues();                    
                    SaveControlSettings(true);
                    SetDefaultColumnsOrder();
                    SetDefaultColumnsVisible();
                    SaveVisibleAndOrderParam(true);
                    RefreshGridData($"#imgSettingTools_{HtmlID}");
                    break;
            }
        }

        protected override void OnInit(EventArgs e)
        {
            if (!V4Page.Listeners.Contains(this)) V4Page.Listeners.Add(this);
            GridCmdListnerIndex = V4Page.Listeners.IndexOf(this);

            base.OnInit(e);

            if (V4Page.V4IsPostBack) return;

            _currentPagingBarCtrl = new PagingBar.PagingBar();
            _currentPagingBarCtrl.V4Page = V4Page;
            _currentPagingBarCtrl.ID = "pagingBar_" + ID;
            _currentPagingBarCtrl.CurrentPageChanged += _currentPagingBarCtrl_CurrentPageChanged;
            _currentPagingBarCtrl.RowsPerPageChanged += _currentPagingBarCtrl_RowsPerPageChanged;
            V4Page.V4Controls.Add(_currentPagingBarCtrl);
            _currentPagingBarCtrl.PreOnInit();
            _currentPagingBarCtrl.V4LocalInit();

            _currentPagingBarCtrl.RowsPerPage = RowsPerPage;
            _currentPagingBarCtrl.CurrentPageNumber = 1;
            _currentPagingBarCtrl.MaxPageNumber = 5;
            //_currentPagingBarCtrl.SetDisabled(true);
        }

        public void InitFromDB()
        {
            if (!isUseQueryTable) return;

            if (V4Page.Request.QueryString["id"] == null)
            {
                V4Page.Response.Write(Resx.GetString("Inv_msgNotCode"));
                V4Page.Response.End();
            }

            try
            {
                QS_Id = int.Parse(V4Page.Request.QueryString["id"]);
            }
            catch
            {
                //Неверно передан код запроса
                V4Page.Response.Write(Resx.GetString("Inv_msgQueryCodeParameterError"));
                V4Page.Response.End();
            }

            Sticker = (V4Page.Request.QueryString["sticker"] == "1");

            if (V4Page.Request.QueryString["clid"] != null && V4Page.Request.QueryString["clid"].Length > 0)
            {
                try
                {
                    Clid = int.Parse(V4Page.Request.QueryString["clid"]);
                }
                catch
                {
                }
            }

            if (V4Page.Request.QueryString["return"] != null)
            {
                try
                {
                    ReturnId = int.Parse(V4Page.Request.QueryString["return"]);
                }
                catch
                {
                    //Не верно передан код признака возврата
                    V4Page.Response.Write(Resx.GetString("Inv_msgReturnParameterError")); 
                    V4Page.Response.End();
                }
            }

            if (!FilterParamPrefix.IsNullEmptyOrZero()) FilterParamPrefix = FilterParamPrefix + QS_Id;
            if (!SortParamPrefix.IsNullEmptyOrZero()) SortParamPrefix = SortParamPrefix + QS_Id;
            if (!RowParamPrefix.IsNullEmptyOrZero()) RowParamPrefix = RowParamPrefix + QS_Id;

            RowsPerPage = LoadRowsControlSettings();

            RequestUrl = Page.Request.Url;

            var dtQuery = new DataTable();
            try
            {
                var sqlParams = new Dictionary<string, object>
                    {{"@id", new object[] {QS_Id, DBManager.ParameterTypes.Int32}}};
                dtQuery = DBManager.GetData(Entities.SQLQueries.SELECT_ЗапросПоКодуЗапроса, Config.DS_user,
                    CommandType.Text, sqlParams);
            }
            catch (Exception ex)
            {
                // неверно выполнен запрос
                V4Page.Response.Write(Resx.GetString("Inv_msgErrorExecuteQuery") + " " + ex.Message);
                V4Page.Response.End();
            }

            if (dtQuery.Rows.Count == 0)
            {
                //Неверно передан код запроса
                V4Page.Response.Write(Resx.GetString("Inv_msgQueryCodeParameterError") + " id=" + QS_Id);
                V4Page.Response.End();
            }

            var title = dtQuery.Rows[0]["Запрос"].ToString();
            if (!string.IsNullOrEmpty(title))
            {
                JS.Write("$('#divTitle_{0}').show();", ID);
                JS.Write("$('#divTitle_{0}').html('{1}');", ID, HttpUtility.JavaScriptStringEncode(title));
                JS.Write("document.title = '{0}';", HttpUtility.JavaScriptStringEncode(title));

            }

            var sqlQueryParams = new Dictionary<string, object>();
            SetDataSource(dtQuery.Rows[0]["SQL"].ToString(), Config.DS_user, CommandType.Text, sqlQueryParams);

            var xUri = dtQuery.Rows[0]["ФормаРедактирования"].ToString();
            if (!string.IsNullOrWhiteSpace(xUri))
            {
                if (xUri.Contains("@"))
                {
                    xUri = xUri.Replace("@", "");
                    xUri = System.Configuration.ConfigurationManager.AppSettings[xUri];
                }
                else
                {
                    xUri = new Uri(RequestUrl, xUri).AbsoluteUri;
                }

                JS.Write("$('.v4Grid').attr('ItemsForm', '{0}');", xUri);

                EditPage = dtQuery.Rows[0]["ФормаРедактирования"].ToString();

                var fieldList = dtQuery.Rows[0]["ID"].ToString().Split(',').ToList();

                SetServiceColumnEdit("v4_grid.rec_edit", fieldList, Resx.GetString("lblGridEdit"));
            }

            xUri = dtQuery.Rows[0]["ФормаНового"].ToString();
            if (!string.IsNullOrWhiteSpace(xUri))
            {
                if (xUri.Contains("@"))
                {
                    xUri = xUri.Replace("@", "");
                    xUri = System.Configuration.ConfigurationManager.AppSettings[xUri];
                }
                else
                {
                    xUri = new Uri(RequestUrl, xUri).AbsoluteUri;
                }

                JS.Write("$('.v4Grid').attr('ItemsAddForm', '{0}');", xUri);
                SetServiceColumnAdd("v4_grid.rec_add", Resx.GetString("lblAdd"));
            }

            SQLDelete = dtQuery.Rows[0]["SQLDelete"].ToString();
            if (!string.IsNullOrWhiteSpace(SQLDelete))
            {
                SetServiceColumnDelete("v4_grid.rec_del", new List<string> {dtQuery.Rows[0]["ID"].ToString() },
                    new List<string>());
            }

            if (ReturnId != 0)
            {
                ExistServiceColumnReturn = true;
                SetServiceColumnReturn("v4_returnValue",
                    new List<string> {dtQuery.Rows[0]["ID"].ToString(), dtQuery.Rows[0]["IDName"].ToString()});
            }
        }

        /// <summary>
        ///     Обработчик изменнения количества строк на странице
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _currentPagingBarCtrl_RowsPerPageChanged(object sender, EventArgs e)
        {
            if (!_currentPagingBarCtrl.Disabled)
            {
                RowsPerPage = _currentPagingBarCtrl.RowsPerPage;
                _currentPagingBarCtrl.CurrentPageNumber = 1;

                SaveRowsControlSettings(RowsPerPage);

                RefreshGridData();
                return;
            }

            V4Page.RestoreCursor();
        }

        /// <summary>
        ///     Обработчик изменнения номера страницы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _currentPagingBarCtrl_CurrentPageChanged(object sender, EventArgs e)
        {
          
            RefreshGridData();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="output"></param>
        public override void RenderControl(HtmlTextWriter output)
        {
            var currentAsm = Assembly.GetExecutingAssembly();
            var pagingBarContent =
                currentAsm.GetManifestResourceStream("Kesco.Lib.Web.Controls.V4.Grid.GridContent.htm");
            if (pagingBarContent == null) return;
            var reader = new StreamReader(pagingBarContent);
            var sourceContent = reader.ReadToEnd();

            sourceContent = sourceContent.Replace(_constIdTag, ID);
            sourceContent = sourceContent.Replace(_constGroupingTag, ShowGroupPanel ? "true" : "false");
            sourceContent = sourceContent.Replace(_constFilteringTag, ShowFilterPanel ? "true" : "false");
            sourceContent = sourceContent.Replace(_constMarginBottom, MarginBottom.ToString());
            sourceContent = sourceContent.Replace(_constGridHeight, GridHeight.ToString());
            sourceContent = sourceContent.Replace(_constGridAutoSize, GridAutoSize.ToString().ToLower());
            sourceContent = sourceContent.Replace(_constGroupingPanelEmptyText,
                Resx.GetString("lblGridGroupingPanelEmptyText"));


            using (TextWriter currentPageTextWriter = new StringWriter())
            {
                var currentPageWriter = new HtmlTextWriter(currentPageTextWriter);
                _currentPagingBarCtrl.RenderContolBody(currentPageWriter);
                sourceContent = sourceContent.Replace(_constCtrlPagingBar, currentPageTextWriter.ToString());
            }

            sourceContent = sourceContent.Replace("\n", "").Replace("\r", "").Replace("\t", "");
            output.Write(sourceContent);
        }

        /// <summary>
        ///     Установка источника данных
        /// </summary>
        /// <param name="dt">Заполненный данными DataTable</param>
        public void SetDataSource(DataTable dt)
        {
            _dbSourceSettings = null;
            ClearDataFilter(false);
            _dtLocal = dt;
            Settings = new GridSettings(_dtLocal, ID, GridCmdListnerIndex, V4Page, !AlwaysLoadDataFromDataBase, null)
            {
                IsGroupEnable = ShowGroupPanel,
                IsFilterEnable = ShowFilterOptions
            };
        }

        private void FillDtLocalByCurrentGridState()
        {
            var pageNum = _currentPagingBarCtrl.CurrentPageNumber;
            var itemsPerPage = RowsPerPage;
            var pageCount = -1;

            if (_dtLocal != null)
            {
                _dtLocal.Clear();
                _dtLocal.Dispose();
            }
            GC.Collect();
            var sotrString4BigData = !IsBigData ? "" : GetResultTableSortingString();

            var sqlParams = new Dictionary<string, object>();

            var sqlQuery = GetSqlQueryForDataBase(sqlParams);

            _dbSourceSettings.SqlParams.ToList().ForEach(x => sqlParams.Add(x.Key, x.Value));

            _dtLocal = DBManager.GetData(sqlQuery, _dbSourceSettings.ConnectionString, _dbSourceSettings.SqlCommandType, sqlParams, ref pageNum, ref itemsPerPage, ref pageCount, out var sRez, IsBigData, sotrString4BigData);

            if (_currentPagingBarCtrl != null)
            {
                _currentPagingBarCtrl.MaxPageNumber = pageCount;
                RowsAllCount = sRez;
            }

        }

        /// <summary>
        ///     Установка источника данных на основании переданных параметров
        /// </summary>
        /// <param name="sql">SQL-запрос или выражение</param>
        /// <param name="cn">Строка подключения</param>
        /// <param name="ctype">Тип запроса или выражения</param>
        /// <param name="args">Параметры sql</param>
        /// <param name="reloadDbSourceSettings">Пересоздавать экземпляр объекта GridDbSourceSettings</param>
        public void SetDataSource(string sql, string cn, CommandType ctype, Dictionary<string, object> args, bool reloadDbSourceSettings = true)
        {
            if (_dbSourceSettings == null || reloadDbSourceSettings)
                _dbSourceSettings = new GridDbSourceSettings
                {
                    SqlQuery = sql,
                    SqlQueryCurrent = sql,
                    ConnectionString = cn,
                    SqlCommandType = ctype,
                    SqlParams = args
                };

            ClearDataFilter(false, reloadDbSourceSettings);


            if (_currentPagingBarCtrl != null)
            {
                _currentPagingBarCtrl.CurrentPageNumber = 1;
            }

            if (AlwaysLoadDataFromDataBase)
            {
                FillDtLocalByCurrentGridState();
            }
            else
                _dtLocal = DBManager.GetData(_dbSourceSettings.SqlQuery, _dbSourceSettings.ConnectionString, _dbSourceSettings.SqlCommandType, _dbSourceSettings.SqlParams);

            if (!AlwaysLoadDataFromDataBase)
            if (reloadDbSourceSettings)
                Settings = new GridSettings(_dtLocal, ID, GridCmdListnerIndex, V4Page, !AlwaysLoadDataFromDataBase, null)
                {
                    IsGroupEnable = ShowGroupPanel,
                    IsFilterEnable = ShowFilterOptions
                };
            else if (!string.IsNullOrEmpty(FilterParamPrefix))
            {
                var oldSetting = Settings.TableColumns;
                Settings = new GridSettings(_dtLocal, ID, GridCmdListnerIndex, V4Page, !AlwaysLoadDataFromDataBase, null)
                {
                    IsGroupEnable = ShowGroupPanel,
                    IsFilterEnable = ShowFilterOptions
                };

                foreach (var col in Settings.TableColumns)
                {
                    var ndx = oldSetting.FindIndex(c => c.FieldName == col.FieldName);
                    if (ndx == -1) continue;
                    foreach (var property in col.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    {
                        if (!property.CanWrite) continue;
                        var oldProperty = oldSetting[ndx].GetType().GetProperty(property.Name)?.GetValue(oldSetting[ndx]);
                        if (oldProperty != null) property.SetValue(col, oldProperty);
                    }
                }

            }
        }

        /// <summary>
        ///     Очистка фильтров
        /// </summary>
        /// <param name="dropClientTable">Удалить с клиента html контейнер</param>
        /// <param name="dropGridSettings">Очистить настройки контрола</param>
        private void ClearDataFilter(bool dropClientTable, bool dropGridSettings = true)
        {
            if (_dtLocal != null)
            {
                _dtLocal.Clear();
                _dtLocal.Dispose();
            }

            if (Settings != null && Settings.DT != null && dropGridSettings)
            {
                Settings.DT.Clear();
                Settings.DT.Dispose();
                Settings.TableColumns = null;
                Settings.GroupingColumns = null;
                Settings.ColumnsDisplayOrder = null;
                Settings = null;
            }

            GC.Collect();
            if (dropClientTable) JS.Write("$(\"#{0}\").html('');", ID);
            JS.Write("$(\".v4SortablePanel_{0}\").find(\".v4GridListGroup\").remove();", ID);
            JS.Write("$(\"#spanGroupingPanelEmpty_{0}\").hide();", ID);
        }

        /// <summary>
        ///     Очистка грида
        /// </summary>
        public void ClearGridData()
        {
            var w = new StringWriter();
            V4Page.JS.Write("v4_grid.fixedHeaderDestroy();");
            ClearDataFilter(true);
            V4Page.RestoreCursor();
            JS.Write("$('#divPageBar_{0}').hide();", ID);
        }

        public void RefreshGridData()
        {
            RefreshGridData(false, "");
        }

        public void RefreshGridData(string focus)
        {
            RefreshGridData(false, focus);
        }

        /// <summary>
        ///     Обновление грида
        /// </summary>
        public void RefreshGridData(bool expand, string focus = "")
        {
            //LoadControlSettings();
            //LoadVisibleAndOrderParam();

           

            var w = new StringWriter();
            RenderGridData(w);
  
            #region Вывод текстового представления фильтра

            if (!HideFilterText)
            {
                var listFilterText = new List<string>();
                
                Settings.TableColumns
                    .Where(x => x.FilterUser != null && !x.IsDublicateColumn || x.FilterUniqueValues != null && x.FilterUniqueValues.Count > 0 && !x.IsDublicateColumn)
                    .ToList().OrderBy(x => x.DisplayOrder).ToList().ForEach(delegate(GridColumn tc)
                    {
                        tc.RenderTextFilter(out var titleFilter, out var filterClick, out var deleteSpan, out var filterRequired, false);
                        if (!filterRequired)
                        titleFilter = string.Join("", "", "<a ", filterClick,
                            $"title=\"{Settings.V4Page.Resx.GetString("lblChangeFilterCondition")}\"", ">", titleFilter, "</a>", "",
                            "<div class=\"v4DivInline\">", deleteSpan, "</div>");
                        
                        listFilterText.Add(titleFilter);
                    });
                
                if (ShowFilterPanel)
                {
                    RenderFilterPanel();
                    V4Page.JS.Write("$('#divFilteringPanel_{0}').show();", ID);
                    V4Page.JS.Write("$('#divSettingTools_{0}').show();", ID);
                    JS.Write("$('#imgSettingTools_{0}').prop('title', '{1}');",ID, Resx.GetString("Inv_lblColumnsTable"));
                    JS.Write("$('#imgFilterTools_{0}').prop('title', '{1}');", ID, Resx.GetString("lblFilter"));
                }
                else
                {
                    V4Page.JS.Write("$('#divFilteringPanel_{0}').hide();", ID);
                    V4Page.JS.Write("$('#divSettingTools_{0}').hide();", ID);
                }

                if (listFilterText.Count > 0 && ShowFilterPanel)
                {
                    var sb = new StringBuilder();

                    sb.Append("<div class=\"v4DivBlock\">");

                    sb.Append("<div class=\"v4DivInline\" style=\"text-align:left !important;\">");
                    sb.Append($"{Settings.V4Page.Resx.GetString("lblFilter")}");
                    
                    if (listFilterText.Count > 1)
                    {
                        sb.Append("<div class=\"v4DivInline\">");
                        sb.Append(
                            $"<span class=\"ui-icon ui-icon-delete\" style=\"display: inline-block;cursor:pointer\" onkeydown=\"var key=v4_getKeyCode(event); if(key == 13 || key == 32) this.click();\" onclick=\"v4_grid.clearFilterAllValues({Settings.GridCmdListnerIndex});\" title=\"{Settings.V4Page.Resx.GetString("lblRemoveAllInstalledFilters")}\" tabindex=0></span>");
                        sb.Append("</div>");
                    }

                    sb.Append(": ");

                    sb.Append(string.Join($" {Settings.V4Page.Resx.GetString("lANDUp")} ", listFilterText.ToArray()));
                    sb.Append("</div>");

                    sb.Append("</div>");

                    V4Page.JS.Write("$('#divFilteringPanel_{0}').html('{1}');", ID, HttpUtility.JavaScriptStringEncode(sb.ToString()));
                }
                else
                {
                    V4Page.JS.Write("$('#divFilteringPanel_{0}').html('');", ID);
                }
            }

            #endregion

            #region Filteting

            if (!HideFilterText && ShowFilterPanel)
            {
                V4Page.JS.Write("setTimeout(function(){{v4_grid.sortable('{0}',{1});}},50);", ID, GridCmdListnerIndex);
            }

            #endregion

            if (ShowGroupPanel)
                {
                if (Settings.GroupingColumns == null || Settings.GroupingColumns.Count == 0)
                    V4Page.JS.Write("$(\"#spanGroupingPanelEmpty_{0}\").show();", ID);
            }


            if (V4Page.V4IsPostBack)
                V4Page.JS.Write("v4_grid.fixedHeaderDestroy();");

            V4Page.JS.Write("$('#{0}').html('{1}');", ID, HttpUtility.JavaScriptStringEncode(w.ToString()));

            V4Page.JS.Write($"v4_grid.fixedHeader('{ID}');");

            //Костыль, чтобы fixedHeader перестроил размер заголовков колонок
            V4Page.JS.Write("setTimeout(function(){{$('#{0}').scrollLeft(1);}},0);", ID);

            V4Page.JS.Write(@"grid_clientLocalization = {{
                ok_button:""{0}"",
                apply_button:""{1}"",
                cancel_button:""{2}"" ,
                empty_filter_value:""{3}"", 
                sort_min_max_value:""{4}"",
                sort_max_min_value:""{5}""
            }};",
                Resx.GetString("cmdOK"),
                Resx.GetString("cmdApply"),
                Resx.GetString("cmdCancel"),
                Resx.GetString("msgEmptyFilterValue"),
                Resx.GetString("msgSortOrderMinMax"),
                Resx.GetString("msgSortOrderMaxMin")
            );

            if (ShowGroupPanel && _dtLocal != null && _dtLocal.Rows.Count > 0)
            {
                V4Page.JS.Write("v4_grid.setWidthGroupingPanel('{0}');", ID);
                if (expand) GroupingExpandColumnApply();
            }

            if (IsMultiWinEdit)
            {
                JS.Write(@"
                    v4_grid.clientEditSettings.push(
                    {{ 
                        Grid:""{0}"",
                        Data: {{
                            Source:""{1}"",
                            Param:""{2}"",
                            Width:""{3}"",
                            Height:""{4}"",
                            Title:""{5}"",
                            Waiter:""{6}""
                        }}
                    }}
                    );
                ",
                    ID, EditPage, EditParam, EditWidth, EditHeight, EditTitle, Resx.GetString("lblWait")
                );
            }

            if (IsMultiWinDetail)
            {
                JS.Write(@"
                    v4_grid.clientDetailSettings.push(
                    {{ 
                        Grid:""{0}"",
                        Data: {{
                            Source:""{1}"",
                            Param:""{2}"",
                            Width:""{3}"",
                            Height:""{4}"",
                            Title:""{5}"",
                            Waiter:""{6}""
                        }}
                    }}
                    );
                ",
                ID, DetailPage, DetailParam, DetailWidth, DetailHeight, DetailTitle, Resx.GetString("lblWait")
                );
            }

            if (!HideFilterText)
                V4Page.JS.Write("var wx=$('#thead_{0}').width(); var ws=$('#divSettingTools_{0}').width(); var wf=wx-ws-12;  $('#divFilteringPanel_{0}').width(wf);", ID);
            
            
            V4Page.RestoreCursor();
            if (focus != "") V4Page.JS.Write("setTimeout(function(){{$('{0}').focus();}},0);", focus);
        }

        private void RenderEmptyDataString(TextWriter w)
        {
            var className = EnumAccessors.GetCssClassByNtfStatus(EmptyDataNtfStatus, true);

            w.Write("<div id='gridEmptyRow_{2}' {1}>{0}</div>", EmptyDataString,
                string.IsNullOrEmpty(className) ? "" : " class='" + className + "'", ID);
        }

        /// <summary>
        ///     Формирование грида по фильтрам
        /// </summary>
        /// <param name="w"></param>
        public void RenderGridData(TextWriter w)
        {
            if (!AlwaysShowHeader && (_dtLocal == null || _dtLocal.Rows.Count == 0))
            {
                RenderEmptyDataString(w);
                _currentPagingBarCtrl.SetDisabled(true, false);
                JS.Write("$('#divPageBar_{0}').hide();", ID);
                JS.Write("$('#{0}').css(\"min-height\",100);", ID);
                return;
            }

            DataTable results = null;
            var defaultSort = "";
            var pageIndex = 0;
            var rowNumber = 0;
            var advEmptyClmn = false;
            var renderEmptyTr = false;

            var columnsWithFilterIn =
                Settings.TableColumns.Where(
                        x =>
                            x.FilterUser == null && x.FilterUniqueValues != null && x.FilterUniqueValues.Count > 0 &&
                            x.FilterEqual == GridColumnFilterEqualEnum.In)
                    .ToList();

            var columnsWithFilterNotIn =
                Settings.TableColumns.Where(
                        x =>
                            x.FilterUser == null && x.FilterUniqueValues != null && x.FilterUniqueValues.Count > 0 &&
                            x.FilterEqual == GridColumnFilterEqualEnum.NotIn)
                    .ToList();


            if (AlwaysLoadDataFromDataBase)
            {
                if (V4Page.V4IsPostBack) FillDtLocalByCurrentGridState();
                results = _dtLocal;
            }
            else
            {
                results = GetResultTableFromDtLocal();
                // Теперь накладываем ограничение по установленным пользовательским фильтрам
                var columnsWithUserFilter = Settings.TableColumns.Where(x => x.FilterUser != null).ToList();
                if (columnsWithUserFilter.Count > 0) results = ApplyUserFilter(results, columnsWithUserFilter);
            }

            if (results.Rows.Count > 0)
            {
                _currentPagingBarCtrl.RowsPerPage = RowsPerPage;
                if (!AlwaysLoadDataFromDataBase)
                _currentPagingBarCtrl.MaxPageNumber = (int) Math.Ceiling(results.Rows.Count / (double) RowsPerPage);
                pageIndex = (_currentPagingBarCtrl.CurrentPageNumber - 1) * RowsPerPage;

                _currentPagingBarCtrl.SetDisabled(false, false);
                JS.Write("$('#divResultCount_{0}').html(' {1}: {2}');", ID, Resx.GetString("lblGridRecordCount"),
                                         AlwaysLoadDataFromDataBase ? RowsAllCount : results.Rows.Count);

                if (ShowPageBar)
                    JS.Write("$('#divPageBar_{0}').show();", ID);
                else
                    JS.Write("$('#divPageBar_{0}').hide();", ID);
            }
            else
            {
                _currentPagingBarCtrl.SetDisabled(true, false);
                JS.Write("$('#divPageBar_{0}').hide();", ID);
            }

            if (_dbSourceSettings != null)
            {
                JS.Write(
                    "$('#btnRefresh_{0}').button({{icons: {{primary: 'ui-icon-refresh'}},text: false}}).prop('title', '{1}');",
                    ID, Resx.GetString("cmdRefreshTitle"));
                JS.Write(
                    "$('#btnRefresh_{0}').unbind('click'); $('#btnRefresh_{0}').bind('click', function(){{$('#btnRefresh_{0}').hide(); cmdasync('cmd', 'Listener', 'ctrlId', {1}, 'cmdName', 'RefreshGridData'); }});",
                    ID, GridCmdListnerIndex);
            }
            else
            {
                JS.Write("$('#btnRefresh_{0}').hide();", ID);
            }

            //Сортируем
            //Сортировка групп
            var sortStrGroup = Settings.GroupingColumns == null
                ? ""
                : string.Join(", ",
                    Settings.GroupingColumns.Select(
                        x =>
                            x.IsDublicateColumn ? "" : (
                            "[" + x.FieldName + "]" +
                            (x.OrderByDirection == GridColumnOrderByDirectionEnum.Desc ? " DESC" : ""))).ToArray());

            //Обычная сортировка
            var sortStr = string.Join(", ",
                Settings.TableColumns.Where(
                        x =>
                            x.OrderByNumber != null && !x.IsDublicateColumn &&
                            (Settings.GroupingColumns == null ||
                             Settings.GroupingColumns != null && !Settings.GroupingColumns.Contains(x)))
                    .OrderBy(x => x.OrderByNumber)
                    .Select(
                        x =>
                            "[" + x.SortFieldName + "]" +
                            (x.OrderByDirection == GridColumnOrderByDirectionEnum.Desc ? " DESC" : "")) 
                    .ToArray());
            
            //Добавляем сортировку по полям группировки
            if (sortStrGroup.Length > 0) sortStr = sortStrGroup + (sortStr.Length > 0 ? ", " + sortStr : "");

            // Добавляем колонки для сортировки по значениям null для типа date
            foreach (var col in Settings.TableColumns)
            {
                if (col.OrderByNumber != null && col.ColumnType == GridColumnTypeEnum.Date)
                {
                    var sortStrCol = "[" + col.SortFieldName + "]";
                    var sortStrColOrder = (col.OrderByDirection == GridColumnOrderByDirectionEnum.Desc ? " DESC" : "");
                    results.Columns.Add(col.SortFieldName + "NullEmptyCheck", typeof(int), col.SortFieldName + (col.SortNullFirst ? " is not Null" : " is Null"));
                    sortStr = sortStr.Replace(sortStrCol + sortStrColOrder, col.SortFieldName + "NullEmptyCheck " + sortStrColOrder + ", " + sortStrCol + sortStrColOrder);
                }
            }

            results.DefaultView.Sort = sortStr.Length > 0 ? sortStr : defaultSort;

            results = results.DefaultView.ToTable();
            //-----------------------------------------------


            w.Write("<table class='{1}' id='table_{0}' {2}>", ID,
                Settings.GroupingColumns != null && Settings.GroupingColumns.Count > 0 ? "gridGroup" : "grid",
                IsWitdthFull? $"style='width:96%;'":""                
                );
            w.Write("<thead>");
            w.Write("<tr id='thead_{0}' class=\"gridHeader\">", ID);

            if (ShowGroupPanel && Settings.GroupingColumns != null)
            {
                for (var i = 0; i < Settings.GroupingColumns.Count; i++)
                    w.Write("<th style=\"width:20px;\">&nbsp;</th>");

                var cntDisplay = Settings.TableColumns.Count(x => x.DisplayVisible);
                var cntGroup = Settings.GroupingColumns.Count;
                if ((cntDisplay == 0 || cntDisplay == cntGroup) && !ExistServiceColumn)
                {
                    w.Write("<th>&nbsp;</th>");
                    advEmptyClmn = true;
                }
            }

            if (ExistServiceColumnReturn)
                w.Write(@"<th>&nbsp;</th>");

            if (_existServiceColumnChecked)
                w.Write(
                    @"<th style=""text-align:center""><input type=""checkbox"" id=""allCheck_{0}"" onclick=""v4_grid.checkedAll('{0}','{1}', this.checked);""></th>",
                    ID, GridCmdListnerIndex);

            var resultD = results.AsEnumerable();
            if (Settings.ColumnDisplayVisibleIfHasValue != null)
            {
                foreach (var col in Settings.ColumnDisplayVisibleIfHasValue)
                {
                    var hasValue = false;
                    if (AlwaysLoadDataFromDataBase)
                    {
                        hasValue = resultD.Any(x => !string.IsNullOrEmpty(x.Field<string>(col)));
                    }
                    else
                    {
                        // ToDo: Необходимо учитывать записи только текущей страницы
                        hasValue = resultD.Any(x => !string.IsNullOrEmpty(x.Field<string>(col)));
                    }

                    if (!hasValue)
                    {
                        var clmn = Settings.TableColumns.FirstOrDefault(x => x.FieldName == col);
                        if (clmn != null)
                        {
                            clmn.DisplayVisible = false;
                        }
                    }
                }
            }

            if (ExistServiceColumn)
            {
                w.Write(@"<th>");
                if (_existServiceColumnAdd)
                {
                    var clientParams = "";
                    _addPkFieldsName?.ForEach(delegate(string paramName)
                    {
                        clientParams += (clientParams.Length > 0 ? "," : "") + $"'{paramName}'";
                    });

                    if (IsMultiWinDetail)
                        clientParams = clientParams.Length > 0
                            ? $"'{ID}','{Guid.NewGuid()}', false" + "," + clientParams
                            : $"'{ID}','{Guid.NewGuid()}', false";

                    w.Write(
                        "<div class=\"v4DivInline\"><span id='theadAdd_{3}' class=\"ui-icon icon-new\" border=\"0\" tabindex=\"0\" onkeydown=\"v4_grid.keydown(event, this);\" style=\"cursor:pointer;\" title=\"{0}\" onclick=\"{1}\" tabindex=\"{2}\"></div>",
                        HttpUtility.HtmlEncode(_addTitle),
                        IsMultiWinEdit
                            ? $"{"v4_grid.multiWinEditable"}('{ID}','{Guid.NewGuid()}', false,'{0}');"
                            : clientParams.Length == 0? $"{_addClientFuncName}();"
                                : $"{_addClientFuncName}({clientParams});",
                        100,
                        ID);
                    w.Write(@"</th>");
                }
            }

            Settings.TableColumns.OrderBy(x => x.DisplayOrder).ToList().ForEach(delegate(GridColumn tc)
            {
                if (!tc.DisplayVisible ||
                    Settings.GroupingColumns != null && Settings.GroupingColumns.Contains(tc)) return;

                tc.RenderColumnSettingsHeader(w, AlwaysLoadDataFromDataBase, this);
            });

            if (ExistServiceColumnDetail)
                w.Write(@"<th>&nbsp;</th>");

            w.Write("</tr>");
            w.Write("</thead>");

            if (Settings.TableColumns.Where(x => x.IsSumValues).ToList().Count > 0)
            {
                w.Write("<tfoot>");
                w.Write("<tr class=\"gridHeader\">");


                if (ShowGroupPanel && Settings.GroupingColumns != null)
                {
                    for (var i = 0; i < Settings.GroupingColumns.Count; i++)
                        w.Write(@"<td>&nbsp;</td>");

                    if (advEmptyClmn)
                        w.Write("<td>&nbsp;</td>");
                }

                if (ExistServiceColumn)
                    w.Write(@"<td>&nbsp;</td>");

                var resultData = results.AsEnumerable();

                Settings.TableColumns.OrderBy(x => x.DisplayOrder).ToList().ForEach(delegate(GridColumn tc)
                {
                    if (!tc.DisplayVisible ||
                        Settings.GroupingColumns != null && Settings.GroupingColumns.Contains(tc)) return;

                    if (!tc.IsSumValues)
                        tc.RenderColumnDataSumFooter(w, null);
                    else
                        switch (tc.ColumnType)
                        {
                            case GridColumnTypeEnum.Short:
                                var sumS = resultData.Sum(x => x.Field<short>(tc.FieldName));
                                tc.RenderColumnDataSumFooter(w, sumS);
                                break;
                            case GridColumnTypeEnum.Long:
                                var sumL = resultData.Sum(x => x.Field<long>(tc.FieldName));
                                tc.RenderColumnDataSumFooter(w, sumL);
                                break;
                            case GridColumnTypeEnum.Int:
                                var sumI = resultData.Sum(x => x.Field<int>(tc.FieldName));
                                tc.RenderColumnDataSumFooter(w, sumI);
                                break;
                            case GridColumnTypeEnum.Decimal:
                                var sumD = resultData.Sum(x => x.Field<decimal>(tc.FieldName));
                                tc.RenderColumnDataSumFooter(w, sumD);
                                break;
                            default:
                                tc.RenderColumnDataSumFooter(w, null);
                                break;
                        }
                });

                w.Write("</tr>");
                w.Write("</tfoot>");
            }

            w.Write("<tbody>");

            var listGroupClmns = new List<GridColumn>();
            var dictCurrentGroups = new Dictionary<int, string>();
            var groupIndex = 0;
            var groupParent = "";
            var noRenderItemsData = Settings.GroupingColumns != null &&
                                    Settings.TableColumns.Count(x => x.DisplayVisible) ==
                                    Settings.GroupingColumns.Count;
            var requiredGrouping = ShowGroupPanel && Settings.GroupingColumns != null;

            for (var i = 0; i < results.Rows.Count; i++)
            {
                if (AlwaysLoadDataFromDataBase || ShowPageBar == false || Settings.IsPrintVersion && i < MaxPrintRenderRows || i >= pageIndex && rowNumber < RowsPerPage)
                {
                    if (!renderEmptyTr)
                    {
                        RenderEmptyTrForFixedHeader(w, advEmptyClmn);
                        renderEmptyTr = true;
                    }

                    if (requiredGrouping)
                    {
                        for (var j = 0; j < Settings.GroupingColumns.Count; j++)
                        {
                            var clmnGroup = Settings.GroupingColumns[j];
                            listGroupClmns.Add(clmnGroup);

                            if (i != 0 && i != pageIndex && (i <= 0 || listGroupClmns.All(x =>
                                                                 results.Rows[i][x.FieldName]
                                                                     .Equals(results.Rows[i - 1][x.FieldName]))))
                                continue;

                            groupIndex = j;
                            var groupItem = Guid.NewGuid().ToString();

                            if (!dictCurrentGroups.ContainsKey(j)) dictCurrentGroups.Add(j, groupItem);
                            else dictCurrentGroups[j] = groupItem;

                            groupParent = dictCurrentGroups.ContainsKey(j - 1) ? dictCurrentGroups[j - 1] : "";

                            var countRecord = GetCountRecordInGroup(results, results.Rows[i], listGroupClmns);
                            RenderGroupTr(w, j, results.Rows[i], clmnGroup, advEmptyClmn, countRecord, groupItem,
                                groupIndex, groupParent, noRenderItemsData);
                        }

                        if (dictCurrentGroups.Count > 0)
                            groupParent = dictCurrentGroups[dictCurrentGroups.Keys.Max()];
                        listGroupClmns.Clear();
                    }

                    if (!noRenderItemsData)
                    {
                        if (requiredGrouping)
                        {
                            w.Write("<tr group-index=\"{0}\" group-parent=\"{1}\">", groupIndex, groupParent);

                            for (var j = 0; j < Settings.GroupingColumns.Count; j++)
                                w.Write("<td>&nbsp;</td>");

                            if (advEmptyClmn)
                                w.Write("<td>&nbsp;</td>");
                        }
                        else
                        {
                            if (!RowColorFieldName.IsNullEmptyOrZero())
                            {
                                w.Write("<tr class=\"{0}\">", results.Rows[i][RowColorFieldName]);
                            }
                            else
                            {
                                w.Write("<tr>");
                            }
                        }

                        if (_existServiceColumnChecked)
                        {
                            w.Write("<td>");
                            w.Write("<div class=\"v4DivTable\">");
                            w.Write("<div class=\"v4DivTableRow\">");


                            RenderServiceCheckedColumn(w, results.Rows[i], i);

                            w.Write("</div>");
                            w.Write("</div>");
                            w.Write("</td>");
                        }

                        if (ExistServiceColumnReturn)
                        {
                            w.Write("<td>");
                            w.Write("<div class=\"v4DivTable\">");
                            w.Write("<div class=\"v4DivTableRow\">");

                            var needRenderingReturn = !RenderConditionServiceColumnReturn.Any(x =>
                                !CheckCellValueOnCondition(results.Rows[i], x.Key, x.Value));

                            if (needRenderingReturn)
                                RenderServiceColumnReturn(w, results.Rows[i], i);

                            w.Write("</div>");
                            w.Write("</div>");
                            w.Write("</td>");
                        }

                        if (ExistServiceColumn)
                        {
                            w.Write("<td>");
                            w.Write("<div class=\"v4DivTable\">");
                            w.Write("<div class=\"v4DivTableRow\">");

                            var needRenderingDelete = !RenderConditionServiceColumnDelete.Any(x =>
                                !CheckCellValueOnCondition(results.Rows[i], x.Key, x.Value));
                            var needRenderingEdit = !RenderConditionServiceColumnEdit.Any(x =>
                                !CheckCellValueOnCondition(results.Rows[i], x.Key, x.Value));

                            if (_existServiceColumnCopy) RenderServiceColumnCopy(w, results.Rows[i], i);
                            if (_existServiceColumnCommandMenu) RenderServiceColumnCommandMenu(w, results.Rows[i], i);
                            if (_existServiceColumnDelete && needRenderingDelete)
                                RenderServiceColumnDelete(w, results.Rows[i], i);
                            if (_existServiceColumnEdit && needRenderingEdit)
                                RenderServiceColumnEdit(w, results.Rows[i], i);

                            w.Write("</div>");
                            w.Write("</div>");
                            w.Write("</td>");
                        }

                        Settings.TableColumns.OrderBy(x => x.DisplayOrder).ToList().ForEach(delegate(GridColumn tc)
                        {
                            if (!tc.DisplayVisible ||
                                Settings.GroupingColumns != null && Settings.GroupingColumns.Contains(tc))
                                return;
                            tc.RenderColumnData(w, results.Rows[i], i);
                        });

                        var needRenderingDetail = !RenderConditionServiceColumnDetail.Any(x =>
                            !CheckCellValueOnCondition(results.Rows[i], x.Key, x.Value));

                        if (ExistServiceColumnDetail)
                        {
                            w.Write("<td>");
                            if (needRenderingDetail)
                            {
                                w.Write("<div class=\"v4DivTable\">");
                                w.Write("<div class=\"v4DivTableRow\">");

                                RenderServiceColumnDetail(w, results.Rows[i], i);

                                w.Write("</div>");
                                w.Write("</div>");
                            }
                            w.Write("</td>");
                        }

                        w.Write("</tr>");
                    }

                    rowNumber++;
                }
                else if (_currentPagingBarCtrl.CurrentPageNumber * RowsPerPage < i)
                {
                    break;
                }
            }

            if (AlwaysShowHeader && (_dtLocal == null || _dtLocal.Rows.Count == 0))
            {
                if (!renderEmptyTr)
                {
                    RenderEmptyTrForFixedHeader(w, advEmptyClmn);
                    renderEmptyTr = true;
                }

                w.Write("<tr><td colspan='100'>");
                RenderEmptyDataString(w);
                w.Write("</td></tr>");
            }

            w.Write("</tbody>");

            w.Write("</table>");

            if (Settings.IsPrintVersion && results.Rows.Count > MaxPrintRenderRows)
                w.Write("<div style=\"font-weight:bold;\">{0} {1} {2} {3} {4}!</div>", Resx.GetString("msgPrintCount"),
                    MaxPrintRenderRows, Resx.GetString("lOf"), results.Rows.Count, Resx.GetString("lblGridRecords"));

            if (AlwaysShowHeader && (_dtLocal == null || _dtLocal.Rows.Count == 0))
                JS.Write("$('#{0}').css(\"min-height\",100);", ID);
        }

        private DataTable GetResultTableFromDtLocal()
        {
            DataTable results = null;

            #region Unique values filter
            // Фильтр по уникальным значениям
            var columnsWithFilterIn =
                Settings.TableColumns.Where(
                        x =>
                            x.FilterUser == null && x.FilterUniqueValues != null && x.FilterUniqueValues.Count > 0 &&
                            x.FilterEqual == GridColumnFilterEqualEnum.In)
                    .ToList();

            var columnsWithFilterNotIn =
                Settings.TableColumns.Where(
                        x =>
                            x.FilterUser == null && x.FilterUniqueValues != null && x.FilterUniqueValues.Count > 0 &&
                            x.FilterEqual == GridColumnFilterEqualEnum.NotIn)
                    .ToList();


            if (columnsWithFilterIn.Count > 0 || columnsWithFilterNotIn.Count > 0)
            {
                //фильтруем
                var applyfilter = from r in _dtLocal.AsEnumerable()
                    where
                        (columnsWithFilterIn.Count == 0 || columnsWithFilterIn.All(
                            clmn =>
                                clmn.FilterUniqueValues.ContainsValue(r.Field<object>(clmn.FieldName) ?? string.Empty))
                        )
                        && (columnsWithFilterNotIn.Count == 0 || !columnsWithFilterNotIn.Any(
                            clmn =>
                                clmn.FilterUniqueValues.ContainsValue(
                                    r.Field<object>(clmn.FieldName) ?? string.Empty)))
                    select r;

                if (applyfilter.Any())
                    results = applyfilter.CopyToDataTable();
                else
                    results = _dtLocal.Clone();
            }
            else
            {
                results = _dtLocal.Copy();
            }

            #endregion

            #region User filter
            // Теперь накладываем ограничение по установленным пользовательским фильтрам
            var columnsWithUserFilter = Settings.TableColumns.Where(x => x.FilterUser != null).ToList();
            if (columnsWithUserFilter.Count > 0) results = ApplyUserFilter(results, columnsWithUserFilter);
            #endregion

            #region Sorting
            results.DefaultView.Sort = GetResultTableSortingString(); 
            results = results.DefaultView.ToTable();
            #endregion
            
            
            return results;
        }

        private string GetResultTableSortingString()
        {
            //TODO: реализовать предварительную загрузку настроек грида
            if (Settings == null) return "";

            //Сортируем
            //Сортировка групп
            var sortStrGroup = Settings.GroupingColumns == null
                ? ""
                : string.Join(", ",
                    Settings.GroupingColumns.Select(
                        x => x.IsDublicateColumn? "" : (
                            "[" + x.FieldName + "]" +
                            (x.OrderByDirection == GridColumnOrderByDirectionEnum.Desc ? " DESC" : ""))).ToArray());

            //Обычная сортировка
            var sortStr = string.Join(", ",
                Settings.TableColumns.Where(
                        x =>
                            x.OrderByNumber != null && !x.IsDublicateColumn &&
                            (Settings.GroupingColumns == null ||
                             Settings.GroupingColumns != null && !Settings.GroupingColumns.Contains(x)))
                    .OrderBy(x => x.OrderByNumber)
                    .Select(
                        x =>
                            "[" + x.SortFieldName + "]" +
                            (x.OrderByDirection == GridColumnOrderByDirectionEnum.Desc ? " DESC" : ""))
                    .ToArray());

            //Добавляем сортировку по полям группировки
            if (sortStrGroup.Length > 0) sortStr = sortStrGroup + (sortStr.Length > 0 ? ", " + sortStr : "");

            return sortStr.Length > 0 ? sortStr : "";
        }

        private string GetSqlQueryForDataBase(Dictionary<string, object> sqlParams)
        {
            var sqlQuery = new StringBuilder();

                    if (Settings == null)
            {
                if (!V4Page.IsPostBack)
                {
                    QueryColumnsList = QueryColumn.GetQueryColumn(QS_Id.ToString());
                    GetQueryStringFilter();
                }

                _dtLocal = CreateDTLocalFromQueryColumn();
                Settings = new GridSettings(_dtLocal, ID, GridCmdListnerIndex, V4Page, !AlwaysLoadDataFromDataBase, QueryColumnsList)
                {
                    IsGroupEnable = ShowGroupPanel,
                    IsFilterEnable = ShowFilterOptions
                };

                LoadControlSettings();
                LoadVisibleAndOrderParam();
            }

            //Потому, что для BigData сортировка добавляется по-другому
            var sortString = !IsBigData ? GetResultTableSortingString() : "";
            sqlQuery.Append($"SELECT {(!string.IsNullOrEmpty(sortString) ? "TOP 100 PERCENT" : "")} * FROM ({_dbSourceSettings.SqlQuery}) x");

            if (Settings != null)
            {
                var whereClause = "";
                // Теперь накладываем ограничение по установленным пользовательским фильтрам
                var columnsWithUserFilter = Settings.TableColumns.Where(x => x.FilterUser != null).ToList();
                if (columnsWithUserFilter.Count > 0)
                {
                    var whereClauseFilter = GetUserFilterWhereClauseSqlString(columnsWithUserFilter, sqlParams);

                    if (!string.IsNullOrEmpty(whereClauseFilter))
                    {
                        sqlQuery.Append(Environment.NewLine);
                        sqlQuery.Append(string.IsNullOrEmpty(whereClause) ? $" WHERE " : $" AND ");
                        whereClause += whereClauseFilter;
                    }
                }

                // Теперь накладываем ограничение по уникальным значениям
                var columnsWithUniqueValues = Settings.TableColumns.Where(x => x.FilterUniqueValues != null && x.FilterUniqueValues.Count > 0).ToList();
                if (columnsWithUniqueValues.Count > 0)
                {
                    var whereUniqueValueClause = GetUniqueValuesWhereClauseSqlString(columnsWithUniqueValues, sqlParams);

                    if (!string.IsNullOrEmpty(whereUniqueValueClause))
                    {
                        if (string.IsNullOrEmpty(whereClause))
                        {
                            sqlQuery.Append(Environment.NewLine);
                            sqlQuery.Append(string.IsNullOrEmpty(whereClause) ? $" WHERE " : $" AND ");
                            whereClause += whereUniqueValueClause;
                        }
                        else
                        {
                            whereClause += " AND " + whereUniqueValueClause;
                        }
                    }
                }

                sqlQuery.Append($"{whereClause}");
            }

            // Добавляем сортировку
            if (!string.IsNullOrEmpty(sortString))
            {
                sqlQuery.Append(Environment.NewLine);
                sqlQuery.Append($" ORDER BY {sortString}");
            }

            return sqlQuery.ToString();
        }

        /// <summary>
        ///     Получение информации о количестве элементов в группе
        /// </summary>
        /// <param name="dt">Таблица</param>
        /// <param name="dr">Текущая запись группы</param>
        /// <param name="clmns">Список колонок группы</param>
        /// <returns>Количество записей</returns>
        private int GetCountRecordInGroup(DataTable dt, DataRow dr, IEnumerable<GridColumn> clmns)
        {
            return dt.AsEnumerable().Count(row => clmns.All(gc => dr[gc.FieldName].Equals(row[gc.FieldName])));
        }

        private void RenderEmptyTrForFixedHeader(TextWriter w, bool advEmptyClmn)
        {
            w.Write("<tr style=\"visibility: collapse;\">");

            if (Settings.GroupingColumns != null)
                for (var i = 0; i < Settings.GroupingColumns.Count; i++)
                    w.Write("<td></td>");

            if (ExistServiceColumnReturn) w.Write("<td></td>");
            if (_existServiceColumnChecked) w.Write("<td></td>");
            if (ExistServiceColumn) w.Write("<td></td>");
            if (advEmptyClmn) w.Write("<td></td>");

            var cnt =
                Settings.TableColumns.Count(
                    x =>
                        x.DisplayVisible && (Settings.GroupingColumns == null ||
                                             Settings.GroupingColumns.Count == 0)
                        ||
                        x.DisplayVisible && Settings.GroupingColumns != null &&
                        !Settings.GroupingColumns.Contains(x));

            for (var i = 0; i < cnt; i++)
                w.Write("<td></td>");


            if (ExistServiceColumnDetail) w.Write("<td></td>");

            w.Write("</tr>");
        }

        private void RenderFilterPanel()
        {
            JS.Write("$('#imgSettingTools_{0}').attr('onclick', \"cmdasync('cmd', 'Listener', 'ctrlId', {1}, 'cmdName', 'RenderColumnsFieldsSettings');\");",
                ID,
                Settings.GridCmdListnerIndex
            );
            JS.Write("$('#imgFilterTools_{0}').attr('onclick', \"cmdasync('cmd', 'Listener', 'ctrlId', {1}, 'cmdName', 'RenderColumnsFieldsFilter');\");",
                ID,
                Settings.GridCmdListnerIndex
            );

        }

        private void RenderColumnsFieldsSettings(Page page)
        {
            var w = new StringWriter();
            RenderFieldBlock(w);
            page.JS.Write("$('#divColumnsFieldForm_Values_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(w.ToString()));

            var wo = new StringWriter();
            RenderSortClearBlock(wo);
            page.JS.Write("$('#divColumnsFieldForm_ClearSort_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(wo.ToString()));

            var wc = new StringWriter();
            RenderOrderClearBlock(wc);
            page.JS.Write("$('#divColumnsFieldForm_ClearOrder_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(wc.ToString()));

            V4Page.JS.Write("$('#ReorderFieldTable_"+ Settings.GridId + " tbody').sortable({helper: v4_grid.fixSortableWithHelper, stop: function(event, ui) {v4_grid.stopSortResort('"+ Settings.GridId + "');} });");

            JS.Write("v4_grid.columnsFieldForm('{1}', {2}, 'imgSettingTools_{0}', '{3}');", 
                ID,
                Settings.GridId,
                Settings.GridCmdListnerIndex,
                Settings.V4Page.Resx.GetString("Inv_lblColumnsTable")
            );
        }

        private void RenderFieldBlock(TextWriter w)
        {
            w.Write("<table id='ReorderFieldTable_{0}' class='v4sortable-table' width='98%'><tbody>", Settings.GridId);

            var rowNum = 0;
            foreach (var clmn in Settings.TableColumns.Where(c => !c.IsCondition).OrderBy(c => c.DisplayOrder))
            {
                if (clmn.IsCondition) continue;
                w.Write("<tr column-id='{0}'>",clmn.Id);

                w.Write("<td><input type='checkbox' id='cfAllField' {0}></td>", clmn.DisplayVisible ? "checked" : "");
                
                w.Write("<td width='20px' style='text-align:center;padding:1px;cursor:hand;textDecoration:none;'>");
                if (clmn.OrderByNumber != null)
                {
                    if (clmn.OrderByDirection == GridColumnOrderByDirectionEnum.Desc)
                    {
                        w.Write("<img src=\"/styles/scrolldownenabled.gif\" border=\"0\" style=\"cursor:pointer;\" title='{0}' onclick=\"{1}\">", Resx.GetString("msgSortOrderMaxMin"), "v4_grid.fnSortOrderClick(this);");
                    }
                    else
                    {
                        w.Write("<img src=\"/styles/scrollupenabled.gif\" border=\"0\" style=\"cursor:pointer;\" title='{0}' onclick=\"{1}\">", Resx.GetString("msgSortOrderMinMax"), "v4_grid.fnSortOrderClick(this);");
                    }
                }
                w.Write("</td>");

                w.Write("<td id=\"tdNumOrder\" onmouseenter=\"v4_grid.fnTblMouseEnter('{0}',{1})\" onmouseleave=\"v4_grid.fnTblMouseLeave('{0}',{1})\" width='20px' style='text-align:center; padding:1px;cursor:hand;textDecoration:none;'", Settings.GridId, rowNum);
                w.Write(">{0}</td>", clmn.OrderByNumber == null ? "" : clmn.OrderByNumber.ToString());

                w.Write("<td class=\"v4PaddingCell\" style=\"text-align:left; white-space: nowrap; padding: 1px;\">");
                w.Write(clmn.Alias);
                w.Write("</td>");

                w.Write("</tr>");
                rowNum++;
            }

            w.Write("</tbody></table>");
            JS.Write("v4_grid.indexSort = 0;");
        }

        private void RenderOrderClearBlock(TextWriter w)
        {
            w.Write("<div style=\"text-align:right; white-space: nowrap;padding-top:5px;\">");
            w.Write(
                "<a onclick=\"v4_grid.closeColumnsFieldForm(); cmdasync('cmd', 'Listener', 'ctrlId', {0}, 'cmdName', 'SetDefaultOrderAndVisible');\"><nobr>{1}</nobr></div>",
                Settings.GridCmdListnerIndex,
                Settings.V4Page.Resx.GetString("Inv_lblRestoreDefaultOrderAndVisibility"));
            w.Write("</div>");
        }

        private void RenderSortClearBlock(TextWriter w)
        {
            w.Write("<div style=\"text-align:right; white-space: nowrap;padding-top:5px;\">");
            w.Write(
                "<a onclick=\"v4_grid.ClearSort('{0}');\"><nobr>{1}</nobr></div>",
                ID,
                Settings.V4Page.Resx.GetString("Inv_lblClearSort"));
            w.Write("</div>");
        }

        private void RenderColumnsFieldsFilter(Page page, bool renderField = true)
        {
            if (AddFilterFieldList == null) AddFilterFieldList = new List<string>();

            if (renderField)
            {
                var w = new StringWriter();
                RenderFilterBlock(w);
                page.JS.Write("$('#divColumnsFilterForm_Fields_{0}').html('{1}');",
                    Settings.GridId,
                    HttpUtility.JavaScriptStringEncode(w.ToString()));
            }

            var wv = new StringWriter();
            RenderFilterBlockValues(wv);
            page.JS.Write("$('#divColumnsFilterForm_Values_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(wv.ToString()));

            JS.Write("v4_grid.columnsFilterForm('{1}', {2}, 'imgFilterTools_{0}', '{3}');",
                ID,
                Settings.GridId,
                Settings.GridCmdListnerIndex,
                Settings.V4Page.Resx.GetString("lblFilter")
            );
        }

        private void RenderFilterBlock(TextWriter w)
        {
            if (TableColumnsFilter == null) return;

            w.Write("<table id='FilterFieldTable_{0}' width='98%'><tbody>", Settings.GridId);

            foreach (var clmn in TableColumnsFilter.OrderBy(c => c.Alias))
            {
                if (clmn.FilterRequired) continue;
                var chk = "";
                if (clmn.FilterUser != null)
                {
                    chk = "checked";
                }
                else if (clmn.FilterUniqueValues != null && clmn.FilterUniqueValues.Count > 0)
                {
                    chk = "checked";
                }
                else if (AddFilterFieldList != null && AddFilterFieldList.Contains(clmn.Id))
                {
                    chk = "checked";
                }

                var onclick = $"cmd('cmd', 'Listener', 'ctrlId', {Settings.GridCmdListnerIndex}, 'cmdName', '{ (chk=="checked"?"DeleteFilterField":"AddFilterField")}', 'ColumnId', '{clmn.Id}');";

                w.Write("<tr column-id='{0}'>", clmn.Id);
                w.Write("<td><input type='checkbox' id='cfAllField' {0} onclick=\"{1}\"></td>", chk, onclick);
                w.Write("<td class=\"v4PaddingCell\" style=\"text-align:left; white-space: nowrap; padding-left: 5px;\">");
                w.Write(clmn.Alias);
                w.Write("</td>");
                w.Write("</tr>");
            }

            w.Write("</tbody></table>");
        }

        private void RenderFilterBlockValues(TextWriter w)
        {
            if (TableColumnsFilter == null) return;

            w.Write("<table id='FilterFieldValuesTable_{0}' width='98%'><tbody>", Settings.GridId);

            foreach (var clmn in AddFilterFieldList)
            {
                var column = TableColumnsFilter.FirstOrDefault(c => c.Id == clmn);

                if (column != null)
                {
                    var filterClick = string.Join(" ", "style=\"cursor:pointer\"", " ",
                        string.Format("onclick=\"v4_grid.openUserFilterFormForSettingCmd({0}, '{1}', {2}, {3});\"",
                            Settings.GridCmdListnerIndex,
                            column.Id,
                            -1,
                            0
                        ));
                    w.Write("<tr column-id='{0}'>", column.Id);
                    w.Write(
                        "<td class=\"v4PaddingCell\" style=\"text-align:left; white-space: nowrap; padding-left: 5px;\">");
                    w.Write(column.Alias + "&nbsp;");
                    if (!column.FilterRequired) w.Write("<a " + filterClick + ">");
                    w.Write("<span class='gridFilterValue'>{0}</span>", Resx.GetString("ppBtnChoose"));
                    if (!column.FilterRequired) w.Write("</a>");
                    w.Write("</td>");
                    w.Write("</tr>");
                }
            }

            foreach (var clmn in TableColumnsFilter.OrderBy(c => c.DisplayOrder))
            {
                if (clmn.FilterUser != null)
                {
                    var filterClick = string.Join(" ", "style=\"cursor:pointer\"", " ",
                        string.Format("onclick=\"v4_grid.openUserFilterFormForSettingCmd({0}, '{1}', {2}, {3});\"",
                            Settings.GridCmdListnerIndex,
                            clmn.Id,
                            (int)clmn.FilterUser.FilterType,
                            1
                        ));

                    w.Write("<tr column-id='{0}'>", clmn.Id);
                    w.Write("<td class=\"v4PaddingCell\" style=\"text-align:left; white-space: nowrap; padding: 5px;\">");

                    if (!clmn.FilterRequired) w.Write("<a " + filterClick + ">");
                    clmn.RenderTextUserFilterBlock(w, false, false);
                    if (!clmn.FilterRequired) w.Write("</a>");

                    w.Write("</td>");
                    w.Write("</tr>");
                }
                else if (clmn.FilterUniqueValues != null && clmn.FilterUniqueValues.Count > 0 && (clmn.ColumnType == GridColumnTypeEnum.Boolean || clmn.ColumnType == GridColumnTypeEnum.List))
                {
                    var filterClick = string.Join(" ", "style=\"cursor:pointer\"", " ",
                        string.Format(
                            "onclick=\"cmdasync('cmd', 'Listener', 'ctrlId', {0}, 'cmdName', 'RenderColumnSettings','ColumnId','{1}','ChangeFilter','False');\"",
                            Settings.GridCmdListnerIndex, clmn.FieldName));

                    w.Write("<tr column-id='{0}'>", clmn.Id);
                    w.Write("<td class=\"v4PaddingCell\" style=\"text-align:left; white-space: nowrap; padding: 5px;\">");
                    if (!clmn.FilterRequired) w.Write("<a " + filterClick + ">");
                    clmn.RenderFilterBlockTextValues(w);
                    if (!clmn.FilterRequired) w.Write("</a>");
                    w.Write("</td>");
                    w.Write("</tr>");
                }
            }

            w.Write("</tbody></table>");
        }

        private void RenderGroupTr(TextWriter w, int inx, DataRow dr, GridColumn clmn, bool advEmptyClmn,
            int countRecord, string groupId, int groupIndex, string groupParent, bool noRenderItemsData)
        {
            var colspan = 0;
            var cntDisplay = Settings.TableColumns.Count(x => x.DisplayVisible);
            if (ExistServiceColumn) colspan++;
            if (ExistServiceColumnDetail) colspan++;
            if (advEmptyClmn) colspan++;

            colspan = colspan + cntDisplay - inx - 1;

            var noGroup = noRenderItemsData && groupIndex == Settings.GroupingColumns.Count - 1;

            w.Write("<tr class=\"grouping\" {0} group-index=\"{1}\" group-parent=\"{2}\">",
                noGroup ? "" : string.Format("group-id=\"{0}\"", groupId), groupIndex, groupParent);
            for (var i = 0; i < inx; i++)
                w.Write("<td>&nbsp;</td>");

            if (noGroup)
                w.Write("<td></td>");
            else
                w.Write(
                    "<td><span id=\"v4sp_{2}\" style=\"cursor:pointer;\" onclick=\"v4_grid.displayGroupById('{0}',{1},'{2}','v4sp');\" title=\"{3}\" class=\"gridNode ui-icon ui-icon-triangle-1-se\" ></span></td>",
                    ID, GridCmdListnerIndex, groupId, Resx.GetString("lblGridGroupExpand"));

            var _colspan = colspan > 1 ? string.Format("colspan=\"{0}\"", colspan) : "";
            var _beforeText = clmn.Alias + ": ";
            var _afterText =
                string.Format("<span style=\"float:right;color:dimgray\" title=\"{1}\">[{0}]</span>",  countRecord, Resx.GetString("lblGridRecordCountGroup"));

            clmn.RenderColumnData(w, dr, 0, _colspan, _beforeText, _afterText, true);

            w.Write("</tr>");
        }

        /// <summary>
        ///     Наложение ограничений по установленным пользовательским фильтрам
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private DataTable ApplyUserFilter(DataTable dt, List<GridColumn> columns)
        {
            columns.ForEach(delegate(GridColumn clmn)
            {
                var filter = clmn.FilterUser;
                var applyfilter = from r in dt.AsEnumerable()
                    where
                        filter.FilterType == GridColumnUserFilterEnum.Указано &&
                        r.Field<object>(clmn.FilterFieldName) != null && !r.Field<object>(clmn.FilterFieldName).Equals("")
                        || 

                        filter.FilterType == GridColumnUserFilterEnum.НеУказано &&
                        (r.Field<object>(clmn.FilterFieldName) == null ||
                         clmn.ColumnType == GridColumnTypeEnum.String &&
                         r.Field<object>(clmn.FilterFieldName).Equals(""))

                        || filter.FilterType == GridColumnUserFilterEnum.Равно &&
                        r.Field<object>(clmn.FilterFieldName) != null &&
                        r.Field<object>(clmn.FilterFieldName).Equals(filter.FilterValue1)
                        
                        || filter.FilterType == GridColumnUserFilterEnum.НеРавно &&
                        r.Field<object>(clmn.FilterFieldName) != null &&
                        !r.Field<object>(clmn.FilterFieldName).Equals(filter.FilterValue1)
                        
                        || filter.FilterType == GridColumnUserFilterEnum.Между &&
                        r.Field<object>(clmn.FilterFieldName) != null &&
                        ((IComparable) filter.FilterValue1).CompareTo(r.Field<object>(clmn.FilterFieldName)) <= 0 &&
                        ((IComparable) r.Field<object>(clmn.FilterFieldName)).CompareTo(filter.FilterValue2) <= 0
                        
                        || filter.FilterType == GridColumnUserFilterEnum.БольшеИлиРавно &&
                        r.Field<object>(clmn.FilterFieldName) != null &&
                        ((IComparable) filter.FilterValue1).CompareTo(r.Field<object>(clmn.FilterFieldName)) <= 0
                        
                        || filter.FilterType == GridColumnUserFilterEnum.МеньшеИлиРавно &&
                        r.Field<object>(clmn.FilterFieldName) != null &&
                        ((IComparable) r.Field<object>(clmn.FilterFieldName)).CompareTo(filter.FilterValue1) <= 0
                        
                        || filter.FilterType == GridColumnUserFilterEnum.Больше &&
                        r.Field<object>(clmn.FilterFieldName) != null &&
                        ((IComparable) filter.FilterValue1).CompareTo(r.Field<object>(clmn.FilterFieldName)) < 0
                        
                        || filter.FilterType == GridColumnUserFilterEnum.Меньше &&
                        r.Field<object>(clmn.FilterFieldName) != null &&
                        ((IComparable) r.Field<object>(clmn.FilterFieldName)).CompareTo(filter.FilterValue1) < 0
                        
                        || filter.FilterType == GridColumnUserFilterEnum.Содержит &&
                        r.Field<object>(clmn.FilterFieldName) != null && r.Field<string>(clmn.FilterFieldName)
                            .IndexOf(filter.FilterValue1.ToString(), StringComparison.OrdinalIgnoreCase) >= 0
                       
                        || filter.FilterType == GridColumnUserFilterEnum.НеСодержит &&
                        r.Field<object>(clmn.FilterFieldName) != null && r.Field<string>(clmn.FilterFieldName)
                            .IndexOf(filter.FilterValue1.ToString(), StringComparison.OrdinalIgnoreCase) < 0
                        
                        || filter.FilterType == GridColumnUserFilterEnum.НачинаетсяС &&
                        r.Field<object>(clmn.FilterFieldName) != null && r.Field<string>(clmn.FilterFieldName)
                            .StartsWith(filter.FilterValue1.ToString(), StringComparison.OrdinalIgnoreCase)
                        
                        || filter.FilterType == GridColumnUserFilterEnum.ЗаканчиваетсяНа &&
                        r.Field<object>(clmn.FilterFieldName) != null && r.Field<string>(clmn.FilterFieldName)
                            .EndsWith(filter.FilterValue1.ToString(), StringComparison.OrdinalIgnoreCase)

                        || filter.FilterType == GridColumnUserFilterEnum.НеНачинаетсяС &&
                        r.Field<object>(clmn.FilterFieldName) != null && !r.Field<string>(clmn.FilterFieldName)
                            .StartsWith(filter.FilterValue1.ToString(), StringComparison.OrdinalIgnoreCase)

                        || filter.FilterType == GridColumnUserFilterEnum.НеЗаканчиваетсяНа &&
                        r.Field<object>(clmn.FilterFieldName) != null && !r.Field<string>(clmn.FilterFieldName)
                            .EndsWith(filter.FilterValue1.ToString(), StringComparison.OrdinalIgnoreCase)

                    select r;

                if (applyfilter.Any())
                    dt = applyfilter.CopyToDataTable();
                else
                    dt.Clear();
            });

            return dt;
        }

        private string GetUserFilterWhereClauseSqlString(List<GridColumn> columns, Dictionary<string, object> sqlParams)
        {
            var clauses = new List<string>();

            columns.ForEach(delegate(GridColumn clmn)
            {
                var filter = clmn.FilterUser;
               
                var sb = new StringBuilder();

                sb.Append("(");
                
                var fieldName = clmn.FilterFieldName;

                switch (filter.FilterType)
                {
                    case GridColumnUserFilterEnum.Указано:

                        sb.Append($"[{fieldName}] IS NOT NULL");
                        if (clmn.ColumnType == GridColumnTypeEnum.String)
                            sb.Append($" AND [{fieldName}] <> ''");

                        break;
                    case GridColumnUserFilterEnum.НеУказано:

                        sb.Append($"[{fieldName}] IS NULL");
                        if (clmn.ColumnType == GridColumnTypeEnum.String)
                            sb.Append($" OR [{fieldName}] = ''");
                        
                        break;
                    case GridColumnUserFilterEnum.Равно:

                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);

                        if (clmn.ColumnType == GridColumnTypeEnum.Date)
                            sb.Append($"CONVERT(date,{fieldName}) = @value1_{clmn.DisplayOrder}");
                        else
                            sb.Append($"[{fieldName}] = @value1_{clmn.DisplayOrder}");

                        break;
                    case GridColumnUserFilterEnum.НеРавно:

                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        if (clmn.ColumnType == GridColumnTypeEnum.Date)
                            sb.Append($"CONVERT(date,{fieldName}) <> @value1_{clmn.DisplayOrder}");
                        else
                            sb.Append($"[{fieldName}] <> @value1_{clmn.DisplayOrder}");

                        break;
                    case GridColumnUserFilterEnum.Между:

                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        sqlParams.Add($"@value2_{clmn.DisplayOrder}", filter.ComputeFilterValue2);
                        if (clmn.ColumnType == GridColumnTypeEnum.Date)
                            sb.Append($"CONVERT(date,{fieldName}) >= @value1_{clmn.DisplayOrder} AND CONVERT(date,{fieldName}) <= @value2_{clmn.DisplayOrder}");
                        else
                            sb.Append($"[{fieldName}] >= @value1_{clmn.DisplayOrder} AND [{fieldName}] <= @value2_{clmn.DisplayOrder}");

                        break;
                    case GridColumnUserFilterEnum.БольшеИлиРавно:

                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        if (clmn.ColumnType == GridColumnTypeEnum.Date)
                            sb.Append($"CONVERT(date,{fieldName}) >= @value1_{clmn.DisplayOrder}");
                        else
                            sb.Append($"[{fieldName}] >= @value1_{clmn.DisplayOrder}");

                        break;
                    case GridColumnUserFilterEnum.МеньшеИлиРавно:
                        
                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        if (clmn.ColumnType == GridColumnTypeEnum.Date)
                            sb.Append($"CONVERT(date,{fieldName}) <= @value1_{clmn.DisplayOrder}");
                        else
                            sb.Append($"[{fieldName}] <= @value1_{clmn.DisplayOrder}");

                        break;
                    case GridColumnUserFilterEnum.Больше:
                        
                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        if (clmn.ColumnType == GridColumnTypeEnum.Date)
                            sb.Append($"CONVERT(date,{fieldName}) > @value1_{clmn.DisplayOrder}");
                        else
                            sb.Append($"[{fieldName}] > @value1_{clmn.DisplayOrder}");

                        break;
                    case GridColumnUserFilterEnum.Меньше:
                        
                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        if (clmn.ColumnType == GridColumnTypeEnum.Date)
                            sb.Append($"CONVERT(date,{fieldName}) < @value1_{clmn.DisplayOrder}");
                        else
                            sb.Append($"[{fieldName}] < @value1_{clmn.DisplayOrder}");

                        break;
                    case GridColumnUserFilterEnum.Содержит:
                        
                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        if (clmn.ColumnType == GridColumnTypeEnum.Date)
                            sb.Append($"CONVERT(date,{fieldName}) LIKE '%' + @value1_{clmn.DisplayOrder} +'%'");
                        else
                            sb.Append($"[{fieldName}] LIKE '%' + @value1_{clmn.DisplayOrder} +'%'");

                        break;
                    case GridColumnUserFilterEnum.НеСодержит:

                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        if (clmn.ColumnType == GridColumnTypeEnum.Date)
                            sb.Append($"CONVERT(date,{fieldName}) NOT LIKE '%' + @value1_{clmn.DisplayOrder} +'%'");
                        else
                            sb.Append($"[{fieldName}] NOT LIKE '%' + @value1_{clmn.DisplayOrder} +'%'");

                        break;
                    case GridColumnUserFilterEnum.НачинаетсяС:

                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        sb.Append($"[{fieldName}] LIKE @value1_{clmn.DisplayOrder} +'%'");

                        break;
                    case GridColumnUserFilterEnum.ЗаканчиваетсяНа:

                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        sb.Append($"[{fieldName}] LIKE '%' + @value1_{clmn.DisplayOrder}");

                        break;
                    case GridColumnUserFilterEnum.НеНачинаетсяС:

                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        sb.Append($"[{fieldName}] NOT LIKE @value1_{clmn.DisplayOrder} +'%'");

                        break;
                    case GridColumnUserFilterEnum.НеЗаканчиваетсяНа:

                        sqlParams.Add($"@value1_{clmn.DisplayOrder}", filter.ComputeFilterValue1);
                        sb.Append($"[{fieldName}] NOT LIKE '%' + @value1_{clmn.DisplayOrder}");

                        break;
                }

                sb.Append(")");
                clauses.Add(sb.ToString());
            });
            
            return String.Join(" AND ", clauses.ToArray());
        }

        private string GetUniqueValuesWhereClauseSqlString(List<GridColumn> columns, Dictionary<string, object> sqlParams)
        {
            var clauses = new List<string>();

            columns.ForEach(delegate(GridColumn clmn)
            {
                var sb = new StringBuilder();

                if (clmn.ColumnType == GridColumnTypeEnum.Boolean)
                {
                    sb.Append("(");
                    var filter = clmn.FilterUniqueValues;

                    if (clmn.FormatString != "")
                        if (filter.Values.First().ToString() == "1")
                            sb.Append(clmn.FormatString);
                        else
                            sb.Append("NOT " + clmn.FormatString);
                    else
                        sb.Append($"[{clmn.FilterFieldName}] = {filter.Values.First()}");

                    sb.Append(")");
                }
                else if (clmn.ColumnType == GridColumnTypeEnum.List)
                {
                    sb.Append("(");
                    var filter = clmn.FilterUniqueValues;

                    var filterVal = "";
                    foreach (var filterValue in filter)
                    {
                        if (filterVal != "") filterVal += ",";
                        filterVal += filterValue.Value;
                    }

                    sb.Append($"[{clmn.FilterFieldName}] ");
                    if (clmn.FilterEqual == GridColumnFilterEqualEnum.NotIn)
                    {
                        sb.Append($" NOT ");
                    }
                    sb.Append($" IN ({filterVal})");
                    sb.Append(")");
                }

                clauses.Add(sb.ToString());
            });

            return String.Join(" AND ", clauses.ToArray());
        }

        /// <summary>
        ///     Создание пользовательского фильтра
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="filterId"></param>
        private void SetFilterColumnByUser(bool changeFilter, string columnId, string filterId)
        {
            var clmn = Settings.TableColumns.FirstOrDefault(x => x.Id == columnId);

            if (!changeFilter)
                clmn = TableColumnsFilter.FirstOrDefault(x => x.Id == columnId);

            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumnFound"), Resx.GetString("alertError"),
                    MessageStatus.Error);
                return;
            }

            var filter = (GridColumnUserFilterEnum) int.Parse(filterId);
            object objField1 = null;
            object objField2 = null;

            if (filter != GridColumnUserFilterEnum.НеУказано && filter != GridColumnUserFilterEnum.Указано)
            {
                objField1 = GetFilterUserControlValue(clmn, clmn.FilterUserCtrlBaseName + "_" + ID + "_1");
                if (filter == GridColumnUserFilterEnum.Между)
                    objField2 = GetFilterUserControlValue(clmn, clmn.FilterUserCtrlBaseName + "_" + ID + "_2");
            }

            if (clmn.ColumnType == GridColumnTypeEnum.Date)
            {
                clmn.FilterUser = new GridColumnUserFilterDate
                {
                    IsCurrentDate = clmn.IsCurrentDate,
                    FilterType = filter,
                    FilterValue1 = clmn.IsCurrentDate ? objField1 ?? 0 : objField1,
                    FilterValue2 = clmn.IsCurrentDate ? objField2 ?? 0 : objField2
                };
            }
            else
            {
                clmn.FilterUser = new GridColumnUserFilter
                {
                    FilterType = filter,
                    FilterValue1 = objField1,
                    FilterValue2 = objField2
                };
            }
        }

        public void SaveVisibleAndOrderParam(bool clearAll = false)
        {
            
            var parametersManager = new AppParamsManager(Clid, new StringCollection());

            if (!string.IsNullOrEmpty(FieldsParamPrefix))
            {
                var gridVisibleAndOrderList = "";
                if (!clearAll)
                {
                    foreach (var column in Settings.TableColumns.FindAll(c => c.DisplayVisible).OrderBy(x => x.DisplayOrder))
                    {
                        if (!gridVisibleAndOrderList.IsNullEmptyOrZero()) gridVisibleAndOrderList += ",";
                        gridVisibleAndOrderList += column.FieldName;
                    }
                }

                parametersManager.Params.Add(new AppParameter(FieldsParamPrefix + QS_Id, gridVisibleAndOrderList, AppParamType.SavedWithClid));
                parametersManager.SaveParams();
            }
        }

        public void LoadVisibleAndOrderParam()
        {
            if (!string.IsNullOrEmpty(FieldsParamPrefix))
            {
                var gridVisibleAndOrderList = new List<string>();

                var parametersManager = new AppParamsManager(Clid, new StringCollection { FieldsParamPrefix + QS_Id });
                var appParam = parametersManager.Params.Find(x => x.Name == FieldsParamPrefix + QS_Id);
                if (appParam != null && appParam.Value != "")
                {
                    try
                    {
                        gridVisibleAndOrderList = appParam.Value.Split(',').ToList();
                    }
                    catch
                    {
                    }
                }

                Settings.ColumnsDisplayOrder = new List<GridColumn>(); 
                if (gridVisibleAndOrderList.Count > 0)
                {
                    var inx = 0;
                    gridVisibleAndOrderList.ForEach(x =>
                    {
                        var clmn = Settings.TableColumns.FirstOrDefault(y => y.FieldName == x);
                        if (clmn != null)
                        {
                            clmn.DisplayOrder = inx++;
                            clmn.DisplayVisible = true;
                            Settings.ColumnsDisplayOrder.Add(clmn);
                        }
                    });

                    foreach (var item in Settings.TableColumns.Except(Settings.ColumnsDisplayOrder))
                    {
                        item.DisplayOrder = inx++;
                        item.DisplayVisible = false;
                    }
                }
            }
        }

        public void SaveControlSettings(bool clearAll = false)
        {            
            var parametersManager = new AppParamsManager(Clid, new StringCollection());

            if (!string.IsNullOrEmpty(SortParamPrefix))
            {
                if (!clearAll)
                {
                    var gridSettingsSortList = new List<GridSettingsSort>();

                    foreach (var column in Settings.TableColumns)
                    {
                        if (!column.IsSaveSettings || column.IsDublicateColumn) continue;

                        if (column.OrderByNumber != null)
                        {
                            var settingsGridSort = new GridSettingsSort
                            {
                                FieldName = column.FieldName,
                                OrderByDirection = column.OrderByDirection,
                                OrderByNumber = column.OrderByNumber
                            };
                            gridSettingsSortList.Add(settingsGridSort);
                        }
                    }


                    if (gridSettingsSortList.Count > 0)
                    {
                        var userSort = new StringWriter();
                        var js = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
                        js.Serialize(gridSettingsSortList, userSort);
                        parametersManager.Params.Add(new AppParameter(SortParamPrefix, userSort.ToString(), AppParamType.SavedWithClid));
                    }
                    else
                        parametersManager.Params.Add(new AppParameter(SortParamPrefix, "", AppParamType.SavedWithClid));
                }else
                    parametersManager.Params.Add(new AppParameter(SortParamPrefix, "", AppParamType.SavedWithClid));

                parametersManager.SaveParams();
            }

            if (!string.IsNullOrEmpty(FilterParamPrefix))
            {
                if (!clearAll)
                {
                    var gridSettingsUserFilterList = new List<GridSettingsUserFilter>();

                    foreach (var column in Settings.TableColumns)
                    {
                        if (!column.IsSaveSettings) continue;

                        if (column.FilterUser != null || column.FilterUniqueValues != null)
                        {
                            if (column.FilterRequired) continue;

                            var settingsGridFilter = new GridSettingsUserFilter
                            {
                                FieldName = column.FieldName,
                                FilterUser = column.FilterUser,
                                FilterUniqueValues = column.FilterUniqueValues
                            };

                            if (column.FilterUniqueValues != null)
                                settingsGridFilter.FilterEqual = column.FilterEqual;
                            else
                                settingsGridFilter.FilterEqual = null;


                            gridSettingsUserFilterList.Add(settingsGridFilter);
                        }
                    }


                    if (gridSettingsUserFilterList.Count > 0)
                    {
                        var userFilter = new StringWriter();
                        var js = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
                        js.Serialize(gridSettingsUserFilterList, userFilter);
                        parametersManager.Params.Add(new AppParameter(FilterParamPrefix, userFilter.ToString(), AppParamType.SavedWithClid));
                    }
                    else
                        parametersManager.Params.Add(new AppParameter(FilterParamPrefix, "", AppParamType.SavedWithClid));
                }
                else
                    parametersManager.Params.Add(new AppParameter(FilterParamPrefix, "", AppParamType.SavedWithClid));

                parametersManager.SaveParams();
            }
        }

        public void SaveRowsControlSettings(int rowsPerPage)
        {            
            var parametersManager = new AppParamsManager(Clid, new StringCollection());

            if (!string.IsNullOrEmpty(RowParamPrefix))
            {
                parametersManager.Params.Add(new AppParameter(RowParamPrefix, rowsPerPage.ToString(), AppParamType.SavedWithClid));
                parametersManager.SaveParams();
            }
        }

        public void LoadControlSettings()
        {
            
            if (!string.IsNullOrEmpty(SortParamPrefix))
            {
                List<GridSettingsSort> gridSettingsSortList = null;
                
                var parametersManager = new AppParamsManager(Clid, new StringCollection { SortParamPrefix });
                var appParam = parametersManager.Params.Find(x => x.Name == SortParamPrefix);
                if (appParam != null && appParam.Value != "")
                {
                    try
                    {
                        gridSettingsSortList = JsonConvert.DeserializeObject<List<GridSettingsSort>>(appParam.Value);
                    }
                    catch{
                    }
                }

                if (gridSettingsSortList != null)
                {
                    foreach (var column in Settings.TableColumns)
                    {
                        if (!column.IsSaveSettings) continue;
                        if (gridSettingsSortList.Any(e => e.FieldName == column.FieldName))
                        {
                            var orderByDirection = gridSettingsSortList.First(e => e.FieldName == column.FieldName).OrderByDirection;
                            {
                                column.OrderByDirection = orderByDirection;
                            }

                            var orderByNumber = gridSettingsSortList.First(e => e.FieldName == column.FieldName).OrderByNumber;
                            if (orderByNumber != null)
                            {
                                column.OrderByNumber = orderByNumber;
                            }

                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(FilterParamPrefix))
            {
                List<GridSettingsUserFilter> gridSettingsUserFilterList = null;

                var parametersManager = new AppParamsManager(Clid, new StringCollection { FilterParamPrefix });
                var appParam = parametersManager.Params.Find(x => x.Name == FilterParamPrefix);
                if (appParam != null && appParam.Value != "")
                {
                    try
                    {
                        gridSettingsUserFilterList = JsonConvert.DeserializeObject<List<GridSettingsUserFilter>>(appParam.Value, new GridColumnUserFilterConverter());
                    }
                    catch{
                    }
                }

                if (gridSettingsUserFilterList != null)
                {
                    try
                    {
                        foreach (var column in Settings.TableColumns)
                        {
                            if (!column.IsSaveSettings) continue;
                            if (gridSettingsUserFilterList.Any(e => e.FieldName == column.FieldName))
                            {
                                var settingGrid = gridSettingsUserFilterList.First(e => e.FieldName == column.FieldName);
                                if (settingGrid != null)
                                {
                                    column.FilterUser = column.ColumnType == GridColumnTypeEnum.Date 
                                        ? ((GridSettingsUserFilterDate)settingGrid).FilterUser 
                                        : settingGrid.FilterUser;
                                }

                                var filterEqual = gridSettingsUserFilterList.First(e => e.FieldName == column.FieldName).FilterEqual;
                                column.FilterEqual = filterEqual;

                                if (column.UniqueValuesOriginal != null && (column.ColumnType == GridColumnTypeEnum.Boolean || column.ColumnType == GridColumnTypeEnum.List))
                                {
                                    var filterUniqueValues = gridSettingsUserFilterList.First(e => e.FieldName == column.FieldName).FilterUniqueValues;
                                    if (filterUniqueValues != null)
                                    {
                                        column.FilterUniqueValues = new Dictionary<object, object>();
                                        foreach (var item in filterUniqueValues)
                                        {
                                            column.FilterUniqueValues.Add(Convert.ToInt32(item.Key), Convert.ToByte(item.Value));
                                        }

                                    }
                                }
                            }
                        }
                    }
                    catch{
                    }
                }
            }

        }

        public int LoadRowsControlSettings()
        {            
            var rowsPerPage = 25;

            var parametersManager = new AppParamsManager(Clid, new StringCollection { RowParamPrefix });

            var appParam = parametersManager.Params.Find(x => x.Name == RowParamPrefix);
            if (appParam != null && appParam.Value != "")
            {
                try
                {
                    rowsPerPage = Convert.ToInt32(appParam.Value);
                }
                catch
                {
                }
            }

            return rowsPerPage;
        }


        /// <summary>
        ///     Установка колонок, по которым будет происходить группировка
        ///     Служебная, вызывается только с клиента
        /// </summary>
        /// <param name="columnIds">Идентификаторы колонок</param>
        private void SetGroupingColumns(string columnIds)
        {
            if (string.IsNullOrEmpty(columnIds))
            {
                if (Settings.GroupingColumns != null)
                    Settings.GroupingColumns.Clear();
                return;
            }

            var ids = columnIds.Split(',').ToList();
            Settings.GroupingColumns = new List<GridColumn>();

            ids.ForEach(x =>
            {
                var clmn = Settings.TableColumns.FirstOrDefault(y => y.Id == x);
                if (clmn != null)
                    Settings.GroupingColumns.Add(clmn);
            });
        }

        /// <summary>
        ///     Удаление колонки из списка группировки
        /// </summary>
        /// <param name="columnId">Идентификтатор колонки</param>
        private void RemoveGroupingColumn(string columnId)
        {
            if (Settings.GroupingColumns == null) return;
            var itemToRemove = Settings.GroupingColumns.SingleOrDefault(r => r.Id == columnId);
            if (itemToRemove != null)
                Settings.GroupingColumns.Remove(itemToRemove);
        }

        /// <summary>
        ///     Развернуть/Свернуть содержимое группы
        /// </summary>
        /// <param name="columnId">Идентификтатор колонки</param>
        private void GroupingExpandColumn(string columnId = "")
        {
            if (Settings.GroupingColumns == null || Settings.GroupingColumns.Count == 0) return;

            var itemToExpand = string.IsNullOrEmpty(columnId)
                ? Settings.GroupingColumns[Settings.GroupingColumns.Count - 1]
                : Settings.GroupingColumns.SingleOrDefault(r => r.Id == columnId);

            if (itemToExpand == null) return;

            Settings.GroupingExpandIndex = Settings.GroupingColumns.FindIndex(x => x.Id == itemToExpand.Id);

            if (!string.IsNullOrEmpty(columnId)) GroupingExpandColumnApply();
        }

        public void GroupingExpandColumnApply()
        {
            V4Page.JS.Write("v4_grid.displayGroupByIndex(\"{0}\",\"{1}\",{2});", ID, GridCmdListnerIndex,
                Settings.GroupingExpandIndex);
        }


        /// <summary>
        /// Перемещение колонки
        /// </summary>
        /// <param name="dropColumnId">Идентификатор колонки, которую тащим</param>
        /// <param name="targetColumnId">Идентификатор колонки, на которую бросаем</param>
        /// <returns>Удачно ли переместили</returns>
        private bool MoveColumn(string dropColumnId, string targetColumnId)
        {
            if (string.IsNullOrEmpty(dropColumnId) || string.IsNullOrEmpty(targetColumnId) || dropColumnId == targetColumnId) 
                return false;
            
            var order = 0;
            
            var clmnT = Settings.TableColumns.FirstOrDefault(x => x.Id == targetColumnId);
            var clmnD = Settings.TableColumns.FirstOrDefault(x => x.Id == dropColumnId);
            if (clmnT == null || clmnD == null)
                return false;


            if (clmnT.DisplayOrder == clmnD.DisplayOrder + 1)
            {
                order = clmnT.DisplayOrder;
                clmnT.DisplayOrder = clmnD.DisplayOrder;
                clmnD.DisplayOrder = order;
            }
            else
            {
                order = clmnT.DisplayOrder;
                clmnT.DisplayOrder++;
                clmnD.DisplayOrder = order;


                Settings.TableColumns.Where(x => x.DisplayOrder >= order + 1).OrderBy(x => x.DisplayOrder).ToList().ForEach(x =>
                {
                    if (x.Id == targetColumnId) return;
                    x.DisplayOrder++;
                });
            }

            return true;           
        }

        /// <summary>
        ///     Установка порядка вывода колонок таблицы
        /// </summary>
        /// <param name="columnIds">Идентификаторы колонок в требуемом порядке</param>
        private void SetColumnsOrder(string columnIds)
        {
            if (string.IsNullOrEmpty(columnIds))
            {
                if (Settings.GroupingColumns != null)
                    Settings.GroupingColumns.Clear();
                return;
            }

            var ids = columnIds.Split(',').ToList();
            var inx = 0;

            Settings.ColumnsDisplayOrder = new List<GridColumn>();
            ids.ForEach(x =>
            {
                var clmn = Settings.TableColumns.FirstOrDefault(y => y.Id == x);
                if (clmn != null)
                {
                    clmn.DisplayOrder = inx++;
                    Settings.ColumnsDisplayOrder.Add(clmn);
                }
            });

            foreach (var item in Settings.TableColumns.Except(Settings.ColumnsDisplayOrder)) item.DisplayOrder = inx++;
            
        }

        /// <summary>
        ///     Установка видимости колонок таблицы
        /// </summary>
        /// <param name="columnIds">Идентификаторы колонок в требуемом порядке</param>
        private void SetColumnsVisible(string columnIds)
        {
            if (string.IsNullOrEmpty(columnIds))
            {
                return;
            }

            var ids = columnIds.Split(',').ToList();

            foreach (var clmn in Settings.TableColumns)
            {
                clmn.DisplayVisible = ids.Contains(clmn.Id);
            }
        }

        /// <summary>
        ///     Установка видимости колонок таблицы по умолчанию
        /// </summary>        
        private void SetDefaultColumnsVisible()
        {
            foreach (var clmn in Settings.TableColumns)
            {
                clmn.DisplayVisible = clmn.DisplayVisibleDefault;
            }
        }

        /// <summary>
        ///     Установка порядка колонок таблицы по умолчанию
        /// </summary>        
        private void SetDefaultColumnsOrder()
        {
            foreach (var clmn in Settings.TableColumns)
            {
                clmn.DisplayOrder = clmn.DisplayOrderDefault;
            }
        }

        /// <summary>
        ///     Получение значений установленных фильтров
        /// </summary>
        /// <param name="clmn"></param>
        /// <param name="ctrlName"></param>
        /// <returns></returns>
        private object GetFilterUserControlValue(GridColumn clmn, string ctrlName)
        {
            object value = null;
            if (!V4Page.V4Controls.ContainsKey(ctrlName)) return value;

            var ctrl = V4Page.V4Controls[ctrlName];
            if (ctrl is DatePicker)
            {
                value = ((DatePicker) ctrl).ValueDate;
            }
            else if (ctrl is Number)
            {
                if (clmn.ColumnType == GridColumnTypeEnum.Int)
                {
                    value = ((Number) ctrl).ValueInt;
                }
                else if (clmn.ColumnType == GridColumnTypeEnum.Short)
                {
                    value = ((Number)ctrl).ValueShort;
                }
                else if (clmn.ColumnType == GridColumnTypeEnum.Long)
                {
                    value = ((Number)ctrl).ValueLong;
                }
                else
                {
                    if (clmn.ColumnType == GridColumnTypeEnum.Double)
                    {
                        var valueDecimal = ((Number) ctrl).ValueDecimal;
                        if (valueDecimal != null) value = (double) valueDecimal;
                    }
                    else
                    {
                        value = ((Number) ctrl).ValueDecimal;
                    }
                }
            }
            else
            {
                value = ctrl.Value;
            }

            return value;
        }

        /// <summary>
        ///     Удаление установленных значений фильтров
        /// </summary>
        /// <param name="columnId"></param>
        private void ClearFilterColumnValues(string columnId)
        {
            var clmn = Settings.TableColumns.FirstOrDefault(x => x.Id == columnId);
            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"),
                    MessageStatus.Error);
                return;
            }

            clmn.FilterUser = null;
            clmn.FilterUniqueValues = null;

            Settings.TableColumns.FindAll(x => x.Id != columnId && x.FieldName == clmn.FieldName).ForEach(dubl =>
            {
                dubl.FilterUser = null;
                dubl.FilterUniqueValues = null;
            });
           
        }

        private void ClearFilterAllValues()
        {
            Settings.TableColumns.ForEach(clmn =>
            {
                if (!clmn.FilterRequired)
                {
                    clmn.FilterUser = null;
                    clmn.FilterUniqueValues = null;
                }
            });
        }

        /// <summary>
        ///     Установка фильтра по выбранному значению
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="data"></param>
        /// <param name="equals"></param>
        private void SetFilterByColumnValues(bool changeFilter, string columnId, string data, string equals)
        {
            var clmn = Settings.TableColumns.FirstOrDefault(x => x.Id == columnId);

            if (!changeFilter)
                clmn = TableColumnsFilter.FirstOrDefault(x => x.Id == columnId);

            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"),
                    MessageStatus.Error);
                return;
            }

            clmn.FilterEqual = equals != null && equals == "0"
                ? GridColumnFilterEqualEnum.NotIn
                : GridColumnFilterEqualEnum.In;

            if (clmn.FilterUniqueValues != null)
                clmn.FilterUniqueValues.Clear();

            if (!string.IsNullOrEmpty(data) && !data.Equals("All") && clmn.UniqueValuesOriginal != null)
            {
                var dataList = data.Split(',').ToList();

                clmn.FilterUniqueValues = clmn.UniqueValuesOriginal.Where(x => dataList.Contains(x.Key.ToString()))
                    .ToDictionary(x => x.Key, x => x.Value);
            }
        }

        /// <summary>
        ///     Сортировка
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="direction"></param>
        private void SetOrderByColumnValues(string columnId, GridColumnOrderByDirectionEnum direction)
        {
            var clmn = Settings.TableColumns.FirstOrDefault(x => x.Id == columnId);
            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"),
                    MessageStatus.Error);
                return;
            }

            clmn.OrderByNumber = 1;
            clmn.OrderByDirection = direction;
            SetOrderSortedColumn(2, clmn.Id);
        }

        /// <summary>
        ///     Установка порядка сортировки колонок
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="excludeColumnId"></param>
        private void SetOrderSortedColumn(int startIndex, string excludeColumnId)
        {
            Settings.TableColumns.Where(x => x.Id != excludeColumnId && x.OrderByNumber != null)
                .OrderBy(x => x.OrderByNumber)
                .ToList()
                .ForEach(clmn0 => clmn0.OrderByNumber = startIndex++);
        }

        /// <summary>
        ///     Отмена сортировки колонки
        /// </summary>
        /// <param name="columnId"></param>
        private void ClearOrderByColumnValues(string columnId)
        {
            var clmn = Settings.TableColumns.FirstOrDefault(x => x.Id == columnId);
            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"),
                    MessageStatus.Error);
                return;
            }

            clmn.OrderByNumber = null;
            SetOrderSortedColumn(1, columnId);
        }

        /// <summary>
        ///     Отмена сортировки всех колонок
        /// </summary>
        private void ClearOrderAllColumn()
        {
            Settings.TableColumns.ForEach(clmn =>
            {
                clmn.OrderByNumber = null;
            });
        }

        /// <summary>
        /// Установка сортировки колонок таблицы
        /// </summary>
        /// <param name="columnIds"></param>
        private void SetOrderColumns(string columnIds)
        {
            if (string.IsNullOrEmpty(columnIds))
            {
                return;
            }

            var colSortList = new List<KeyValuePair<string, string>>();
            foreach (var col in columnIds.Split(',').ToList())
            {
                var colOrder = col.Split(' ');
                colSortList.Add(new KeyValuePair<string, string>(colOrder[0], colOrder[1]));
            }

            var iCol = 1;
            foreach (var clmn in colSortList)
            {
                var tableColumns = Settings.TableColumns.Find(c => c.Id == clmn.Key);
                tableColumns.OrderByNumber = iCol;
                tableColumns.OrderByDirection = clmn.Value == "asc"
                    ? GridColumnOrderByDirectionEnum.Asc
                    : GridColumnOrderByDirectionEnum.Desc;
                iCol++;
            }
        }

        /// <summary>
        ///     Сортировка
        /// </summary>
        /// <param name="fieldName">Название поля</param>
        /// <param name="direction">Направление сортировки</param>
        public void SetOrderBy(string fieldName, GridColumnOrderByDirectionEnum direction)
        {
            var columnId = Settings?.TableColumns?.FirstOrDefault(x => x.FieldName == fieldName)?.Id;
            SetOrderByColumnValues(columnId, direction);
        }

        public decimal GetSumDecimalByColumnValue(string fieldName)
        {
            if (_dtLocal == null) return 0;
            return _dtLocal.AsEnumerable().Sum(f => f.Field<decimal>(fieldName));
        }

        /// <summary>
        ///     Переход на указанную страницу
        /// </summary>
        public void GoToPage(int pageNumber)
        {
            if (_currentPagingBarCtrl.CurrentPageNumber == pageNumber)
                return;

            if (pageNumber > _currentPagingBarCtrl.MaxPageNumber)
                pageNumber = _currentPagingBarCtrl.MaxPageNumber;

            _currentPagingBarCtrl.CurrentPageNumber = pageNumber;
            RefreshGridData();
        }

        /// <summary>
        ///     Переход на последнюю страницу
        /// </summary>
        public void GoToLastPage()
        {
            if (_currentPagingBarCtrl.CurrentPageNumber == _currentPagingBarCtrl.MaxPageNumber)
                return;
            _currentPagingBarCtrl.CurrentPageNumber = _currentPagingBarCtrl.MaxPageNumber;
            RefreshGridData();
        }

        /// <summary>
        ///     Возвращает текущую страницу
        /// </summary>
        public int GеtCurrentPage()
        {
            return _currentPagingBarCtrl.CurrentPageNumber;
        }

        /// <summary>
        ///     Возвращает количество записей
        /// </summary>
        public int GеtRowCount()
        {
            return _dtLocal == null ? 0 : _dtLocal.Rows.Count;
        }

        /// <summary>
        ///     Проверка значения ячейки таблицы на удовлетворение условию
        /// </summary>
        /// <param name="dr">Текущая запись</param>
        /// <param name="columnId">Идентификтатор колонки</param>
        /// <param name="values">Список условий</param>
        /// <returns></returns>
        private bool CheckCellValueOnCondition(DataRow dr, string columnId, List<object> values)
        {
            return !values.Any(x => !x.Equals(dr[columnId]));
        }

        #region ServiceColumn

        private string _returnClientFuncName = "";
        private List<string> _returnPkFieldsName;
        private string _returnTitle = "возврат";

        private bool _existServiceColumnAdd;
        private string _addClientFuncName = "";
        private List<string> _addPkFieldsName;
        private string _addTitle = "добавить";

        private bool _existServiceColumnEdit;
        private string _editClientFuncName = "";
        private List<string> _editPkFieldsName;
        private string _editTitle = "редактировать";

        private bool _existServiceColumnCopy;
        private string _copyClientFuncName = "";
        private List<string> _copyPkFieldsName;
        private string _copyTitle = "копировать";

        private bool _existServiceColumnCommandMenu;
        private string _commandMenuClientFuncName = "";
        private List<string> _commandMenuPkFieldsName;
        private string _commandMenuTitle = "операции";

        private bool _existServiceColumnDelete;
        private string _deleteClientFuncName = "";
        private List<string> _deletePkFieldsName;
        private List<string> _deleteMessageFieldsName;
        private string _deleteTitle = "удалить";

        private bool _existServiceColumnChecked;
        private List<string> _checkedFieldsName;

        /// <summary>
        ///     Свойство указывающее, что у грида есть колонка с управляющими иконками
        ///     на текущий момент реализовано редактирование, копирование, удаление
        /// </summary>
        public bool ExistServiceColumn { get; set; }


        /// <summary>
        ///     Свойство указывающее, что у грида есть колонка с управляющей иконкой детализации
        /// </summary>
        public bool ExistServiceColumnDetail { get; set; }

        /// <summary>
        ///     Свойство указывающее, что у грида есть колонка с управляющей иконкой возврата значения
        /// </summary>
        public bool ExistServiceColumnReturn { get; set; }

        /// <summary>
        ///     Свойство, указывающее при каком условии рисовать кнопку возврата значения в таблице
        /// </summary>
        public Dictionary<string, List<object>> RenderConditionServiceColumnReturn { get; set; }

        /// <summary>
        ///     Свойство, указывающее при каком условии рисовать кнопку редактирования записи в таблице
        /// </summary>
        public Dictionary<string, List<object>> RenderConditionServiceColumnEdit { get; set; }

        /// <summary>
        ///     Свойство, указывающее при каком условии рисовать кнопку удаления записи в таблице
        /// </summary>
        public Dictionary<string, List<object>> RenderConditionServiceColumnDelete { get; set; }

        /// <summary>
        ///     Свойство, указывающее при каком условии рисовать кнопку детализации записи
        /// </summary>
        public Dictionary<string, List<object>> RenderConditionServiceColumnDetail { get; set; }
        
        /// <summary>
        ///     Настройка кнопки возврата значения записи из таблицы
        /// </summary>
        /// <param name="clientFuncName">Клиентская функция, которая будет вызываться при нажатии на иконку детализации</param>
        /// <param name="pkFieldsName">Параметры клиентской функции</param>
        /// <param name="title">Всплывающая подсказка</param>
        public void SetServiceColumnReturn(string clientFuncName, List<string> pkFieldsName, string title = "")
        {
            _returnClientFuncName = clientFuncName;
            _returnPkFieldsName = pkFieldsName;
            _returnTitle = title;
        }

        /// <summary>
        ///     Настройка кнопки добавления записи в таблице
        /// </summary>
        /// <param name="clientFuncName">Клиентская функция, которая будет вызываться при нажатии на иконку добавления</param>
        /// <param name="title">Всплывающая подсказка</param>
        public void SetServiceColumnAdd(string clientFuncName, string title = "")
        {
            SetServiceColumnAdd(clientFuncName, null, title);
        }

        /// <summary>
        ///     Настройка кнопки добавления записи в таблице
        /// </summary>
        /// <param name="clientFuncName">Клиентская функция, которая будет вызываться при нажатии на иконку добавления</param>
        /// <param name="pkFieldsName">Параметры клиентской функции</param>
        /// <param name="title">Всплывающая подсказка</param>
        public void SetServiceColumnAdd(string clientFuncName, List<string> pkFieldsName, string title = "")
        {
            _existServiceColumnAdd = true;
            _addClientFuncName = clientFuncName;
            _addPkFieldsName = pkFieldsName;
            _addTitle = title;
        }

        /// <summary>
        ///     Настройка кнопки редактирования записи в таблице
        /// </summary>
        /// <param name="clientFuncName">Клиентская функция, которая будет вызываться при нажатии на иконку редактирования</param>
        /// <param name="pkFieldsName">Параметры клиентской функции</param>
        /// <param name="title">Всплывающая подсказка</param>
        public void SetServiceColumnEdit(string clientFuncName, List<string> pkFieldsName, string title = "")
        {
            _existServiceColumnEdit = true;
            _editClientFuncName = clientFuncName;
            _editPkFieldsName = pkFieldsName;
            _editTitle = title;
        }

        /// <summary>
        ///     Включить подсказку на иконке Редактировать запись
        ///     с информацией о том, кем и когда была изменена запись
        /// </summary>
        /// <param name="modifyUserColumn">Название поля с пользователем, изменившим запись</param>
        /// <param name="modifyDateColumn">Название поля с датой последнего изменения</param>
        public void SetModifyInfoTooltip(string modifyUserColumn, string modifyDateColumn)
        {
            _showModifyInfoTooltip = true;
            _modifyUserColumn = modifyUserColumn;
            _modifyDateColumn = modifyDateColumn;
        }

        /// <summary>
        ///     Настройка кнопки копирования записи в таблице
        /// </summary>
        /// <param name="clientFuncName">Клиентская функция, которая будет вызываться при нажатии на иконку копирования</param>
        /// <param name="pkFieldsName">Параметры клиентской функции</param>
        /// <param name="title">Всплывающая подсказка</param>
        public void SetServiceColumnCopy(string clientFuncName, List<string> pkFieldsName, string title = "")
        {
            _existServiceColumnCopy = true;
            _copyClientFuncName = clientFuncName;
            _copyPkFieldsName = pkFieldsName;
            _copyTitle = title;
        }

        /// <summary>
        ///     Настройка кнопки меню операций в таблице
        /// </summary>
        /// <param name="clientFuncName">Клиентская функция, которая будет вызываться при нажатии на иконку меню операций</param>
        /// <param name="pkFieldsName">Параметры клиентской функции</param>
        /// <param name="title">Всплывающая подсказка</param>
        public void SetServiceColumnCommandMenu(string clientFuncName, List<string> pkFieldsName, string title = "")
        {
            _existServiceColumnCommandMenu = true;
            _commandMenuClientFuncName = clientFuncName;
            _commandMenuPkFieldsName = pkFieldsName;
            _commandMenuTitle = title;
        }

        /// <summary>
        ///     Настройка кнопки удаления записи в таблице
        /// </summary>
        /// <param name="clientFuncName">Клиентская функция, которая будет вызываться при нажатии на иконку удаления</param>
        /// <param name="pkFieldsName">Параметры клиентской функции</param>
        /// <param name="title">Всплывающая подсказка</param>
        public void SetServiceColumnDelete(string clientFuncName, List<string> pkFieldsName,
            List<string> messageFieldsName, string title = "")
        {
            _existServiceColumnDelete = true;
            _deleteClientFuncName = clientFuncName;
            _deletePkFieldsName = pkFieldsName;
            _deleteMessageFieldsName = messageFieldsName;
            _deleteTitle = title;
        }


        public void SetServiceColumnChecked(List<string> checkedFieldsName)
        {
            _existServiceColumnChecked = true;
            _checkedFieldsName = checkedFieldsName;
        }


        #region Render Service Column

        private void RenderServiceColumnReturn(TextWriter w, DataRow dr, int tabIndex)
        {
            var clientParams = "";
            _returnPkFieldsName.ForEach(delegate(string fieldName)
            {
                clientParams += (clientParams.Length > 0 ? "," : "") + string.Format("'{0}'",
                                    HttpUtility.JavaScriptStringEncode(dr[fieldName].ToString()));
            });

            w.Write("<div class=\"v4DivTableCell\">");
            w.Write(
                "<img src=\"/styles/BackToList.gif\" border=\"0\" style=\"cursor:pointer;\" title='{0}' onclick=\"{1}\" tabindex=\"{2}\">",
                HttpUtility.HtmlEncode(_returnTitle),
                string.Format("{0}({1});", _returnClientFuncName, clientParams),
                tabIndex * 10 + 100 + 1);
            w.Write("</div>");
        }

        private void RenderServiceColumnCopy(TextWriter w, DataRow dr, int tabIndex)
        {
            var clientParams = "";
            _copyPkFieldsName.ForEach(delegate(string fieldName)
            {
                clientParams += (clientParams.Length > 0 ? "," : "") + string.Format("'{0}'",
                                    HttpUtility.JavaScriptStringEncode(dr[fieldName].ToString()));
            });

            w.Write("<div class=\"v4DivTableCell\">");
            w.Write(
                "<img src=\"/styles/copy.gif\" border=\"0\" style=\"cursor:pointer;\" title='{0}' onclick=\"{1}\" tabindex=\"{2}\">",
                HttpUtility.HtmlEncode(_copyTitle),
                string.Format("{0}({1});", _copyClientFuncName, clientParams),
                tabIndex * 10 + 100 + 2);
            w.Write("</div>");
        }

        private void RenderServiceColumnCommandMenu(TextWriter w, DataRow dr, int tabIndex)
        {
            var clientParams = "";
            _commandMenuPkFieldsName.ForEach(delegate (string fieldName)
            {
                clientParams += (clientParams.Length > 0 ? "," : "") + string.Format("'{0}'",
                                    HttpUtility.JavaScriptStringEncode(dr[fieldName].ToString()));
            });

            w.Write("<div class=\"v4DivTableCell\">");
            w.Write(
                "<img id=\"commandMenuIcon_{3}\" class=\"commandMenuIcon\"  src=\"/styles/CommandMenu.gif\" border=\"0\" style=\"cursor:pointer;\" title='{0}' onclick=\"{1}\" tabindex=\"{2}\">",
                HttpUtility.HtmlEncode(_commandMenuTitle),
                string.Format("{0}({1});", _commandMenuClientFuncName, clientParams),
                tabIndex * 10 + 100 + 2,
                dr[_commandMenuPkFieldsName[0]]);
            w.Write("</div>");
        }

        private void RenderServiceColumnDelete(TextWriter w, DataRow dr, int tabIndex)
        {
            var messageFields = "";
            var clientParams = "";
            var strConfirm = "";


            _deleteMessageFieldsName.ForEach(delegate(string fieldName)
            {
                messageFields += (messageFields.Length > 0 ? ", " : "") +
                                 string.Format("[{0}]", HttpUtility.HtmlEncode(dr[fieldName].ToString()));
            });

            if (messageFields.Length > 0)
                messageFields = (MessageDeleteConfirm.IsNullEmptyOrZero()? Resx.GetString("msgDeleteConfirm"): MessageDeleteConfirm) + " " + messageFields + "?";
            else
                messageFields = Resx.GetString("CONFIRM_StdMessage");

            messageFields = HttpUtility.JavaScriptStringEncode(messageFields);

            _deletePkFieldsName.ForEach(delegate(string fieldName)
            {
                clientParams += (clientParams.Length > 0 ? "," : "") + string.Format("{0}",
                                    HttpUtility.JavaScriptStringEncode(dr[fieldName].ToString()));
            });

            strConfirm = string.Format("v4_showConfirm('{0}', '{1}', '{2}', '{3}', '{4}', 300);",
                messageFields,
                HttpUtility.JavaScriptStringEncode(Resx.GetString("errDoisserWarrning")),
                HttpUtility.JavaScriptStringEncode(Resx.GetString("CONFIRM_StdCaptionYes")),
                HttpUtility.JavaScriptStringEncode(Resx.GetString("CONFIRM_StdCaptionNo")),
                string.Format("{0}({1});", _deleteClientFuncName, clientParams)
            );

            // function(message, title, captionYes, captionNo, callbackYes, width) {

            w.Write("<div class=\"v4DivTableCell\">");
            w.Write(
                "<span tabindex='0' class=\"ui-icon ui-icon-delete\" border=\"0\" style=\"cursor:pointer;\" title='{0}' onkeydown=\"v4_grid.keydown(event, this);\" onclick=\"{1}\" tabindex=\"{2}\">",
                HttpUtility.HtmlEncode(_deleteTitle),
                strConfirm,
                tabIndex * 10 + 100 + 3);
            w.Write("</div>");
        }

        private void RenderServiceColumnEdit(TextWriter w, DataRow dr, int tabIndex)
        {
            var clientParams = "";
            var elementId = $"imgEdit_{tabIndex}";
           
            var recordId = "";
            _editPkFieldsName.ForEach(delegate(string fieldName)
            {
                if (recordId.Length > 0) recordId += (char)31;
                recordId += dr[fieldName].ToString();                
            });

            if (recordId.Length > 0)
                clientParams = $"'{ HttpUtility.JavaScriptStringEncode(recordId)}'";

            w.Write("<div class=\"v4DivTableCell\">");

            if (IsMultiWinEdit) clientParams = clientParams.Length > 0 ? $"'{ID}','{Guid.NewGuid()}', false" + "," + clientParams + "" : $"'{ID}','{Guid.NewGuid()}', false";

            w.Write(
                "<span tabindex='0' class=\"ui-icon ui-icon-pencil\" border=\"0\" style=\"cursor:pointer;\" title=\"{0}\" onkeydown=\"v4_grid.keydown(event, this);\" onclick=\"{1}\" tabindex=\"{2}\" class=\"imgEdit\" id=\"{3}\">",
                HttpUtility.HtmlEncode(_editTitle),
                IsMultiWinEdit ?
                string.Format("{0}({1});", "v4_grid.multiWinEditable", clientParams)
                :
                string.Format("{0}({1}, '{2}');", _editClientFuncName, clientParams, _editPkFieldsName.Count == 1 ? _editPkFieldsName[0] : "gEdit"),
                tabIndex * 10 + 100 + 4,
                elementId);

            if (_showModifyInfoTooltip)
            {
                // текст подсказки с информацией о том, кем и когда была изменена запись
                var title = string.Format("\"{0}\\r\\n{1}:\\r\\n{2}\\r\\n\"",
                    _editTitle,
                    Resx.GetString("lblLastModify"),
                    dr[_modifyUserColumn]);

                title += string.Format("+ v4_toLocalTime(\"{0}\",\"dd.mm.yyyy hh:mi:ss\")",
                    ((DateTime) dr[_modifyDateColumn]).ToString("yyyy-MM-dd HH:mm:ss"));

                w.Write("<script>$(\"#{0}\").prop(\"title\",{1});</script>", elementId, title);
            }

            w.Write("</div>");
        }

        private void RenderServiceCheckedColumn(TextWriter w, DataRow dr, int tabIndex)
        {
            var val = "";
            _checkedFieldsName.ForEach(x =>
                val += val.Length > 0 ? ((char) 31).ToString() : dr[x] == null ? "" : dr[x].ToString()
            );

            w.Write("<input type='checkbox' class=\"v4GridCheckbox\" data-id=\"{0}\"/> ", val);
        }

        #endregion

        #endregion

        #region DetailColumn

        private string _detailClientFuncName = "";
        private List<string> _detailPkFieldsName;
        private string _detailTitle = "";

        public Grid()
        {
            _addTitle = Resx.GetString("lblGridAdd");
            _editTitle = Resx.GetString("lblGridEdit");
            _copyTitle = Resx.GetString("lblGridCopy");
            _commandMenuTitle = Resx.GetString("lblGridCommandMenu");
            _deleteTitle = Resx.GetString("lblGridDelete");
            GridHeight = 0;
            GridAutoSize = true;
            RenderConditionServiceColumnReturn = new Dictionary<string, List<object>>();
            RenderConditionServiceColumnEdit = new Dictionary<string, List<object>>();
            RenderConditionServiceColumnDelete = new Dictionary<string, List<object>>();
            RenderConditionServiceColumnDetail = new Dictionary<string, List<object>>();
            ShowFilterOptions = true;
            ShowPageBar = true;
            RowsPerPage = 50;
            AlwaysShowHeader = false;
        }

        /// <summary>
        ///     Настройка кнопки детализации записи в таблице
        /// </summary>
        /// <param name="clientFuncName">Клиентская функция, которая будет вызываться при нажатии на иконку детализации</param>
        /// <param name="pkFieldsName">Параметры клиентской функции</param>
        /// <param name="title">Всплывающая подсказка</param>
        public void SetServiceColumnDetail(string clientFuncName, List<string> pkFieldsName, string title = "")
        {
            _detailClientFuncName = clientFuncName;
            _detailPkFieldsName = pkFieldsName;
            _detailTitle = title;
        }

        private void RenderServiceColumnDetail(TextWriter w, DataRow dr, int tabIndex)
        {
            if (_detailPkFieldsName == null || _detailPkFieldsName.Count == 0) return;
            var clientParams = "";
            _detailPkFieldsName.ForEach(delegate(string fieldName)
            {
                clientParams += (clientParams.Length > 0 ? "," : "") + string.Format("'{0}'",
                                    HttpUtility.JavaScriptStringEncode(dr[fieldName].ToString()));
            });

            if (IsMultiWinDetail) clientParams = clientParams.Length > 0 ? $"'{ID}','{Guid.NewGuid()}', true" + "," + clientParams : $"'{ID}','{Guid.NewGuid()}', true";

            w.Write("<div class=\"v4DivTableCell\">");
            w.Write(
                "<img src=\"/styles/detail.gif\" border=\"0\" style=\"cursor:pointer;\" title='{0}' onclick=\"{1}\" tabindex=\"{2}\">",
                HttpUtility.HtmlEncode(_detailTitle),
                IsMultiWinDetail
                    ? $"{"v4_grid.multiWinEditable"}({clientParams});"
                    : clientParams.Length == 0 ? $"{_detailClientFuncName}();"
                        : $"{_detailClientFuncName}({clientParams});",
                tabIndex * 10 + 100 + 5);
            w.Write("</div>");
        }

        #endregion

        public void CloseDialogForm(string gridDialogId, string gridDialogIdp)
        {
            KescoHub.SendMessage(new SignalMessage
            {
                PageId = V4Page.IDPage,
                ItemId = V4Page.ItemId.ToString(),
                ItemName = V4Page.ItemName,
                IsV4Script = true,
                Message = $"<js>v4_grid.recordClose('{gridDialogId}', '{gridDialogIdp}');</js>"
            }, SignaRReceiveClientsMessageEnum.Self);
        }

        public void RefreshGrid()
        {
            KescoHub.SendMessage(new SignalMessage
            {
                PageId = V4Page.IDPage,
                ItemId = V4Page.ItemId.ToString(),
                ItemName = V4Page.ItemName,
                IsV4Script = true,
                Message = $"<js>cmdasync('cmd', 'Listener', 'ctrlId', {GridCmdListnerIndex}, 'cmdName', 'RefreshGridData');</js>"
            }, SignaRReceiveClientsMessageEnum.Self);
        }

        private void GetQueryStringFilter()
        {
            foreach (var col in QueryColumnsList)
            {
                if (!col.Key.IsNullEmptyOrZero())
                {
                    if (V4Page.Request.QueryString[col.Key] != null)
                    {
                        col.FilterRequired = true;
                        col.FilterValue = V4Page.Request.QueryString[col.Key];
                    }
                    else if (V4Page.Request.QueryString["_" + col.Key] != null)
                    {
                        col.FilterRequired = false;
                        col.FilterValue = V4Page.Request.QueryString["_" + col.Key];
                    }
                }
            }
        }

        private DataTable CreateDTLocalFromQueryColumn()
        {
            var dataTable = new DataTable();
            foreach (var col in QueryColumnsList)
            {
                var type = typeof(string);
                switch (col.Type)
                {
                    case 1: // число
                        type = typeof(int);
                        break;
                    case 2: // строка
                        type = typeof(string);
                        break;
                    case 3: // дата
                        type = typeof(DateTime);
                        break;
                    case 4: // ссылка
                        type = typeof(string);
                        break;
                    case 5: // список
                     type = typeof(int);
                        break;
                    case 6: // условие булево
                        type = typeof(bool);
                        break;
                    case 7: // булево
                        type = typeof(bool);
                        break;
                    case 8: // HTML текст
                        type = typeof(string);
                        break;
                    case 9: // пиктограмма
                        type = typeof(string);
                        break;
                    case 10: // дата локальная
                        type = typeof(DateTime);
                        break;
                    default: // условие список
                        type = typeof(string);
                        break;
                }

                var column = new DataColumn(col.ColumnName, type);

                var properties = column.ExtendedProperties;
                properties.Add("ColumnId", col.ColumnId);

                if (dataTable.Columns.Contains(col.ColumnName))
                {
                    var i = 0;
                    while (dataTable.Columns.Contains(column.ColumnName))
                    {
                        i++;
                        column.ColumnName = col.ColumnName + "@" + i;
                       
                    }
                }

                dataTable.Columns.Add(column);
            }

            return dataTable;
        }

        private static List<GridColumn> Clone(List<GridColumn> tableColumns)
        {
            var newtableColumnsList = new List<GridColumn>();
            foreach (var item in tableColumns)
            {
                var newItem = item.Clone();
                if (item.FilterUniqueValues != null)
                {
                    var filterUniqueValues = new Dictionary<object, object>();
                    foreach (var f in item.FilterUniqueValues)
                    {
                        filterUniqueValues.Add(f.Key, f.Value);
                    }
                    newItem.FilterUniqueValues = filterUniqueValues;
                }

                newtableColumnsList.Add(newItem);
            }

            return newtableColumnsList;
        }

        private void SetFilterColumns()
        {
            foreach (var column in Settings.TableColumns)
            {
                var clmn = TableColumnsFilter.FirstOrDefault(x => x.Id == column.Id);
                if (clmn != null)
                {
                    column.FilterUser = clmn.FilterUser;
                    column.FilterUniqueValues = clmn.FilterUniqueValues;
                    column.FilterEqual = clmn.FilterEqual;
                }
            }
        }
    }


    public class GridColumnUserFilterConverter : JsonCreationConverter<GridSettingsUserFilter>
    {
        protected override GridSettingsUserFilter Create(Type objectType, JObject jObject)
        {
            return FieldExists("IsCurrentDate", jObject) ? new GridSettingsUserFilterDate() : new GridSettingsUserFilter();
        }

        private bool FieldExists(string fieldName, JObject jObject)
        {
            return jObject["FilterUser"]?[fieldName] != null;
        }
    }

}
