using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Web.Controls.V4.Common;
using System.Reflection;
using Kesco.Lib.Entities.Corporate;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Делегат вызывается перед поиском
    /// </summary>
    /// <param name="sender">Инициатор вызова</param>
    public delegate void BeforeSearchEventHandler(object sender);

    /// <summary>
    ///     Класс выбора бизнес-сущностей
    /// </summary>
    
    public abstract class Select : V4Control
    {
        /// <summary>
        /// Класс описывающий событие выбора элемента 
        /// </summary>
        public class CheckEventArgs : EventArgs
        {
            public CheckEventArgs(bool checkedValue)
            {
                Checked = checkedValue;
            }

            public bool Checked { get; set; }
        }

        /// <summary>
        /// В том случае, если используется перечень полей, указывается, какое поле брать в качестве названия
        /// </summary>
        public int IndexField;

        /// <summary>
        ///     Конструктор
        /// </summary>
        protected Select()
        {
            DependingControlsOfCurrentElement = "";
            CLID = 0;
            SearchText = "";
            KeyField = "";
            URLShowEntity = "";
            URLAdvancedSearch = "";
            FuncShowEntity = "";
        }

        #region Настройка внешнего вида

        /// <summary>
        ///     CSS
        /// </summary>
        public string ClassFieldset { get; set; }

        #endregion

        /// <summary>
        ///     Настройка клиента. Передавать всегда. Используется при сохранении пользовательских настроек
        /// </summary>
        public int CLID { get; set; }

        #region Делегаты

        /// <summary>
        ///     Делегат заполнения выпадающего списка
        /// </summary>
        public EnumerableStringDelegate FillPopupWindow;

        /// <summary>
        ///     Делегат получения названия сущности по ID
        /// </summary>
        public ObjectStringDelegate SelectedItemById;

        /// <summary>
        ///     Событие, вызываемое перед поиском
        /// </summary>
        public event BeforeSearchEventHandler BeforeSearch;

        /// <summary>
        /// Функция обратного вызова для события выбора элемента
        /// </summary>
        public event EventHandler<CheckEventArgs> CheckChanged;

        #endregion

        #region Field

        /// <summary>
        ///     Коллекция условий
        /// </summary>
        private readonly List<Item> _clauseItems = new List<Item>();

        //Связанный с элементом список выбранных значений
        private ListItems _list = null;

        private const string _fieldset_style = "position: relative; padding-top: 5px;";
        private const string _fieldset_div_style = "width: 20px; height:20px; position: absolute; top: 0px; right: 0px;";

        private string _fieldset_input_onclick(string id)
        {
            return string.Format("cmd('ctrl', '{0}', 'checked', this.checked ? '1' : '0'); if(gi('{0}_2').checked) gi('{0}_1').focus(); else gi('{0}_2').focus();", id);
        }

        /// <summary>
        /// Признак использования элемента CheckBox
        /// </summary>
        public bool HasCheckbox
        {
            get { return _has_checkbox; }
            set
            {
                if (_has_checkbox != value)
                {
                    if (null != _list)
                        _list.IsDisabled = IsDisabled || HasCheckbox && !Checked;

                    _has_checkbox = value;
                    SetPropertyChanged("HasCheckbox");
                }
            }
        }

        //Значение свойства HasCheckbox
        private bool _has_checkbox;

        /// <summary>
        /// Свойство значения элемента CheckBox
        /// </summary>
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked != value)
                {
                    _checked = value;

                    if (null != _list)
                        _list.IsDisabled = IsDisabled || HasCheckbox && !_checked;

                    SetPropertyChanged("Checked");
                    if (null != CheckChanged)
                        CheckChanged(this, new CheckEventArgs(value));
                }
            }
        }

        //Значение свойства Checked
        private bool _checked;

        /// <summary>
        ///     Признак возможности удаления (для множественного выбора)
        /// </summary>
        public bool IsRemove { get; set; }

        /// <summary>
        ///     Спрашивать подтверждения удаления (для множественного выбора)
        /// </summary>
        public bool ConfirmRemove { get; set; }

        /// <summary>
        ///     Сообщение вопрос-подтверждение удаления (для множественного выбора)
        /// </summary>
        /// <example>Вы уверены, что хотите удалить документ?</example>
        public string ConfirmRemoveMsg { get; set; }

        /// <summary>
        ///     Порядок вывода элементов. В строчку - true, в столбец - false (для множественного выбора)
        /// </summary>
        public bool IsRow { get; set; }

        /// <summary>
        ///     Признак отображения в тултипе списка компаний, к которым принадлежит сущность (для множественного выбора)
        /// </summary>
        public bool IsItemCompany { get; set; }

        /// <summary>
        ///     Перечисление через "," колонок в заголовке выпадающего списка
        /// </summary>
        private string _anvancedHeaderPopupResult = "";

        /// <summary>
        ///     Список название полей из источника данных, которые должы отображаться в выпадающем окне
        /// </summary>
        private string _displayFields = "";

        /// <summary>
        ///     Количество отображаемых в выпадающем списке значений. По-умолчанию - 8
        /// </summary>
        private int _maxItemsInPopup = 8;

        /// <summary>
        ///     Максимальное количество записей, которое должен вернуть источник данных. Удалить в дальнейшем, т.к. это всегда
        ///     [_maxItemsInPopup + 1]
        /// </summary>
        private int _maxItemsInQuery;

        /// <summary>
        ///     Старое значение
        /// </summary>
        private string _oldValue = "";

        /// <summary>
        ///     Словарь выбранных элементов, поле ключа - ID, поле значения - наименование
        /// </summary>
        private List<Entities.Item> _selectedItems = new List<Entities.Item>();

        /// <summary>
        ///     Словарь выбранных элементов, копия
        /// </summary>
        private readonly List<Entities.Item> _selectedItemsCopy = new List<Entities.Item>();

        /// <summary>
        ///     Путь к расширенному поиску сущности
        /// </summary>
        private string _urlAdvancedSearch = "";

        /// <summary>
        ///     Текущее значение
        /// </summary>
        private string _value = "";

        /// <summary>
        ///     Выбранное условие фильтрации
        /// </summary>
        private string _valueSelectEnum = "0";

        /// <summary>
        ///     Текущая значение, отображаемое клиенту
        /// </summary>
        private string _valueText = "";

        /// <summary>
        ///     Значение в коллекции выбранных элементов
        /// </summary>
        private string Field { get; set; }

        /// <summary>
        /// Добавлять ли к найденному запись, которой нет в БД
        /// </summary>
        public bool IsCustomRecordInPopup { get; set; }

        /// <summary>
        /// При выборе customId все остальные выбранные элементы будут очищены
        /// </summary>
        public bool IsSelectOnlyCustomRecord { get; set; }

        /// <summary>
        /// Идентификатор дополнительной записи
        /// </summary>
        public string CustomRecordId { get; set; }

        /// <summary>
        /// Название дополнительной записи
        /// </summary>
        public string CustomRecordText { get; set; }

        #endregion

        #region Render

        /// <summary>
        ///     Отрисовка списка условий в виде попап дива
        /// </summary>
        /// <param name="w">Ответ сервера</param>
        /// <param name="data">Коллекция условий</param>
        private void RenderPopupWindowClause(TextWriter w, List<Item> data)
        {
            JS.Write("v4s_popup.ids='{0}';", HtmlID);
            w.Write("<table class='v4_p_clause'>");
            foreach (var item in data)
            {
                w.Write("<tr cmd='setHead' idItem='{0}'>", item.Code);
                w.Write("<td name='tblClause{1}'>{0}</td>", HttpUtility.HtmlEncode(item.Name), HtmlID);
                w.Write("</tr>");
            }
            w.Write("</table>");
        }

        /// <summary>
        ///     Отрисовка попап со списком сущностей
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="data">Данные</param>
        private void RenderPopupWindow(TextWriter w, IEnumerable data)
        {
            JS.Write("v4s_popup.ids='{0}';", HtmlID);
            w.Write("<table class='{0}v4s_p' cellpadding=\"0\" cellspacing=\"0\">", AnvancedHeaderPopupResult.Length>0?"gridSelect ":"");
           
            if (IsCustomRecordInPopup)
            {
                if (SearchText == "" || CustomRecordText.Contains(SearchText))
                {
                    w.Write("<tr cmd='select' idItem='{0}' textItem='{1}'>", CustomRecordId, CustomRecordText);

                    w.Write("<td>{0}</td>", CustomRecordText);
                    w.Write("</tr>");
                }
            }

            var n = 0;
            var displayFields = DisplayFields.Split(',');
            if (AnvancedHeaderPopupResult.Length > 0)
            {
                w.Write(AnvancedHeaderPopupResult);
            }
           
            foreach (var obj in data)
            {
                n++;
                if (n > _maxItemsInPopup && !IsNotUseSelectTop) break;
                var isDataRow = (obj is DataRow);
                var t = obj.GetType();
                string key;
                if (isDataRow)
                {
                    key = (obj as DataRow)[KeyField].ToString();
                }
                else
                {
                    key = t.GetProperty(KeyField).GetValue(obj, null).ToString();
                }

                var result = "";
                var wTr = false;
                foreach (var field in displayFields)
                {
                    string val;
                    var currentField = field.Trim();
                    if (isDataRow)
                    {
                        val = (obj as DataRow)[currentField].ToString();
                    }
                    else
                    {
                        val = GetFieldValue(obj, currentField);
                    }
                    result = HttpUtility.HtmlEncode(val);
                    if (!wTr) w.Write("<tr cmd='select' idItem='{0}' textItem='{1}'>", key, result);
                    wTr = true;

                    w.Write("<td noWrap>{0}</td>", result);

                }
                w.Write("</tr>");
            }
            var colSpan = displayFields.Length > 1 ? " colspan='" + displayFields.Length + "'" : "";
            if (n > _maxItemsInPopup && !IsNotUseSelectTop)
            {
                w.Write("<tr class='v4s_noselect'><td{1} class='v4s_pc'>" + Resx.GetString("sFindMore") + "</td></tr>",
                    _maxItemsInPopup, colSpan);
            }
            if (n == 0)
            {
                w.Write("<tr class='v4s_noselect'><td{0} class='v4s_pc'>" + Resx.GetString("sNotFound") + "</td></tr>",
                    colSpan);
            }
            

            if (((n > _maxItemsInPopup && !IsNotUseSelectTop) || n == 0 || IsAlwaysAdvancedSearch) &&
                URLAdvancedSearch.Length > 0)
            {
                w.Write(
                    "<tr cmd='search'><td{0} class='v4s_pc_over'><img src=\"/styles/Search.gif\" />&nbsp;{1}</td></tr>",
                    colSpan, Resx.GetString("sAdvancedSearch"));
            }
            else if (((n > _maxItemsInPopup && !IsNotUseSelectTop) || n == 0) && (URLAdvancedSearch.Length == 0))
            {
                w.Write(
                    "<tr class='v4s_noselect'><td{0} class='v4s_pc'>" + Resx.GetString("sRefineSearch") + "</td></tr>",
                    colSpan);
            }

            if (URIsCreateEntity != null && !IsNoAlwaysCreateEntity && n <= _maxItemsInPopup)
            {
                foreach (var uce in URIsCreateEntity)
                {
                    w.Write(
                        "<tr cmd='create' idUrl='{0}'><td{1} class='v4s_pc_over'><img src=\"{2}\" />&nbsp;{3}</td></tr>",
                        uce.Id, colSpan, uce.ImgPath, uce.Label);
                }
            }

            w.Write("</table>");
        }

        /// <summary>
        ///     Отрисовка кнопки
        /// </summary>
        /// <param name="w">Поток</param>
        private void RenderButton(TextWriter w, string disabled_attribute)
        {
            w.Write("<input type='button' id='{0}_1' {1}", HtmlID, disabled_attribute);

            if (Value.Length > 0 && URLShowEntity.Length > 0)
                w.Write(" class='v4s_btnDetail' onclick=\"v4_windowOpen('{0}', '');\"",
                    URLShowEntity + (URLShowEntity.IndexOf("?") > 0 ? "&" : "?") + "id=" + Value);
            else if (Value.Length > 0 && FuncShowEntity.Length > 0)
                w.Write(" class='v4s_btnDetail'");
            else
                w.Write(" class='v4s_btn' value='...' onclick=\"v4s_btnClick('{0}')\"", HtmlID);

            if (TabIndex.HasValue)
            {
                w.Write(" TabIndex={0} ", TabIndex);
            }

            if (URLShowEntity.Length > 0)
                w.Write(" urlShowEntity='{0}' ", HttpUtility.JavaScriptStringEncode(URLShowEntity));

            if (FuncShowEntity.Length > 0)
                w.Write(" funcShowEntity='{0}' ", FuncShowEntity);


            w.Write(" help='{0}' />", HttpUtility.HtmlEncode(Help));
        }

        /// <summary>
        ///     Отрисовка выпадающего списка
        /// </summary>
        /// <param name="w">Поток вывода</param>
        protected virtual void RenderSelectBody(TextWriter w)
        {
            if (!IsReadOnly)
            {
                string disabled_attribute = IsDisabled
                    || HasCheckbox && !Checked
                    || ValueSelectEnum == ((int)SelectEnum.NoValue).ToString(CultureInfo.InvariantCulture)
                    || ValueSelectEnum == ((int)SelectEnum.Any).ToString(CultureInfo.InvariantCulture) ? "disabled='disabled'" : string.Empty;

                w.Write("<table class='v4s' style='align:left' cellpadding=\"0\" cellspacing=\"0\">");
                w.Write("<tr>");
                w.Write("<td>");
                w.Write("<nobr><div class='v4DivInline' id=\"v3il_{0}\"></div>", HtmlID);
                w.Write("<input type='text' value='{1}' id='{0}_0' {2} {3} {4}", HtmlID, HttpUtility.HtmlEncode(ValueText),
                    (IsCaller || IsItemCompany ? "data-id='" + HttpUtility.HtmlEncode(Value) + "'" : ""),
                    (IsCaller && CallerType != CallerTypeEnum.Empty ? "caller-type='" + (int)CallerType + "'" : ""), disabled_attribute);

                w.Write(" style=\"width: {0}\"", Width.IsEmpty ? "100%" : Width.ToString(CultureInfo.InvariantCulture));
                w.Write(" isRequired={0}", IsRequired ? 1 : 0);
                w.Write(" t='{0}'", HttpUtility.HtmlEncode(ValueText));
                w.Write(" v='{0}'", HttpUtility.HtmlEncode(Value));
                w.Write(" stxt=''");

                if (IsCustomRecordInPopup)
                    w.Write(" crp='{0}'", HttpUtility.HtmlEncode(CustomRecordId));

                w.Write(" help='{0}'", HttpUtility.HtmlEncode(Help));

                if (Title.Length > 0)
                {
                    w.Write(" title='{0}'", HttpUtility.HtmlEncode(Title));
                }

                if (!string.IsNullOrEmpty(NextControl))
                {
                    if (NextControl == HtmlID)
                        w.Write(" nc='{0}'", NextControl + "_0");
                    else
                        w.Write(" nc='{0}'", GetHtmlIdNextControl());
                }

                w.Write(" onblur=\"v4s_onBlur(event);\"");
                w.Write(" onkeydown=\"return v4s_keyDown(event);\"");
                w.Write(" oninput=\"v4s_textChange(event, '{0}', 0);\"", HtmlID);
                w.Write(" onpropertychange=\"v4s_textChange(event, '{0}', 1);\"", HtmlID);

                var cssSelectClass = "v4si";

                if (IsRequired && Value.Length == 0)
                {
                    cssSelectClass += " v4s_required";
                    
                }
                else if (!String.IsNullOrEmpty(ClassFieldset))
                {
                    cssSelectClass += " " + ClassFieldset;                    
                }
                else if (IsCaller)
                {
                    cssSelectClass += " v4_callerControl";                    
                }
                else if (IsItemCompany && !String.IsNullOrEmpty(ValueText))
                {
                    cssSelectClass += " v4_itemCompanyControl";                                  
                }

                w.Write(" class='{0}' ", cssSelectClass);

                if (TabIndex.HasValue)
                {
                    w.Write(" TabIndex={0} ", TabIndex);
                }
                w.Write("/></nobr></td>");

                w.Write("<td id='v3sb_{0}' style='width:100%'>", HtmlID);
                RenderButton(w, disabled_attribute);
                w.Write("</td>");

                w.Write("</tr>");
                w.Write("</table>");
        
            }
            else
            {
                w.Write("<nobr><div class='v4DivInline' id=\"v3il_{0}\"></div>", HtmlID);
                if (URLShowEntity.Length > 0)
                {
                    w.Write("<a href=\"javascript:void(0);\" onclick=\"javascript:cmd('ctrl', '{1}', 'cmd', 'btn')\" "
                            +
                            (IsCaller && !IsItemCompany
                                ? string.Format("class='v4_callerControl' data-id='{0}' caller-type='{1}'",
                                    HttpUtility.HtmlEncode(Value), (int) CallerType)
                                : "") + " "
                            + (!IsCaller && IsItemCompany ? "class='v4_itemCompanyControl'" : "") + ">{0}</a>",
                        HttpUtility.HtmlEncode(ValueText), HtmlID);
                }
                else if (FuncShowEntity.Length > 0 && !string.IsNullOrEmpty(Value))
                {
                    w.Write(
                        "<a href=\"javascript:void(0);\" onclick=\"javascript:cmd('ctrl', '{1}', 'cmd', 'OpenDocument', 'id', {2});\">{0}</a>",
                        HttpUtility.HtmlEncode(ValueText), HtmlID, Value);
                }
                else
                {
                    w.Write(HttpUtility.HtmlEncode(ValueText));
                }
                w.Write("</nobr>");
            }
        }


        /// <summary>
        ///     Метод вывода описания фильтра
        /// </summary>
        /// <param name="htmlElementId">Идентификатор html-элемента куда надо вывести описание фильтр</param>
        public void RenderFilterText(string htmlElementId)
        {
            JS.Write("gi('{0}').innerHTML = '{1}';", HttpUtility.JavaScriptStringEncode(htmlElementId),
                V4Page.GetFilterText());
        }

        /// <summary>
        ///     Отрисовка множественного выбора
        /// </summary>
        /// <param name="w">Поток вывода</param>
        public void RenderSelectedItems(TextWriter w)
        {
            RenderSelectedItems(w, _selectedItems, URLShowEntity, FuncShowEntity, KeyField, ValueField, IsRemove, IsRow,
                IsCaller, CallerType, IsItemCompany);
        }

        /// <summary>
        ///     Отрисовка множественного выбора
        /// </summary>
        /// <param name="w">Response</param>
        /// <param name="list">Источник данных</param>
        /// <param name="openPath">Адрес формы сущности</param>
        /// <param name="key">Ключ в коллекции</param>
        /// <param name="field">Значение в коллекции</param>
        /// <param name="isRemove">Признак возможности удаления</param>
        /// <param name="isRow">Порядок вывода элементов. В строчку - true, в столбец - false</param>
        /// <param name="isCaller">Признак использования приложения Контакты</param>
        /// <param name="isItemCompany">Признак использования списка компаний для сущности</param>
        public void RenderSelectedItems(TextWriter w, IEnumerable list, string openPath, string openFunc, string key,
            string field, bool isRemove = true, bool isRow = false,
            bool isCaller = false, CallerTypeEnum callerType = CallerTypeEnum.Empty, bool isItemCompany = false)
        {
            if (V4Page.V4Controls.ContainsKey(HtmlID + "Data"))
            {
                Field = field;
                var item = (ListItems) V4Page.V4Controls[HtmlID + "Data"];
                item.RenderListItems(w, list, openPath, openFunc, key, field, TabIndex, isRemove, ConfirmRemove, isRow,
                    HtmlID, IndexField, isCaller, callerType, isItemCompany);
            }
        }

        /// <summary>
        ///     Обновление блока выбранных элементов
        /// </summary>
        public virtual void RefreshDataBlock()
        {
            if (IsMultiSelect)
            {
                V4Page.RefreshHtmlBlock(string.Concat(HtmlID, "Data"), RenderSelectedItems);
            }
        }

        #endregion

        #region Override

        /// <summary>
        ///     Текущее значение
        /// </summary>
        public override string Value
        {
            get { return _value; }
            set
            {
                if (!_value.Equals(value))
                {
                    SetPropertyChanged("Value");
                    SetControlValue((value ?? "").Trim());
                }
            }
        }

        /// <summary>
        ///     Инициализируем коллекцию условий и устанавливаем другие свойства составного контрола
        /// </summary>
        public override void V4OnInit()
        {
            if (!IsUseCondition && !IsMultiSelect) return;
            if (IsUseCondition)
            {
                _clauseItems.Add(new Item(((int) SelectEnum.Contain).ToString(CultureInfo.InvariantCulture),
                    IsMultiSelect ? Resx.GetString("Lin") : Resx.GetString("LinOnce")));
                _clauseItems.Add(new Item(((int) SelectEnum.NotContain).ToString(CultureInfo.InvariantCulture),
                    IsMultiSelect ? Resx.GetString("Lnotin") : Resx.GetString("LnotinOnce")));
            }
            if (!IsNotUseEmpty)
            {
                _clauseItems.Add(new Item(((int) SelectEnum.Any).ToString(CultureInfo.InvariantCulture),
                    Resx.GetString("lblAnyValue")));
                _clauseItems.Add(new Item(((int) SelectEnum.NoValue).ToString(CultureInfo.InvariantCulture),
                    Resx.GetString("lblNotValue")));
            }
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            if (null != _list)
            {
                if (PropertyChanged.Contains("IsReadOnly") || PropertyChanged.Contains("IsDisabled"))
                {
                    //Дрогой возможности изменить эти параметры нет;
                    _list.IsDisabled = IsDisabled || HasCheckbox && !Checked;
                    _list.IsReadOnly = IsReadOnly;
                }
            }
            
            base.Flush();
            SearchText = "";

            //Базовые реализации для IsReadOnly IsDisabled реализуют тоже самое
            if (PropertyChanged.Contains("Checked") || PropertyChanged.Contains("HasCheckbox"))
            {
                V4Page.RefreshHtmlBlock(HtmlID, RenderControl);
                return;
            }

            if (PropertyChanged.Contains("ValueText") || PropertyChanged.Contains("Value") || (RefreshRequired && !IsReadOnly))
            {
                V4Page.JS.Write("if(gi('{0}_0')) {{gi('{0}_0').value='{1}';", HtmlID, HttpUtility.JavaScriptStringEncode(ValueText));
                V4Page.JS.Write("gi('{0}_0').setAttribute('t','{1}');", HtmlID,
                    Value.Length == 0 ? "" : HttpUtility.JavaScriptStringEncode(ValueText));
                V4Page.JS.Write("gi('{0}_0').setAttribute('v','{1}');}}", HtmlID, Value);

                if (IsCaller || IsItemCompany)
                {
                    V4Page.JS.Write("$('#{0}_0').attr('data-id', '{1}');", HtmlID,
                        HttpUtility.JavaScriptStringEncode(Value));
                    V4Page.JS.Write("$('#{0}_0').attr('caller-type', '{1}');", HtmlID, (int) CallerType);
                    if (Value.Length == 0)
                        V4Page.JS.Write("if ($('#{0}_0').qtip('api')) $('#{0}_0').qtip('api').destroy();", HtmlID);
                }

                if (IsRequired)
                {
                    V4Page.JS.Write("v4_replaceStyleRequired(gi('{0}_0'));", HtmlID);
                }

                if ((URLShowEntity.Length > 0 || FuncShowEntity.Length > 0))
                {
                    V4Page.JS.Write("v4s_btnStyle('{0}');", HtmlID);
                }
            }

            if (PropertyChanged.Contains("IsRequired") || (RefreshRequired && !IsReadOnly))
            {
                JS.Write("gi('{0}_0').setAttribute('isRequired','{1}');", HtmlID, IsRequired ? 1 : 0);
                JS.Write("v4_replaceStyleRequired(gi('{0}_0'));", HtmlID);
            }

            if (PropertyChanged.Contains("ListChanged"))
            {
                Item item = _clauseItems.Find(x => x.Code == ValueSelectEnum);
                if (null != item)
                    JS.Write("gi('" + ID + "HeadControl').innerHTML = '{0}';",item.Name);

                if (HasCheckbox && !Checked
                    || ValueSelectEnum == ((int) SelectEnum.NoValue).ToString(CultureInfo.InvariantCulture)
                    || ValueSelectEnum == ((int) SelectEnum.Any).ToString(CultureInfo.InvariantCulture))
                {
                    JS.Write("v4_setDisableSelect('{0}', true);", HtmlID);
                }
                else
                {
                    JS.Write("v4_setDisableSelect('{0}', false);", HtmlID);
                }
            }
            
        }
        
        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="collection">Коллекция параметров</param>
        public override void ProcessCommand(NameValueCollection collection)
        {
            base.ProcessCommand(collection);

            if (collection["checked"] != null)
            {
                bool is_checked = collection["checked"] == "1";
                if (_checked != is_checked)
                {
                    Checked = is_checked;
                }

                return;
            }

            if (collection["vn"] != null)
            {
                var tn = "";
                if (collection["tn"] != null) tn = collection["tn"].Trim();
                SetControlValue(collection["vn"], tn);
                ChangeProperty("Value");
            }
            else if (collection["cmd"] != null)
            {
                switch (collection["cmd"])
                {
                    case "popup":
                        string st_param = collection["st"];
                        SearchText = st_param == null ? string.Empty : st_param.Trim();

                        string tn_param = collection["tn"];
                        string clientValueText = tn_param==null ? string.Empty : tn_param.Trim();

                        if (clientValueText != SearchText && SearchText.Length > 0)
                        {
                            Value = "";
                            ValueText = SearchText;
                        }
                        ShowPopupWindow();
                        break;

                    case "btn":
                        if (Value.Length == 0 || SearchText.Length > 0 || URLShowEntity.Length == 0)
                        {
                            SearchText = collection["tn"].Trim();
                            Value = "";
                            ValueText = SearchText;
                            ShowPopupWindow();
                        }
                        break;
                    case "Refresh":
                        SetControlValue(Value, ValueText);
                        break;
                    case "popupHead":
                        ShowPopupWindowClause();
                        break;
                    case "setHead":
                        SetControlClause(collection["val"]);
                        break;
                    case "RemoveSelectedItem":
                        if (ConfirmRemove && collection["ask"] == null || collection["ask"] == "1")
                            RemoveItemComfirm(collection["id"]);
                        else
                            RemoveSelectedItem(collection["id"]);
                        break;
                    case "clearSelectedItems":
                        ClearSelectedItems();
                        break;
                }
            }
            else if (collection["tn"] != null)
            {
                if (string.IsNullOrEmpty(collection["tn"]))
                {
                    var oldVal = Value;
                    Value = "";
                    ValueText = "";
                    ValueObject = null;
                    OnChanged(new ProperyChangedEventArgs(oldVal, collection["vn"]));
                }
                else
                {
                    SearchText = collection["tn"].Trim();
                    Value = "";
                    ValueText = SearchText;
                    ShowPopupWindow();
                    if (ValueText != collection["tn"])
                        SetPropertyChanged("ValueText");
                }
            }
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток вывода</param>
        protected override void RenderControlBody(TextWriter w)
        {
            if (!IsUseCondition && !IsMultiSelect && !HasCheckbox)
            {
                RenderSelectBody(w);
                return;
            }

            string disabled_attribute = IsDisabled || IsReadOnly ? "disabled='disabled'" : string.Empty;

            if (HasCheckbox)
            {
                w.Write("<fieldset style='{0}' {1}>", _fieldset_style, disabled_attribute);
                string checked_attribute = Checked ? "checked='checked'" : string.Empty;

                string div_style = _fieldset_div_style;
                if (!IsUseCondition) div_style += "margin-top:-5px;";

                w.Write("<div style='{3}' ><input id='{0}_2' type='checkbox' tabindex='0' {1} {2} onclick=\"{4}\"/></div>", HtmlID, checked_attribute, disabled_attribute, div_style, _fieldset_input_onclick(HtmlID));
            }
            else
            {
                if (IsUseCondition)
                    w.Write("<fieldset {0} >", disabled_attribute);
            }

            if (IsUseCondition)
            {
                bool fEnabled = (!HasCheckbox || Checked) && !IsDisabled && !IsReadOnly;
                string strClass = fEnabled ? "v4_selectClause" : "v4_selectClauseDisabled";

                Item modeItem = _clauseItems.Find(x => x.Code == ValueSelectEnum);
                if (null == modeItem || null == modeItem.Name)
                    modeItem = _clauseItems.Find(x => x.Code == ((int)SelectEnum.Contain).ToString());

                w.Write(@"<legend><div id=""{1}"" class=""{5}"" 
onclick=""if (!v4s_isPopupOpen) cmd('ctrl', '{2}', 'cmd', 'popupHead');"" 
onkeydown=""var key=v4_getKeyCode(event); if((key == 13 || key == 32) && !v4s_isPopupOpen) cmd('ctrl', '{2}', 'cmd', 'popupHead'); else v4s_keyDown(event);"" 
{3} help='{4}'>{0}</div>",
                    modeItem.Name,
                    ID + "HeadControl",
                    HtmlID,
                    fEnabled ? (TabIndex.HasValue ? " tabindex='" + TabIndex.Value + "'" : "tabindex='0'") : string.Empty,
                    HttpUtility.HtmlEncode(Help),
                    strClass);
                w.Write("</legend>");
            }
            else
            {
                w.Write(@"<div style='display: block; height: 5px;'></div>");
            }

            if (IsMultiSelect)
            {
                if (null == _list)
                {
                    _list = new ListItems
                    {
                        HtmlID = HtmlID + "Data",
                        ID = HtmlID + "Data",
                        ParentHtmlID = HtmlID,
                        V4Page = V4Page,
                        DisplayStyle = "block",
                        List = _selectedItems,
                        OpenPath = URLShowEntity,
                        OpenFunc = FuncShowEntity,
                        Key = KeyField,
                        Field = ValueField,
                        IsRemove = IsRemove,
                        ConfirmRemove = ConfirmRemove,
                        IsReadOnly = IsReadOnly,
                        IsDisabled = IsDisabled || HasCheckbox && !Checked,
                        IsRow = IsRow,
                        IsCaller = IsCaller,
                        CallerType = CallerType,
                        IsItemCompany = IsItemCompany,
                        MethodsGetEntityValue = MethodsGetEntityValue
                    };

                    V4Page.V4Controls.Add(_list);
                }

                //При полной перерисовке элемента управления RenderControl не генерирует основной тэг <div>
                //из-за того, что проверяемое в RenderControl свойство V4Page.V4IsPostBack установлено как True
                w.Write("<div id='{0}'>", _list.HtmlID);
                RenderSelectedItems(w);
                w.Write("</div>", _list.HtmlID);
                //_list.RenderControl(w);
            }

            RenderSelectBody(w);
            if (HasCheckbox || IsUseCondition)
                w.Write("</fieldset>");
            IsCanUseFilter = CanUseFilter();
        }

        #endregion

        #region События

        /// <summary>
        ///     Событие изменения ValueText свойства контрола
        /// </summary>
        public event ValueChangedEventHandler TextChanged;

        /// <summary>
        ///     Событие изменения ValueText свойства контрола
        /// </summary>
        public void OnTextChanged(ValueChangedEventArgs e)
        {
            if (TextChanged != null)
            {
                TextChanged(this, e);
            }
        }

        /// <summary>
        ///     Метод, вызываемый перед поиском
        /// </summary>
        public virtual void OnBeforeSearch()
        {
            if (BeforeSearch != null)
                BeforeSearch(this);
        }

        /// <summary>
        ///     Метод, вызываемый при изменении состояния контрола
        /// </summary>
        public override void OnChanged(ProperyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.NewValue) && IsMultiSelect)
            {
                if (!e.IsChange) return;

                Value = "";
                if (IsCustomRecordInPopup && e.NewValue == CustomRecordId)
                {
                    if (!SelectedItems.Exists(x => x.Id == CustomRecordId))
                    {
                        if (IsSelectOnlyCustomRecord) SelectedItems.Clear();
                        SelectedItems.Add(new Entities.Item
                        {
                            Id = CustomRecordId,
                            Value = new Lib.Entities.Item {Id = CustomRecordId, Value = CustomRecordText}
                        });

                        V4Page.RefreshHtmlBlock(string.Concat(HtmlID, "Data"), RenderSelectedItems);
                        base.OnChanged(e);
                    }
                }
                else
                {
                    var item = ValueObject;
                    var itemType = item.GetType();
                    var id = itemType.GetProperty(KeyField).GetValue(item, null).ToString();

                    if (IsCustomRecordInPopup && IsSelectOnlyCustomRecord)
                    {
                        var p = SelectedItems.FirstOrDefault(x => x.Id == CustomRecordId);
                        if (!object.Equals(p, default(Entities.Item)))
                            SelectedItems.Remove(p);
                    }

                    if (!SelectedItems.Exists(x => x.Id == id))
                    {
                        SelectedItems.Add(new Entities.Item {Id = id, Value = item});
                        V4Page.RefreshHtmlBlock(string.Concat(HtmlID, "Data"), RenderSelectedItems);
                        base.OnChanged(e);
                    }
                }
                Focus();
            }
            else
                base.OnChanged(e);
        }

        /// <summary>
        ///     Принудительная инициализация события OnChange
        /// </summary>
        /// <param name="prop">Навзание изменяемого свойства</param>
        private void ChangeProperty(string prop)
        {
            SetPropertyChanged(prop);
            if (prop == "Value") OnValueChanged(new ValueChangedEventArgs(_value, _oldValue));
            OnChanged(new ProperyChangedEventArgs(_oldValue, _value));
            if (!V4Page.JS.ToString().Contains("isChanged=true;"))
                JS.Write("isChanged=true;");
        }

        #endregion

        #region Расширенный поиск и открытие формы сущности

        /// <summary>
        ///     Список urls для создания новых сущностей
        /// </summary>
        protected List<URICreateEntity> URIsCreateEntity;

        /// <summary>
        ///     Путь к сущности
        /// </summary>
        public string URLShowEntity { get; set; }

        /// <summary>
        ///     Функция открытия сущности
        /// </summary>
        public string FuncShowEntity { get; set; }


        /// <summary>
        ///     Путь к расширенному поиску сущности
        /// </summary>
        public string URLAdvancedSearch
        {
            get { return _urlAdvancedSearch; }
            set { _urlAdvancedSearch = value; }
        }

        #endregion

        #region Множественный выбор

        /// <summary>
        ///     Признак множественного выбора
        /// </summary>
        public bool IsMultiSelect { get; set; }

        /// <summary>
        ///     Признак множественного выбора
        /// </summary>
        public bool IsMultiReturn { get; set; }

        /// <summary>
        ///     Признак, что при указанной ссылке на расширенный поиск, данная ссылка будет рисоваться всегда, независимо от
        ///     количества найденных элементов
        /// </summary>
        public bool IsAlwaysAdvancedSearch { get; set; }

        /// <summary>
        ///     Признак, что при указанной ссылке на создание сущности
        ///     ссылки будут рисоваться не всегда
        /// </summary>
        public bool IsNoAlwaysCreateEntity { get; set; }

        /// <summary>
        ///     Строка с перечислением через "," ID выбранных элементов
        /// </summary>
        public virtual string SelectedItemsString
        {
            get
            {
                var temp = _selectedItems.Aggregate("", (current, item) => current + (item.Id + ","));
                if (!String.IsNullOrEmpty(temp)) temp = temp.Remove(temp.Length - 1, 1);
                return temp;
            }
        }

        /// <summary>
        ///     Аксессор к словарю выбранных элементов
        /// </summary>
        public List<Entities.Item> SelectedItems
        {
            get { return _selectedItems; }
            set { _selectedItems = value; }
        }

        /// <summary>
        ///     Очистить список выбранных элементов
        /// </summary>
        public void ClearSelectedItems(bool saveCopy = false)
        {
            _selectedItemsCopy.Clear();
            if (saveCopy)
            {
                foreach (var si in _selectedItems)
                {
                    _selectedItemsCopy.Add(si);
                }
            }
            _selectedItems.Clear();
            V4Page.RefreshHtmlBlock(string.Concat(HtmlID, "Data"), RenderSelectedItems);
            ChangeProperty("Value");
        }

        private void RestoreSelectedItems()
        {
            _selectedItems.Clear();
            foreach (var si in _selectedItemsCopy)
            {
                _selectedItems.Add(si);
            }
            _selectedItemsCopy.Clear();
            V4Page.RefreshHtmlBlock(string.Concat(HtmlID, "Data"), RenderSelectedItems);
        }

        /// <summary>
        ///     Вывести подтверждение удаления элемента из списка
        /// </summary>
        public void RemoveItemComfirm(string item)
        {
            if (string.IsNullOrWhiteSpace(ConfirmRemoveMsg))
                ConfirmRemoveMsg = Resx.GetString("msgDeleteFromList");

            JS.Write("CustomConfirmChangedTwoButtons.save = function() {SetItemParam('ConfirmChangedTwoButtons');" +
                     "gi('v4_divDialogBox').style.display = \"none\";" +
                     "gi('v4_divDialogOverlay').style.display = \"none\";" +
                     string.Format("cmd('ctrl', '{0}','cmd','RemoveSelectedItem','id','{1}','ask', '0');", HtmlID,
                         HttpUtility.JavaScriptStringEncode(item.Replace("\"", " ").Replace("'", " ")))
                     + "};");

            JS.Write("CustomConfirmChangedTwoButtons.render('Удаление основания', '{0}', '{1}', '{2}', '');",
                ConfirmRemoveMsg, Resx.GetString("btnDelete"), Resx.GetString("ppBtnCancel"));
        }

        /// <summary>
        ///     Удалить элемент из списка
        /// </summary>
        /// <param name="item">Идентификатор элемента, который необходимо удалить из списка</param>
        public void RemoveSelectedItem(string item)
        {
            _selectedItems.Remove(_selectedItems.Find(x => x.Id.Replace("\"", " ").Replace("'", " ") == item));
            OnDeleted(new ProperyDeletedEventArgs(item));
            V4Page.RefreshHtmlBlock(string.Concat(HtmlID, "Data"), RenderSelectedItems);
            ChangeProperty("Value");
            Focus();
        }

        #endregion

        #region Значения контрола

        /// <summary>
        ///     Название ключевого поля
        /// </summary>
        public string KeyField { get; set; }

        /// <summary>
        ///     Значение полей, которые отображаются в выпадающем окне
        /// </summary>
        public string ValueField { get; set; }
        
        /// <summary>
        /// Список, указывающий на то, что чтобы получить значение поля у объекта используется функция
        /// </summary>
        public List<SelectMethodGetEntityValue> MethodsGetEntityValue;

        /// <summary>
        ///     Всплявающая подстказка полей, которые отображаются в выпадающем окне
        /// </summary>
        public string TitleField { get; set; }

        /// <summary>
        ///     Значение (тип object)
        /// </summary>
        public object ValueObject { get; private set; }

        /// <summary>
        ///     Значение (тип int?)
        /// </summary>
        public int? ValueInt
        {
            get
            {
                if (string.IsNullOrEmpty(Value))
                {
                    return null;
                }
                return int.Parse(Value);
            }
            set { Value = value == null ? "" : value.Value.ToString(CultureInfo.InvariantCulture); }
        }

        /// <summary>
        ///     Значение (тип Guid)
        /// </summary>
        public Guid ValueGuid
        {
            get { return Value.Length > 0 ? Guid.Parse(Value) : Guid.Empty; }
            set { Value = value.ToString(); }
        }

        /// <summary>
        ///     Значение, отображаемое в контроле
        /// </summary>
        public string ValueText
        {
            get { return _valueText; }
            set
            {
                value = value.TrimNoNullError();
                var changed = _valueText != value;
                var oldValue = _valueText;
                _valueText = value;

                //если поменялся текст то при формировании xml клиенту нужно обновить поле.
                if (changed)
                {
                    SetPropertyChanged("ValueText");
                    OnTextChanged(new ValueChangedEventArgs(value, oldValue));
                }
            }
        }

        /// <summary>
        ///     Значение выбранного условия (из перечисления)
        /// </summary>
        public string ValueSelectEnum
        {
            get { return _valueSelectEnum; }
            set { _valueSelectEnum = value; }
        }

        /// <summary>
        ///     Строка поиска
        /// </summary>
        public string SearchText { get; set; }

        #endregion

        #region Установка значения контрола

        /// <summary>
        ///     Установка выбранного условия фильтрации
        /// </summary>
        /// <param name="val">Выбранное условие</param>
        private void SetControlClause(object val)
        {
            if (val != null)
            {
                ValueSelectEnum = val.ToString();
                if (ValueSelectEnum == ((int) SelectEnum.NoValue).ToString(CultureInfo.InvariantCulture) ||
                    ValueSelectEnum == ((int) SelectEnum.Any).ToString(CultureInfo.InvariantCulture))
                {
                    Value = ValueText = "";
                    if (IsMultiSelect)
                    {
                        SetFocusToNextCtrl();
                        ClearSelectedItems(true);
                    }
                    else
                    {
                        FocusToNextCtrl();
                    }
                }
                else
                {
                    if (SelectedItems.Count == 0)
                        RestoreSelectedItems();
                    OnChanged(new ProperyChangedEventArgs(ValueSelectEnum, _value));
                    Focus();
                }
                IsCanUseFilter = CanUseFilter();
                SetPropertyChanged("ListChanged");
            }
            else
            {
                Focus();
            }
        }

        /// <summary>
        ///     Подстановка выбранного элемента из выпадающего списка в контрол
        /// </summary>
        /// <param name="val">Выбранный элемент</param>
        private void SetControlValue(string val, string valueText = "")
        {
            SetPropertyChanged("ValueText");
            if (val.Length > 0 && val.Equals(Guid.Empty.ToString()))
            {
                val = "";
            }
            _oldValue = _value;
            _value = val.Trim();
            ValueText = "";

            if (string.IsNullOrEmpty(Value))
            {
                if (DependingControlsOfCurrentElement.Length > 0)
                {
                    foreach (var x in DependingControlsOfCurrentElement.Split(','))
                    {
                        var ctrl = V4Page.V4Controls.Values.FirstOrDefault(z => z.ID == x);
                        if (ctrl != null)
                        {
                            ctrl.Value = "";
                        }
                    }
                }
            }
            else
            {
                if (IsCustomRecordInPopup && Value.Equals(CustomRecordId))
                    ValueText = CustomRecordText;
                else
                {
                    ValueObject = SelectedItemById(Value, valueText);
                    if (ValueObject != null)
                    {
                        SetDependingControlsValue(ValueObject);
                    }
                    else
                    {
                        ValueText = "#" + val;
                    }
                }
            }
        }

        /// <summary>
        ///     Установка и обновление значений в контролах, от которых зависит текущий контрол, и в контролах, значения которых
        ///     зависят от текущего контрола
        /// </summary>
        /// <param name="o">Выбранный ряд в текущем контроле</param>
        private void SetDependingControlsValue(object o)
        {
            ValueObject = o;
            string key = "", val = "";
            if (o != null)
            {
                var isDataRow = (o is DataRow);
                string par;
                if (isDataRow)
                {
                    key = (o as DataRow)[KeyField].ToString();
                    val = (o as DataRow)[ValueField].ToString();

                    foreach (var x in GetHeadControlsOfCurrentElement())
                    {
                        par = (o as DataRow)[x.Value].ToString();
                        var parent = V4Page.V4Controls.Values.FirstOrDefault(z => z.ID == x.Key);
                        if (parent != null && key.Length > 0 && (parent.Value.Length == 0 || !parent.Value.Equals(par)))
                            //можно обновить родительский элемент, а можно просто не устанавливать значение в текущий если родитель другой.
                        {
                            parent.Value = par;
                        }
                    }
                }
                else
                {
                    var t = o.GetType();
                    key = t.GetProperty(KeyField).GetValue(o, null).ToString();
                    if (AnvancedHeaderPopupResult.Length > 0)
                    {
                        var arr = ValueField.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);
                        if (arr.Any())
                        {
                            if (t.GetProperty(arr[IndexField]).GetValue(o, null) != null)
                            {
                                val = t.GetProperty(arr[IndexField]).GetValue(o, null).ToString();
                            }
                        }
                        else
                        {
                            if (t.GetProperty(ValueField).GetValue(o, null) != null)
                            {
                                val = t.GetProperty(ValueField).GetValue(o, null).ToString();
                            }
                        }
                    }
                    else
                    {
                        val = GetFieldValue(o, ValueField);
                    }

                    foreach (var x in GetHeadControlsOfCurrentElement())
                    {
                        par = t.GetProperty(x.Value).GetValue(o, null).ToString();
                        var parent = V4Page.V4Controls.Values.FirstOrDefault(z => z.ID == x.Key);
                        if (parent != null && key.Length > 0 && (parent.Value.Length == 0 || !parent.Value.Equals(par)))
                            //можно обновить родительский элемент, а можно просто не устанавливать значение в текущий если родитель другой.
                        {
                            parent.Value = par;
                        }
                    }
                }

                if (IsCustomRecordInPopup && key.Equals(CustomRecordId))
                    val = CustomRecordText;
                else
                {
                    if (String.IsNullOrEmpty(val))
                        val = "#" + key;
                }
                ValueText = val;
            }

            _value = key;

            if (!string.IsNullOrEmpty(DependingControlsOfCurrentElement))
            {
                foreach (var ch in DependingControlsOfCurrentElement.Split(','))
                {
                    var chCtrl = V4Page.V4Controls.Values.FirstOrDefault(x => x.ID == ch) as Select;
                    if (chCtrl != null)
                    {
                        chCtrl.TryFindSingleValue();
                    }
                }
            }
        }

        private string GetFieldValue(object obj, string currentField)
        {
            var val = "";
            var t = obj.GetType();
            if (MethodsGetEntityValue == null || MethodsGetEntityValue.Count == 0 ||
                                       MethodsGetEntityValue.FirstOrDefault(
                                           x =>
                                               String.Equals(x.ValueField, currentField,
                                                   StringComparison.InvariantCultureIgnoreCase)) == null)
            {
                val = t.GetProperty(currentField).GetValue(obj, null).ToString();
            }
            else
            {
                var mSettings =
                    MethodsGetEntityValue.FirstOrDefault(
                        x =>
                            String.Equals(x.ValueField, currentField,
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
                        x => x.GetType() == typeof(Employee) && ((Employee)x).IsLazyLoadingByCurrentUser);
                if (urs != null)
                {
                    var inx = Array.IndexOf(paramObjects, urs);
                    paramObjects[inx] = V4Page.CurrentUser;
                }
                val = mInfo.Invoke(obj, paramObjects).ToString();
            }
            return val;
        }

        /// <summary>
        ///     Установка единственного элемента из списка
        /// </summary>
        public bool TryFindSingleValue()
        {
            Value = "";
            return TryFindSingleValue(FillPopupWindow(""));
        }

        /// <summary>
        ///     Установка единственного элемента из списка
        /// </summary>
        /// <param name="data">Отфильтрванные значения</param>
        /// <returns>признак успешности выполнения</returns>
        private bool TryFindSingleValue(IEnumerable data)
        {
            var en = data.GetEnumerator();
            var count = 0;
            object o = null;
            while (en.MoveNext())
            {
                o = en.Current;
                count++;
                if (count == 2) break;
            }

            if (count == 1)
            {
                if (o == null) return false;
                var t = o.GetType();
                if (t.GetProperty(KeyField).GetValue(o, null) != null)
                {
                    var val = t.GetProperty(KeyField).GetValue(o, null).ToString();
                    var name = "";
                    if (AnvancedHeaderPopupResult.Length > 0)
                    {
                        var arr = ValueField.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        if (arr.Any())
                        {
                            if (t.GetProperty(arr[IndexField]).GetValue(o, null) != null)
                            {
                                name = t.GetProperty(arr[IndexField]).GetValue(o, null).ToString();
                            }
                        }
                        else
                        {
                            if (t.GetProperty(ValueField).GetValue(o, null) != null)
                            {
                                name = t.GetProperty(ValueField).GetValue(o, null).ToString();
                            }
                        }
                    }
                    else
                    {
                        name = GetFieldValue(o, ValueField);
                    }

                    SetSingleValue(val, name);
                    
                    return true;
                }
            }
            return false;
        }
        
        private bool TrySetCustomSingleValue(IEnumerable data)
        {
            if (!IsCustomRecordInPopup) return false;
            if (CustomRecordText == "") return false;
            var en = data.GetEnumerator();
            while (en.MoveNext()) return false;

            if (SearchText == "") return false;
            if (!CustomRecordText.Contains(SearchText)) return false;

            SetSingleValue(CustomRecordId, CustomRecordText);

            return true;
        }

        private void SetSingleValue(string val, string name)
        {
            var res = SelectedItemById(val, name);
            SetDependingControlsValue(res);
           
            V4Page.JS.Write("v4_setFocus2NextCtrl('{0}');", GetFocusControl());
        }


        #endregion

        #region Связи между контролами

        /// <summary>
        ///     Список идентификаторов элементов управления, от значений которых зависит значение текущего контрола
        /// </summary>
        public string HeadControlsOfCurrentElement { get; set; }

        /// <summary>
        ///     Список идентификаторов элементов управления, значения которых зависят от значения текущего контрола
        /// </summary>
        public string DependingControlsOfCurrentElement { get; set; }

        /// <summary>
        ///     Получение списка родителей
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetHeadControlsOfCurrentElement()
        {
            if (string.IsNullOrEmpty(HeadControlsOfCurrentElement))
            {
                return new Dictionary<string, string>();
            }
            return HeadControlsOfCurrentElement.Split(',').Select(x => x.Split(':')).ToDictionary(x => x[0], x => x[1]);
        }

        /// <summary>
        ///     Установка фокуса на следующий контрол
        /// </summary>
        private void SetFocusToNextCtrl()
        {
            var thisCtrl = false;
            foreach (var key in V4Page.V4Controls.Keys)
            {
                if (thisCtrl && !V4Page.V4Controls[key].IsReadOnly && !V4Page.V4Controls[key].IsDisabled && V4Page.V4Controls[key].Visible)
                {
                    V4Page.V4Controls[key].Focus();
                    break;
                }
                if (key.Equals(HtmlID))
                {
                    thisCtrl = true;
                }
            }
        }

        #endregion

        #region Настройка и отображение выпадающего списка

        /// <summary>
        ///     Признак отображения всех элементов независимо от MaxItemsInPopup
        /// </summary>
        public bool IsNotUseSelectTop { get; set; }

        /// <summary>
        ///     Отображаемое поле или список полей в выпадающем списке
        /// </summary>
        public string DisplayFields
        {
            get { return _displayFields.Length == 0 ? ValueField : _displayFields; }
            set { _displayFields = value; }
        }

        /// <summary>
        ///     Максимальное количество элементов, отображаемых в попапе
        /// </summary>
        public int MaxItemsInPopup
        {
            get { return _maxItemsInPopup; }
            set { _maxItemsInPopup = value > 0 ? value : 8; }
        }

        /// <summary>
        ///     Максимальное количество элементов в запросе
        /// </summary>
        public int MaxItemsInQuery
        {
            get { return _maxItemsInQuery == 0 ? _maxItemsInPopup + 1 : _maxItemsInQuery; }
            set { _maxItemsInQuery = value > 0 ? value : 0; }
        }

        /// <summary>
        ///     Создание собственного заголовка в таблице в выпадающем списке. Использовать, если необходимо отобразить более одной
        ///     колонки. Колонки перечислять через ","
        /// </summary>
        public string AnvancedHeaderPopupResult
        {
            get { return _anvancedHeaderPopupResult; }
            set { _anvancedHeaderPopupResult = value; }
        }

        /// <summary>
        ///     Отображаем попап с условиями фильтра
        /// </summary>
        private void ShowPopupWindowClause()
        {
            if (!IsUseCondition || HasCheckbox && !Checked || IsDisabled || IsReadOnly) return;

            using (TextWriter w = new StringWriter())
            {
                RenderPopupWindowClause(w, _clauseItems);
                V4Page.HTMLBlock.Add("v4s_popup", w.ToString());
                JS.Write("v4f_showPopup('{0}_0', '{0}HeadControl');", HtmlID);
            }
        }

        /// <summary>
        ///     Если в выподающем списке одно значение то выставлять его автоматически
        /// </summary>
        public bool AutoSetSingleValue { get; set; }
       
        /// <summary>
        ///     Отображение попап c отфильтрованными значениями
        /// </summary>
        private void ShowPopupWindow()
        {
            var oldVal = _value;
            var dt = FillPopupWindow(SearchText);

            if (!TrySetCustomSingleValue(dt))
            {

                if ((!IsCustomRecordInPopup || IsCustomRecordInPopup && SearchText != "") && AutoSetSingleValue &&
                    TryFindSingleValue(dt))
                {
                    V4Page.JS.Write("v4s_hidePopup();");
                    SetPropertyChanged("ValueText");
                    FocusToNextCtrl();
                }
                else
                {
                    Focus();

                    using (TextWriter w = new StringWriter())
                    {
                        RenderPopupWindow(w, dt);
                        V4Page.HTMLBlock.Add("v4s_popup", w.ToString());
                    }

                    JS.Write("v4s_showPopup('{0}_0'{1});", HtmlID, IsNotUseSelectTop ? ", 1" : "");
                }
            }
            else
            {
                V4Page.JS.Write("v4s_hidePopup();");
                SetPropertyChanged("ValueText");
                FocusToNextCtrl();
            }

            if (!oldVal.Equals(_value))
            {
                OnChanged(new ProperyChangedEventArgs(oldVal, _value));
            }
        }

        #endregion

        #region Использование при фильтрации

        /// <summary>
        ///     Проверка на возможность использования значения контрола в запросе
        /// </summary>
        /// <returns>возможность использования значения контрола в запросе</returns>
        private bool CanUseFilter()
        {
            switch ((SelectEnum) Convert.ToInt32(ValueSelectEnum))
            {
                case SelectEnum.Any:
                case SelectEnum.NoValue:
                    return true;
                case SelectEnum.Contain:
                case SelectEnum.NotContain:
                    if (IsMultiSelect)
                    {
                        var item = (ListItems) V4Page.V4Controls[HtmlID + "Data"];
                        if (item.IsFill) return true;
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(Value)) return true;
                    }
                    break;
            }
            return false;
        }


        /// <summary>
        ///     Получение выбранного условия поиска в текстовом виде
        /// </summary>
        /// <returns>Возвращает описательную строку условия фильтрации и значение конрола</returns>
        public string GetFilterClauseText()
        {
            if (String.IsNullOrEmpty(Description)) return "";
            var item = "";
            if (IsMultiSelect)
            {
                //это работает только при добавлении новых элементов через выпадающий список,
                //поэтому невозможно получать описание фильтра для значений добавленных
                //через свойство SelectedItems при инициализации элементов
                /*
                if (V4Page.V4Controls.ContainsKey(HtmlID + "Data"))
                {
                    var c = (ListItems) V4Page.V4Controls[HtmlID + "Data"];
                    if (c.List != null)
                    {
                        foreach (var i in c.List)
                        {
                            var t = i.GetType();
                            var o = t.GetProperty("Value").GetValue(i, null) ?? "";
                            var to = o.GetType();
                            var oText = to.GetProperty(Field).GetValue(o, null) ?? "";
                            var text = oText.ToString();
                            item += text + ", ";
                        }
                        if (item.Length > 1)
                        {
                            item = item.Remove(item.Length - 2, 2);
                        }
                    }
                }
                */

                foreach (Entities.Item i in SelectedItems)
                {
                    object o = i.Value;
                    if (null == o) continue;

                    PropertyInfo pi = o.GetType().GetProperty(ValueField);
                    if (null == pi) continue;
                    object text = pi.GetValue(o, null);

                    if (null != text)
                    {
                        if (!string.IsNullOrEmpty(text.ToString()))
                        {
                            if (item.Length > 0) item += ", ";
                            item += text;
                        }
                    }
                }
            }
            else
            {
                item = ValueText;
            }
            var condition = "";
            if (IsUseCondition)
            {
                switch ((SelectEnum) Convert.ToInt32(ValueSelectEnum))
                {
                    case SelectEnum.Contain:
                        condition = (IsMultiSelect ? Resx.GetString("Lin") : Resx.GetString("LinOnce"));
                        break;
                    case SelectEnum.NotContain:
                        condition = (IsMultiSelect ? Resx.GetString("Lnotin") : Resx.GetString("LnotinOnce"));
                        break;
                }
            }
            switch ((SelectEnum) Convert.ToInt32(ValueSelectEnum))
            {
                case SelectEnum.Any:
                    return Description + ": " + Resx.GetString("lblAnyValue").ToLower();
                case SelectEnum.NoValue:
                    return Description + ": " + Resx.GetString("lblNotValue").ToLower();
                case SelectEnum.Contain:
                case SelectEnum.NotContain:
                    return String.IsNullOrEmpty(item) ? "" : Description + ": " + condition.ToLower() + " " + item;
            }
            return "";
        }

        #endregion
    }
}