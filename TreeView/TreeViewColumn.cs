using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using Kesco.Lib.BaseExtention.Enums;
using Kesco.Lib.Web.Controls.V4.Common;

namespace Kesco.Lib.Web.Controls.V4.TreeView
{
    /// <summary>
    ///     Класс, описывающий колонку в контроле TreeView
    /// </summary>
    public class TreeViewColumn
    {
        private readonly string _applyImage =
            "<span class=\"ui-icon ui-icon-check\" border=0 style=\"display:inline-block\"></span>";

        private readonly string _cssClassInterval = "filterType" + (int) TreeViewColumnUserFilterEnum.Между;

        private string _filterFieldName = "";

        public TreeViewColumnUserFilter FilterUser {get;set;}

        public TreeViewColumnUserFilter FilterUserOriginal;

        public Dictionary<TreeViewColumnUserFilterEnum, string> FilterUserClause;

        public string FilterUserCtrlBaseName = "v4_ctrlFilterClause";

        public delegate void RenderColumnDelegate(TextWriter w, DataRow dr);

        /// <summary>
        ///     Конструктор
        /// </summary>
        /// <param name="settings">Настройки грида</param>
        public TreeViewColumn(TreeViewSettings settings)
        {
            Settings = settings;
        }

        public bool IsSaveSettings { get; set; } = true;

        /// <summary>
        ///     Признак того, что возможна фильтрация по данной колонке
        /// </summary>
        public bool IsFilteredColumn => Settings.FilteredColumns.Contains(FilterFieldName);

        public string HeaderTitle { get; set; }
        public string Title { get; set; }

        public TreeViewColumnTypeEnum ColumnType { get; set; }

        public string Id { get; set; }
        public string FieldName { get; set; }
        public string FilterFieldName
        {
            get
            {
                if (string.IsNullOrEmpty(_filterFieldName)) return FieldName;
                return _filterFieldName;
            }
            set { _filterFieldName = value; }
        }
        public string Alias { get; set; }
        public bool DisplayVisible { get; set; }
        public bool IsAllowNull { get; set; }

        public object DefaultValue { get; set; }

        private TreeViewSettings Settings { get; }

        /// <summary>
        ///     Признак того, что фильтр имет списочный тип
        /// </summary>
        public bool IsFilteredListColumn { get; set; }

        #region Render

        #region Render User Filter

        /// <summary>
        ///     Формирование формы пользовательских фильтров
        /// </summary>
        /// <param name="page"></param>
        /// <param name="filterId"></param>
        /// <param name="setValue"></param>
        public void RenderColumnUserFilterForm(Page page, string filterId, string setValue, Dictionary<string, string> fieldValuesList = null)
        {
            var w = new StringWriter();

            if (fieldValuesList == null)
                RenderColumnUserFilterForm_Clause(w, page, filterId, setValue.Equals("1"));
            else
                RenderColumnUserFilterForm_List(w, page, filterId, setValue.Equals("1"), fieldValuesList);

            if (Settings.IsFilterEnable && IsFilteredColumn)
            {
                if (FilterUser != null && FilterUser.FilterType == (TreeViewColumnUserFilterEnum)int.Parse(filterId))
                {
                    w.Write("<br/>");
                    RenderClearFilterBlock(w);
                }
            }

            page.JS.Write("v4_columnSettingsAdvSearchForm(\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\");",
                Settings.TreeViewId,
                Settings.TreeViewCmdListnerIndex,
                filterId,
                Id,
                HttpUtility.JavaScriptStringEncode(string.Format("{0}",
                    Settings.V4Page.Resx.GetString("lblSettingFilter"))),
                fieldValuesList != null);

            page.JS.Write("$('#divColumnSettingsUserFilterForm_Header_{0}').html('{1}');",
                Settings.TreeViewId,
                HttpUtility.JavaScriptStringEncode(string.Format("{0} [{1}]",
                    Settings.V4Page.Resx.GetString("lblFieldValue"), Alias)));
            page.JS.Write("$('#divColumnSettingsUserFilterForm_Body_{0}').html('{1}');",
                Settings.TreeViewId,
                HttpUtility.JavaScriptStringEncode(w.ToString()));

            page.JS.Write(
                "$('#v4_selectFilterUserClause_{2}').selectmenu({{width : 'auto', change: function() {{v4_selectFilterUserClauseAdvSearch_OnChange(\"{2}\", this, {0}, \"{1}\");}}}}); ",
                (int) TreeViewColumnUserFilterEnum.Между, _cssClassInterval, Settings.TreeViewId);
            page.JS.Write("v4_selectFilterUserClauseAdvSearch_OnChange(\"{2}\", null, {0}, \"{1}\");",
                (int) TreeViewColumnUserFilterEnum.Между, _cssClassInterval, Settings.TreeViewId);
            page.JS.Write("setTimeout(function(){{$(\"#{0}_{1}_1_0\").focus();}},10);", FilterUserCtrlBaseName,
                Settings.TreeViewId);
        }

        /// <summary>
        ///     Формирование выбора условий для фильтра
        /// </summary>
        /// <param name="w"></param>
        /// <param name="page"></param>
        /// <param name="filterId"></param>
        /// <param name="setValue"></param>
        private void RenderColumnUserFilterForm_Clause(TextWriter w, Page page, string filterId, bool setValue)
        {
            if (FilterUserClause == null || FilterUserClause.Count == 0) FillUserFilterClause();
            var inx = 1;
            var selectedRunMenu = FilterUserClause.Select(item => new SelectedRunItem
            {
                Name = item.Value,
                Value = (int) item.Key,
                Order = inx++,
                IsSelected = (int) item.Key == int.Parse(filterId)
            }).ToList();


            w.Write("<div class=\"v4DivTable\">");
            w.Write("<div class=\"v4DivTableRow\">");
            w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");

            w.Write(@"<select name=""{0}_{1}"" id=""{0}_{1}"">", "v4_selectFilterUserClause", Settings.TreeViewId);
            selectedRunMenu.OrderBy(x => x.Order)
                .ToList()
                .ForEach(
                    delegate(SelectedRunItem ri)
                    {
                        w.Write(@"<option {0} value=""{1}"">{2}</option>", ri.IsSelected ? "selected" : "", ri.Value,
                            ri.Name);
                    });
            w.Write("</select>");
            w.Write("</div>");
            w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
            if (int.Parse(filterId) < 200)
                RenderControlsUserFilterClause(w, page, setValue);
            w.Write("</div>");
            w.Write("</div>");
            w.Write("</div>");
        }

        private void RenderColumnUserFilterForm_List(TextWriter w, Page page, string filterId, bool setValue, Dictionary<string, string> fieldValuesList)
        {
            var checkState = "";
            var valueInFilter = false;

            w.Write("<div id=\"ColumnUserFilterList_{0}_{1}\" class=\"v4DivTable\">", Settings.TreeViewId, Id);

            string[] filterValues = null;
            if (FilterUser != null)
                filterValues = FilterUser.FilterValue1.ToString().Split(',');

            foreach (var item in fieldValuesList)
            {
                if (filterValues != null)
                    valueInFilter = filterValues.Contains(item.Key);

                checkState = valueInFilter ? "checked" : "";
                w.Write("<div class=\"v4DivTableRow\">");
                w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
                w.Write("<input type='checkbox' class=\"classValueCheckBox\" {0} data-id=\"{1}\">",checkState,item.Key);
                w.Write("</div>");
                w.Write("<div class=\"v4DivTableCell v4PaddingCell\" style=\"text-align:left; white-space: nowrap;\">");
                w.Write("{0}", item.Value);
                w.Write("</div>");
                w.Write("</div>");
            }
            w.Write("</div>");
        }

        /// <summary>
        ///     Отрисовка элементов формы заданий условий
        /// </summary>
        /// <param name="w"></param>
        /// <param name="page"></param>
        /// <param name="setValue"></param>
        private void RenderControlsUserFilterClause(TextWriter w, Page page, bool setValue)
        {
            w.Write("<div class=\"v4DivTable\">");
            for (var i = 1; i <= 2; i++)
            {
                w.Write("<div class=\"v4DivTableRow {0}\" {0}>", i == 2 ? _cssClassInterval : "");
                w.Write("<div class=\"v4DivTableCell v4PaddingCell v4PaddingTop {0}\">", _cssClassInterval);
                w.Write(" {0} ", i == 1 ? "с" : "по");
                w.Write("</div>");
                w.Write("<div class=\"v4DivTableCell v4PaddingCell v4ClauseNeedTop\">");

                RenderControlsV4UserFilterClause(w, page, i, setValue);

                w.Write("</div>");
                w.Write("</div>");
            }

            w.Write("</div>");
        }

        /// <summary>
        ///     Формирование контрола фильтра в зависимости от выбранного условия
        /// </summary>
        /// <param name="w"></param>
        /// <param name="page"></param>
        /// <param name="inx"></param>
        /// <param name="setValue"></param>
        private void RenderControlsV4UserFilterClause(TextWriter w, Page page, int inx, bool setValue)
        {
            var ctrlId = FilterUserCtrlBaseName + "_" + Settings.TreeViewId + "_" + inx;
            var nextCtrl = inx == 2
                ? "btnUFilter_Apply_" + Settings.TreeViewId
                : FilterUserCtrlBaseName + "_" + Settings.TreeViewId + "_2" + "_0";

            if (page.V4Controls.ContainsKey(ctrlId))
                page.V4Controls.Remove(ctrlId);
            var ctrlValue = "";

            if (setValue)
            {
                if (FilterUser.FilterType == TreeViewColumnUserFilterEnum.Между && inx == 2)
                {
                    if (FilterUser.FilterValue2 != null)
                    {
                        if (ColumnType != TreeViewColumnTypeEnum.Date)
                            ctrlValue = FilterUser.FilterValue2.ToString();

                        else
                            ctrlValue = ((DateTime) FilterUser.FilterValue2).ToString("dd.MM.yyyy");
                    }
                }
                else
                {
                    if (FilterUser.FilterValue1 != null)
                    {
                        if (ColumnType != TreeViewColumnTypeEnum.Date)
                            ctrlValue = FilterUser.FilterValue1.ToString();
                        else
                            ctrlValue = ((DateTime) FilterUser.FilterValue1).ToString("dd.MM.yyyy");
                    }
                }
            }

            switch (ColumnType)
            {
                case TreeViewColumnTypeEnum.Decimal:
                case TreeViewColumnTypeEnum.Double:
                case TreeViewColumnTypeEnum.Float:
                case TreeViewColumnTypeEnum.Int:
                    var ctrlN = new Number
                    {
                        HtmlID = ctrlId,
                        ID = ctrlId,
                        V4Page = page,
                        NextControl = nextCtrl,
                        Value = ctrlValue,
                        CSSClass = "v4NumberTextAlign"
                    };
                    page.V4Controls.Add(ctrlN);
                    ctrlN.RenderControl(w);
                    break;
                case TreeViewColumnTypeEnum.Date:
                    var ctrlD = new DatePicker
                    {
                        HtmlID = ctrlId,
                        ID = ctrlId,
                        V4Page = page,
                        Value = ctrlValue,
                        NextControl = nextCtrl
                    };
                    page.V4Controls.Add(ctrlD);
                    ctrlD.RenderControl(w);
                    break;
                default:
                    var ctrlT = new TextBox
                    {
                        HtmlID = ctrlId,
                        ID = ctrlId,
                        V4Page = page,
                        Value = ctrlValue,
                        NextControl = nextCtrl
                    };
                    page.V4Controls.Add(ctrlT);
                    ctrlT.RenderControl(w);
                    break;
            }
        }

        /// <summary>
        ///     Отрисовка формы задания фильтра
        /// </summary>
        /// <param name="w"></param>
        public void RenderUserFilterBlock(TextWriter w, Dictionary<string, string> fieldValuesList = null)
        {
            if (fieldValuesList == null)
            {
                var filterName = FillUserFilterClause();

                w.Write("<li>{0}{1}", FilterUser == null ? "" : _applyImage, Alias);
                w.Write("<ul>");
                foreach (var item in FilterUserClause)
                    w.Write(
                        "<li style=\"white-space:nowrap;\" data-columnId=\"{1}\" data-filterId=\"{2}\" data-filterType=\"clause\" data-filterActive=\"{4}\">{3}{0}</li>",
                        item.Value, Id, (int)item.Key, 
                        FilterUser != null && item.Key == FilterUser.FilterType ? _applyImage : "",
                        FilterUser != null && item.Key == FilterUser.FilterType ? "1" : "0");
                w.Write("</ul>");
                w.Write("</li>");
            }
            else
            {
                w.Write(
                    "<li style=\"white-space:nowrap;\" data-columnId=\"{2}\" data-filterType=\"list\" data-filterId=\"{3}\" data-filterActive=\"{4}\">{0}{1}</li>",
                    FilterUser == null ? "" : _applyImage, Alias, Id, 10, 1);

                /*
                w.Write("<li>{0}<div>{1}</div>", FilterUser == null ? "" : _applyImage, Alias);
                w.Write("<ul>");
                foreach (var item in fieldValuesList)
                    w.Write(
                        "<li><div style=\"white-space:nowrap;\" data-columnId=\"{1}\" data-filterId=\"{2}\" data-filterType=\"list\">{0}</div></li>",
                        item.Value, Id, Convert.ToInt32(item.Key));
                w.Write("</ul>");
                w.Write("</li>");
                */
            }
        }

        /// <summary>
        ///     Отрисовка формы задания фильтра
        /// </summary>
        /// <param name="w"></param>
        public void RenderStartUserFilterBlock(TextWriter w)
        {
            w.Write("<ul id=\"v4_userFilterMenu_{0}_{1}\">", Settings.TreeViewId, Id);
        }

        /// <summary>
        ///     Отрисовка формы задания фильтра
        /// </summary>
        /// <param name="w"></param>
        public void RenderEndUserFilterBlock(TextWriter w)
        {
            w.Write("</ul>");
        }

        /// <summary>
        ///     Отрисовка выбранного значения фильтра
        /// </summary>
        /// <param name="w"></param>
        /// <param name="isHtml"></param>
        public void RenderTextUserFilterBlock(TextWriter w, int filterNum, Dictionary<string, string> fieldValuesList = null, bool isHtml = true)
        {
            if (FilterUser == null)
            {
                w.Write("");
                return;
            }

            if (isHtml)
            {
                w.Write("<div class=\"v4DivTable\">");
                w.Write("<div class=\"v4DivTableRow\">");
                w.Write("<div class=\"v4DivTableCell\" style=\"text-align:left;\">");
            }

            if (isHtml)
            {
                w.Write("</div>");
                w.Write("</div>");
            }

            if (isHtml)
            {
                w.Write("<div class=\"v4DivTableRow\">");
                w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
            }

            w.Write(filterNum > 0 ? " и" : "");

            if (isHtml)
            {
                w.Write("</div>");
                w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");

                w.Write(
                    "<span class=\"ui-icon ui-icon-delete\" tabindex=\"0\" onkeydown=\"v4_element_keydown(event, this);\" title=\"{2}\" style=\"display:inline-block; cursor:pointer\" onclick=\"v4_clearAdvSearchColumnValues({0}, '{1}');\"></span>",
                    Settings.TreeViewCmdListnerIndex,
                    Id,
                    Settings.V4Page.Resx.GetString("Inv_lblDeleteFilter"));

                w.Write(
    "<span class=\"ui-icon ui-icon-pencil\" tabindex=\"0\" title=\"{4}\" onkeydown=\"v4_element_keydown(event, this);\" style=\"display:inline-block; cursor:pointer\" onclick=\"v4_OpenUserAdvSearchFilterFormCmd({1}, '{2}', {3}, 1);\"></span>",
    Settings.V4Page.Resx.GetString("lblEditFilter"),
    Settings.TreeViewCmdListnerIndex,
    Id,
    (int)FilterUser.FilterType,
    Settings.V4Page.Resx.GetString("Inv_lblEditFilter"));


                w.Write("</div>");
                w.Write("<div class=\"v4DivTableCell\" style=\"text-align:left;\">");
            }

            w.Write(" {0} [{1}] ", Settings.V4Page.Resx.GetString("lblFieldValue"), Alias);

            switch (ColumnType)
            {
                case TreeViewColumnTypeEnum.Date:
                    w.Write(Settings.V4Page.Resx.GetString(FilterUser.FilterType
                        .GetAttribute<TreeViewColumnUserFilterAttribute>().AliasDate));
                    break;
                case TreeViewColumnTypeEnum.Decimal:
                case TreeViewColumnTypeEnum.Double:
                case TreeViewColumnTypeEnum.Float:
                case TreeViewColumnTypeEnum.Int:
                    w.Write(Settings.V4Page.Resx.GetString(FilterUser.FilterType
                        .GetAttribute<TreeViewColumnUserFilterAttribute>().AliasNumber));
                    break;
                case TreeViewColumnTypeEnum.Boolean:
                    w.Write(Settings.V4Page.Resx.GetString(FilterUser.FilterType
                        .GetAttribute<TreeViewColumnUserFilterAttribute>().AliasBoolean));
                    break;
                default:
                    w.Write(Settings.V4Page.Resx.GetString(FilterUser.FilterType
                        .GetAttribute<TreeViewColumnUserFilterAttribute>().AliasString));
                    break;
            }

            if ((int) FilterUser.FilterType > 1)
            {
                w.Write(" ");

                if (FilterUser.FilterType == TreeViewColumnUserFilterEnum.Между)
                {
                    if (ColumnType == TreeViewColumnTypeEnum.Date)
                    {
                        w.Write(" {0} [{1}]", Settings.V4Page.Resx.GetString("lFrom"),
                            ((DateTime) FilterUser.FilterValue1).ToString("dd.MM.yyyy"));
                        w.Write(" {0} [{1}]", Settings.V4Page.Resx.GetString("lTo"),
                            ((DateTime) FilterUser.FilterValue2).ToString("dd.MM.yyyy"));
                    }
                    else
                    {
                        w.Write(" [{0}]", FilterUser.FilterValue1);
                        w.Write(" {0} [{1}]", Settings.V4Page.Resx.GetString("lAnd"), FilterUser.FilterValue2);
                    }
                }
                else
                {
                    if (ColumnType == TreeViewColumnTypeEnum.Date)
                        w.Write("[{0}]", ((DateTime) FilterUser.FilterValue1).ToString("dd.MM.yyyy"));
                    else if (ColumnType == TreeViewColumnTypeEnum.Boolean)
                        w.Write(" ");
                    else if (fieldValuesList != null)
                    {
                        w.Write("[");
                        var itemCount = 0;
                        var allList = "";
                        var firstList = "";
                        foreach (var item in FilterUser.FilterValue1.ToString().Split(','))
                        {
                            if (itemCount > 0)
                            {
                                allList += " или ";
                            }
                            else
                            {
                                firstList = fieldValuesList[item];
                            }
                            allList += fieldValuesList[item] + " ";
                            itemCount++;
                        }
                        
                        if (!isHtml || itemCount == 1)
                            w.Write(allList);
                        else
                            w.Write(firstList + " <span title='{0}'>или ...</span>", allList);
                        
                        w.Write("]");
                    }
                    //w.Write("[{0}]", fieldValuesList[FilterUser.FilterValue1.ToString()]);
                    else
                        w.Write("[{0}]", FilterUser.FilterValue1);
                }
            }

            if (isHtml)
            {
                w.Write("</div>");
                w.Write("</div>");
                w.Write("</div>");
            }
        }

        #endregion

        /// <summary>
        ///     Отрисовка кнопки отмены фильтрации
        /// </summary>
        /// <param name="w"></param>
        private void RenderClearFilterBlock(TextWriter w)
        {
            w.Write("<div class=\"v4DivTable\">");

            w.Write("<div class=\"v4DivTableRow\">");

            w.Write(
                "<div class=\"v4DivTableCell v4PaddingCell\">");

            w.Write(
                "<span class=\"ui-icon ui-icon-delete\" tabindex=\"0\" onkeydown=\"v4_element_keydown(event, this);\" style=\"display: inline-block;cursor:pointer\" onclick=\"v4_clearAdvSearchColumnValues({0}, '{1}');\"></span>",
                Settings.TreeViewCmdListnerIndex, Id);

            w.Write(
                "</div><div class=\"v4DivTableCell\" tabindex=\"0\" onkeydown=\"v4_element_keydown(event, this);\" style=\"text-align:left;\"><a onclick=\"v4_clearAdvSearchColumnValues({0},'{1}');\"><nobr>{2}</nobr></a></div>",
                Settings.TreeViewCmdListnerIndex, Id, Settings.V4Page.Resx.GetString("Inv_lblDeleteFilterFromColumn") + " «" + Alias + "»");
            w.Write("</div>");
            w.Write("</div>");
            w.Write("</div>");
        }

        #endregion

        #region Fill

        /// <summary>
        ///     Вывод наименования типа фильтра
        /// </summary>
        /// <returns></returns>
        private string FillUserFilterClause()
        {
            var filterName = "";
            FilterUserClause = new Dictionary<TreeViewColumnUserFilterEnum, string>();
            switch (ColumnType)
            {
                case TreeViewColumnTypeEnum.Decimal:
                case TreeViewColumnTypeEnum.Float:
                case TreeViewColumnTypeEnum.Double:
                case TreeViewColumnTypeEnum.Int:
                    filterName = Settings.V4Page.Resx.GetString("lblTreeViewSetNumericFilter");
                    FillUserFilterClauseInt(IsAllowNull);
                    break;
                case TreeViewColumnTypeEnum.Date:
                    filterName = Settings.V4Page.Resx.GetString("lblTreeViewSetDateFilter");
                    FillUserFilterClauseDate(IsAllowNull);
                    break;
                case TreeViewColumnTypeEnum.Boolean:
                    filterName = Settings.V4Page.Resx.GetString("lblTreeViewSetBooleanFilter");
                    FillUserFilterClauseBoolean();
                    break;
                default:
                    filterName = Settings.V4Page.Resx.GetString("lblTreeViewSetTextFilter");
                    FillUserFilterClauseString(IsAllowNull);
                    break;
            }

            return filterName;
        }

        /// <summary>
        ///     Формирование условий для фильтра типа даты
        /// </summary>
        private void FillUserFilterClauseDate(bool isAllowNull)
        {
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Равно,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Равно.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasDate) + "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.НеРавно,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.НеРавно.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasDate) + "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Больше,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Больше.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasDate) + "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.БольшеИлиРавно,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.БольшеИлиРавно.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasDate) +
                "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Меньше,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Меньше.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasDate) + "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.МеньшеИлиРавно,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.МеньшеИлиРавно.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasDate) +
                "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Между,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Между.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasDate) + "...");

            if (isAllowNull)
            {
                FilterUserClause.Add(TreeViewColumnUserFilterEnum.Указано,
                    Settings.V4Page.Resx.GetString(
                        TreeViewColumnUserFilterEnum.Указано.GetAttribute<TreeViewColumnUserFilterAttribute>()
                            .AliasDate));
                FilterUserClause.Add(TreeViewColumnUserFilterEnum.НеУказано,
                    Settings.V4Page.Resx.GetString(
                        TreeViewColumnUserFilterEnum.НеУказано.GetAttribute<TreeViewColumnUserFilterAttribute>()
                            .AliasDate));
            }
        }

        /// <summary>
        ///     Формирование условий для фильтра логического типа
        /// </summary>
        private void FillUserFilterClauseBoolean()
        {
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Да,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Да.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasBoolean));
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Нет,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Нет.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasBoolean));
        }

        /// <summary>
        ///     Формирование условий для фильтра строкового типа
        /// </summary>
        private void FillUserFilterClauseString(bool isAllowNull)
        {
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Равно,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Равно.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasString) + "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.НеРавно,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.НеРавно.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasString) +
                "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.НачинаетсяС,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.НачинаетсяС.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasString) +
                "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.ЗаканчиваетсяНа,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.ЗаканчиваетсяНа.GetAttribute<TreeViewColumnUserFilterAttribute>()
                        .AliasString) +
                "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Содержит,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Содержит.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasString) +
                "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.НеСодержит,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.НеСодержит.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasString) +
                "...");

            if (isAllowNull)
            {
                FilterUserClause.Add(TreeViewColumnUserFilterEnum.НеУказано,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.НеУказано.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasString));
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Указано,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Указано.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasString));
            }
        }

        /// <summary>
        ///     Формирование условий для фильтра цифрового типа
        /// </summary>
        private void FillUserFilterClauseInt(bool isAllowNull)
        {
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Равно,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Равно.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasNumber) + "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.НеРавно,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.НеРавно.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasNumber) +
                "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Больше,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Больше.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasNumber) + "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.БольшеИлиРавно,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.БольшеИлиРавно.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasNumber) +
                "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Меньше,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Меньше.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasNumber) + "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.МеньшеИлиРавно,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.МеньшеИлиРавно.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasNumber) +
                "...");
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.Между,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Между.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasNumber) + "...");

            if (isAllowNull)
            {
                FilterUserClause.Add(TreeViewColumnUserFilterEnum.Указано,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.Указано.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasNumber));
            FilterUserClause.Add(TreeViewColumnUserFilterEnum.НеУказано,
                Settings.V4Page.Resx.GetString(
                    TreeViewColumnUserFilterEnum.НеУказано.GetAttribute<TreeViewColumnUserFilterAttribute>().AliasNumber));
            }
        }

        #endregion
    }
}