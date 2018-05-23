using System;
using System.Collections;
using System.IO;
using System.Resources;
using System.Web;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Localization;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Класс для отображения выбранных элементов списка, работает в паре с контролом Select
    /// </summary>
    public class ListItems : V4Control
    {
        /// <summary>
        ///     Идентификатор всплывающего контрола с контактами
        /// </summary>
        private string _idToolTip = "";

        /// <summary>
        ///     Индекс поля (используется для вывода таблиц)
        /// </summary>
        public int Index;

        /// <summary>
        ///     Коллекция выбранных элементов
        /// </summary>
        public IEnumerable List;

        /// <summary>
        ///     Локализация
        /// </summary>
        public new ResourceManager Resx = Resources.Resx;

        /// <summary>
        ///     Если ссылка, то путь к ресурсу
        /// </summary>
        public string OpenPath { get; set; }

        /// <summary>
        ///     Если ссылка, то путь к ресурсу
        /// </summary>
        public string OpenFunc { get; set; }

        /// <summary>
        ///     Ключ в коллекции
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///     Значение в коллекции
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        ///     Код сотрудника (для приложения Contacts)
        /// </summary>
        public string KeyToolTip { get; set; }

        /// <summary>
        ///     Id родителя
        /// </summary>
        public string ParentHtmlID { get; set; }

        /// <summary>
        ///     Нужно ли подтверждение удаления
        /// </summary>
        public bool ConfirmRemove { get; set; }

        /// <summary>
        ///     Признак возможности удаления
        /// </summary>
        public bool IsRemove { get; set; }

        /// <summary>
        ///     Порядок вывода элементов. В строчку - true, в столбец - false
        /// </summary>
        public bool IsRow { get; set; }

        /// <summary>
        ///     Признак использования приложения Контакты
        /// </summary>
        public bool IsCaller { get; set; }

        /// <summary>
        ///     Тип сущности абонента
        /// </summary>
        public CallerTypeEnum CallerType { get; set; }

        /// <summary>
        ///     Признак отображения в тултипе списка компаний, к которым принадлежит сущность (для множественного выбора)
        /// </summary>
        public bool IsItemCompany { get; set; }

        /// <summary>
        ///     Признак наличия элементов в списке
        /// </summary>
        public bool IsFill
        {
            get
            {
                var c = List as ICollection;
                return c != null && c.Count > 0;
            }
        }

        /// <summary>
        ///     Отрисовка тела элемента управления
        /// </summary>
        /// <param name="w">Объект для записи HTML-разметки</param>
        protected override void RenderControlBody(TextWriter w)
        {
            RenderListItems(w, List, OpenPath, OpenFunc, Key, Field, TabIndex, IsRemove, ConfirmRemove, IsRow,
                ParentHtmlID, Index, IsCaller, CallerType, IsItemCompany);
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Response</param>
        /// <param name="list">Источник данных</param>
        /// <param name="openPath">Если ссылка, то путь к ресурсу</param>
        /// <param name="key">Ключ в коллекции</param>
        /// <param name="field">Значение в коллекции</param>
        /// <param name="tabIndex">Индекс перехода по Tab</param>
        /// <param name="isRemove">Признак возможности удаления</param>
        /// <param name="isRow">Порядок вывода элементов. В строчку - true, в столбец - false</param>
        /// <param name="isCaller">Признак использования приложения Контакты</param>
        /// <param name="isItemCompany">Признак использования списка компаний для сущности</param>
        public void RenderListItems(TextWriter w, IEnumerable list, string openPath, string openFunc, string key,
            string field, int? tabIndex, bool isRemove = true, bool confirmRemove = false, bool isRow = false,
            string htmlID = "", int index = 0, bool isCaller = false, CallerTypeEnum callerType = CallerTypeEnum.Empty,
            bool isItemCompany = false)
        {
            Index = index;
            List = list;
            var toolTip = "";
            var arr = field.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length > 1)
            {
                field = arr[Index];
            }
            if (isRow)
            {
                foreach (var item in List)
                {
                    if (item == null) return;
                    var t = item.GetType();
                    var o = t.GetProperty("Value").GetValue(item, null) ?? "";
                    var to = o.GetType();
                    var oId = to.GetProperty(key).GetValue(o, null) ?? "";
                    var id = oId.ToString();
                    var oText = to.GetProperty(field).GetValue(o, null) ?? "";
                    var text = oText.ToString();
                    if (String.IsNullOrEmpty(text))
                        text = "#" + id;
                    if (isCaller && !callerType.Equals(CallerTypeEnum.Empty))
                    {
                        toolTip = String.Format(" class=\"v4_callerControl\" data-id=\"{0}\" caller-type=\"{1}\"",
                            HttpUtility.UrlEncode(id), (int) callerType);
                    }
                    else if (isItemCompany)
                    {
                        toolTip = String.Format(" class=\"v4_itemCompanyControl\" data-id=\"{0}\"",
                            HttpUtility.UrlEncode(id));
                    }
                    w.Write("<span>");
                    if (isRemove && !IsReadOnly)
                    {
                        if (IsDisabled)
                        {
                            w.Write(@"<td><img src='/STYLES/DeleteGrayed.gif' alt='{0}'></td>", Resx.GetString("removeFromList"));
                        }
                        else
                        {
                            w.Write(@"<img src=""/STYLES/delete.gif"" style=""cursor:pointer;"" alt=""{1}"" title=""{1}"" {3}
onclick=""cmdasync('ctrl', '{2}', 'cmd', 'RemoveSelectedItem','id', '{0}', 'ask', '{4}');""
onkeydown = ""var key=v4_getKeyCode(event); if(key == 13 || key == 32) cmdasync('ctrl', '{2}', 'cmd', 'RemoveSelectedItem','id','{0}', 'ask', '{4}');""
>",
                                HttpUtility.JavaScriptStringEncode(id.Replace("\"", " ").Replace("'", " ")),
                                Resx.GetString("removeFromList"),
                                htmlID == "" ? HtmlID : htmlID,
                                tabIndex.HasValue ? "tabindex='" + tabIndex.Value + "'" : "tabindex='0'",
                                confirmRemove ? 1 : 0); //removefromList.gif
                        }
                    }

                    if (IsDisabled)
                    {
                        w.Write("<span class='v4_selectItemDisabled'>");
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(openPath))
                            w.Write("<a href=\"javascript:v4_windowOpen('{0}{1}id={2}');\"{3}>", openPath,
                                openPath.IndexOf("?", StringComparison.InvariantCulture) > -1 ? "&" : "?", id, toolTip);
                        else if (!String.IsNullOrEmpty(openFunc))
                            w.Write("<a href=\"#\" onclick='{0}' {1}>",
                                string.Format(openFunc, id, htmlID == "" ? HtmlID : htmlID), toolTip);
                        else
                            w.Write("<span {0}>", toolTip);
                    }

                    w.Write(text);

                    if (!IsDisabled && (!String.IsNullOrEmpty(openPath) || !String.IsNullOrEmpty(openFunc)))
                        w.Write("</a>");
                    else
                        w.Write("</span>");
                    w.Write("</span>  ");
                }
                return;
            }

            var ci = List as ICollection;
            if (ci != null && ci.Count == 0) return;

            w.Write("<table cellspacing=\"0\" cellpadding=\"0\">");
            foreach (var item in List)
            {
                if (item == null) return;
                var t = item.GetType();
                var o = t.GetProperty("Value").GetValue(item, null) ?? "";
                var to = o.GetType();
                var oId = to.GetProperty(key).GetValue(o, null) ?? "";
                var id = oId.ToString();
                var oText = to.GetProperty(field).GetValue(o, null) ?? "";
                var text = oText.ToString();
                if (String.IsNullOrEmpty(text))
                    text = "#" + id;
                if (isCaller && !callerType.Equals(CallerTypeEnum.Empty))
                {
                    toolTip = String.Format(" class=\"v4_callerControl\" data-id=\"{0}\" caller-type=\"{1}\"",
                        HttpUtility.UrlEncode(id), (int) callerType);
                }
                else if (isItemCompany)
                {
                    toolTip = String.Format(" class=\"v4_itemCompanyControl\" data-id=\"{0}\"",
                        HttpUtility.UrlEncode(id));
                }
                w.Write("<tr>");
                if (isRemove && !IsReadOnly)
                {
                    if (IsDisabled)
                    {
                        w.Write(@"<td><img src='/STYLES/DeleteGrayed.gif' alt='{0}'></td>", Resx.GetString("removeFromList"));
                    }
                    else
                    {
                        w.Write(@"<td><img src=""/STYLES/delete.gif"" style=""cursor:pointer;"" alt=""{1}"" title=""{1}"" {3}
onclick=""cmdasync('ctrl', '{2}', 'cmd', 'RemoveSelectedItem','id','{0}', 'ask', '{4}');""
onkeydown = ""var key=v4_getKeyCode(event); if(key == 13 || key == 32) cmdasync('ctrl', '{2}', 'cmd', 'RemoveSelectedItem','id','{0}', 'ask', '{4}');""
></td>",
                            HttpUtility.JavaScriptStringEncode(id.Replace("\"", " ").Replace("'", " ")),
                            Resx.GetString("removeFromList"),
                            htmlID == "" ? HtmlID : htmlID,
                            tabIndex.HasValue ? "tabindex='" + tabIndex.Value +"'": "tabindex='0'",
                            confirmRemove ? 1 : 0); //removefromList.gif
                    }
                }

                w.Write("<td width='99%'>");

                if (IsDisabled)
                {
                    w.Write("<span class='v4_selectItemDisabled'>");
                }
                else
                {
                    if (!String.IsNullOrEmpty(openPath))
                        w.Write("<a href=\"javascript:v4_windowOpen('{0}{1}id={2}');\"{3}>", openPath,
                            openPath.IndexOf("?", StringComparison.InvariantCulture) > -1 ? "&" : "?", id, toolTip);
                    else if (!String.IsNullOrEmpty(openFunc))
                        w.Write("<a href=\"#\" onclick='{0}' {1}>",
                            string.Format(openFunc, id, htmlID == "" ? HtmlID : htmlID), toolTip);
                    else
                        w.Write("<span {0}>", toolTip);
                }

                w.Write(text);

                if (!IsDisabled && (!String.IsNullOrEmpty(openPath) || !String.IsNullOrEmpty(openFunc)))
                    w.Write("</a>");
                else
                    w.Write("</span>");

                w.Write("</td></tr>");
            }
            w.Write("</table>");
        }
    }
}