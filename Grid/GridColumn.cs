using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Web.Controls.V4.Common;
using Convert = Kesco.Lib.ConvertExtention.Convert;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    /// <summary>
    /// Класс, описывающий колонку в контроле Grid
    /// </summary>
    public class GridColumn
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="settings">Настройки грида</param>
        public GridColumn(GridSettings settings)
        {
            Settings = settings;
        }

        private readonly string _applyImage = "<img src=\"/styles/ok.gif\" border=0/>";
        private readonly string _cssClassInterval = "filterType" + (int) GridColumnUserFilterEnum.Между;
        
        public string FilterUserCtrlBaseName = "v4_ctrlFilterClause";

        public Dictionary<object, object> FilterStandartType;
        public Dictionary<object, object> FilterUniqueValues;
        public Dictionary<object, object> UniqueValues;
        public Dictionary<object, object> UniqueValuesOriginal;

        public Dictionary<GridColumnUserFilterEnum, string> FilterUserClause;

        public GridColumnUserFilter FilterUser;
        
        public string FormatString { get; set; }

        public string ScaleFieldName { get; set; }
        public bool IsScaleByValue { get; set; }
        public int MaxScale { get; set; }
        public int DefaultScale { get; set; }

        public bool IsTimeSecond { get; set; }

        public bool IsBit { get; set; }
       
        public string HrefIdFieldName { get; set; }
        public string HrefUri { get; set; }
        public bool HrefIsDocument { get; set; }
        public bool HrefIsEmployee { get; set; }
        public Dictionary<string, Dictionary<string, string>> HrefClauses;
        public bool HrefIsClause { get; set; }
        
        public string SumValuesText { get; set; }
        public bool IsSumValues { get; set; }

        public string BackGroundColor { get; set; }
        public string HeaderTitle { get; set; }
        public string Title { get; set; }
        
        public GridColumnFilterEqualEnum FilterEqual { get; set; }
        public GridColumnOrderByDirectionEnum OrderByDirection { get; set; }
        public GridColumnTypeEnum ColumnType { get; set; }
        
        public string Id { get; set; }
        public string FieldName { get; set; }
        public string Alias { get; set; }

        public int? OrderByNumber { get; set; }
        public int DisplayOrder { get; set; }
        public bool DisplayVisible { get; set; }
        public int FilterOrder { get; set; }

        public string TextAlign { get; set; }
        public bool IsNoWrap { get; set; }

        private GridSettings Settings { get; set; }

        #region Render

        /// <summary>
        ///     Формирование данных в колонке в зависимости от ее типа
        /// </summary>
        /// <param name="w"></param>
        /// <param name="dr"></param>
        public void RenderColumnData(TextWriter w, DataRow dr)
        {
            var sbAdvProp = new StringBuilder();
            var textAlignStyle =  (!string.IsNullOrEmpty(TextAlign))? string.Format("text-align:{0} !important;", TextAlign):"";
            var textNoWrap = IsNoWrap ? "white-space: nowrap;" : "";

            sbAdvProp.AppendFormat(" style=\"{0}{1}{2}\" ", 
                !string.IsNullOrEmpty(BackGroundColor) ? BackGroundColor + ";" : "",
                textAlignStyle,
                textNoWrap);

            if (!string.IsNullOrEmpty(Title))
                sbAdvProp.AppendFormat(" title=\"{0}\" ", Title);

        
            switch (ColumnType)
            {
                case GridColumnTypeEnum.Date:
                    w.Write("<td {0}>{1}</td>", sbAdvProp,
                        FormatString != ""
                            ? ((DateTime) dr[FieldName]).ToString(FormatString)
                            : dr[FieldName].ToString());
                    break;
                case GridColumnTypeEnum.Double:
                case GridColumnTypeEnum.Float:
                case GridColumnTypeEnum.Decimal:

                    w.Write("<td {0} class=\"v4NumberTextAlign\">", sbAdvProp);

                    int scale = DefaultScale;
                    if (!string.IsNullOrEmpty(ScaleFieldName))
                        scale = (dr[ScaleFieldName] == null ? DefaultScale : (int) dr[ScaleFieldName]);
                    else if (IsScaleByValue)
                    {
                        if (ColumnType == GridColumnTypeEnum.Decimal)
                            scale = ((Decimal) dr[FieldName]).GetScaleValue(DefaultScale, MaxScale);
                    }

                    if (ColumnType == GridColumnTypeEnum.Decimal)
                        w.Write(((Decimal) dr[FieldName]).ToString(FormatString + scale));
                    else
                        w.Write(((double) dr[FieldName]).ToString(FormatString + scale));

                    w.Write("</td>");
                    break;

                case GridColumnTypeEnum.Int:
                    if (IsTimeSecond)
                        w.Write("<td {0} class=\"v4NumberTextAlign\">{1}</td>", sbAdvProp,
                            Convert.Second2TimeFormat((int) dr[FieldName]));
                    else
                        w.Write("<td {0} class=\"v4NumberTextAlign\">{1}</td>", sbAdvProp, dr[FieldName]);
                    break;
                case GridColumnTypeEnum.Boolean:
                    if (IsBit)
                        w.Write("<td {0}>{1}</td>", sbAdvProp,
                            dr[FieldName].ToString()=="1" ? "<img src='/styles/ok.gif' border='0' />" : "&nbsp;"
                            );
                    else
                        w.Write("<td {0}>{1}</td>", sbAdvProp, dr[FieldName]);
                    break;
                default:
                    w.Write("<td {0}>", sbAdvProp);

                    if (dr[FieldName].Equals(DBNull.Value) || dr[FieldName].ToString() == "")
                        w.Write("&nbsp;");
                    else
                    {
                        if ((!string.IsNullOrEmpty(HrefIdFieldName) && !dr[HrefIdFieldName].Equals(DBNull.Value)) || HrefIsClause)
                        {
                            if (HrefIsDocument)
                            {
                                Settings.V4Page.RenderLinkDocument(w, int.Parse(dr[HrefIdFieldName].ToString()));
                                w.Write(dr[FieldName]);
                                Settings.V4Page.RenderLinkEnd(w);
                            }
                            else if(HrefIsEmployee)
                            {
                                var htmlId = Guid.NewGuid().ToString();
                                Settings.V4Page.RenderLinkEmployee(w, htmlId, dr[HrefIdFieldName].ToString(), dr[FieldName].ToString(), NtfStatus.Empty);
                            }
                            else if (HrefIsClause)
                            {
                                if (HrefClauses.Count == 0)
                                    w.Write(dr[FieldName]);
                                else
                                {
                                    var fieldWithid = "";
                                    var hrefByField = "";

                                    foreach (var field in HrefClauses)
                                    {
                                        foreach (var clause in field.Value.Where(clause => dr[clause.Key] != null && int.Parse(dr[clause.Key].ToString()) == 1))
                                        {
                                            fieldWithid = field.Key;
                                            hrefByField = clause.Value;
                                        }
                                    }

                                    if (string.IsNullOrEmpty(fieldWithid))
                                        w.Write(dr[FieldName]);
                                    else
                                        Settings.V4Page.RenderLink(w, dr[FieldName].ToString(),
                                                hrefByField + (hrefByField.Contains("?") ? "&" : "?") + "id=" + dr[fieldWithid], "200");
                                }
                            }
                            else
                                Settings.V4Page.RenderLink(w, dr[FieldName].ToString(),
                                    HrefUri + (HrefUri.Contains("?") ? "&" : "?") + "id=" + dr[HrefIdFieldName], "200");
                        }
                        else
                            w.Write(dr[FieldName]);
                    }
                    w.Write("</td>");
                    break;
            }
        }

        /// <summary>
        ///     Формирование итоговых значений (footer)
        /// </summary>
        /// <param name="w"></param>
        /// <param name="value"></param>
        public void RenderColumnDataSumFooter(TextWriter w, object value)
        {
            if (!IsSumValues && string.IsNullOrEmpty(SumValuesText))
            {
                w.Write("<td>{0}</td>", "&nbsp;");
                return;
            }

            if (!string.IsNullOrEmpty(SumValuesText))
            {
                w.Write("<td class=\"v4NumberTextAlign v4Bold\">{0}:</td>", SumValuesText);
                return;
            }

            switch (ColumnType)
            {
                case GridColumnTypeEnum.Decimal:
                case GridColumnTypeEnum.Double:
                case GridColumnTypeEnum.Float:
                    w.Write("<td class=\"v4NumberTextAlign v4Bold\">{0}</td>",
                        FormatString != "" ? ((Decimal) value).ToString(FormatString + DefaultScale) : value.ToString());
                    break;
                case GridColumnTypeEnum.Int:
                    if (IsTimeSecond)
                        w.Write("<td class=\"v4NumberTextAlign v4Bold\">{0}</td>",
                            Convert.Second2TimeFormat((int) value));
                    else
                        w.Write("<td class=\"v4NumberTextAlign v4Bold\">{0}</td>", value);
                    break;
                default:
                    w.Write("<td class=\"v4Bold\">{0}</td>", value);
                    break;
            }
        }

        #region Render User Filter

        /// <summary>
        ///     Формирование формы пользовательских фильтров
        /// </summary>
        /// <param name="page"></param>
        /// <param name="filterId"></param>
        /// <param name="setValue"></param>
        public void RenderColumnUserFilterForm(Page page, string filterId, string setValue)
        {
            var w = new StringWriter();
            RenderColumnUserFilterForm_Clause(w, page, filterId, setValue.Equals("1"));

            page.JS.Write("v4_columnSettingsUserFilterForm(\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\");",
                Settings.GridId,
                Settings.GridCmdListnerIndex,
                filterId,
                Id,
                HttpUtility.JavaScriptStringEncode(string.Format("{0}",
                    Settings.V4Page.Resx.GetString("lblSettingFilter"))));

            page.JS.Write("$('#divColumnSettingsUserFilterForm_Header_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(string.Format("{0} [{1}]",
                    Settings.V4Page.Resx.GetString("lblFieldValue"), Alias)));
            page.JS.Write("$('#divColumnSettingsUserFilterForm_Body_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(w.ToString()));

            page.JS.Write(
                "$('#v4_selectFilterUserClause_{2}').selectmenu({{width : 'auto', change: function() {{v4_selectFilterUserClause_OnChange(\"{2}\", this, {0}, \"{1}\");}}}}); ",
                (int) GridColumnUserFilterEnum.Между, _cssClassInterval, Settings.GridId);
            page.JS.Write("v4_selectFilterUserClause_OnChange(\"{2}\", null, {0}, \"{1}\");",
                (int) GridColumnUserFilterEnum.Между, _cssClassInterval, Settings.GridId);
            page.JS.Write("setTimeout(function(){$(\"#v4_ctrlFilterClause_1_0\").focus();},10);");
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

            w.Write(@"<select name=""{0}_{1}"" id=""{0}_{1}"">", "v4_selectFilterUserClause", Settings.GridId);
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
            RenderControlsUserFilterClause(w, page, setValue);
            w.Write("</div>");
            w.Write("</div>");
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
            var ctrlId = FilterUserCtrlBaseName + "_" + inx;
            var nextCtrl = inx == 2 ? "btnUFilter_Apply" : FilterUserCtrlBaseName + "_2" + "_0";

            if (page.V4Controls.ContainsKey(ctrlId))
                page.V4Controls.Remove(ctrlId);
            var ctrlValue = "";

            if (setValue)
            {
                if (FilterUser.FilterType == GridColumnUserFilterEnum.Между && inx == 2)
                {
                    if (FilterUser.FilterValue2 != null)
                    {
                        if (ColumnType != GridColumnTypeEnum.Date)
                            ctrlValue = FilterUser.FilterValue2.ToString();

                        else
                            ctrlValue = ((DateTime) FilterUser.FilterValue2).ToString("dd.MM.yyyy");
                    }
                }
                else
                {
                    if (FilterUser.FilterValue1 != null)
                    {
                        if (ColumnType != GridColumnTypeEnum.Date)
                            ctrlValue = FilterUser.FilterValue1.ToString();
                        else
                            ctrlValue = ((DateTime) FilterUser.FilterValue1).ToString("dd.MM.yyyy");
                    }
                }
            }

            switch (ColumnType)
            {
                case GridColumnTypeEnum.Decimal:
                case GridColumnTypeEnum.Double:
                case GridColumnTypeEnum.Float:
                case GridColumnTypeEnum.Int:
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
                case GridColumnTypeEnum.Date:
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
        private void RenderUserFilterBlock(TextWriter w)
        {
            var filterName = FillUserFilterClause();

            w.Write("<ul id=\"v4_userFilterMenu_{0}\">", Settings.GridId);
            w.Write("<li><div>{0}</div>", filterName);
            w.Write("<ul>");
            foreach (var item in FilterUserClause)
            {
                w.Write(
                    "<li><div style=\"white-space:nowrap;\" data-columnId=\"{1}\" data-filterId=\"{2}\">{0}</div></li>",
                    item.Value, Id, (int) item.Key);
            }
            w.Write("</ul>");
            w.Write("</li>");
            w.Write("</ul>");
        }

        /// <summary>
        ///     Отрисовка выборанного значения фильтра
        /// </summary>
        /// <param name="w"></param>
        /// <param name="isHtml"></param>
        private void RenderTextUserFilterBlock(TextWriter w, bool isHtml = true)
        {
            if (FilterUser == null)
            {
                w.Write("");
                return;
            }
            if (isHtml)
            {
                w.Write("<div >");
                w.Write(_applyImage);
            }
            w.Write(Settings.V4Page.Resx.GetString("lblSetFilter") + ": ");
            if (isHtml) w.Write("</div>");

            if (isHtml)
            {
                w.Write("<div class=\"v4GridFilterText\">");
                w.Write(
                    "<img src=\"/styles/EditGray.gif\" style=\"cursor:pointer\" title=\"{0}\" onclick=\"v4_OpenUserFilterFormCmd({1}, '{2}', {3}, 1);\" />&nbsp;",
                    Settings.V4Page.Resx.GetString("lblEditFilter"),
                    Settings.GridCmdListnerIndex,
                    Id,
                    (int) FilterUser.FilterType);
            }
            w.Write("{0} [{1}] ", Settings.V4Page.Resx.GetString("lblFieldValue"), Alias);
            switch (ColumnType)
            {
                case GridColumnTypeEnum.Date:
                    w.Write(FilterUser.FilterType.GetAttribute<GridColumnUserFilterAttribute>().AliasDate);
                    break;
                case GridColumnTypeEnum.Decimal:
                case GridColumnTypeEnum.Double:
                case GridColumnTypeEnum.Float:
                case GridColumnTypeEnum.Int:
                    w.Write(FilterUser.FilterType.GetAttribute<GridColumnUserFilterAttribute>().AliasNumber);
                    break;
                default:
                    w.Write(FilterUser.FilterType.GetAttribute<GridColumnUserFilterAttribute>().AliasString);
                    break;
            }

            if ((int) FilterUser.FilterType > 1)
            {
                w.Write(" ");

                if (FilterUser.FilterType == GridColumnUserFilterEnum.Между)
                {
                    if (ColumnType == GridColumnTypeEnum.Date)
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
                    if (ColumnType == GridColumnTypeEnum.Date)
                        w.Write("[{0}]", ((DateTime) FilterUser.FilterValue1).ToString("dd.MM.yyyy"));
                    else
                        w.Write("[{0}]", FilterUser.FilterValue1);
                }
            }

            if (isHtml) w.Write("</div>");
        }

        #endregion

        #region Render Values Block

        /// <summary>
        ///     Формирование контрола выбора всех значений колонки
        /// </summary>
        /// <param name="w"></param>
        private void RenderAllValuesBlock(TextWriter w)
        {
            var disabled = FilterUser != null;
            if (UniqueValues == null) return;
            w.Write("<div class=\"v4DivTable\">");
            w.Write("<div class=\"v4DivTableRow\">");
            w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
            w.Write(
                "<input type='checkbox' id=\"cfAllValues\" {0} {1} onclick =\"v4_columnValuesChecked(this.checked, 'classValueCheckBox');\">",
                disabled ? "" : "checked", disabled ? "disabled" : "");
            w.Write("</div>");

            w.Write("<div class=\"v4DivTableCell v4PaddingCell\" style=\"text-align:left; white-space: nowrap;\">");
            w.Write("({0})", Settings.V4Page.Resx.GetString("lblSelectAll"));
            w.Write("</div>");

            w.Write("</div>");
            w.Write("</div>");
        }

        /// <summary>
        ///     Формирование контролов выбора уникальных значений колонки
        /// </summary>
        /// <param name="w"></param>
        private void RenderValuesBlock(TextWriter w)
        {
            if (UniqueValues == null)
            {
                w.Write("<div class=\"v4GridFilterText\">{0}</div>", Settings.V4Page.Resx.GetString("msgGridUniqError"));
                w.Write("<div class=\"v4GridFilterText\">{0}<div>",
                    Settings.V4Page.Resx.GetString("msgGridUseFilterFieldType"));
                return;
            }
            var disabled = FilterUser != null;
            w.Write("<div class=\"v4DivTable\" >");
            var existEmpty = false;
            var existValues = false;
            var existFilter = FilterUniqueValues != null && FilterUniqueValues.Count > 0;
            var valueInFilter = false;
            var checkState = "";


            foreach (var item in UniqueValues)
            {
                if (item.Value.ToString().Length == 0)
                {
                    existEmpty = true;
                    continue;
                }
                if (!existValues) existValues = true;

                if (existFilter)
                {
                    var r =
                        FilterUniqueValues.Where(x => x.Key.Equals(item.Key))
                            .Select(x => (KeyValuePair<object, object>?) x)
                            .FirstOrDefault();
                    valueInFilter = r != null;
                }

                if ((!existFilter || valueInFilter && FilterEqual == GridColumnFilterEqualEnum.In)
                    || (existFilter && !valueInFilter && FilterEqual == GridColumnFilterEqualEnum.NotIn))
                    checkState = "checked";
                else
                    checkState = "";

                w.Write("<div class=\"v4DivTableRow\">");
                w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
                w.Write(
                    "<input type='checkbox' class=\"classValueCheckBox\" {0} {2} data-id=\"{1}\" onclick=\"v4_setStateCheckAllValues('cfAllValues', 'classValueCheckBox');\">",
                    disabled ? "" : checkState,
                    item.Key,
                    disabled ? "disabled" : "");
                w.Write("</div>");

                w.Write("<div class=\"v4DivTableCell v4PaddingCell\" style=\"text-align:left; white-space: nowrap;\">");

                switch (ColumnType)
                {
                    case GridColumnTypeEnum.Date:
                        w.Write(FormatString != "" ? ((DateTime) item.Value).ToString(FormatString) : item.Value);
                        break;
                    case GridColumnTypeEnum.Decimal:
                    case GridColumnTypeEnum.Double:
                    case GridColumnTypeEnum.Float:
                        if (ColumnType == GridColumnTypeEnum.Decimal)
                            w.Write(FormatString != ""
                                ? ((Decimal) item.Value).ToString(FormatString + DefaultScale)
                                : item.Value.ToString());
                        else
                            w.Write(FormatString != ""
                               ? ((double)item.Value).ToString(FormatString + DefaultScale)
                               : item.Value.ToString());
                        break;
                    case GridColumnTypeEnum.Int:
                        if (IsTimeSecond)
                            w.Write(Convert.Second2TimeFormat((int) item.Value));
                        else
                            w.Write(item.Value);
                        break;
                    default:
                        w.Write(item.Value);
                        break;
                }


                w.Write("</div>");

                w.Write("</div>");
            }

            if (existEmpty || !existValues)
            {
                w.Write("<div class=\"v4DivTableRow\">");
                w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
                checkState = "checked";
                if (existFilter)
                {
                    var r =
                        FilterUniqueValues.Where(x => x.Key.Equals(0))
                            .Select(x => (KeyValuePair<object, object>?) x)
                            .FirstOrDefault();
                    valueInFilter = r != null;

                    if ((valueInFilter && FilterEqual == GridColumnFilterEqualEnum.In)
                        || (!valueInFilter && FilterEqual == GridColumnFilterEqualEnum.NotIn))
                        checkState = "checked";
                    else
                        checkState = "";
                }

                w.Write(
                    "<input type='checkbox' class=\"classValueCheckBox\" {0} {1} data-id=\"0\" onclick=\"v4_setStateCheckAllValues('cfAllValues', 'classValueCheckBox');\">",
                    checkState,
                    disabled ? "disabled" : ""
                    );
                w.Write("</div>");

                w.Write("<div class=\"v4DivTableCell v4PaddingCell\" style=\"text-align:left; white-space: nowrap;\">");
                w.Write("({0})", Settings.V4Page.Resx.GetString("lEmpty"));
                w.Write("</div>");

                w.Write("</div>");
            }

            w.Write("</div>");
        }

        #endregion

        /// <summary>
        ///     Формирование заголовков
        /// </summary>
        /// <param name="w"></param>
        public void RenderColumnSettingsHeader(TextWriter w)
        {
            if (Settings.IsPrintVersion)
            {
                w.Write(@"<th>{0}</th>", Alias);
                return;
            }

            var titleFilter = "";
            var filterClick = "";

            if (FilterUser != null)
            {
                var wr = new StringWriter();
                RenderTextUserFilterBlock(wr, false);
                titleFilter = wr.ToString();

                filterClick = string.Join(" ", "style=\"cursor:pointer\"", " ",
                    string.Format("onclick=\"v4_OpenUserFilterFormCmd({0}, '{1}', {2}, 1);\"",
                        Settings.GridCmdListnerIndex,
                        Id,
                        (int) FilterUser.FilterType
                        ));
            }
            else if (FilterUniqueValues != null && FilterUniqueValues.Count > 0)
            {
                var wr = new StringWriter();
                RenderTextValuesFilterBlock(wr);
                titleFilter = wr.ToString();

                filterClick = string.Join(" ", "style=\"cursor:pointer\"", " ",
                    string.Format(
                        "onclick=\"Wait.render(true); cmdasync('cmd', 'Listener', 'ctrlId', {0}, 'cmdName', 'RenderColumnSettings','ColumnId','{1}');\"",
                        Settings.GridCmdListnerIndex, FieldName));
            }
            w.Write(@"
<th {1}>
    <div class=""v4DivTable"">
	    <div class=""v4DivTableRow"">
		    <div class=""v4DivTableCell v4PaddingCell"" style=""width:100%;"">{0}</div>
		    <div class=""v4DivTableCell v4PaddingCell"" style=""text-align:right;""><nobr>{2}{3}{4}</nobr></div>
	    </div>
    </div>
</th>
",
                "<span class=\"v4GroupingData\">" + Alias + "</span>",
                !string.IsNullOrEmpty(HeaderTitle) ? string.Format("title=\"{0}\"", HeaderTitle) : "",
                string.Format(
                    "<img src=\"/styles/DownGrayed.gif\" id=\"imgSettings{1}_{3}\" style=\"cursor:pointer\" onclick=\" Wait.render(true); cmdasync('cmd', 'Listener', 'ctrlId', '{3}', 'cmdName', 'RenderColumnSettings','ColumnId','{0}');\" border=0 title=\"{2}\"/>",
                    FieldName, DisplayOrder, Settings.V4Page.Resx.GetString("msgOpenSettingFilter"),
                    Settings.GridCmdListnerIndex),
                (FilterUniqueValues != null && FilterUniqueValues.Count > 0) || FilterUser != null
                    ? string.Format("<img src=\"/styles/FilterApply.gif\" border=\"0\" title=\"{0}\" {1}/>", titleFilter,
                        filterClick)
                    : "",
                OrderByNumber != null
                    ? "<img src=\"/styles/" +
                      (OrderByDirection == GridColumnOrderByDirectionEnum.Asc ? "sort-asc.png" : "sort-desc.png")
                      +
                      string.Format(
                          "\" border=\"0\" title=\"{0} {1}. {2}\" style=\"cursor:pointer\" onclick=\"v4_setOrderByColumnValues({3}, '{4}',{5});\"/>",
                          Settings.V4Page.Resx.GetString("lblSortedBy"),
                          OrderByDirection == GridColumnOrderByDirectionEnum.Asc
                              ? Settings.V4Page.Resx.GetString("lblAscendingSort")
                              : Settings.V4Page.Resx.GetString("lblDescendingSort"),
                          Settings.V4Page.Resx.GetString("msgGrigSordBackOrder"),
                          Settings.GridCmdListnerIndex,
                          Id,
                          OrderByDirection == GridColumnOrderByDirectionEnum.Asc ? 1 : 0)
                      +
                      string.Format("<span style=\"font-size:5pt\" title=\"{1}\">{0}</span>", OrderByNumber,
                          Settings.V4Page.Resx.GetString("lblColumnSortOrder"))
                    : ""
                );
        }

        /// <summary>
        /// </summary>
        /// <param name="page"></param>
        public void RenderColumnSettings(Page page)
        {
            var w = new StringWriter();
            RenderSortBlock(w);
            page.JS.Write("$('#divColumnSettingsForm_Sort_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(w.ToString()));

            if ((FilterUniqueValues != null && FilterUniqueValues.Count > 0) || FilterUser != null)
            {
                w = new StringWriter();
                RenderClearFilterBlock(w);
                page.JS.Write(
                    "$('#divColumnSettingsForm_ClearUserFilter_{0}').html('{1}');$('#divColumnSettingsForm_ClearUserFilter_{0}').show();",
                    Settings.GridId,
                    HttpUtility.JavaScriptStringEncode(w.ToString()));
            }
            else
            {
                page.JS.Write(
                    "$('#divColumnSettingsForm_ClearUserFilter_{0}').html('');$('#divColumnSettingsForm_ClearUserFilter_{0}').hide();",
                    Settings.GridId);
            }


            if (ColumnType != GridColumnTypeEnum.Boolean)
            {
                w = new StringWriter();
                if (FilterUser == null) RenderUserFilterBlock(w);
                RenderTextUserFilterBlock(w);
                page.JS.Write(
                    "$('#divColumnSettingsForm_UserFilter_{0}').html('{1}');$('#divColumnSettingsForm_UserFilter_{0}').show();",
                    Settings.GridId,
                    HttpUtility.JavaScriptStringEncode(w.ToString()));
                page.JS.Write(
                    "$('#v4_userFilterMenu_{0}').menu({{select: function(event, ui) {{v4_openUserFilterForm(ui.item.children(), {1});}}}});",
                    Settings.GridId, Settings.GridCmdListnerIndex);
            }
            else
            {
                page.JS.Write(
                    "$('#divColumnSettingsForm_UserFilter_{0}').html('');$('#divColumnSettingsForm_UserFilter_{0}').hide();",
                    Settings.GridId);
            }

            FillUniqueValues();
            w = new StringWriter();
            RenderAllValuesBlock(w);
            page.JS.Write("$('#divColumnSettingsForm_AllValues_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(w.ToString()));

            w = new StringWriter();
            RenderValuesBlock(w);
            page.JS.Write("$('#divColumnSettingsForm_Values_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(w.ToString()));

            page.JS.Write("v4_setStateCheckAllValues('cfAllValues', 'classValueCheckBox');");
            page.JS.Write("v4_columnSettingsForm('{0}', {1}, 'imgSettings{2}_{1}', 'classValueCheckBox', '{3}', '{4}');",
                Settings.GridId,
                Settings.GridCmdListnerIndex,
                DisplayOrder,
                Id,
                HttpUtility.JavaScriptStringEncode(string.Format("{0} [{1}]",
                    Settings.V4Page.Resx.GetString("lblFilterAndSort"), Alias)));
        }

        private void RenderSortBlock(TextWriter w)
        {
            var asc = Settings.V4Page.Resx.GetString("msgSortOrderAZ");
            var desc = Settings.V4Page.Resx.GetString("msgSortOrderZA");
            var classSortAsc = "";
            var classSortDesc = "";


            if (ColumnType.Equals(GridColumnTypeEnum.Int) || ColumnType.Equals(GridColumnTypeEnum.Double) || ColumnType.Equals(GridColumnTypeEnum.Decimal) || ColumnType.Equals(GridColumnTypeEnum.Boolean))
            {
                asc = Settings.V4Page.Resx.GetString("msgSortOrderMinMax");
                desc = Settings.V4Page.Resx.GetString("msgSortOrderMaxMin");
            }

            if (OrderByNumber != null)
            {
                w.Write("<div class=\"v4DivTable\">");
                w.Write("<div class=\"v4DivTableRow\">");

                w.Write(
                    "<div class=\"v4DivTableCell v4PaddingCell\"><img  style=\"cursor:pointer;\" src=\"/styles/delete.gif\" onclick=\"v4_clearOrderByColumnValues({0}, '{1}');\"/></div><div class=\"v4DivTableCell\" style=\"text-align:left;\"><a href=\"javascript:void(0);\" onclick=\"v4_clearOrderByColumnValues({0}, '{1}');\"><nobr>{2}</nobr></a></div>",
                    Settings.GridCmdListnerIndex, Id, Settings.V4Page.Resx.GetString("msgGrigNoSort"));
                w.Write("</div>");

                w.Write("</div>");
                w.Write("</div>");

                if (OrderByDirection == GridColumnOrderByDirectionEnum.Asc)
                    classSortAsc = _applyImage;
                else
                    classSortDesc = _applyImage;
            }

            w.Write("<div class=\"v4DivTable {0}\">", OrderByNumber != null ? "v4PaddingTop" : "");
            w.Write("<div class=\"v4DivTableRow\">");
            w.Write(
                "<div class=\"v4DivTableCell v4PaddingCell\">{0}<img style=\"cursor:pointer;\" src=\"/styles/sort-asc.png\" onclick=\"v4_setOrderByColumnValues({1}, '{2}', 0);\" /></div><div class=\"v4DivTableCell v4PaddingCell\" style=\"margin-left:15px\"><a href=\"javascript:void(0);\" onclick=\"v4_setOrderByColumnValues({1}, '{2}', 0);\"><nobr>{3}</nobr></a></div>",
                classSortAsc, Settings.GridCmdListnerIndex, Id, asc);
            w.Write("</div>");
            w.Write("<div class=\"v4DivTableRow\" style=\"margin-top:50px;\">");
            w.Write(
                "<div class=\"v4DivTableCell v4PaddingCell\">{0}<img style=\"cursor:pointer;\" src=\"/styles/sort-desc.png\" onclick=\"v4_setOrderByColumnValues({1}, '{2}', 1);\" /></div><div class=\"v4DivTableCell v4PaddingCell\" style=\"margin-left:15px\"><a href=\"javascript:void(0);\" onclick=\"v4_setOrderByColumnValues({1}, '{2}', 1);\"><nobr>{3}</nobr></a></div>",
                classSortDesc, Settings.GridCmdListnerIndex, Id, desc);
            w.Write("</div>");


            w.Write("</div>");
        }

        /// <summary>
        ///     Отрисовка кнопки отмены сортировки
        /// </summary>
        /// <param name="w"></param>
        private void RenderClearFilterBlock(TextWriter w)
        {
            w.Write("<div class=\"v4DivTable\">");

            w.Write("<div class=\"v4DivTableRow\">");

            w.Write(
                "<div class=\"v4DivTableCell v4PaddingCell\"><img  style=\"cursor:pointer;\" src=\"/styles/delete.gif\" onclick=\"v4_clearFilterColumnValues({0}, '{1}');\"/></div><div class=\"v4DivTableCell\" style=\"text-align:left;\"><a href=\"javascript:void(0);\" onclick=\"v4_clearFilterColumnValues({0},'{1}');\"><nobr>{2}</nobr></a></div>",
                Settings.GridCmdListnerIndex, Id, Settings.V4Page.Resx.GetString("msgGrigNoFilter"));
            w.Write("</div>");

            w.Write("</div>");
            w.Write("</div>");
        }

        /// <summary>
        ///     Отрисовка значения установленного фильтра
        /// </summary>
        /// <param name="w"></param>
        private void RenderTextValuesFilterBlock(TextWriter w)
        {
            if (FilterUniqueValues == null || FilterUniqueValues.Count == 0) return;

            var textValues =
                string.Join(
                    FilterEqual == GridColumnFilterEqualEnum.NotIn
                        ? string.Format(" {0} ", Settings.V4Page.Resx.GetString("lANDUp"))
                        : string.Format(" {0} ", Settings.V4Page.Resx.GetString("lORUp")),
                    FilterUniqueValues.Select(x => "[" + x.Value + "]").ToArray());

            w.Write(Settings.V4Page.Resx.GetString("lblSetFilter") + ": "
                    + string.Format("{0} [{1}] ", Settings.V4Page.Resx.GetString("lblFieldValue"), Alias)
                    +
                    (FilterEqual == GridColumnFilterEqualEnum.NotIn ? Settings.V4Page.Resx.GetString("lNo") + " " : "")
                    + Settings.V4Page.Resx.GetString("lblEqually") + " "
                    + ((textValues.Length > 201) ? textValues.Left(200) + "..." : textValues));
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
            FilterUserClause = new Dictionary<GridColumnUserFilterEnum, string>();
            switch (ColumnType)
            {
                case GridColumnTypeEnum.Decimal:
                case GridColumnTypeEnum.Float:
                case GridColumnTypeEnum.Double:
                case GridColumnTypeEnum.Int:
                    filterName = Settings.V4Page.Resx.GetString("lblGridSetNumericFilter");
                    FillUserFilterClauseInt();
                    break;
                case GridColumnTypeEnum.Date:
                    filterName = Settings.V4Page.Resx.GetString("lblGridSetDateFilter");
                    FillUserFilterClauseDate();
                    break;
                default:
                    filterName = Settings.V4Page.Resx.GetString("lblGridSetTextFilter");
                    FillUserFilterClauseString();
                    break;
            }

            return filterName;
        }

        /// <summary>
        ///     Формирование условий для фильтра типа даты
        /// </summary>
        private void FillUserFilterClauseDate()
        {
            FilterUserClause.Add(GridColumnUserFilterEnum.Равно,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Равно.GetAttribute<GridColumnUserFilterAttribute>().AliasDate) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.НеРавно,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.НеРавно.GetAttribute<GridColumnUserFilterAttribute>().AliasDate) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.Больше,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Больше.GetAttribute<GridColumnUserFilterAttribute>().AliasDate) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.БольшеИлиРавно,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.БольшеИлиРавно.GetAttribute<GridColumnUserFilterAttribute>().AliasDate) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.Меньше,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Меньше.GetAttribute<GridColumnUserFilterAttribute>().AliasDate) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.МеньшеИлиРавно,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.МеньшеИлиРавно.GetAttribute<GridColumnUserFilterAttribute>().AliasDate) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.Между,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Между.GetAttribute<GridColumnUserFilterAttribute>().AliasDate) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.Указано,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Указано.GetAttribute<GridColumnUserFilterAttribute>().AliasDate));
            FilterUserClause.Add(GridColumnUserFilterEnum.НеУказано,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.НеУказано.GetAttribute<GridColumnUserFilterAttribute>().AliasDate));
        }

        /// <summary>
        ///     Формирование условий для фильтра строкового типа
        /// </summary>
        private void FillUserFilterClauseString()
        {
            FilterUserClause.Add(GridColumnUserFilterEnum.Равно,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Равно.GetAttribute<GridColumnUserFilterAttribute>().AliasString) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.НеРавно,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.НеРавно.GetAttribute<GridColumnUserFilterAttribute>().AliasString) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.НачинаетсяС,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.НачинаетсяС.GetAttribute<GridColumnUserFilterAttribute>().AliasString) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.ЗаканчиваетсяНа,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.ЗаканчиваетсяНа.GetAttribute<GridColumnUserFilterAttribute>().AliasString) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.Содержит,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Содержит.GetAttribute<GridColumnUserFilterAttribute>().AliasString) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.НеСодержит,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.НеСодержит.GetAttribute<GridColumnUserFilterAttribute>().AliasString) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.НеУказано,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.НеУказано.GetAttribute<GridColumnUserFilterAttribute>().AliasString));
            FilterUserClause.Add(GridColumnUserFilterEnum.Указано,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Указано.GetAttribute<GridColumnUserFilterAttribute>().AliasString));
        }

        /// <summary>
        ///     Формирование условий для фильтра цифрового типа
        /// </summary>
        private void FillUserFilterClauseInt()
        {
            FilterUserClause.Add(GridColumnUserFilterEnum.Равно,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Равно.GetAttribute<GridColumnUserFilterAttribute>().AliasNumber) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.НеРавно,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.НеРавно.GetAttribute<GridColumnUserFilterAttribute>().AliasNumber) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.Больше,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Больше.GetAttribute<GridColumnUserFilterAttribute>().AliasNumber) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.БольшеИлиРавно,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.БольшеИлиРавно.GetAttribute<GridColumnUserFilterAttribute>().AliasNumber) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.Меньше,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Меньше.GetAttribute<GridColumnUserFilterAttribute>().AliasNumber) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.МеньшеИлиРавно,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.МеньшеИлиРавно.GetAttribute<GridColumnUserFilterAttribute>().AliasNumber) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.Между,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Между.GetAttribute<GridColumnUserFilterAttribute>().AliasNumber) + "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.Указано,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Указано.GetAttribute<GridColumnUserFilterAttribute>().AliasNumber));
            FilterUserClause.Add(GridColumnUserFilterEnum.НеУказано,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.НеУказано.GetAttribute<GridColumnUserFilterAttribute>().AliasNumber));
        }

        /// <summary>
        ///     Получение списка уникальных значений колонки
        /// </summary>
        public void FillUniqueValues()
        {
            var columnWithFilter =
                Settings.TableColumns.Where(
                    x => x.Id != Id && x.FilterUniqueValues != null && x.FilterUniqueValues.Count > 0).ToList();

            if (columnWithFilter.Count > 0)
            {
                var results = from r in Settings.DT.AsEnumerable()
                    where
                        columnWithFilter.All(
                            clmn =>
                                clmn.FilterEqual == GridColumnFilterEqualEnum.In &&
                                clmn.FilterUniqueValues.ContainsValue(r.Field<object>(clmn.FieldName))
                                ||
                                clmn.FilterEqual == GridColumnFilterEqualEnum.NotIn &&
                                !clmn.FilterUniqueValues.ContainsValue(r.Field<object>(clmn.FieldName))
                            )
                    group r by new {MyValue = r.Field<object>(FieldName)}
                    into myGroup
                    orderby myGroup.Key.MyValue
                    select new {myGroup.Key.MyValue};

                if (UniqueValuesOriginal != null)
                {
                    UniqueValues =
                        UniqueValuesOriginal.Where(
                            x =>
                                results.Any(
                                    y =>
                                        (y.MyValue != null && y.MyValue.Equals(x.Value)) ||
                                        (y.MyValue == null && x.Value == null)))
                            .ToDictionary(v => v.Key, v => v.Value);
                }
            }
            else
                UniqueValues = UniqueValuesOriginal;
        }

        #endregion
    }
}