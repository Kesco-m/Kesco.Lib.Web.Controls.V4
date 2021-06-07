using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Web;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Entities.Corporate;
using Kesco.Lib.Localization;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Класс для отображения выбранных элементов списка, работает в паре с контролом Select
    /// </summary>
    public class ListItems : V4Control
    {
        /// <summary>
        ///     Индекс поля (используется для вывода таблиц)
        /// </summary>
        public int Index;

        /// <summary>
        ///     Коллекция выбранных элементов
        /// </summary>
        public IEnumerable List;

        /// <summary>
        ///     Список, указывающий на то, что чтобы получить значение поля у объекта используется функция
        /// </summary>
        public List<SelectMethodGetEntityValue> MethodsGetEntityValue;

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
        public new bool IsCaller { get; set; }

        /// <summary>
        ///     Тип сущности абонента
        /// </summary>
        public new CallerTypeEnum CallerType { get; set; }

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
            if (arr.Length > 1) field = arr[Index];
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
                    if (string.IsNullOrEmpty(text))
                        text = "#" + id;
                    if (isCaller && !callerType.Equals(CallerTypeEnum.Empty))
                        toolTip = string.Format(" class=\"v4_callerControl\" data-id=\"{0}\" caller-type=\"{1}\"",
                            HttpUtility.UrlEncode(id), (int) callerType);
                    else if (isItemCompany)
                        toolTip = string.Format(" class=\"v4_itemCompanyControl\" data-id=\"{0}\"",
                            HttpUtility.UrlEncode(id));
                    w.Write("<span>");
                    if (isRemove && !IsReadOnly)
                    {
                        if (IsDisabled)
                            w.Write(@"<td><img src='/STYLES/DeleteGrayed.gif' alt='{0}'></td>",
                                Resx.GetString("removeFromList"));
                        else
                            w.Write(
                                @"<img src=""/STYLES/delete.gif"" style=""cursor:pointer;"" alt=""{1}"" title=""{1}"" {3}
onclick=""cmdasync('ctrl', '{2}', 'cmd', 'RemoveSelectedItem','id', '{0}', 'ask', '{4}');""
onkeydown = ""var key=v4_getKeyCode(event); if(key == 13 || key == 32) cmdasync('ctrl', '{2}', 'cmd', 'RemoveSelectedItem','id','{0}', 'ask', '{4}');""
>",
                                HttpUtility.JavaScriptStringEncode(id.Replace("\"", " ").Replace("'", " ")),
                                Resx.GetString("removeFromList"),
                                htmlID == "" ? HtmlID : htmlID,
                                tabIndex.HasValue ? "tabindex='" + tabIndex.Value + "'" : "tabindex='0'",
                                confirmRemove ? 1 : 0); //removefromList.gif
                    }

                    if (IsDisabled)
                    {
                        w.Write("<span class='v4_selectItemDisabled'>");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(openPath))
                            w.Write("<a id=\"linkLI_{2}\" href=\"javascript:void(0);\" onclick=\"Kesco.windowOpen('{0}{1}id={2}', null, null, 'linkLI');\"{3}>", openPath,
                                openPath.IndexOf("?", StringComparison.InvariantCulture) > -1 ? "&" : "?", id, toolTip);
                        else if (!string.IsNullOrEmpty(openFunc))
                            w.Write("<a href=\"#\" onclick='{0}' {1}>",
                                string.Format(openFunc, id, htmlID == "" ? HtmlID : htmlID), toolTip);
                        else
                            w.Write("<span {0}>", toolTip);
                    }

                    w.Write(text);

                    if (!IsDisabled && (!string.IsNullOrEmpty(openPath) || !string.IsNullOrEmpty(openFunc)))
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

                //var oText = to.GetProperty(field).GetValue(o, null) ?? "";
                //var text = oText.ToString();

                var text = GetFieldValue(o, field.Trim());

                if (string.IsNullOrEmpty(text))
                    text = "#" + id;
                if (isCaller && !callerType.Equals(CallerTypeEnum.Empty))
                    toolTip = string.Format(" class=\"v4_callerControl\" data-id=\"{0}\" caller-type=\"{1}\"",
                        HttpUtility.UrlEncode(id), (int) callerType);
                else if (isItemCompany)
                    toolTip = string.Format(" class=\"v4_itemCompanyControl\" data-id=\"{0}\"",
                        HttpUtility.UrlEncode(id));
                w.Write("<tr>");
                if (isRemove && !IsReadOnly)
                {
                    if (IsDisabled)
                        w.Write(@"<td><img src='/STYLES/DeleteGrayed.gif' alt='{0}'></td>",
                            Resx.GetString("removeFromList"));
                    else
                        w.Write(
                            @"<td><img src=""/STYLES/delete.gif"" style=""cursor:pointer;"" alt=""{1}"" title=""{1}"" {3}
onclick=""cmdasync('ctrl', '{2}', 'cmd', 'RemoveSelectedItem','id','{0}', 'ask', '{4}');""
onkeydown = ""var key=v4_getKeyCode(event); if(key == 13 || key == 32) cmdasync('ctrl', '{2}', 'cmd', 'RemoveSelectedItem','id','{0}', 'ask', '{4}');""
></td>",
                            HttpUtility.JavaScriptStringEncode(id.Replace("\"", " ").Replace("'", " ")),
                            Resx.GetString("removeFromList"),
                            htmlID == "" ? HtmlID : htmlID,
                            tabIndex.HasValue ? "tabindex='" + tabIndex.Value + "'" : "tabindex='0'",
                            confirmRemove ? 1 : 0); //removefromList.gif
                }

                w.Write("<td width='99%'>");

                if (IsDisabled)
                {
                    w.Write("<span class='v4_selectItemDisabled'>");
                }
                else
                {
                    if (!string.IsNullOrEmpty(openPath))
                        w.Write("<a id=\"linkLI_{2}\" href=\"javascript:void(0);\" onclick=\"Kesco.windowOpen('{0}{1}id={2}', null, null, 'linkLI');\"{3}>", openPath,
                            openPath.IndexOf("?", StringComparison.InvariantCulture) > -1 ? "&" : "?", id, toolTip);
                    else if (!string.IsNullOrEmpty(openFunc))
                        w.Write("<a href=\"#\" onclick='{0}' {1}>",
                            string.Format(openFunc, id, htmlID == "" ? HtmlID : htmlID), toolTip);
                    else
                        w.Write("<span {0}>", toolTip);
                }

                w.Write(text);

                if (!IsDisabled && (!string.IsNullOrEmpty(openPath) || !string.IsNullOrEmpty(openFunc)))
                    w.Write("</a>");
                else
                    w.Write("</span>");

                w.Write("</td></tr>");
            }

            w.Write("</table>");
        }

        private string GetFieldValue(object obj, string currentField)
        {
            var val = "";
            var t = obj.GetType();
            if (MethodsGetEntityValue == null || MethodsGetEntityValue.Count == 0 ||
                MethodsGetEntityValue.FirstOrDefault(
                    x =>
                        string.Equals(x.ValueField, currentField,
                            StringComparison.InvariantCultureIgnoreCase)) == null)
            {
                val = t.GetProperty(currentField).GetValue(obj, null).ToString();
            }
            else
            {
                var mSettings =
                    MethodsGetEntityValue.FirstOrDefault(
                        x =>
                            string.Equals(x.ValueField, currentField,
                                StringComparison.InvariantCultureIgnoreCase));
                if (mSettings == null)
                    throw new Exception(string.Format("Некорретно настроен элемент управления #{0}!", ID));

                var mInfo = t.GetMethod(mSettings.MethodName);
                if (mInfo == null)
                    throw new Exception(
                        string.Format("В классе объекта элемента управления #{0} не найден метод #{1}!", ID,
                            mSettings.MethodName));

                var paramObjects = mSettings.MethodParams;
                var urs =
                    paramObjects.FirstOrDefault(
                        x => x.GetType() == typeof(Employee) && ((Employee) x).IsLazyLoadingByCurrentUser);
                if (urs != null)
                {
                    var inx = Array.IndexOf(paramObjects, urs);
                    paramObjects[inx] = V4Page.CurrentUser;
                }

                val = mInfo.Invoke(obj, paramObjects).ToString();
            }

            return val;
        }
    }
}