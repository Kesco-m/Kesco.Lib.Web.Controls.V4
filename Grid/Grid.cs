using System;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Kesco.Lib.Localization;
using Kesco.Lib.Web.Controls.V4.Common;
using Page = Kesco.Lib.Web.Controls.V4.Common.Page;
using System.Collections.Generic;
using Kesco.Lib.BaseExtention.Enums;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.DALC;
using Kesco.Lib.Web.Settings;
using Kesco.Lib.Entities;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    /// <summary>
    ///     Контрол Grid (Таблица)
    /// </summary>
    public class Grid: V4Control, IClientCommandProcessor
    {
        private const string _constIdTag = "[CID]";
        private const string _constGroupingTag = "[GROUPING]";
        private const string _constCtrlPagingBar = "[C_PAGINGBAR]";
        private const string _constMarginBottom = "[MARGIN_BOTTOM]";
        private const string _constGridHeight = "[GRID_HEIGHT]";
        private const string _consGroupingPanelEmptyText = "[GROUPINGPANELEMPTYTEXT]";
        

        private int _rowsPerPage = 50;
        private int _marginBottom = 100;
        private int _gridHeight = 0;
        private string _emptyDataString = "";
        private NtfStatus _emptyDataNtfStatus = NtfStatus.Empty;
        private int? _maxPrintRenderRows;
        private const int MAX_RENDER_ROWS = 4000;

        private PagingBar.PagingBar _currentPagingBarCtrl;

        public DataTable _dtLocal;
        private GridSettings _gridSettings;

        private GridDbSourceSettings _dbSourceSettings;

        /// <summary>
        /// Настройки
        /// </summary>
        public GridSettings Settings
        {
            get {return _gridSettings;}
        }

        protected int GridCmdListnerIndex;

        /// <summary>
        /// Отступ от нижнего края страницы
        /// </summary>
        public int MarginBottom {
            get { return _marginBottom; }
            set { _marginBottom = value; }
        }

        /// <summary>
        /// Высота грида, если не указана, используется MarginBottom
        /// </summary>
        public int GridHeight
        {
            get { return _gridHeight; }
            set { _gridHeight = value; }
        }

        /// <summary>
        /// Строка, которая выводится, если источник данных пустой
        /// </summary>
        public string EmptyDataString {
            get { return string.IsNullOrEmpty(_emptyDataString) ? Resx.GetString("lNoData") : _emptyDataString; }
            set { _emptyDataString = value; }
        }

        /// <summary>
        /// Тип строки, которая выводится, если источник данных пустой
        /// </summary>
        public NtfStatus EmptyDataNtfStatus
        {
            get { return _emptyDataNtfStatus; }
            set { _emptyDataNtfStatus = value; }
        }
        
        /// <summary>
        /// Максимальное количество строк
        /// </summary>
        public int MaxPrintRenderRows
        {
            get { return _maxPrintRenderRows ?? MAX_RENDER_ROWS; }
            set { _maxPrintRenderRows = value; }
        }

        /// <summary>
        /// Поддержка контролом грида группировки
        /// </summary>
        public bool ShowGroupPanel { get; set; }

        /// <summary>
        ///     Акцессор V4Page
        /// </summary>
        public Page V4Page
        {
            get { return Page as Page; }
            set { Page = value; }
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
            
            _currentPagingBarCtrl.RowsPerPage = 50;
            _currentPagingBarCtrl.CurrentPageNumber = 1;
            _currentPagingBarCtrl.MaxPageNumber = 5;
            //_currentPagingBarCtrl.SetDisabled(true);
        }

        /// <summary>
        /// Обработчик изменнения количества строк на странице
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _currentPagingBarCtrl_RowsPerPageChanged(object sender, EventArgs e)
        {
            if (!_currentPagingBarCtrl.Disabled)
            {
                _rowsPerPage = _currentPagingBarCtrl.RowsPerPage;
                _currentPagingBarCtrl.CurrentPageNumber = 1;
                RefreshGridData();
                return;
            }

            V4Page.RestoreCursor();
        }

        /// <summary>
        /// Обработчик изменнения номера страницы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _currentPagingBarCtrl_CurrentPageChanged(object sender, EventArgs e)
        {
            RefreshGridData();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _editTitle = Resx.GetString("lblGridEdit");
            _copyTitle = Resx.GetString("lblGridCopy");
            _deleteTitle = Resx.GetString("lblGridDelete");
        }

        /// <summary>
        /// Отрисовка контрола
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
            sourceContent = sourceContent.Replace(_constGroupingTag, ShowGroupPanel?"true":"false");
            sourceContent = sourceContent.Replace(_constMarginBottom, _marginBottom.ToString());
            sourceContent = sourceContent.Replace(_constGridHeight, _gridHeight.ToString());
            sourceContent = sourceContent.Replace(_consGroupingPanelEmptyText, Resx.GetString("lblGridGroupingPanelEmptyText"));
            

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
        /// Обработка клиентских команд
        /// </summary>
        /// <param name="param"></param>
        public void ProcessClientCommand(NameValueCollection param)
        {
            switch (param["cmdName"])
            {
                case "RefreshGridData":
                    if (_dbSourceSettings!=null)
                        SetDataSource(_dbSourceSettings.SqlQuery, _dbSourceSettings.ConnectionString,_dbSourceSettings.SqlCommandType, _dbSourceSettings.SqlParams, false);
                    RefreshGridData();
                    V4Page.RestoreCursor();
                    V4Page.JS.Write("setTimeout(function(){{$('#btnRefresh_{0}').show()}}, 1000);", ID);
                    break;
                //Отрисовка настроек колонки
                case "RenderColumnSettings":
                     var p = _gridSettings.TableColumns.FirstOrDefault(x => x.FieldName == param["ColumnId"]);
                    if (p != null)
                        p.RenderColumnSettings(V4Page);
                    else
                        V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"), MessageStatus.Error);
                    V4Page.RestoreCursor();
                    break;
                //Пользовательский фильтр
                case "OpenUserFilterForm":
                    var p0 = _gridSettings.TableColumns.FirstOrDefault(x => x.Id == param["ColumnId"]);
                    if (p0 != null)
                        p0.RenderColumnUserFilterForm(V4Page, param["FilterId"], param["SetValue"]);
                    else
                        V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"), MessageStatus.Error);
                    break;
                case "SetFilterColumnByUser":
                    SetFilterColumnByUser(param["ColumnId"], param["FilterId"]);
                    _currentPagingBarCtrl.CurrentPageNumber = 1;
                    RefreshGridData();
                    V4Page.RestoreCursor();
                    break;
                //Фильтр по выбранным значениям
                case "SetFilterByColumnValues":
                    SetFilterByColumnValues(param["ColumnId"], param["Data"], param["Equals"]);
                    _currentPagingBarCtrl.CurrentPageNumber = 1;
                    RefreshGridData();
                    V4Page.RestoreCursor();
                    break;
                //Очистка фильтров
                case "ClearDataFilter":
                    ClearDataFilter(true);
                    break;
                case "ClearFilterColumnValues":
                    ClearFilterColumnValues(param["ColumnId"]);
                    _currentPagingBarCtrl.CurrentPageNumber = 1;
                    RefreshGridData();
                    V4Page.RestoreCursor();
                    break;
                //Сортировка
                case "SetOrderByColumnValues":
                    SetOrderByColumnValues(param["ColumnId"], param["Direction"]);
                    RefreshGridData();
                    V4Page.RestoreCursor();
                    break;

                case "ClearOrderByColumnValues":
                    ClearOrderByColumnValues(param["ColumnId"]);
                    RefreshGridData();
                    V4Page.RestoreCursor();
                    break;
            }
           
        }
        
        /// <summary>
        /// Установка источника данных
        /// </summary>
        /// <param name="dt">Заполненный данными DataTable</param>
        public void SetDataSource(DataTable dt)
        {
            _dbSourceSettings = null;
            ClearDataFilter(false);
            _dtLocal = dt;
            _gridSettings = new GridSettings(_dtLocal, ID, GridCmdListnerIndex, V4Page) {IsGroupEnable = ShowGroupPanel};
        }

        /// <summary>
        /// Установка источника данных на основании переданных параметров
        /// </summary>
        /// <param name="sql">SQL-запрос или выражение</param>
        /// <param name="cn">Строка подключения</param>
        /// <param name="ctype">Тип запроса или выражения</param>
        /// <param name="args">Параметры sql</param>
        /// <param name="reloadDbSourceSettings">Пересоздавать экземпляр объекта GridDbSourceSettings</param>
        public void SetDataSource(string sql, string cn, CommandType ctype, Dictionary<string, object> args, bool reloadDbSourceSettings = true)
        {
            if (_dbSourceSettings == null || reloadDbSourceSettings)
            {
                _dbSourceSettings = new GridDbSourceSettings
                {
                    SqlQuery = sql,
                    ConnectionString = cn,
                    SqlCommandType = ctype,
                    SqlParams = args
                };
            }

            ClearDataFilter(false, reloadDbSourceSettings);
            if (_currentPagingBarCtrl != null) _currentPagingBarCtrl.CurrentPageNumber = 1;
            _dtLocal = DBManager.GetData(_dbSourceSettings.SqlQuery, _dbSourceSettings.ConnectionString, _dbSourceSettings.SqlCommandType, _dbSourceSettings.SqlParams = args);

            if (reloadDbSourceSettings)
            {
                _gridSettings = new GridSettings(_dtLocal, ID, GridCmdListnerIndex, V4Page)
                {
                    IsGroupEnable = ShowGroupPanel
                };
            }
        }

        /// <summary>
        /// Очистка фильтров
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

            if (_gridSettings != null && _gridSettings.DT != null && dropGridSettings)
            {
                _gridSettings.DT.Clear();
                _gridSettings.DT.Dispose();
                _gridSettings.TableColumns = null;
                _gridSettings = null;
            }
            GC.Collect();
            if (dropClientTable)
            {
               // JS.Write("mwp_fixedHeaderDestroy();");
                JS.Write("$(\"#{0}\").html('');", ID);
            }
        }

        /// <summary>
        /// Очистка грида
        /// </summary>
        public void ClearGridData()
        {
            var w = new StringWriter();
            V4Page.JS.Write("v4_fixedHeaderDestroy();");
            ClearDataFilter(true);
            V4Page.RestoreCursor();
        }

        /// <summary>
        /// Обновление грида
        /// </summary>
        public void RefreshGridData()
        {
            var w = new StringWriter();
            RenderGridData(w);

            if (ShowGroupPanel)
               V4Page.JS.Write("setTimeout(function(){{v4_gridEnableGrouping('{0}');}},50);", ID);
            
            V4Page.JS.Write("v4_fixedHeaderDestroy();");
            V4Page.JS.Write("$('#{0}').html('{1}');", ID, HttpUtility.JavaScriptStringEncode(w.ToString()));
            V4Page.JS.Write("setTimeout(v4_fixedHeader,50);");
            V4Page.JS.Write(@"grid_clientLocalization = {{
                ok_button:""{0}"",
                cancel_button:""{1}"" ,
                empty_filter_value:""{2}"" 
            }};",
                Resx.GetString("cmdApply"),
                Resx.GetString("cmdCancel"),
                Resx.GetString("msgEmptyFilterValue")
            );

            if (ShowGroupPanel)
                V4Page.JS.Write("v4_setWidthGroupingPanel('{0}');", ID);

            V4Page.RestoreCursor();
        }

        /// <summary>
        /// Формирование грида по фильтрам
        /// </summary>
        /// <param name="w"></param>
        public void RenderGridData(TextWriter w)
        {
            if (_dtLocal == null || _dtLocal.Rows.Count == 0)
            {
                var className = EnumAccessors.GetCssClassByNtfStatus(_emptyDataNtfStatus);

                w.Write("<div{1}>{0}</div>", EmptyDataString, string.IsNullOrEmpty(className) ? "" : " class='" + className + "'");
                //w.Write(EmptyDataString);
                _currentPagingBarCtrl.SetDisabled(true, false);
                JS.Write("$('#divPageBar_{0}').hide();", ID);
                return;
            }

            DataTable results = null;
            var defaultSort = "";
            var pageIndex = 0;
            var rowNumber = 0;

            var columnsWithFilterIn =
                _gridSettings.TableColumns.Where(
                    x =>
                        x.FilterUser == null && x.FilterUniqueValues != null && x.FilterUniqueValues.Count > 0 &&
                        x.FilterEqual == GridColumnFilterEqualEnum.In)
                    .ToList();

            var columnsWithFilterNotIn =
                _gridSettings.TableColumns.Where(
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
                                              clmn.FilterUniqueValues.ContainsValue(r.Field<object>(clmn.FieldName) ?? string.Empty)))
                                      && (columnsWithFilterNotIn.Count == 0 || !columnsWithFilterNotIn.Any(
                                          clmn =>
                                              clmn.FilterUniqueValues.ContainsValue(r.Field<object>(clmn.FieldName) ?? string.Empty)))
                                  select r;

                if (applyfilter.Any())
                    results = applyfilter.CopyToDataTable();
                else
                {
                    results = _dtLocal.Clone();
                }
            }
            else
                results = _dtLocal.Copy();

            // Теперь накладываем ограничение по установленным пользовательским фильтрам
            var columnsWithUserFilter = _gridSettings.TableColumns.Where(x => x.FilterUser != null).ToList();
            if (columnsWithUserFilter.Count > 0)
            {
                results = ApplyUserFilter(results, columnsWithUserFilter);
            }

            if (results.Rows.Count > 0)
            {
                _currentPagingBarCtrl.RowsPerPage = _rowsPerPage;
                _currentPagingBarCtrl.MaxPageNumber = (int)Math.Ceiling(results.Rows.Count / (double)_rowsPerPage);
                pageIndex = (_currentPagingBarCtrl.CurrentPageNumber - 1) * _rowsPerPage;
                _currentPagingBarCtrl.SetDisabled(false, false);
                JS.Write("$('#divResultCount_{0}').html(' {1}: {2}');", ID, Resx.GetString("lblGridRecordCount"), results.Rows.Count);
                JS.Write("$('#divPageBar_{0}').show();",ID);
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
                    "$('#btnRefresh_{0}').unbind('click'); $('#btnRefresh_{0}').bind('click', function(){{Wait.render(true); $('#btnRefresh_{0}').hide(); cmdasync('cmd', 'Listener', 'ctrlId', {1}, 'cmdName', 'RefreshGridData'); }});",
                    ID, GridCmdListnerIndex);
            }
            else
                JS.Write("$('#btnRefresh_{0}').hide();", ID);

            //Сортируем
            var sortStr = string.Join(", ",
                _gridSettings.TableColumns.Where(x => x.OrderByNumber != null)
                    .OrderBy(x => x.OrderByNumber)
                    .Select(
                        x =>
                            "[" + x.FieldName + "]" +
                            ((x.OrderByDirection == GridColumnOrderByDirectionEnum.Desc) ? " DESC" : ""))
                    .ToArray());

            results.DefaultView.Sort = sortStr.Length > 0 ? sortStr : defaultSort;
            results = results.DefaultView.ToTable();
            //-----------------------------------------------

            w.Write("<table class='grid' id='table_{0}'>", ID);
            w.Write("<thead class='v4DropPanel'>");
            w.Write("<tr id='thead_{0}' class=\"gridHeader\">", ID);

            if (ExistServiceColumn)
                w.Write(@"<th>&nbsp;</th>");

            _gridSettings.TableColumns.OrderBy(x => x.DisplayOrder).ToList().ForEach(delegate(GridColumn tc)
            {
                if (!tc.DisplayVisible) return;
                tc.RenderColumnSettingsHeader(w);
            });

            if (ExistServiceColumnDetail)
                w.Write(@"<th>&nbsp;</th>");

            w.Write("</tr>");
            w.Write("</thead>");

            if (_gridSettings.TableColumns.Where(x => x.IsSumValues).ToList().Count > 0)
            {

                w.Write("<tfoot>");
                w.Write("<tr class=\"gridHeader\">");

                if (ExistServiceColumn)
                    w.Write(@"<td>&nbsp;</td>");

                var resultData = results.AsEnumerable();

                _gridSettings.TableColumns.OrderBy(x => x.DisplayOrder).ToList().ForEach(delegate(GridColumn tc)
                {
                    if (!tc.DisplayVisible) return;
                    if (!tc.IsSumValues)
                        tc.RenderColumnDataSumFooter(w, null);
                    else
                    {
                        switch (tc.ColumnType)
                        {
                            case GridColumnTypeEnum.Int:
                                var sum = resultData.Sum(x => x.Field<int>(tc.FieldName));
                                tc.RenderColumnDataSumFooter(w, sum);
                                break;
                            case GridColumnTypeEnum.Decimal:
                                var sum0 = resultData.Sum(x => x.Field<decimal>(tc.FieldName));
                                tc.RenderColumnDataSumFooter(w, sum0);
                                break;
                            default:
                                tc.RenderColumnDataSumFooter(w, null);
                                break;
                        }
                        
                            
                    }
                });

                w.Write("</tr>");
                w.Write("</tfoot>");
            }


            w.Write("<tbody>");
            for (var i = 0; i < results.Rows.Count; i++)
            {
                if ((i >= pageIndex && rowNumber < _rowsPerPage) || (Settings.IsPrintVersion && i < MaxPrintRenderRows))
                {
                    w.Write("<tr>");

                    if (ExistServiceColumn)
                    {
                        w.Write("<td>");
                        w.Write("<div class=\"v4DivTable\">");
                        w.Write("<div class=\"v4DivTableRow\">");

                        if (_existServiceColumnCopy) RenderServiceColumnCopy(w, results.Rows[i], i);
                        if (_existServiceColumnDelete) RenderServiceColumnDelete(w, results.Rows[i], i);
                        if (_existServiceColumnEdit) RenderServiceColumnEdit(w, results.Rows[i], i);

                        w.Write("</div>");
                        w.Write("</div>");
                        w.Write("</td>");
                    }

                    _gridSettings.TableColumns.OrderBy(x => x.DisplayOrder).ToList().ForEach(delegate(GridColumn tc)
                    {
                        if (!tc.DisplayVisible) return;
                        tc.RenderColumnData(w, results.Rows[i]);
                    });

                    if (ExistServiceColumnDetail)
                    {
                        w.Write("<td>");
                        w.Write("<div class=\"v4DivTable\">");
                        w.Write("<div class=\"v4DivTableRow\">");

                        RenderServiceColumnDetail(w, results.Rows[i], i);

                        w.Write("</div>");
                        w.Write("</div>");
                        w.Write("</td>");
                    }

                    w.Write("</tr>");
                    rowNumber++;
                }
                else if (_currentPagingBarCtrl.CurrentPageNumber * _rowsPerPage < i) break;
            }
            w.Write("</tbody>");

            w.Write("</table>");

            if (Settings.IsPrintVersion && results.Rows.Count > MaxPrintRenderRows)
            {
                w.Write("<div style=\"font-weight:bold;\">{0} {1} {2} {3} {4}!</div>", Resx.GetString("msgPrintCount"), MaxPrintRenderRows, Resx.GetString("lOf"), results.Rows.Count, Resx.GetString("lblGridRecords"));
            }


        }

        /// <summary>
        /// Наложение ограничений по установленным пользовательским фильтрам
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
                                      (filter.FilterType == GridColumnUserFilterEnum.Указано &&
                                       ((r.Field<object>(clmn.FieldName) != null && !r.Field<object>(clmn.FieldName).Equals(""))))
                                      || (filter.FilterType == GridColumnUserFilterEnum.НеУказано &&
                                          (r.Field<object>(clmn.FieldName) == null ||
                                           (clmn.ColumnType == GridColumnTypeEnum.String &&
                                            r.Field<object>(clmn.FieldName).Equals(""))))
                                      || (filter.FilterType == GridColumnUserFilterEnum.Равно &&
                                          r.Field<object>(clmn.FieldName).Equals(filter.FilterValue1))
                                      || (filter.FilterType == GridColumnUserFilterEnum.НеРавно &&
                                          !r.Field<object>(clmn.FieldName).Equals(filter.FilterValue1))
                                      || (filter.FilterType == GridColumnUserFilterEnum.Между &&
                                          (
                                              (r.Field<object>(clmn.FieldName) != null &&
                                               ((IComparable)filter.FilterValue1).CompareTo(r.Field<object>(clmn.FieldName)) <= 0 &&
                                               ((IComparable)r.Field<object>(clmn.FieldName)).CompareTo(filter.FilterValue2) <= 0)
                                              )
                                          )
                                      || (filter.FilterType == GridColumnUserFilterEnum.БольшеИлиРавно &&
                                          (r.Field<object>(clmn.FieldName) != null &&
                                           ((IComparable)filter.FilterValue1).CompareTo(r.Field<object>(clmn.FieldName)) <= 0))
                                      || (filter.FilterType == GridColumnUserFilterEnum.МеньшеИлиРавно &&
                                          (r.Field<object>(clmn.FieldName) != null &&
                                           ((IComparable)r.Field<object>(clmn.FieldName)).CompareTo(filter.FilterValue1) <= 0))
                                      || (filter.FilterType == GridColumnUserFilterEnum.Больше &&
                                          (r.Field<object>(clmn.FieldName) != null &&
                                           ((IComparable)filter.FilterValue1).CompareTo(r.Field<object>(clmn.FieldName)) < 0))
                                      || (filter.FilterType == GridColumnUserFilterEnum.Меньше &&
                                          (r.Field<object>(clmn.FieldName) != null &&
                                           ((IComparable)r.Field<object>(clmn.FieldName)).CompareTo(filter.FilterValue1) < 0))
                                      || (filter.FilterType == GridColumnUserFilterEnum.Содержит &&
                                          (r.Field<object>(clmn.FieldName) != null &&
                                           r.Field<string>(clmn.FieldName)
                                               .IndexOf(filter.FilterValue1.ToString(), StringComparison.OrdinalIgnoreCase) >= 0))
                                      || (filter.FilterType == GridColumnUserFilterEnum.НеСодержит &&
                                          (r.Field<object>(clmn.FieldName) != null &&
                                           r.Field<string>(clmn.FieldName)
                                               .IndexOf(filter.FilterValue1.ToString(), StringComparison.OrdinalIgnoreCase) < 0))
                                      || (filter.FilterType == GridColumnUserFilterEnum.НачинаетсяС &&
                                          (r.Field<object>(clmn.FieldName) != null &&
                                           r.Field<string>(clmn.FieldName)
                                               .StartsWith(filter.FilterValue1.ToString(), StringComparison.OrdinalIgnoreCase)))
                                      || (filter.FilterType == GridColumnUserFilterEnum.ЗаканчиваетсяНа &&
                                          (r.Field<object>(clmn.FieldName) != null &&
                                           r.Field<string>(clmn.FieldName)
                                               .EndsWith(filter.FilterValue1.ToString(), StringComparison.OrdinalIgnoreCase)))
                                  select r;

                if (applyfilter.Any())
                    dt = applyfilter.CopyToDataTable();
                else
                {
                    dt.Clear();
                }
            });

            return dt;
        }

        /// <summary>
        /// Создание пользовательского фильтра
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="filterId"></param>
        private void SetFilterColumnByUser(string columnId, string filterId)
        {
            var clmn = _gridSettings.TableColumns.FirstOrDefault(x => x.Id == columnId);
            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumnFound"), Resx.GetString("alertError"), MessageStatus.Error);
                return;
            }

            var filter = (GridColumnUserFilterEnum)int.Parse(filterId);
            object objField1 = null;
            object objField2 = null;

            if (filter == GridColumnUserFilterEnum.НеУказано || filter == GridColumnUserFilterEnum.Указано)
            {
                clmn.FilterUser = new GridColumnUserFilter { FilterType = filter };
            }
            else
            {
                objField1 = GetFilterUserControlValue(clmn, clmn.FilterUserCtrlBaseName + "_" + ID + "_1");
                if (filter == GridColumnUserFilterEnum.Между)
                    objField2 = GetFilterUserControlValue(clmn, clmn.FilterUserCtrlBaseName + "_" + ID + "_2");
            }

            clmn.FilterUser = new GridColumnUserFilter
            {
                FilterType = filter,
                FilterValue1 = objField1,
                FilterValue2 = objField2
            };
        }

        /// <summary>
        /// Получение значений установленных фильтров
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
                value = ((DatePicker)ctrl).ValueDate;
            else if (ctrl is Number)
            {
                if (clmn.ColumnType == GridColumnTypeEnum.Int)
                    value = ((Number) ctrl).ValueInt;
                else
                {
                    if (clmn.ColumnType == GridColumnTypeEnum.Double)
                    {
                        var valueDecimal = ((Number)ctrl).ValueDecimal;
                        if (valueDecimal != null) value = (double)valueDecimal;
                    }
                    else
                        value = ((Number) ctrl).ValueDecimal;
                }
            }
            else
                value = ctrl.Value;
            return value;
        }

        /// <summary>
        /// Удаление установленных значений фильтров
        /// </summary>
        /// <param name="columnId"></param>
        private void ClearFilterColumnValues(string columnId)
        {
            var clmn = _gridSettings.TableColumns.FirstOrDefault(x => x.Id == columnId);
            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"), MessageStatus.Error);
                return;
            }
            clmn.FilterUser = null;
            clmn.FilterUniqueValues = null;
        }

        /// <summary>
        /// Установка фильтра по выбранному значению
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="data"></param>
        /// <param name="equals"></param>
        private void SetFilterByColumnValues(string columnId, string data, string equals)
        {
            var clmn = _gridSettings.TableColumns.FirstOrDefault(x => x.Id == columnId);
            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"), MessageStatus.Error);
                return;
            }

            clmn.FilterEqual = @equals != null && @equals == "0"
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
        /// Сортировка
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="direction"></param>
        private void SetOrderByColumnValues(string columnId, string direction)
        {
            var clmn = _gridSettings.TableColumns.FirstOrDefault(x => x.Id == columnId);
            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"), MessageStatus.Error);
                return;
            }

            clmn.OrderByNumber = 1;
            clmn.OrderByDirection = @direction != null && @direction == "0"
                ? GridColumnOrderByDirectionEnum.Asc
                : GridColumnOrderByDirectionEnum.Desc;

            SetOrderSortedColumn(2, columnId);
        }

        /// <summary>
        /// Установка порядка сортировки колонок
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="excludeColumnId"></param>
        private void SetOrderSortedColumn(int startIndex, string excludeColumnId)
        {
            _gridSettings.TableColumns.Where(x => x.Id != excludeColumnId && x.OrderByNumber != null)
                .OrderBy(x => x.OrderByNumber)
                .ToList()
                .ForEach(
                    delegate(GridColumn clmn0) { clmn0.OrderByNumber = startIndex++; });
        }

        /// <summary>
        /// Отмена сортировки колонки
        /// </summary>
        /// <param name="columnId"></param>
        private void ClearOrderByColumnValues(string columnId)
        {
            var clmn = _gridSettings.TableColumns.FirstOrDefault(x => x.Id == columnId);
            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"), MessageStatus.Error);
                return;
            }
            clmn.OrderByNumber = null;
            SetOrderSortedColumn(1, columnId);
        }

        #region ServiceColumn
        
        private bool _existServiceColumnEdit = false;
        private string _editClientFuncName = "";
        private List<string> _editPkFieldsName;
        private string _editTitle = "редактировать"; 

        private bool _existServiceColumnCopy = false;
        private string _copyClientFuncName = "";
        private List<string> _copyPkFieldsName;
        private string _copyTitle = "копировать";

        private bool _existServiceColumnDelete = false;
        private string _deleteClientFuncName = "";
        private List<string> _deletePkFieldsName;
        private List<string> _deleteMessageFieldsName;
        private string _deleteTitle = "удалить";


        /// <summary>
        /// Свойство указывающее, что у грида есть колонка с управляющими иконками
        /// на текущий момент реализовано редактирование, копирование, удаление
        /// </summary>
        public bool ExistServiceColumn { get; set; }

        /// <summary>
        /// Свойство указывающее, что у грида есть колонка с управляющей иконкой детализации
        /// </summary>
        public bool ExistServiceColumnDetail { get; set; }

        /// <summary>
        /// Настройка кнопки редатирования записи в таблице
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
        /// Настройка кнопки копирования записи в таблице
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
        /// Настройка кнопки удаления записи в таблице
        /// </summary>
        /// <param name="clientFuncName">Клиентская функция, которая будет вызываться при нажатии на иконку удаления</param>
        /// <param name="pkFieldsName">Параметры клиентской функции</param>
        /// <param name="title">Всплывающая подсказка</param>
        public void SetServiceColumnDelete(string clientFuncName, List<string> pkFieldsName, List<string> messageFieldsName, string title = "")
        {
            _existServiceColumnDelete = true;
            _deleteClientFuncName = clientFuncName;
            _deletePkFieldsName = pkFieldsName;
            _deleteMessageFieldsName = messageFieldsName;
            _deleteTitle = title;
        }

        #region Render Icons

        private void RenderServiceColumnCopy(TextWriter w, DataRow dr, int tabIndex)
        {
            var clientParams = "";
            _copyPkFieldsName.ForEach(delegate(string fieldName)
            {
                clientParams += (clientParams.Length > 0 ? "," : "") + string.Format("'{0}'", HttpUtility.JavaScriptStringEncode(dr[fieldName].ToString()));
            });

            w.Write("<div class=\"v4DivTableCell\">");
            w.Write("<img src=\"/styles/copy.gif\" border=\"0\" style=\"cursor:pointer;\" title='{0}' onclick=\"{1}\" tabindex=\"{2}\">",
                HttpUtility.HtmlEncode(_copyTitle),
                string.Format("{0}({1});", _copyClientFuncName, clientParams),
                tabIndex*10+100+1);
            w.Write("</div>");
        }

        private void RenderServiceColumnDelete(TextWriter w, DataRow dr, int tabIndex)
        {
            var messageFields = "";
            var clientParams = "";
            var strConfirm = "";


            _deleteMessageFieldsName.ForEach(delegate(string fieldName)
            {
                messageFields += (messageFields.Length > 0 ? ", " : "") + string.Format("[{0}]", HttpUtility.HtmlEncode(dr[fieldName].ToString()));
            });
            
            if (messageFields.Length>0)
                messageFields = Resx.GetString("msgDeleteConfirm")  + " " + messageFields + "?";
            else
                messageFields = Resx.GetString("CONFIRM_StdMessage");

            messageFields = HttpUtility.JavaScriptStringEncode(messageFields);

            _deletePkFieldsName.ForEach(delegate(string fieldName)
            {
                clientParams += (clientParams.Length > 0 ? "," : "") + string.Format("{0}", HttpUtility.JavaScriptStringEncode(dr[fieldName].ToString()));
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
            w.Write("<img src=\"/styles/delete.gif\" border=\"0\" style=\"cursor:pointer;\" title='{0}' onclick=\"{1}\" tabindex=\"{2}\">", 
                HttpUtility.HtmlEncode(_deleteTitle),
                strConfirm,
                tabIndex * 10 + 100 + 2);
            w.Write("</div>");
        }

        private void RenderServiceColumnEdit(TextWriter w, DataRow dr, int tabIndex)
        {
            var clientParams = "";
            _editPkFieldsName.ForEach(delegate(string fieldName)
            {
                clientParams += (clientParams.Length>0?",":"") + string.Format("'{0}'",HttpUtility.JavaScriptStringEncode(dr[fieldName].ToString()));
            });

            w.Write("<div class=\"v4DivTableCell\">");
            w.Write("<img src=\"/styles/edit.gif\" border=\"0\" style=\"cursor:pointer;\" title=\"{0}\" onclick=\"{1}\" tabindex=\"{2}\">", 
                HttpUtility.HtmlEncode(_editTitle),
                string.Format("{0}({1});",_editClientFuncName,clientParams),
                tabIndex * 10 + 100 + 3);
             
            w.Write("</div>");

        }

        #endregion

        #endregion

        #region DetailColumn
        private string _detailClientFuncName = "";
        private List<string> _detailPkFieldsName;
        private string _detailTitle = "";

        /// <summary>
        /// Настройка кнопки детализации записи в таблице
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
            if (_copyPkFieldsName == null || _copyPkFieldsName.Count == 0) return;
            var clientParams = "";
            _copyPkFieldsName.ForEach(delegate(string fieldName)
            {
                clientParams += (clientParams.Length > 0 ? "," : "") + string.Format("'{0}'", HttpUtility.JavaScriptStringEncode(dr[fieldName].ToString()));
            });

            w.Write("<div class=\"v4DivTableCell\">");
            w.Write("<img src=\"/styles/detail.gif\" border=\"0\" style=\"cursor:pointer;\" title='{0}' onclick=\"{1}\" tabindex=\"{2}\">",
                HttpUtility.HtmlEncode(_detailTitle),
                string.Format("{0}({1});", _detailClientFuncName, clientParams),
                tabIndex * 10 + 100 + 1);
            w.Write("</div>");
        }

        #endregion

        public decimal GetSumDecimalByColumnValue(string fieldName)
        {
            if (_dtLocal == null) return 0;
            return _dtLocal.AsEnumerable().Sum(f => f.Field<decimal>(fieldName));
        }

        /// <summary>
        /// Переход на указанную страницу
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
        /// Переход на последнюю страницу
        /// </summary>
        public void GoToLastPage()
        {
            if (_currentPagingBarCtrl.CurrentPageNumber == _currentPagingBarCtrl.MaxPageNumber)
                return;
            _currentPagingBarCtrl.CurrentPageNumber = _currentPagingBarCtrl.MaxPageNumber;
            RefreshGridData();
        }

        /// <summary>
        /// Возвращает текущую страницу
        /// </summary>
        public int GеtCurrentPage()
        {
            return _currentPagingBarCtrl.CurrentPageNumber;
        }


    }
}
