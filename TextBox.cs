using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using Kesco.Lib.BaseExtention.Enums.Controls;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол текстовое поле
    /// </summary>
    [DefaultProperty("Value")]
    [ToolboxData("<{0}:TextBox runat=server />")]
    public class TextBox : V4Control
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
        ///     Контейнер доступных значений фильтра
        /// </summary>
        private List<Item> _list = new List<Item>();

        /// <summary>
        ///     Значение выбранного условия (из перечисления)
        /// </summary>
        public string ValueTextBoxEnum = "0";

        /// <summary>
        /// Функция обратного вызова для события выбора элемента
        /// </summary>
        public event EventHandler<CheckEventArgs> CheckChanged;

        private const string _fieldset_style = "position: relative; padding-top: 5px;";
        private const string _fieldset_div_style = "width: 20px; height:20px; position: absolute; top: 0px; right: 0px;";

        private string _fieldset_input_onclick(string id)
        {
            return string.Format("var input_text=gi('{0}_0'); if(input_text) {{ input_text.disabled=input_text.readonly=!this.checked; if(this.checked) input_text.focus(); }} if(this.checked) gi('{0}HeadControl').className='v4_selectClause'; else {{ gi('{0}HeadControl').className='v4_selectClauseDisabled'; }} cmdasync('ctrl', '{0}', 'checked', this.checked ? '1' : '0');", id);
            //return string.Format("var input_text=gi('{0}_0'); if(input_text) input_text.disabled=input_text.readonly=!this.checked; function f(el, disabled){{var next=el.nextSibling; if(null==next) return; next.disabled=disabled; return f(next, disabled);}} f(this.parentNode, !this.checked); cmdasync('ctrl', '{0}', 'checked', this.checked ? '1' : '0');", id);
            //return string.Format("var input_text=gi('{0}_0'); if(input_text) input_text.readonly=input_text.disabled=!this.checked; cmdasync('ctrl', '{0}', 'checked', this.checked ? '1' : '0');", id);
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
                    SetPropertyChanged("Checked");
                    if (null != CheckChanged)
                        CheckChanged(this, new CheckEventArgs(value));
                }
            }
        }

        //Значение свойства Checked
        private bool _checked;

        /// <summary>
        ///     Конструктор
        /// </summary>
        public TextBox()
        {
            MaxLength = 0;
            Type = TextBoxType.Text;
            CSSClass = "";
            IsDisabled = false;
        }

        /// <summary>
        ///     Тип текстового поля
        /// </summary>
        public TextBoxType Type { get; set; }

        /// <summary>
        ///     Максимальная длина
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        ///     Регулярное выражение
        /// </summary>
        public string RegEx { get; set; }

        /// <summary>
        ///     Сообщение валидации
        /// </summary>
        public string ValidationMessage { get; set; }

        /// <summary>
        ///     Аксессор к списку с доступными Popup-фильтрами
        /// </summary>
        protected List<Item> TextBoxFiltersList
        {
            get { return _list; }
            set { _list = value; }
        }

        /// <summary>
        ///     Инициализируем коллекцию условий и устанавливаем другие свойства составного контрола
        /// </summary>
        public override void V4OnInit()
        {
            if (!IsUseCondition) return;
            _list.Add(new Item(((int) TextBoxEnum.ContainsAll).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cContainsAll")));
            _list.Add(new Item(((int)TextBoxEnum.NotContainsAny).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cNotContainAny")));

            _list.Add(new Item(((int)TextBoxEnum.ContainsAllOrdered).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cContainsAllOrdered")));
            _list.Add(new Item(((int)TextBoxEnum.NotContainsAllOrdered).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cNotContainsAllOrdered")));

            _list.Add(new Item(((int) TextBoxEnum.ContainsAny).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cContainsAny")));
            _list.Add(new Item(((int)TextBoxEnum.NotContainsAll).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cNotContainAll")));

            _list.Add(new Item(((int) TextBoxEnum.Starts).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cStarts")));
            _list.Add(new Item(((int) TextBoxEnum.NotStart).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cNotStart")));

            _list.Add(new Item(((int)TextBoxEnum.Ends).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cEnds")));
            _list.Add(new Item(((int)TextBoxEnum.NotEnds).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cNotEnds")));

            _list.Add(new Item(((int) TextBoxEnum.Matches).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cMatches")));
            _list.Add(new Item(((int) TextBoxEnum.NotMatches).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("cNotMatches")));

            if (!IsNotUseEmpty)
            {
                _list.Add(new Item(((int) TextBoxEnum.Empty).ToString(CultureInfo.InvariantCulture),
                    Resx.GetString("cEmpty")));
                _list.Add(new Item(((int) TextBoxEnum.NotEmpty).ToString(CultureInfo.InvariantCulture),
                    Resx.GetString("cNotEmpty")));
            }
        }

        /// <summary>
        /// Определят возможно ли редактирование текста
        /// </summary>
        /// <returns>true если  возможно редактирование текста</returns>
        protected virtual bool IsEditable()
        {
            return !(IsDisabled || IsUseCondition && HasCheckbox && !Checked
                              || ValueTextBoxEnum == ((int)TextBoxEnum.Empty).ToString()
                              || ValueTextBoxEnum == ((int)TextBoxEnum.NotEmpty).ToString());
        }

        /// <summary>
        ///     Отрисовка тела элемента управления
        /// </summary>
        /// <param name="w">Объект для записи HTML-разметки</param>
        protected override void RenderControlBody(TextWriter w)
        {
            if (IsUseCondition)
            {
                string disabled_attribute = IsDisabled || IsReadOnly ? "disabled='disabled'" : string.Empty;

                if (HasCheckbox)
                {
                    w.Write("<fieldset style='{0}' {1}>", _fieldset_style, disabled_attribute);
                    string checked_attribute = Checked ? "checked='checked'" : string.Empty;
                    w.Write("<div style='{3}' ><input id='{0}_2' type='checkbox' tabindex='0' {1} {2} onclick=\"{4}\"/></div>", HtmlID, checked_attribute, disabled_attribute, _fieldset_div_style, _fieldset_input_onclick(HtmlID));
                }
                else
                {
                    w.Write("<fieldset {0} >", disabled_attribute);
                }

                bool fEnabled = (!HasCheckbox || Checked) && !IsDisabled && !IsReadOnly;
                string strClass = fEnabled ? "v4_selectClause" : "v4_selectClauseDisabled";

                w.Write(@"<legend><div id=""{1}"" class=""{4}"" 
onclick=""cmd('ctrl', '{2}', 'cmd', 'popupHead');"" 
onkeydown=""var key=v4_getKeyCode(event); if((key == 13 || key == 32) && !v4s_isPopupOpen) cmd('ctrl', '{2}', 'cmd', 'popupHead'); else v4s_keyDown(event);"" 
{3}>{0}</div></legend><div>",
                    _list.Find(x => x.Code == ValueTextBoxEnum).Name,
                    ID + "HeadControl",
                    HtmlID,
                    fEnabled ? (TabIndex.HasValue ? " tabindex='" + TabIndex.Value + "'" : " tabindex='0'") : string.Empty,
                    strClass);
            }

            if (IsReadOnly)
            {
                //Недопустимо устанавливать текст всего элемента, т.к. он может содержать вложенный элемент div _ntf
                w.Write("<span>" + HttpUtility.HtmlEncode(Value) + "</span>");
            }
            else
            {
                string strDisabledAttrChecked = IsEditable() ? string.Empty : "disabled='disabled'";

                w.Write("<input  id='{0}_0' style='{1}{2}' value='{3}' type='{4}' {5}",
                    HtmlID,
                    string.Concat("width:", Width.IsEmpty ? "100%;" : string.Concat(Width.ToString(), ";")),
                    Height.IsEmpty ? "" : string.Concat("height:", Height.ToString(), ";"),
                    HttpUtility.HtmlEncode(Value),
                    Type, strDisabledAttrChecked);

                if (MaxLength > 0)
                    w.Write(" maxlength='{0}'", MaxLength);

                if (!string.IsNullOrEmpty(NextControl))
                    w.Write(" nc='{0}'", GetHtmlIdNextControl());

                w.Write(" isRequired={0}", IsRequired ? 1 : 0);
                w.Write(" onkeydown=\"return v4t_keyDown(event);");
                if (IsRequired)
                    w.Write("v4_replaceStyleRequired(this);");
                w.Write("\"");
                w.Write(" onchange=\"v4_ctrlChanged('{0}', false);\"", HtmlID);

                w.Write(" t='{0}' help='{1}' ov='{2}'", HttpUtility.HtmlEncode(Value), HttpUtility.HtmlEncode(Help), HttpUtility.HtmlEncode(OriginalValue));

                var className = "";
                if (IsCaller)
                {
                    className = "v4_callerControl";
                    w.Write(" caller-type='" + (int) CallerType + "'");
                }

                if (IsRequired && (Value==null || Value.Length == 0))
                    className += "v4s_required";

                if (className.Length > 0)
                    w.Write(" class='{0}'", className.Trim());

                if (Title.Length > 0)
                    w.Write(" title='{0}'", HttpUtility.HtmlEncode(Title));


                if (TabIndex.HasValue)
                    w.Write(" TabIndex={0} ", TabIndex);

                w.Write(" />");
            }

            if (IsUseCondition)
            {
                w.Write("</div></fieldset>");
                IsCanUseFilter = CheckCanUse();
            }
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            base.Flush();
            if (PropertyChanged.Contains("Value"))
            {
                if (Value == null) Value = "";
                if (IsReadOnly)
                    //Недопустимо устанавливать свойство innerText всего элемента, т.к. он может содержать вложенный элемент div _ntf
                    JS.Write("if(gi('{0}'))gi('{0}').firstChild.innerText='{1}';", HtmlID,
                        HttpUtility.JavaScriptStringEncode(Value));
                else
                {
                    JS.Write("if(gi('{0}_0')){{gi('{0}_0').value='{1}';", HtmlID,
                        HttpUtility.JavaScriptStringEncode(Value));

                    JS.Write("gi('{0}_0').setAttribute('t','{1}');}}", HtmlID, Value.Length == 0 ? "" : HttpUtility.JavaScriptStringEncode(Value));

                    if (IsRequired)
                        JS.Write("if(gi('{0}_0'))v4_replaceStyleRequired(gi('{0}_0'));", HtmlID);
                }
            }
            else if (PropertyChanged.Contains("IsRequired"))
            {
                JS.Write("if(gi('{0}_0'))gi('{0}_0').setAttribute('isRequired','{1}');", HtmlID, IsRequired ? 1 : 0);
                JS.Write("if(gi('{0}_0'))v4_replaceStyleRequired(gi('{0}_0'));", HtmlID);
            }

            /////////////////////////////////////////
            if (IsUseCondition)
            {
                if (PropertyChanged.Contains("HasCheckbox") || PropertyChanged.Contains("IsDisabled"))
                {
                    if (HasCheckbox)
                    {
                        string disabled_attribute = IsDisabled || IsReadOnly ? "disabled='disabled'" : string.Empty;
                        string checked_attribute = Checked ? "checked='checked'" : string.Empty;
                        string strJS = string.Format("void function(){{ var el=gi('{0}').firstChild; if(el && null==gi('{0}_2')) {{el.setAttribute('style','{3}'); var el_div=document.createElement('div'); el.insertBefore(el_div, el.firstChild); el_div.setAttribute('style','{4}') ; el_div.innerHTML = \"<input id='{0}_2' type='checkbox' {1} {2} onclick=\\\"{5}\\\"/>\";}} }}();", HtmlID, checked_attribute, disabled_attribute, _fieldset_style, _fieldset_div_style, _fieldset_input_onclick(HtmlID));
                        JS.Write(strJS);
                    }
                    else
                    {
                        JS.Write("void function(){{var el=gi('{0}').firstChild; if(el) el.removeAttribute('style'); var el_input=gi('{0}_2'); if (el_input) el.removeChild(el_input.parentNode);}}();", HtmlID);
                    }

                    if (IsDisabled || IsReadOnly) JS.Write("gi('{0}').getElementsByTagName('fieldset')[0].setAttribute('disabled', 'disabled');", HtmlID);
                    else JS.Write("gi('{0}').getElementsByTagName('fieldset')[0].removeAttribute('disabled');", HtmlID);
                }

                if (PropertyChanged.Contains("Checked"))
                {
                    bool fEnabled = (!HasCheckbox || Checked) && !IsDisabled && !IsReadOnly;
                    string strClass = fEnabled ? "v4_selectClause" : "v4_selectClauseDisabled";
                    JS.Write("void function(){{var input_checkbox=gi('{0}_2'); if(input_checkbox) input_checkbox.checked={1}; var input_text=gi('{0}_0'); if(input_text) input_text.readonly=input_text.disabled=!input_checkbox.checked; var legend=gi('{0}HeadControl'); if(legend) legend.className='{2}';}}();", HtmlID, Checked ? "true" : "false", strClass);
                }
            }
            ///////////////////////////////////////////

            if (!IsReadOnly && PropertyChanged.Contains("IsDisabled"))
            {
                JS.Write("if(gi('{0}'))gi('{0}').disabled={1};", HtmlID, IsDisabled ? "1" : "null");
            }

            if (PropertyChanged.Contains("ListChanged"))
            {
                JS.Write("gi('" + ID + "HeadControl').innerHTML = '{0}';",
                    _list.Find(x => x.Code == ValueTextBoxEnum).Name);

                if (IsEditable())
                    JS.Write("if(gi('{0}_0'))gi('{0}_0').disabled=false;", HtmlID);
                else
                    JS.Write("if(gi('{0}_0'))gi('{0}_0').disabled=true;", HtmlID);
            }
        }

        /// <summary>
        ///     Получение выбранного условия поиска в текстовом виде
        /// </summary>
        /// <returns></returns>
        public virtual string GetFilterClauseText()
        {
            if (String.IsNullOrEmpty(Description)) return "";
            switch ((TextBoxEnum) Convert.ToInt32(ValueTextBoxEnum))
            {
                case TextBoxEnum.NotEmpty:
                    return Description + ": " + Resx.GetString("cNotEmpty").ToLower();
                case TextBoxEnum.Empty:
                    return Description + ": " + Resx.GetString("cEmpty").ToLower();
                case TextBoxEnum.NotEnds:
                    return String.IsNullOrEmpty(Value)
                        ? ""
                        : Description + ": " + Resx.GetString("cNotEnds").ToLower() + " " + Value;
                case TextBoxEnum.Ends:
                    return String.IsNullOrEmpty(Value) ? "" : Description + ": " + Resx.GetString("cEnds").ToLower() + " " + Value;
                case TextBoxEnum.NotMatches:
                    return String.IsNullOrEmpty(Value)
                        ? ""
                        : Description + ": " + Resx.GetString("cNotMatches").ToLower() + " " + Value;
                case TextBoxEnum.Matches:
                    return String.IsNullOrEmpty(Value)
                        ? ""
                        : Description + ": " + Resx.GetString("cMatches").ToLower() + " " + Value;
                case TextBoxEnum.NotStart:
                    return String.IsNullOrEmpty(Value)
                        ? ""
                        : Description + ": " + Resx.GetString("cNotStart").ToLower() + " " + Value;
                case TextBoxEnum.Starts:
                    return String.IsNullOrEmpty(Value)
                        ? ""
                        : Description + ": " + Resx.GetString("cStarts").ToLower() + " " + Value;
                case TextBoxEnum.NotContainsAll:
                    return String.IsNullOrEmpty(Value)
                        ? ""
                        : Description + ": " + Resx.GetString("cNotContainAll").ToLower() + " " + Value;
                case TextBoxEnum.ContainsAll:
                    return String.IsNullOrEmpty(Value)
                        ? ""
                        : Description + ": " + Resx.GetString("cContainsAll").ToLower() + " " + Value;
                case TextBoxEnum.NotContainsAny:
                    return String.IsNullOrEmpty(Value)
                        ? ""
                        : Description + ": " + Resx.GetString("cNotContainAny").ToLower() + " " + Value;
                case TextBoxEnum.ContainsAny:
                    return String.IsNullOrEmpty(Value)
                        ? ""
                        : Description + ": " + Resx.GetString("cContainsAny").ToLower() + " " + Value;

                case TextBoxEnum.ContainsAllOrdered:
                    return String.IsNullOrEmpty(Value)
                        ? ""
                        : Description + ": " + Resx.GetString("cContainsAllOrdered").ToLower() + " " + Value;

                case TextBoxEnum.NotContainsAllOrdered:
                    return String.IsNullOrEmpty(Value)
                        ? ""
                        : Description + ": " + Resx.GetString("cNotContainsAllOrdered").ToLower() + " " + Value;

            }

            return "";
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
                    _checked = is_checked;

                    if (is_checked)
                    {
                        //Вся HTML разметка меняется на стороне клиента, серверу нет необходимости вносить изменения

                        //При включении элемента управления разрешается редактирование, в некоторых случаях его следует снова запретить
                        if (!IsEditable())
                        {
                            JS.Write("if(gi('{0}_0')) gi('{0}_0').disabled=true;", HtmlID);
                        }
                    }

                    if (null != CheckChanged)
                        CheckChanged(this, new CheckEventArgs(_checked));
                }

                return;
            }

            if (collection["vn"] != null)
            {
                var oldVal = Value;
                var origVal = collection["ov"];
                Value = collection["vn"];
                OnChanged(new ProperyChangedEventArgs(oldVal, Value, origVal));
            }
            if (collection["cmd"] != null)
            {
                switch (collection["cmd"])
                {
                    case "popupHead":
                        ShowPopupWindowClause();
                        break;
                    case "setHead":
                        SetControlClause(collection["val"]);
                        break;
                }
            }
        }

        /// <summary>
        ///     Валидация значения контрола
        /// </summary>
        /// <returns>Результат валидации</returns>
        public override bool Validation()
        {
            if (!base.Validation())
                return false;
            if (!string.IsNullOrEmpty(RegEx) && !string.IsNullOrEmpty(Value) && !Regex.IsMatch(Value, RegEx))
            {
                V4Page.ShowMessage(ValidationMessage);
                Focus();
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Проверка на возможность использования значения контрола в запросе
        /// </summary>
        /// <returns>возможность использования значения контрола в запросе</returns>
        private bool CheckCanUse()
        {
            switch ((TextBoxEnum) Convert.ToInt32(ValueTextBoxEnum))
            {
                case TextBoxEnum.Empty:
                case TextBoxEnum.NotEmpty:
                    return true;
                default:
                    if (!String.IsNullOrEmpty(Value)) return true;
                    break;
            }
            return false;
        }

        /// <summary>
        ///     Установка заголовка
        /// </summary>
        /// <param name="val">Значение заголовка</param>
        private void SetControlClause(object val)
        {
            if (val != null)
            {
                ValueTextBoxEnum = val.ToString();
                if (ValueTextBoxEnum == ((int) TextBoxEnum.Empty).ToString(CultureInfo.InvariantCulture) ||
                    ValueTextBoxEnum == ((int) TextBoxEnum.NotEmpty).ToString(CultureInfo.InvariantCulture))
                {
                    Value = "";
                    FocusToNextCtrl();
                }
                IsCanUseFilter = CheckCanUse();
                SetPropertyChanged("ListChanged");
                OnChanged(new ProperyChangedEventArgs(Value, Value));
            }
        }

        /// <summary>
        ///     Отображаем попап с условиями фильтра
        /// </summary>
        private void ShowPopupWindowClause()
        {
            if (HasCheckbox && !Checked || IsDisabled || IsReadOnly) return;

            using (TextWriter w = new StringWriter())
            {
                RenderPopupWindowClause(w, _list);
                V4Page.HTMLBlock.Add("v4s_popup", w.ToString());
            }

            JS.Write("v4f_showPopup('{0}_0', '{0}HeadControl');", HtmlID);
        }

        /// <summary>
        ///     Отрисовка списка условий в виде попап дива
        /// </summary>
        /// <param name="w">Ответ сервера</param>
        /// <param name="data">Коллекция условий</param>
        private void RenderPopupWindowClause(TextWriter w, IEnumerable<Item> data)
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
    }
}