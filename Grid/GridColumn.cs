using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Web;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Entities.Documents;
using Kesco.Lib.Web.Controls.V4.Common;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Convert = Kesco.Lib.ConvertExtention.Convert;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    /// <summary>
    ///     Класс, описывающий колонку в контроле Grid
    /// </summary>
    public class GridColumn
    {
        private readonly string _applyImage =
            "<span class=\"ui-icon ui-icon-check\" border=0 style=\"display:inline-block\"></span>";

        private readonly string _cssClassInterval = "filterType" + (int) GridColumnUserFilterEnum.Между;

        private string _fieldName = "";        
        private string _sortFieldName = "";
        private string _filterFieldName = "";
        
        public Dictionary<object, object> FilterStandartType;
        public Dictionary<object, object> FilterUniqueValues;

        public GridColumnUserFilter FilterUser { get; set; }
        public Dictionary<GridColumnUserFilterEnum, string> FilterUserClause;

        public string FilterUserCtrlBaseName = "v4_ctrlFilterClause";
        public string FilterUserCtrlCurrentDateName = "v4_ctrlFilterCurrentDate";

        public Dictionary<string, Dictionary<string, string>> HrefClauses;
        public Dictionary<object, object> UniqueValues;
        public Dictionary<object, object> UniqueValuesOriginal;

        //public Action<TextWriter, DataRow> RenderColumnAction;
        public delegate void RenderColumnDelegate(TextWriter w, DataRow dr);

        public RenderColumnDelegate renderColumnDelegate;

        /// <summary>
        ///     Конструктор
        /// </summary>
        /// <param name="settings">Настройки грида</param>
        public GridColumn(GridSettings settings)
        {
            Settings = settings;
        }

        public string FormatString { get; set; }

        public string ScaleFieldName { get; set; }
        public bool IsScaleByValue { get; set; }
        public int MaxScale { get; set; }
        public int DefaultScale { get; set; }
        public bool IsSaveSettings { get; set; } = true;
        public bool IsTimeSecond { get; set; }

        /// <summary>
        ///     Признак того, что в данной колонке должны выводиться локальные дата и время
        /// </summary>
        public bool IsLocalTime { get; set; }

        /// <summary>
        ///     Признак того, что возможна сортировка по данной колонке
        /// </summary>
        public bool IsSortedColumn => Settings.SortingColumns.Contains(SortFieldName);

        /// <summary>
        ///     Признак того, что возможна фильтрация по данной колонке
        /// </summary>
        public bool IsFilteredColumn => Settings.FilteredColumns.Contains(FilterFieldName);

        public bool IsDublicateColumn { get; set; }

        public bool IsBit { get; set; }
        public bool IsCurrentDate { get; set; }

        public bool IsCondition { get; set; }

        public string HrefId { get; set; }
        public string HrefIdFieldName { get; set; }
        public string HrefUri { get; set; }
        public bool HrefUriFieldName { get; set; }
        public bool HrefIsDocument { get; set; }
        public bool IsGetDocumentNameByEachRow { get; set; }
        public bool HrefIsEmployee { get; set; }
        public bool HrefIsPerson { get; set; }
        public bool HrefIsEquipment { get; set; }
        public bool HrefIsClause { get; set; }

        public Dictionary<string, string> ValueBooleanCaption { get; set; }

        public string SumValuesText { get; set; }
        public bool IsSumValues { get; set; }

        public string BackGroundColor { get; set; }
        public string HeaderTitle { get; set; }
        public string Title { get; set; }

        public GridColumnFilterEqualEnum? FilterEqual { get; set; }
        public bool FilterRequired { get; set; }
        public GridColumnOrderByDirectionEnum OrderByDirection { get; set; }
        public GridColumnTypeEnum ColumnType { get; set; }

        public string Id { get; set; }
        public string FieldName { 
            get {
                return _fieldName = Regex.Replace(_fieldName, "@\\d+$", "");
            } 
            set {
                _fieldName = value;
            } 
        }

        public string Alias { get; set; }
    

        public string SortFieldName {
            get
            {
                if (string.IsNullOrEmpty(_sortFieldName)) return FieldName;
                return _sortFieldName;
            }
            set { _sortFieldName = value; }
        }
        public string FilterFieldName
        {
            get
            {
                if (string.IsNullOrEmpty(_filterFieldName)) return FieldName;
                return _filterFieldName;
            }
            set { _filterFieldName = value; }
        }
       

        public int? OrderByNumber { get; set; }
        public int DisplayOrder { get; set; }
        public int DisplayOrderDefault { get; set; }
        public bool DisplayVisible { get; set; }
        public bool DisplayVisibleDefault { get; set; }
        public bool SortNullFirst { get; set; } = true;
        
        public int FilterOrder { get; set; }

        public string TextAlign { get; set; }
        public bool IsNoWrap { get; set; }

        private GridSettings Settings { get; }

        public string ClickMessageConfirm { get; set; }
        public string ClickClientFuncName { get; set; }
        public List<string> ClickPkFieldsName { get; set; }
        public List<string> ClickMessageFieldsName { get; set; }
        public string ClickTitle { get; set; }

        #region Render

        /// <summary>
        ///     Формирование данных в колонке в зависимости от ее типа
        /// </summary>
        /// <param name="w">Поток вывода</param>
        /// <param name="dr">Строка записи</param>
        /// <param name="colspan">Количество объединяемых ячеек</param>
        /// <param name="beforeText">Текст перед данными</param>
        /// <param name="afterText">Текст после данных</param>
        /// <param name="isGroupHeader">По данной колонке происходит группировка</param>
        public void RenderColumnData(TextWriter w, DataRow dr, int rowIndex, string colspan = "",
            string beforeText = "",
            string afterText = "", bool isGroupHeader = false)
        {
            if (renderColumnDelegate != null)
            {
                renderColumnDelegate.Invoke(w, dr);
                return;
            }
            
            var fieldValue = dr[FieldName];

            var sbAdvProp = new StringBuilder();
            var textAlignStyle = !string.IsNullOrEmpty(TextAlign) && !isGroupHeader
                ? string.Format("text-align:{0} !important;", TextAlign)
                : "";
            var textNoWrap = IsNoWrap ? "white-space: nowrap;" : "";

            sbAdvProp.AppendFormat(" style=\"{0}{1}{2}\" ",
                !string.IsNullOrEmpty(BackGroundColor) ? BackGroundColor + ";" : "",
                textAlignStyle,
                textNoWrap);

            if (!string.IsNullOrEmpty(Title))
                sbAdvProp.AppendFormat(" title=\"{0}\" ", Title);

            var onClickAttr = "";
            var clickTitle = ClickTitle.IsNullEmptyOrZero()? "" : ClickTitle;
            if (!string.IsNullOrEmpty(ClickClientFuncName) && ClickClientFuncName.Length > 0)
            {
                var messageFields = "";
                var clientParams = "";

                if (ClickMessageFieldsName != null)
                { 
                    ClickMessageFieldsName.ForEach(delegate (string fieldName)
                    {
                        messageFields += (messageFields.Length > 0 ? ", " : "") + $"[{HttpUtility.HtmlEncode(dr[fieldName].ToString())}]";
                    });
                }

                if (!string.IsNullOrEmpty(ClickClientFuncName))
                    messageFields = ClickMessageConfirm + " " + messageFields + "?";

                messageFields = HttpUtility.JavaScriptStringEncode(messageFields);

                ClickPkFieldsName.ForEach(delegate (string fieldName)
                {
                    clientParams += (clientParams.Length > 0 ? "," : "") + $"{HttpUtility.JavaScriptStringEncode(dr[fieldName].ToString())}";
                });

                if (!string.IsNullOrEmpty(ClickClientFuncName) && ClickMessageConfirm.Length > 0)
                {
                    onClickAttr = string.Format("v4_showConfirm('{0}', '{1}', '{2}', '{3}', '{4}', 300);",
                        messageFields,
                        HttpUtility.JavaScriptStringEncode(Settings.V4Page.Resx.GetString("errDoisserWarrning")),
                        HttpUtility.JavaScriptStringEncode(Settings.V4Page.Resx.GetString("CONFIRM_StdCaptionYes")),
                        HttpUtility.JavaScriptStringEncode(Settings.V4Page.Resx.GetString("CONFIRM_StdCaptionNo")),
                    string.Format("{0}({1});", ClickClientFuncName, clientParams)
                    );
                }
                else
                {
                    onClickAttr = $"{ClickClientFuncName}({clientParams});";
                }

                //onClickAttr = "onclick = " + onClickAttr;
            }

            switch (ColumnType)
            {
                case GridColumnTypeEnum.Date:
                    if (string.IsNullOrEmpty(fieldValue.ToString()) || (DateTime)fieldValue == DateTimeExtensionMethods.MinDateTime) { w.Write("<td>&nbsp;"); break;}
                    w.Write("<td {0} {1} {5}>{2}{4}{3}</td>", sbAdvProp, colspan, beforeText, afterText,
                        IsLocalTime
                            ? string.Format("<script>$(\"#{0}_{1}\").html(v4_toLocalTime(\"{2}\",\"{3}\"));</script>",
                                Id,
                                rowIndex,
                                    ((DateTime)fieldValue).ToString("yyyy-MM-dd HH:mm:ss"), "dd.mm.yyyy hh:mi:ss")
                            : FormatString != ""
                                ? ((DateTime)fieldValue).ToString(FormatString)
                                : fieldValue.ToString(),
                        IsLocalTime ? string.Format("id=\"{0}_{1}\"", Id, rowIndex) : "");
                    break;
                case GridColumnTypeEnum.Double:
                case GridColumnTypeEnum.Float:
                case GridColumnTypeEnum.Decimal:

                    w.Write("<td {0} {1} class=\"v4NumberTextAlign\">{2}", sbAdvProp, colspan, beforeText);

                    var scale = DefaultScale;
                    if (!string.IsNullOrEmpty(ScaleFieldName))
                        scale = dr[ScaleFieldName] == null ? DefaultScale : (int) dr[ScaleFieldName];
                    else if (IsScaleByValue)
                        if (ColumnType == GridColumnTypeEnum.Decimal)
                        {
                            if (!String.IsNullOrEmpty(fieldValue.ToString()))
                                scale = ((decimal)fieldValue).GetScaleValue(DefaultScale, MaxScale);
                        }

                    if (ColumnType == GridColumnTypeEnum.Decimal)
                    {
                        if (!String.IsNullOrEmpty(fieldValue.ToString()))
                            w.Write(((decimal)fieldValue).ToString(FormatString + scale));
                        else
                            w.Write("");
                    }
                    else
                        w.Write(((double)fieldValue).ToString(FormatString + scale));

                    w.Write("{0}</td>", afterText);
                    break;

                case GridColumnTypeEnum.Int:
                case GridColumnTypeEnum.Short:
                case GridColumnTypeEnum.Long:

                    if (ColumnType== GridColumnTypeEnum.Int && IsTimeSecond)
                        w.Write("<td {0} {1} class=\"v4NumberTextAlign\">{2}{4}{3}</td>", sbAdvProp, colspan,
                            beforeText, afterText,
                            Convert.Second2TimeFormat((int)fieldValue));
                    else
                        w.Write("<td {0} {1} class=\"v4NumberTextAlign\">{2}{4}{3}</td>", sbAdvProp, colspan,
                            beforeText, afterText, fieldValue);
                    break;
                case GridColumnTypeEnum.Boolean:

                    if (IsBit)
                    {
                        if (isGroupHeader)
                        {
                            w.Write("<td {0} {1}>{2}{4}{3}</td>", sbAdvProp, colspan, beforeText, afterText,
                                fieldValue.ToString() == "1"
                                    ? "[" + Settings.V4Page.Resx.GetString("lblGridColumnValueBooleanTrue") + "]"
                                    : "[" + Settings.V4Page.Resx.GetString("lblGridColumnValueBooleanFalse") + "]"
                                );
                        }
                        else
                        {
                            var isChecked = "";

                            if (FormatString != "" && fieldValue.ToString() != "" && fieldValue.ToString() != "0") isChecked = "Checked";
                            else if (fieldValue.ToString() == "1") isChecked = "Checked";

                            w.Write("<td {0} {1}>{2}<img src=\"/styles/CheckBox{4}.gif\" border=\"0\" {5}{6}{7}/>{3}</td>", sbAdvProp, colspan, beforeText, afterText,
                                isChecked, 
                                clickTitle.Length > 0 ? " title=\"" + ClickTitle + "\"" : "",
                                onClickAttr.Length > 0 ? " onclick=\""+ onClickAttr + "\"" : "",
                                onClickAttr.Length > 0 ? " style='cursor: pointer;'" : "");
                        }
                    }
                    else
                        w.Write("<td {0} {1}>{2}{4}{3}</td>", sbAdvProp, colspan, beforeText, afterText, fieldValue);
                    break;
                case GridColumnTypeEnum.List:
                    w.Write("<td {0} {1}>{2}", sbAdvProp, colspan, beforeText);

                    if (fieldValue.Equals(DBNull.Value) || fieldValue.ToString() == "")
                    {
                        w.Write("&nbsp;");
                    }
                    else
                    {
                        var listTypeFieldList = JsonConvert.DeserializeObject<List<KeyValuePair<int, string>>>(FormatString);
                        var fv = listTypeFieldList.Find(k => k.Key == System.Convert.ToInt32(fieldValue));
                        w.Write(fv.Value);
                    }
                    w.Write("{0}</td>", afterText);
                    break;
                default:
                    w.Write("<td {0} {1}>{2}", sbAdvProp, colspan, beforeText);

                    if (fieldValue.Equals(DBNull.Value) || fieldValue.ToString() == "")
                    {
                        w.Write(isGroupHeader
                            ? string.Format("<span style=\"color:dimgray;\">[{0}]</span>",
                                Settings.V4Page.Resx.GetString("lblNotIndicatedo"))
                            : "&nbsp;");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(HrefIdFieldName) && !dr[HrefIdFieldName].Equals(DBNull.Value) ||
                            HrefIsClause)
                        {
                            if (HrefIsDocument)
                            {
                                if (IsGetDocumentNameByEachRow)
                                {
                                    var doc = new Document(dr[HrefIdFieldName].ToString());
                                    Settings.V4Page.RenderLinkDocument(w, int.Parse(dr[HrefIdFieldName].ToString()), doc, false, NtfStatus.Empty,"", HrefIdFieldName);
                                }
                                else
                                {
                                    Settings.V4Page.RenderLinkDocument(w, int.Parse(dr[HrefIdFieldName].ToString()), fieldValue.ToString(), null, false, NtfStatus.Empty, "", HrefIdFieldName);
                                }
                            }
                            else if (HrefIsEmployee)
                            {
                                var htmlId = Guid.NewGuid().ToString();
                                Settings.V4Page.RenderLinkEmployee(w, htmlId, dr[HrefIdFieldName].ToString(), fieldValue.ToString(), NtfStatus.Empty, false, HrefIdFieldName);
                            }
                            else if (HrefIsPerson)
                            {
                                var htmlId = Guid.NewGuid().ToString();
                                Settings.V4Page.RenderLinkPerson(w, htmlId, dr[HrefIdFieldName].ToString(), fieldValue.ToString(), NtfStatus.Empty, true, true, HrefIdFieldName);
                            }
                            else if (HrefIsEquipment)
                            {
                                var htmlId = Guid.NewGuid().ToString();
                                Settings.V4Page.RenderLinkEquipment(w, htmlId, dr[HrefIdFieldName].ToString(), fieldValue.ToString(), NtfStatus.Empty, "", "", HrefIdFieldName);
                            }
                            else if (HrefIsClause)
                            {
                                if (HrefClauses.Count == 0)
                                {
                                    w.Write(fieldValue);
                                }
                                else
                                {
                                    var fieldWithid = "";
                                    var hrefByField = "";

                                    foreach (var field in HrefClauses)
                                    foreach (var clause in field.Value.Where(clause =>
                                        dr[clause.Key] != null && int.Parse(dr[clause.Key].ToString()) == 1))
                                    {
                                        fieldWithid = field.Key;
                                        hrefByField = clause.Value;
                                    }

                                    if (string.IsNullOrEmpty(fieldWithid))
                                        w.Write(fieldValue);
                                    else
                                        Settings.V4Page.RenderLink(w, fieldValue.ToString(), hrefByField + (hrefByField.Contains("?") ? "&" : "?") + HrefId + "=" +dr[fieldWithid], "", "200", HrefIdFieldName);
                                }
                            }
                            else
                            {
                                if (HrefUriFieldName)                               
                                    Settings.V4Page.RenderLink(w, fieldValue.ToString(), fieldValue.ToString().Contains("http") ? fieldValue.ToString() : "http://" + fieldValue, "", "200", HrefIdFieldName);                               
                                else                                
                                    Settings.V4Page.RenderLink(w, fieldValue.ToString(), HrefUri + (HrefUri.Contains("?") ? "&" : "?") + HrefId + "=" + dr[HrefIdFieldName], "", "200", HrefIdFieldName);
                            }
                        }
                        else
                        {
                            w.Write(fieldValue);
                        }
                    }

                    w.Write("{0}</td>", afterText);
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
                        FormatString != ""
                            ? ((decimal) value).ToString(FormatString + DefaultScale)
                            : value.ToString());
                    break;

                case GridColumnTypeEnum.Short:
                case GridColumnTypeEnum.Int:
                case GridColumnTypeEnum.Long:
                    if (ColumnType== GridColumnTypeEnum.Int && IsTimeSecond)
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

        private GridColumnUserFilterEnum DefaultUserFilterId
        {
            get
            {
                switch (ColumnType)
                {
                    case GridColumnTypeEnum.String:
                        return GridColumnUserFilterEnum.Содержит;
                    default:
                        return GridColumnUserFilterEnum.Равно; 
                }
            }
        }

        public void RefreshColumnUserFilterForm(Page page, string filterId, string setValue, bool changeFilter, string isCurrentDate)
        {
            var w = new StringWriter();

            if (filterId == "-1")
            {
                if (FilterUser != null)
                {
                    filterId = ((int)FilterUser.FilterType).ToString();
                    setValue = "1";
                }
                else
                    filterId = ((int)DefaultUserFilterId).ToString();
            }

            IsCurrentDate = isCurrentDate == "true";

            RenderColumnUserFilterForm_CurrentDate(w, page, false, changeFilter);
            RenderColumnUserFilterForm_Clause(w, page, filterId, false);

            if (Settings.IsFilterEnable && IsFilteredColumn && changeFilter)
            {
                if (FilterUniqueValues != null && FilterUniqueValues.Count > 0 || FilterUser != null)
                {
                    w.Write("<br/>");
                    RenderClearFilterBlock(w);
                }
            }

            page.JS.Write("$('#divColumnSettingsUserFilterForm_Body_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(w.ToString()));

            page.JS.Write(
                "$('#v4_selectFilterUserClause_{2}').selectmenu({{width : 'auto', change: function() {{v4_grid.changeFilterUserClause(\"{2}\", this, {0}, \"{1}\");}}}}); ",
                (int)GridColumnUserFilterEnum.Между, _cssClassInterval, Settings.GridId);
            page.JS.Write("v4_grid.changeFilterUserClause(\"{2}\", null, {0}, \"{1}\");",
                (int)GridColumnUserFilterEnum.Между, _cssClassInterval, Settings.GridId);
            page.JS.Write("setTimeout(function(){{$(\"#{0}_{1}_1_0\").focus();}},10);", FilterUserCtrlBaseName,
                Settings.GridId);
            if (ColumnType == GridColumnTypeEnum.Int || ColumnType == GridColumnTypeEnum.Date && IsCurrentDate)
                page.JS.Write("setTimeout(function(){{$(\"#{0}_{1}_1_0\").select();}},10);", FilterUserCtrlBaseName,
                    Settings.GridId);

        }

        /// <summary>
        ///     Формирование формы пользовательских фильтров
        /// </summary>
        /// <param name="page"></param>
        /// <param name="filterId"></param>
        /// <param name="setValue"></param>
        public void RenderColumnUserFilterForm(Page page, string filterId, string setValue, bool changeFilter)
        {
            var w = new StringWriter();
            
            if (filterId == "-1")
            {
                if (FilterUser != null)
                {
                    filterId = ((int)FilterUser.FilterType).ToString();
                    setValue = "1";
                }
                else
                {
                    filterId = ((int) DefaultUserFilterId).ToString();
                }
            }

            if (FilterUser != null)
            {
                if (ColumnType == GridColumnTypeEnum.Date)
                {
                    IsCurrentDate = ((GridColumnUserFilterDate)FilterUser).IsCurrentDate;
                }
                else
                {
                    IsCurrentDate = false;
                }
            }

            if (ColumnType == GridColumnTypeEnum.Date)
            {
                RenderColumnUserFilterForm_CurrentDate(w, page, true, changeFilter);
            }

            RenderColumnUserFilterForm_Clause(w, page, filterId, setValue.Equals("1"));

            if (Settings.IsFilterEnable && IsFilteredColumn && changeFilter)
            {
                if (FilterUniqueValues != null && FilterUniqueValues.Count > 0 || FilterUser != null)
                {
                    w.Write("<br/>");
                    RenderClearFilterBlock(w);
                }
            }

            page.JS.Write("v4_grid.columnSettingsUserFilterForm(\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\");",
                Settings.GridId,
                Settings.GridCmdListnerIndex,
                filterId,
                Id,
                HttpUtility.JavaScriptStringEncode(string.Format("{0}",
                    Settings.V4Page.Resx.GetString("lblSettingFilter"))),
                changeFilter
                );

            page.JS.Write("$('#divColumnSettingsUserFilterForm_Header_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(string.Format("{0} [{1}]",
                    Settings.V4Page.Resx.GetString("lblFieldValue"), Alias)));
            page.JS.Write("$('#divColumnSettingsUserFilterForm_Body_{0}').html('{1}');",
                Settings.GridId,
                HttpUtility.JavaScriptStringEncode(w.ToString()));

            page.JS.Write(
                "$('#v4_selectFilterUserClause_{2}').selectmenu({{width : 'auto', change: function() {{v4_grid.changeFilterUserClause(\"{2}\", this, {0}, \"{1}\");}}}}); ",
                (int) GridColumnUserFilterEnum.Между, _cssClassInterval, Settings.GridId);
            page.JS.Write("v4_grid.changeFilterUserClause(\"{2}\", null, {0}, \"{1}\");",
                (int) GridColumnUserFilterEnum.Между, _cssClassInterval, Settings.GridId);
            page.JS.Write("setTimeout(function(){{$(\"#{0}_{1}_1_0\").focus();}},10);", FilterUserCtrlBaseName,
                Settings.GridId);
            if(ColumnType==GridColumnTypeEnum.Int || ColumnType == GridColumnTypeEnum.Date && IsCurrentDate)
                page.JS.Write("setTimeout(function(){{$(\"#{0}_{1}_1_0\").select();}},10);", FilterUserCtrlBaseName,
                    Settings.GridId);
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
        ///     Формирование чекбокса 'относительно текущей даты'
        /// </summary>
        /// <param name="w"></param>
        private void RenderColumnUserFilterForm_CurrentDate(TextWriter w, Page page, bool setValue, bool changeFilter)
        {
            
            w.Write("<div class=\"v4DivTable\">");
            w.Write("<div class=\"v4DivTableRow\">");
            w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");

            var ctrlId = FilterUserCtrlCurrentDateName + "_" + Settings.GridId;

            var checkedStatus = "";
            if (setValue)
            {
                if ((FilterUser != null && ((GridColumnUserFilterDate) FilterUser).IsCurrentDate))
                    checkedStatus = "checked";
                else
                    if (IsCurrentDate) checkedStatus = "checked";
            }
            else
            {
                if (IsCurrentDate) checkedStatus = "checked";
            }

            w.Write("<td><input type='checkbox' id='{0}' {1} onclick='{2}'></td>", 
                ctrlId,
                checkedStatus,
                $"v4_grid.changeDataFiterCurrentDate(\"{Settings.GridCmdListnerIndex}\", \"{Id}\", \"{(FilterUser!=null ? (int)FilterUser.FilterType : -1)}\", \"{(setValue?1:0)}\", this.checked, \"{changeFilter}\");"
                );

            w.Write("</div>");
            w.Write("<div class=\"v4DivTableCell v4PaddingCell\" style=\"text-align:left; white-space: nowrap;\">");
            w.Write(" {0}", Settings.V4Page.Resx.GetString("lblGridSetDateFilterByCurrent"));
            w.Write("</div>");

            w.Write("</div>");
            w.Write("</div>");

            w.Write("<hr class=\"v4HrPopup\">");
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
            var ctrlId = FilterUserCtrlBaseName + "_" + Settings.GridId + "_" + inx;
            var nextCtrl = inx == 2
                ? "btnUFilter_Apply_" + Settings.GridId
                : FilterUserCtrlBaseName + "_" + Settings.GridId + "_2" + "_0";

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
                            ctrlValue = IsCurrentDate
                                ? System.Convert.ToInt32(FilterUser.FilterValue2).ToString()
                                : ((DateTime)FilterUser.ComputeFilterValue2).ToString("dd.MM.yyyy");
                    }
                }
                else
                {
                    if (FilterUser.FilterValue1 != null)
                    {
                        if (ColumnType != GridColumnTypeEnum.Date)
                            ctrlValue = FilterUser.FilterValue1.ToString();
                        else
                        {
                            ctrlValue = IsCurrentDate 
                                ? System.Convert.ToInt32(FilterUser.FilterValue1).ToString() 
                                : ((DateTime)FilterUser.ComputeFilterValue1).ToString("dd.MM.yyyy");
                        }

                    }
                }
            }

            switch (ColumnType)
            {
                case GridColumnTypeEnum.Decimal:
                case GridColumnTypeEnum.Double:
                case GridColumnTypeEnum.Float:
                case GridColumnTypeEnum.Short:
                case GridColumnTypeEnum.Int:
                case GridColumnTypeEnum.Long:
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
                    if (
                            (setValue && FilterUser != null && ((GridColumnUserFilterDate)FilterUser).IsCurrentDate)
                            || (!setValue && IsCurrentDate)
                        )
                    {
                        var ctrlD = new Number()
                        {
                            HtmlID = ctrlId,
                            ID = ctrlId,
                            V4Page = page,
                            Value = string.IsNullOrEmpty(ctrlValue)?"0": ctrlValue,
                            NextControl = nextCtrl,
                            Width = 70
                        };
                        page.V4Controls.Add(ctrlD);
                        ctrlD.RenderControl(w);
                    }
                    else
                    {
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
                    }
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
                w.Write(
                    "<li><div style=\"white-space:nowrap;\" data-columnId=\"{1}\" data-filterId=\"{2}\">{0}</div></li>",
                    item.Value, Id, (int) item.Key);
            w.Write("</ul>");
            w.Write("</li>");
            w.Write("</ul>");
        }

        /// <summary>
        ///     Отрисовка выборанного значения фильтра
        /// </summary>
        /// <param name="w"></param>
        /// <param name="isHtml"></param>
        public void RenderTextUserFilterBlock(TextWriter w, bool isHtml = true, bool writeLabel = true)
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
                w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
                w.Write(_applyImage);
                w.Write("</div>");
                w.Write("<div class=\"v4DivTableCell\" style=\"text-align:left;\">");
            }
            if (writeLabel) w.Write(Settings.V4Page.Resx.GetString("lblSetFilter") + ": ");
            if (isHtml)
            {
                w.Write("</div>");
                w.Write("</div>");
            }

            if (isHtml)
            {
                w.Write("<div class=\"v4DivTableRow\">");
                w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
                w.Write(
                    "<span class=\"ui-icon ui-icon-pencil\"  style=\"display:inline-block; cursor:pointer\" onclick=\"v4_grid.openUserFilterFormCmd({1}, '{2}', {3}, 1);\"></span>",
                    Settings.V4Page.Resx.GetString("lblEditFilter"),
                    Settings.GridCmdListnerIndex,
                    Id,
                    (int) FilterUser.FilterType);
                w.Write("</div>");
                w.Write("<div class=\"v4DivTableCell\" style=\"text-align:left;\">");
            }

            if (writeLabel)
                w.Write("{0} ", Settings.V4Page.Resx.GetString("lblFieldValue"));
            w.Write("{0} ", Alias);
            switch (ColumnType)
            {
                case GridColumnTypeEnum.Date:
                    w.Write(Settings.V4Page.Resx.GetString(FilterUser.FilterType
                        .GetAttribute<GridColumnUserFilterAttribute>().AliasDate));
                    break;
                case GridColumnTypeEnum.Decimal:
                case GridColumnTypeEnum.Double:
                case GridColumnTypeEnum.Float:
                case GridColumnTypeEnum.Short:
                case GridColumnTypeEnum.Int:
                case GridColumnTypeEnum.Long:

                    w.Write(Settings.V4Page.Resx.GetString(FilterUser.FilterType
                        .GetAttribute<GridColumnUserFilterAttribute>().AliasNumber));

                    break;
                default:
                    w.Write(Settings.V4Page.Resx.GetString(FilterUser.FilterType
                        .GetAttribute<GridColumnUserFilterAttribute>().AliasString));
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
                            ((DateTime)((GridColumnUserFilterDate)FilterUser).ComputeFilterValue1).ToString("dd.MM.yyyy"));
                        w.Write(" {0} [{1}]", Settings.V4Page.Resx.GetString("lTo"),
                            ((DateTime)((GridColumnUserFilterDate)FilterUser).ComputeFilterValue2).ToString("dd.MM.yyyy"));
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
                        w.Write("[{0}]", ((DateTime) ((GridColumnUserFilterDate)FilterUser).ComputeFilterValue1).ToString("dd.MM.yyyy"));
                    else
                        w.Write("[<span class='gridFilterValue'>{0}</span>]", FilterUser.FilterValue1);
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
                "<input type='checkbox' id=\"cfAllValues\" {0} {1} onclick =\"v4_grid.columnValuesChecked(this.checked, 'classValueCheckBox');\">",
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
                if (!Settings.IsFilterUniqueEnable && ColumnType != GridColumnTypeEnum.Boolean) return;

                w.Write("<div class=\"v4GridFilterText\">{0}</div>",
                    Settings.V4Page.Resx.GetString("msgGridUniqError"));
                w.Write("<div class=\"v4GridFilterText\">{0}<div>",
                    Settings.V4Page.Resx.GetString("msgGridUseFilterFieldType"));
                return;
            }

            var existFilter = FilterUniqueValues != null && FilterUniqueValues.Count > 0;

            var disabled = FilterUser != null;
            w.Write("<div class=\"v4DivTable\" >");
            var existEmpty = false;
            var existValues = false;
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
                    valueInFilter = FilterUniqueValues.ToList().Exists(x => x.Value.ToString() == item.Value.ToString());
                }

                if (!existFilter || valueInFilter && FilterEqual == GridColumnFilterEqualEnum.In
                                 || existFilter && !valueInFilter && FilterEqual == GridColumnFilterEqualEnum.NotIn)
                    checkState = "checked";
                else
                    checkState = "";

                w.Write("<div class=\"v4DivTableRow\">");
                w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
                w.Write(
                    "<input type='checkbox' class=\"classValueCheckBox\" {0} {2} data-id=\"{1}\" onclick=\"v4_grid.setStateCheckAllValues('{3}','cfAllValues', 'classValueCheckBox');\">",
                    disabled ? "" : checkState,
                    item.Key,
                    disabled ? "disabled" : "", Settings.GridId);
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
                                ? ((decimal) item.Value).ToString(FormatString + DefaultScale)
                                : item.Value.ToString());
                        else
                            w.Write(FormatString != ""
                                ? ((double) item.Value).ToString(FormatString + DefaultScale)
                                : item.Value.ToString());
                        break;
                    case GridColumnTypeEnum.Short:
                    case GridColumnTypeEnum.Int:
                    case GridColumnTypeEnum.Long:
                        if (ColumnType == GridColumnTypeEnum.Int && IsTimeSecond)
                            w.Write(Convert.Second2TimeFormat((int) item.Value));
                        else
                            w.Write(item.Value);
                        break;
                    case GridColumnTypeEnum.Boolean:
                        if (ValueBooleanCaption != null)
                            w.Write(item.Value.ToString() == "0"
                                ? "[" + (ValueBooleanCaption.ContainsKey("0") ? ValueBooleanCaption["0"] : item.Value) +
                                  "]"
                                : "[" + (ValueBooleanCaption.ContainsKey("1") ? ValueBooleanCaption["1"] : item.Value) +
                                  "]");
                        else
                            w.Write(item.Value);
                        break;
                    case GridColumnTypeEnum.List:
                        if (ValueBooleanCaption != null)
                            w.Write("[" + ValueBooleanCaption[item.Key.ToString()] + "]");
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
                        FilterUniqueValues.Where(x => x.Key.ToString().Equals("0"))
                            .Select(x => (KeyValuePair<object, object>?) x)
                            .FirstOrDefault();
                    valueInFilter = r != null;

                    if (valueInFilter && FilterEqual == GridColumnFilterEqualEnum.In
                        || !valueInFilter && FilterEqual == GridColumnFilterEqualEnum.NotIn)
                        checkState = "checked";
                    else
                        checkState = "";
                }

                w.Write(
                    "<input type='checkbox' class=\"classValueCheckBox\" {0} {1} data-id=\"0\" onclick=\"v4_grid.setStateCheckAllValues('{2}', 'cfAllValues', 'classValueCheckBox');\">",
                    disabled ? "" : checkState,
                    disabled ? "disabled" : "",
                    Settings.GridId
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
        public void RenderColumnSettingsHeader(TextWriter w, bool alwaysLoadDataFromDataBase, Grid grid)
        {
            if (Settings.IsPrintVersion)
            {
                w.Write(@"<th>{0}</th>", Alias);
                return;
            }

            RenderTextFilter(out var titleFilter, out var filterClick, out var deleteSpan, out var filterRequired, false);
            
            w.Write(@"
<th column-id=""{8}"" {1}>
    <div class=""v4DivBlock"" >
	    <div class=""v4DivBlock"">
		    {5}
            <div class=""v4DivInline v4PaddingCell"">{2}</div>
            <div class=""v4DivInline v4PaddingCell"">{0}</div>
		    {3}
		    {4}
            {6} 
            {7}      
	    </div>
    </div>
</th>
",
                IsSortedColumn
                    ? "<span style =\"cursor:pointer\""
                      + " tabindex =\"0\" onkeydown=\"v4_grid.keydown(event, this);\""

                      +  (!grid.HideFilterText && grid.ShowFilterPanel ? string.Format(" draggable=\"true\" ondragstart=\"v4_grid.columnDragDrop('{0}',{1},'{2}');\"", Settings.GridId, Settings.GridCmdListnerIndex, Id):"")
                      +  (!grid.HideFilterText && grid.ShowFilterPanel ? $" column-id=\"{Id}\"":"")

                      + " onmouseover=\"this.style.textDecoration='underline';\""
                      + " onmouseout=\"this.style.textDecoration='none';\"" 
                      + string.Format(" id=\"imgOrders{3}_{0}\" onclick=\"v4_grid.setOrderByColumnValues({0}, '{1}',{2});\"",
                          Settings.GridCmdListnerIndex,
                          Id,
                          OrderByNumber == null ? 0 : OrderByDirection == GridColumnOrderByDirectionEnum.Asc ? 1 : 0, DisplayOrder)
                      + ">" + Alias + "</span>"
                    : "<span>" + Alias + "</span>",
               
                !string.IsNullOrEmpty(HeaderTitle) ? string.Format("title=\"{0}\"", HeaderTitle) : "",
                
                Settings.IsFilterEnable && IsFilteredColumn && !alwaysLoadDataFromDataBase
                    ? string.Format(
                        "<span class=\"ui-icon ui-icon-wrench\" tabindex=\"0\" onkeydown=\"v4_grid.keydown(event, this);\" style=\"display: inline-block;cursor:pointer\" id=\"imgSettings{1}_{3}\" onclick=\"cmdasync('cmd', 'Listener', 'ctrlId', '{3}', 'cmdName', 'RenderColumnSettings','ColumnId','{0}');\" border=0 title=\"{2}\"></span>",
                        FieldName, DisplayOrder, Settings.V4Page.Resx.GetString("msgOpenSettingFilter"),
                        Settings.GridCmdListnerIndex)
                    : "",
               
                ((FilterUniqueValues != null && FilterUniqueValues.Count > 0) || FilterUser != null) && !alwaysLoadDataFromDataBase
                    ? string.Format("<div class=\"v4DivInline v4PaddingCell\"><nobr><span class=\"ui-icon ui-icon-volume-off\" style=\"display: inline-block;cursor:pointer;\" border=\"0\" title=\"{0}\" {1}></span></div>",
                        titleFilter,
                        filterClick)
                    : "",
               
                IsSortedColumn && OrderByNumber != null 
                    ? "<div class=\"v4DivInline v4PaddingCell\"><img src=\"/styles/" +
                      (OrderByDirection == GridColumnOrderByDirectionEnum.Asc ? "sort-asc.png" : "sort-desc.png")
                      +
                      string.Format(
                          "\" border=\"0\" title=\"{0} {1}. {2}\" style=\"cursor:pointer; display:inline-block\" onclick=\"v4_grid.setOrderByColumnValues({3}, '{4}',{5});\">",
                          Settings.V4Page.Resx.GetString("lblSortedBy"),
                          OrderByDirection == GridColumnOrderByDirectionEnum.Asc
                              ? Settings.V4Page.Resx.GetString("lblAscendingSort")
                              : Settings.V4Page.Resx.GetString("lblDescendingSort"),
                          Settings.V4Page.Resx.GetString("msgGrigSordBackOrder"),
                          Settings.GridCmdListnerIndex,
                          Id,
                          OrderByDirection == GridColumnOrderByDirectionEnum.Asc ? 1 : 0)
                      +
                      string.Format("<span style=\"font-size:5pt\" title=\"{1}\">{0}</span></div>", OrderByNumber,
                          Settings.V4Page.Resx.GetString("lblColumnSortOrder"))
                    : "",
                
                !Settings.IsGroupEnable
                    ? ""
                    : @"<div class=""v4DivInline v4PaddingCell""><span class=""v4GroupToggle ui-icon ui-icon-arrow-4""></span></div>",
                
                !Settings.IsGroupEnable
                    ? ""
                    : string.Format(
                        "<div class=\"v4DivInline v4PaddingCell v4ExpandColumn\" style=\"display:none;\"><img src=\"/styles/GroupTree.gif\" onclick=\"v4_grid.groupingExpandColumn('{0}','{1}','{2}');\" style=\"cursor:pointer;\" border=0 title=\"{3}\"/></div>",
                        Settings.GridId,
                        Settings.GridCmdListnerIndex,
                        Id,
                        Settings.Resx.GetString("lblGridGroupExpand")),
               
                !Settings.IsGroupEnable
                    ? ""
                    : string.Format(
                        "<div class=\"v4DivInline v4PaddingCell v4DeleteColumn\" style=\"display:none;\"><img src=\"/styles/RemoveFromList.gif\" onclick=\"v4_grid.groupingRemoveColumn('{0}','{1}','{2}');\" style=\"cursor:pointer;\" border=0 title=\"{3}\"/></div>",
                        Settings.GridId,
                        Settings.GridCmdListnerIndex,
                        Id,
                        Settings.Resx.GetString("lblGridDeleteCulumnGrouping")),
                
                Id
            );
        }

        public void RenderTextFilter(out string titleFilter, out string filterClick, out string deleteSpan, out bool filterRequired, bool fl)
        {
            titleFilter = "";
            filterClick = "";
            deleteSpan = "";
            filterRequired = false;

            if (FilterUser != null)
            {
                filterRequired = FilterRequired;

                var w = new StringWriter();
                RenderClearFilterBlock(w, false);
                deleteSpan = w.ToString();

                var wr = new StringWriter();
                RenderTextUserFilterBlock(wr, false, false);
                titleFilter = wr.ToString();
                
                filterClick = string.Join(" ", "style=\"cursor:pointer\"", " ",
                    string.Format("onclick=\"v4_grid.openUserFilterFormCmd({0}, '{1}', {2}, 1);\"",
                        Settings.GridCmdListnerIndex,
                        Id,
                        (int)FilterUser.FilterType
                    ));
            }
            else if (FilterUniqueValues != null && FilterUniqueValues.Count > 0)
            {
                filterRequired = FilterRequired;

                var w = new StringWriter();
                RenderClearFilterBlock(w, false);
                deleteSpan = w.ToString();

                var wr = new StringWriter();
                RenderTextValuesFilterBlock(wr, false);
                titleFilter = wr.ToString();

                filterClick = string.Join(" ", "style=\"cursor:pointer\"", " ",
                    string.Format(
                        "onclick=\"cmdasync('cmd', 'Listener', 'ctrlId', {0}, 'cmdName', 'RenderColumnSettings','ColumnId','{1}');\"",
                        Settings.GridCmdListnerIndex, FieldName));
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="page"></param>
        public void RenderColumnSettings(Page page, bool changeFilter, bool alwaysLoadDataFromDataBase)
        {
            var w = new StringWriter();

            if (IsSortedColumn && changeFilter)
            {
                RenderSortBlock(w);
                page.JS.Write("$('#divColumnSettingsForm_Sort_{0}').html('{1}');",
                    Settings.GridId,
                    HttpUtility.JavaScriptStringEncode(w.ToString()));
            }

            if (Settings.IsFilterEnable && changeFilter)
            {
                if (FilterUniqueValues != null && FilterUniqueValues.Count > 0 || FilterUser != null)
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


                if (ColumnType != GridColumnTypeEnum.Boolean && ColumnType != GridColumnTypeEnum.List)
                {
                    w = new StringWriter();
                    if (FilterUser == null) RenderUserFilterBlock(w);
                    RenderTextUserFilterBlock(w);
                    page.JS.Write(
                        "$('#divColumnSettingsForm_UserFilter_{0}').html('{1}');$('#divColumnSettingsForm_UserFilter_{0}').show();",
                        Settings.GridId,
                        HttpUtility.JavaScriptStringEncode(w.ToString()));
                    page.JS.Write(
                        "$('#v4_userFilterMenu_{0}').menu({{select: function(event, ui) {{v4_grid.openUserFilterForm(ui.item.children(), {1});}}}});",
                        Settings.GridId, Settings.GridCmdListnerIndex);
                }
                else
                {
                    page.JS.Write(
                        "$('#divColumnSettingsForm_UserFilter_{0}').html('');$('#divColumnSettingsForm_UserFilter_{0}').hide();",
                        Settings.GridId);
                }
            }

            FillUniqueValues(alwaysLoadDataFromDataBase);

            if (Settings.IsFilterEnable && IsFilteredColumn)
            {
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

                page.JS.Write("v4_grid.setStateCheckAllValues('{0}', 'cfAllValues', 'classValueCheckBox');", Settings.GridId);
            }

            page.JS.Write(
                "v4_grid.columnSettingsForm('{0}', {1}, 'imgSettings{2}_{1}', 'classValueCheckBox', '{3}', '{4}', {5}, '{6}');",
                Settings.GridId,
                Settings.GridCmdListnerIndex,
                DisplayOrder,
                Id,
                HttpUtility.JavaScriptStringEncode(string.Format("{0} [{1}]",
                    Settings.V4Page.Resx.GetString(Settings.IsFilterEnable && IsFilteredColumn ? "lblFilterAndSort" : "lblSort"), Alias)),
                !Settings.IsFilterUniqueEnable?"150":"null",
                changeFilter
                );
        }

        private void RenderSortBlock(TextWriter w)
        {
            var asc = Settings.V4Page.Resx.GetString("msgSortOrderAZ");
            var desc = Settings.V4Page.Resx.GetString("msgSortOrderZA");
            var classSortAsc = "";
            var classSortDesc = "";


            if (ColumnType.Equals(GridColumnTypeEnum.Short) || ColumnType.Equals(GridColumnTypeEnum.Int) || ColumnType.Equals(GridColumnTypeEnum.Long) || ColumnType.Equals(GridColumnTypeEnum.Double) ||
                ColumnType.Equals(GridColumnTypeEnum.Decimal) || ColumnType.Equals(GridColumnTypeEnum.Boolean))
            {
                asc = Settings.V4Page.Resx.GetString("msgSortOrderMinMax");
                desc = Settings.V4Page.Resx.GetString("msgSortOrderMaxMin");
            }

            if (OrderByNumber != null)
            {
                w.Write("<div class=\"v4DivTable\">");
                w.Write("<div class=\"v4DivTableRow\">");

                w.Write(
                    "<div class=\"v4DivTableCell v4PaddingCell\"><span style=\"cursor:pointer; display:inline-block;\" class=\"ui-icon ui-icon-closethick\" onclick=\"v4_grid.clearOrderByColumnValues({0}, '{1}');\"></span></div><div class=\"v4DivTableCell\" style=\"text-align:left;\"><a onclick=\"v4_grid.clearOrderByColumnValues({0}, '{1}');\"><nobr>{2}</nobr></a></div>",
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
                "<div class=\"v4DivTableCell v4PaddingCell\">{0}<span style=\"cursor:pointer;display:inline-block;\" src=\"/styles/sort-asc.png\" onclick=\"v4_grid.getColumnValuesFilter('{4}', {1}, 'classValueCheckBox', '{2}', 0, 'True');\" ></span></div><div class=\"v4DivTableCell v4PaddingCell\" style=\"margin-left:15px\"><a onclick=\"v4_grid.getColumnValuesFilter('{4}', {1}, 'classValueCheckBox', '{2}', 0);\"><nobr>{3}</nobr></a></div>",
                classSortAsc, Settings.GridCmdListnerIndex, Id, asc, Settings.GridId);
            w.Write("</div>");
            w.Write("<div class=\"v4DivTableRow\" style=\"margin-top:50px;\">");
            w.Write(
                "<div class=\"v4DivTableCell v4PaddingCell\">{0}<span style=\"cursor:pointer;display:inline-block;\" src=\"/styles/sort-desc.png\" onclick =\"v4_grid.getColumnValuesFilter('{4}', {1}, 'classValueCheckBox', '{2}', 1, 'True');\" ></span></div><div class=\"v4DivTableCell v4PaddingCell\" style=\"margin-left:15px\"><a onclick=\"v4_grid.getColumnValuesFilter('{4}', {1}, 'classValueCheckBox', '{2}', 1);\"><nobr>{3}</nobr></a></div>",
                classSortDesc, Settings.GridCmdListnerIndex, Id, desc, Settings.GridId);
            w.Write("</div>");

            w.Write("</div>");
        }

        /// <summary>
        ///     Отрисовка кнопки отмены сортировки
        /// </summary>
        /// <param name="w"></param>
        private void RenderClearFilterBlock(TextWriter w, bool isHtml = true)
        {
            if (isHtml)
            {
                w.Write("<div class=\"v4DivTable\">");
                w.Write("<div class=\"v4DivTableRow\">");
                w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
            }

            w.Write(
                "<span title=\"{2}\" class=\"ui-icon ui-icon-delete\" style=\"display: inline-block;cursor:pointer\" onclick=\"v4_grid.clearFilterColumnValues({0}, '{1}');\"></span>",
                Settings.GridCmdListnerIndex, Id, Settings.V4Page.Resx.GetString("msgGrigNoFilter"));

            if (isHtml)
            { 
                w.Write(
                    "</div><div class=\"v4DivTableCell\" style=\"text-align:left;\"><a onclick=\"v4_grid.clearFilterColumnValues({0},'{1}');\"><nobr>{2}</nobr></a></div>",
                    Settings.GridCmdListnerIndex, Id, Settings.V4Page.Resx.GetString("msgGrigNoFilter"));

                w.Write("</div>");
                w.Write("</div>");
                w.Write("</div>");
            }
        }

        /// <summary>
        ///     Отрисовка значения установленного фильтра
        /// </summary>
        /// <param name="w"></param>
        public void RenderTextValuesFilterBlock(TextWriter w, bool writeLabel = true)
        {
            if (FilterUniqueValues == null || FilterUniqueValues.Count == 0) return;

            var textValues = "";

            if (ColumnType == GridColumnTypeEnum.List)
            {
                var listTypeFieldList = JsonConvert.DeserializeObject<List<KeyValuePair<int, string>>>(FormatString);
                textValues =
                    string.Join(
                            FilterEqual == GridColumnFilterEqualEnum.NotIn
                            ? string.Format(" {0} ", Settings.V4Page.Resx.GetString("lANDUp"))
                            : string.Format(" {0} ", Settings.V4Page.Resx.GetString("lORUp")),
                        FilterUniqueValues.Select(x => "[<span class='gridFilterValue'>" + listTypeFieldList.Find(k => k.Key == System.Convert.ToInt32(x.Value)).Value + "</span>]").ToArray());
            }
            else
            {
                textValues =
                    string.Join(
                        FilterEqual == GridColumnFilterEqualEnum.NotIn
                            ? string.Format(" {0} ", Settings.V4Page.Resx.GetString("lANDUp"))
                            : string.Format(" {0} ", Settings.V4Page.Resx.GetString("lORUp")),
                        FilterUniqueValues.Select(x => "[<span class='gridFilterValue'>" + x.Value + "</span>]").ToArray());

            }

            if (writeLabel)
            {
                w.Write(Settings.V4Page.Resx.GetString("lblSetFilter") + ": ");
                w.Write("{0} ", Settings.V4Page.Resx.GetString("lblFieldValue"));
            }

            if (ColumnType == GridColumnTypeEnum.Boolean)
            {
                if (ValueBooleanCaption["0"] == "Нет")
                    w.Write($"{Alias}=[" + (FilterUniqueValues.Values.First().ToString() == "0" ? ValueBooleanCaption["0"] : ValueBooleanCaption["1"]) + "] ");
                else
                    w.Write($"[" + (textValues == "[0]" ? ValueBooleanCaption["0"] : ValueBooleanCaption["1"]) + "] ");
            }
            else
            {
                w.Write($"{Alias} "
                        +
                        (FilterEqual == GridColumnFilterEqualEnum.NotIn
                            ? Settings.V4Page.Resx.GetString("lNo") + " "
                            : "")
                        + Settings.V4Page.Resx.GetString("lblEqually") + " "
                        + (textValues.Length > 201
                            ? textValues.Left(200) + "..."
                            : textValues));
            }
        }

        /// <summary>
        ///     Отрисовка значения установленного фильтра
        /// </summary>
        /// <param name="w"></param>
        public void RenderFilterBlockTextValues(TextWriter w)
        {
            if (FilterUniqueValues == null || FilterUniqueValues.Count == 0) return;

            var textValues = "";

            if (ColumnType == GridColumnTypeEnum.List)
            {
                var listTypeFieldList = JsonConvert.DeserializeObject<List<KeyValuePair<int, string>>>(FormatString);
                textValues =
                    string.Join(
                            FilterEqual == GridColumnFilterEqualEnum.NotIn
                            ? string.Format(" {0} ", Settings.V4Page.Resx.GetString("lANDUp"))
                            : string.Format(" {0} ", Settings.V4Page.Resx.GetString("lORUp")),
                        FilterUniqueValues.Select(x => "[<span class='gridFilterValue'>" + listTypeFieldList.Find(k => k.Key == System.Convert.ToInt32(x.Value)).Value + "</span>]").ToArray());
            }
            else
            {
                textValues =
                    string.Join(
                        FilterEqual == GridColumnFilterEqualEnum.NotIn
                            ? string.Format(" {0} ", Settings.V4Page.Resx.GetString("lANDUp"))
                            : string.Format(" {0} ", Settings.V4Page.Resx.GetString("lORUp")),
                        FilterUniqueValues.Select(x => "[<span class='gridFilterValue'>" + x.Value + "</span>]").ToArray());

            }

            if (ColumnType == GridColumnTypeEnum.Boolean)
            {
                if (ValueBooleanCaption["0"] == "Нет")
                    w.Write($"{Alias}=[" + (FilterUniqueValues.Values.First().ToString() == "0" ? ValueBooleanCaption["0"] : ValueBooleanCaption["1"]) + "] ");
                else
                    w.Write($"[" + (textValues == "[0]" ? ValueBooleanCaption["0"] : ValueBooleanCaption["1"]) + "] ");
            }
            else
            {
                w.Write($"{Alias} "
                        +
                        (FilterEqual == GridColumnFilterEqualEnum.NotIn
                            ? Settings.V4Page.Resx.GetString("lNo") + " "
                            : "")
                        + Settings.V4Page.Resx.GetString("lblEqually") + " "
                        + (textValues.Length > 201
                            ? textValues.Left(200) + "..."
                            : textValues));
            }
        }

        /// <summary>
        ///     Создает новый объект, являющийся копией текущего экземпляра.
        /// </summary>
        public GridColumn Clone()
        {
            return (GridColumn)MemberwiseClone();
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
                case GridColumnTypeEnum.Short:
                case GridColumnTypeEnum.Long:
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
                    GridColumnUserFilterEnum.НеРавно.GetAttribute<GridColumnUserFilterAttribute>().AliasString) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.НачинаетсяС,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.НачинаетсяС.GetAttribute<GridColumnUserFilterAttribute>().AliasString) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.НеНачинаетсяС,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.НеНачинаетсяС.GetAttribute<GridColumnUserFilterAttribute>().AliasString) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.ЗаканчиваетсяНа,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.ЗаканчиваетсяНа.GetAttribute<GridColumnUserFilterAttribute>()
                        .AliasString) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.НеЗаканчиваетсяНа,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.НеЗаканчиваетсяНа.GetAttribute<GridColumnUserFilterAttribute>()
                        .AliasString) +
                "...");
            FilterUserClause.Add(GridColumnUserFilterEnum.Содержит,
                Settings.V4Page.Resx.GetString(
                    GridColumnUserFilterEnum.Содержит.GetAttribute<GridColumnUserFilterAttribute>().AliasString) +
                "...");
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
                    GridColumnUserFilterEnum.НеРавно.GetAttribute<GridColumnUserFilterAttribute>().AliasNumber) +
                "...");
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
        public void FillUniqueValues(bool alwaysLoadDataFromDataBase)
        {
            var columnWithFilter =
                Settings.TableColumns.Where(
                    x => x.Id != Id && x.FilterUniqueValues != null && x.FilterUniqueValues.Count > 0).ToList();

            if (!alwaysLoadDataFromDataBase && columnWithFilter.Count > 0)
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

                UniqueValues =
                        UniqueValuesOriginal.Where(
                                x =>
                                    results.Any(
                                        y =>
                                            y.MyValue != null && y.MyValue.Equals(x.Value) ||
                                            (y.MyValue == null || y.MyValue.ToString() == "") && x.Value.ToString() == ""))
                            .ToDictionary(v => v.Key, v => v.Value);
            }
            else
            {
                UniqueValues = UniqueValuesOriginal;
            }
        }

        #endregion
    }
}