using System;
using System.Collections.Specialized;
using System.IO;
using System.Web.UI;
using Kesco.Lib.BaseExtention;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Класс описывающий элемент пользовательского элемента GroupBox (группа элементов),
    ///     с дополнительной возможностью выбора из списка заголовка группирующей рамки,
    ///     который обычно используется как выбор способа фильтрации по содержащимся
    ///     в группе элементов
    /// </summary>
    public class FilterGroupBox : V4Control
    {
        private const string _div_fieldset_style = "position: relative; padding-top: 0px; padding-right: 0px;";
        private const string _div_input_style = "width: 20px; height:20px; position: absolute; top: 0px; right: 3px;";

        //Значение свойства Checked
        private bool _checked;

        //Значение свойства HasCheckbox
        private bool _has_checkbox;

        public FilterGroupBox()
        {
            CSSClass = "v5groupbox";
        }

        /// <summary>
        ///     Признак использования элемента CheckBox
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

        /// <summary>
        ///     Свойство значения элемента CheckBox
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
                        CheckChanged(this, new CheckEventArgs(_checked));
                }
            }
        }

        /// <summary>
        ///     Список доступных значений фильтра
        /// </summary>
        public string[] FilterOptions { get; set; }

        /// <summary>
        ///     Функция обратного вызова для события выбора нового элемента из списка
        /// </summary>
        public event EventHandler<FilterEventArgs> FilterChanged;

        /// <summary>
        ///     Функция обратного вызова для события выбора элемента
        /// </summary>
        public event EventHandler<CheckEventArgs> CheckChanged;

        private string _fieldset_input_onclick(string id)
        {
            return string.Format(
                "cmdasync('ctrl', '{0}', 'checked', this.checked ? '1' : '0'); if(this.checked) {{var fs = gi('{0}_1'); fs.className='v4_selectClause'; fs.tabindex=0;}} else {{ gi('{0}_1').className='v4_selectClauseDisabled'; }}",
                id);
        }

        public override void RenderControl(TextWriter w)
        {
            var disabled_attribute = IsDisabled || IsReadOnly ? "disabled='disabled'" : string.Empty;

            w.Write("<div id='{0}' style='{1}' class='{2}'>", HtmlID, _div_fieldset_style, CSSClass);

            if (HasCheckbox)
            {
                var checked_attribute = Checked ? "checked='checked'" : string.Empty;
                w.Write(
                    "<div style='{4}' ><input id='{0}_0' type='checkbox' tabindex='0' {1} {2} onclick=\"{3}\"/></div>",
                    HtmlID, checked_attribute, disabled_attribute, _fieldset_input_onclick(HtmlID), _div_input_style);
            }

            var strLegend = FilterOptions.Length > 0 && Value.ToInt() >= 0
                ? FilterOptions[Value.ToInt()]
                : string.Empty;

            var fEnabled = (!HasCheckbox || Checked) && !IsDisabled && !IsReadOnly;

            var strClass = fEnabled ? "v4_selectClause" : "v4_selectClauseDisabled";

            w.Write("<fieldset {0} >", disabled_attribute);
            w.Write(@"<legend><div id=""{1}_1"" class=""{4}""
            onclick=""cmd('ctrl', '{2}', 'cmd', 'popupHead');""
            onkeydown=""var key=v4_getKeyCode(event); if((key == 13 || key == 32) && !v4s_isPopupOpen) cmd('ctrl', '{2}', 'cmd', 'popupHead'); else v4s_keyDown(event);""
            {3}>{0}</div></legend>",
                strLegend,
                ID,
                HtmlID,
                fEnabled ? TabIndex.HasValue ? " tabindex='" + TabIndex.Value + "'" : " tabindex='0'" : string.Empty,
                strClass
            );

            RenderChildren(w as HtmlTextWriter);

            w.Write("</fieldset></div>");
        }

        public override void Flush()
        {
            if (PropertyChanged.Contains("HasCheckbox") || PropertyChanged.Contains("IsDisabled") ||
                PropertyChanged.Contains("IsReadOnly"))
            {
                if (HasCheckbox)
                {
                    var disabled_attribute = IsDisabled || IsReadOnly ? "disabled='disabled'" : string.Empty;
                    var checked_attribute = Checked ? "checked='checked'" : string.Empty;
                    JS.Write(
                        "{{ var el=gi('{0}'); if(el && null==gi('{0}_0')) {{el.setAttribute('style','{1}'); var el_div=document.createElement('div'); el.insertBefore(el_div, el.firstChild); el_div.setAttribute('style','{2}') ; el_div.innerHTML = \"<input id='{0}_0' type='checkbox' tabindex='0' {3} {4} onclick=\\\"{5}\\\"/>\";}} }}",
                        HtmlID, _div_fieldset_style, _div_input_style, checked_attribute, disabled_attribute,
                        _fieldset_input_onclick(HtmlID));
                }
                else
                {
                    JS.Write(
                        "{{var el=gi('{0}'); if(el) el.removeAttribute('style'); var el_input=gi('{0}_0'); if (el_input) el.removeChild(el_input.parentNode);}}",
                        HtmlID);
                }

                if (IsDisabled || IsReadOnly)
                    JS.Write("gi('{0}').getElementsByTagName('fieldset')[0].setAttribute('disabled', 'disabled');",
                        HtmlID);
                else JS.Write("gi('{0}').getElementsByTagName('fieldset')[0].removeAttribute('disabled');", HtmlID);
            }

            if (PropertyChanged.Contains("Checked"))
            {
                var fEnabled = (!HasCheckbox || Checked) && !IsDisabled && !IsReadOnly;
                var strClass = fEnabled ? "v4_selectClause" : "v4_selectClauseDisabled";

                JS.Write(
                    "if(gi('{0}_0')) gi('{0}_0').checked={1}; var legend=gi('{0}_1'); if(legend) legend.className='{2}';",
                    HtmlID, Checked ? "true" : "false", strClass);
            }

            if (PropertyChanged.Contains("Visible"))
                JS.Write("gi('{0}').style.display='{1}';", HtmlID, Visible ? "" : "none");

            if (PropertyChanged.Contains("Value"))
            {
                TextWriter inner_writer = new StringWriter();
                var htw = new HtmlTextWriter(inner_writer);
                RenderChildren(htw);

                JS.Write("gi('{0}_1').innerHTML = '{1}';", HtmlID, FilterOptions[Value.ToInt()]);
            }
        }

        public override void ProcessCommand(NameValueCollection collection)
        {
            if (collection["checked"] != null)
            {
                var is_checked = collection["checked"] == "1";
                if (_checked != is_checked)
                {
                    _checked = is_checked;
                    if (null != CheckChanged)
                        CheckChanged(this, new CheckEventArgs(_checked));
                }

                return;
            }

            if (collection["cmd"] != null)
                switch (collection["cmd"])
                {
                    case "popupHead":
                        ShowPopupFilter();
                        return;

                    case "setHead":
                        SetFilterValue(collection["val"]);
                        return;
                }

            base.ProcessCommand(collection);
        }

        /// <summary>
        ///     Установка заголовка
        /// </summary>
        /// <param name="val">Значение заголовка</param>
        private void SetFilterValue(string val)
        {
            if (string.IsNullOrWhiteSpace(val) || val.ToInt() < 0) val = "0";
            if (val.ToInt() >= FilterOptions.Length) val = (FilterOptions.Length - 1).ToString();

            if (Value != val)
            {
                Value = val;
                if (null != FilterChanged)
                    FilterChanged(this, new FilterEventArgs(val));
            }
        }

        /// <summary>
        ///     Отображаем список с условиями фильтра
        /// </summary>
        private void ShowPopupFilter()
        {
            if (HasCheckbox && !Checked || IsDisabled || IsReadOnly) return;

            if (null == FilterOptions) return;
            if (2 > FilterOptions.Length) return;

            using (TextWriter w = new StringWriter())
            {
                RenderPopupFilter(w);
                V4Page.HTMLBlock.Add("v4s_popup", w.ToString());
            }

            JS.Write("v4f_showPopup('{0}', '{0}');", HtmlID + "_1");
        }

        /// <summary>
        ///     Отрисовка списка условий для сортировки по значениям во вложенных элементах
        /// </summary>
        /// <param name="w">Ответ сервера</param>
        private void RenderPopupFilter(TextWriter w)
        {
            if (null == FilterOptions) return;

            JS.Write("v4s_popup.ids='{0}';", HtmlID);
            w.Write("<table class='v4_p_clause'>");

            for (var i = 0; i < FilterOptions.Length; i++)
            {
                //Здесь ошибка элементы tr не могут содержать атрибуты cmd или idItem
                w.Write("<tr cmd='setHead' idItem='{0}'>", i);
                w.Write("<td name='tblClause{1}'>{0}</td>", FilterOptions[i], HtmlID);
                w.Write("</tr>");
            }

            w.Write("</table>");
        }

        /// <summary>
        ///     Класс описывающий событие выбора элемента фильтрации из выпадающего списка
        /// </summary>
        public class FilterEventArgs : EventArgs
        {
            public FilterEventArgs(string filter)
            {
                if (null != filter)
                    Filter = filter.ToInt();
            }

            public int? Filter { get; set; }
        }

        /// <summary>
        ///     Класс описывающий событие выбора элемента
        /// </summary>
        public class CheckEventArgs : EventArgs
        {
            public CheckEventArgs(bool checkedValue)
            {
                Checked = checkedValue;
            }

            public bool Checked { get; set; }
        }
    }
}