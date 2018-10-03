using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums.Controls;
using BindingDirection = Kesco.Lib.BaseExtention.Enums.Controls.BindingDirection;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Класс для работы с датами
    /// </summary>
    [DefaultProperty("Value")]
    [ToolboxData("<{0}:DatePicker runat=server />")]
    public class DatePicker : V4Control
    {
        private readonly string _dateFormat = "dd.MM.yyyy";

        /// <summary>
        ///     Коллекция условий
        /// </summary>
        private readonly List<Item> _list = new List<Item>();

        /// <summary>
        ///     Значение максиммальной даты для выбора в формате даты
        /// </summary>
        private DateTime _maxDate;

        /// <summary>
        ///     Значение минимальной даты для выбора в формате даты
        /// </summary>
        private DateTime _minDate;

        private DateTime? _valueDate;

        /// <summary>
        ///     Значение выбранного условия (из перечисления)
        /// </summary>
        public string ValueDatePickerEnum = "0";

        /// <summary>
        ///     Минимальная дата, возможная для выбора
        /// </summary>
        public string MinDate
        {
            set
            {
                if (!String.IsNullOrEmpty(value))
                    DateTime.TryParseExact(value, _dateFormat, CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None,
                        out _minDate);
            }
        }

        /// <summary>
        ///     Максимальная дата, возможная для выбора
        /// </summary>
        public string MaxDate
        {
            set
            {
                if (!String.IsNullOrEmpty(value))
                    DateTime.TryParseExact(value, _dateFormat, CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None,
                        out _maxDate);
            }
        }

        /// <summary>
        ///     Значение контрола DatePicker
        /// </summary>
        public override string Value
        {
            get { return base.Value; }
            set
            {
                if (base.Value != value)
                {
                    DateTime date;
                    var succeed = DateTime.TryParse(value, CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None,
                        out date);
                    _valueDate = succeed ? date : (DateTime?) null;
                    base.Value = succeed ? ToDateFormat(date) : "";
                }
            }
        }

        /// <summary>
        ///     Значение контрола DatePicker
        /// </summary>
        public DateTime? ValueDate
        {
            get { return _valueDate; }
            set
            {
                if (_valueDate != value)
                {
                    _valueDate = value;
                    base.Value = value != null ? ToDateFormat(value.Value) : "";
                }
            }
        }

        private string ToDateFormat(DateTime date)
        {
            if (date <= DateTime.MinValue)
                return "";

            return date.GetIndependenceDate();
        }

        /// <summary>
        ///     Событие изменения свойств контрола
        /// </summary>
        /// <param name="sender">контрол</param>
        /// <param name="properyChangedEventArgs">Инициатор события</param>
        private void PeriodOnChanged(object sender, ProperyChangedEventArgs properyChangedEventArgs)
        {
            OnChanged(new ProperyChangedEventArgs(Value, Value));
        }

        /// <summary>
        ///     Установка заголовка
        /// </summary>
        /// <param name="val">Значение заголовка</param>
        private void SetControlClause(object val)
        {
            if (val != null)
            {
                ValueDatePickerEnum = val.ToString();
                if (ValueDatePickerEnum == ((int) DatePickerEnum.Null).ToString(CultureInfo.InvariantCulture) ||
                    ValueDatePickerEnum == ((int) DatePickerEnum.Any).ToString(CultureInfo.InvariantCulture))
                {
                    Value = "";
                    FocusToNextCtrl();
                }
                IsInterval = ValueDatePickerEnum ==
                             ((int) DatePickerEnum.Interval).ToString(CultureInfo.InvariantCulture);
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
            using (TextWriter w = new StringWriter())
            {
                RenderPopupWindowClause(w, _list);
                V4Page.HTMLBlock.Add("v4s_popup", w.ToString());
            }

            JS.Write("v4f_showPopup('{0}_f', '{0}HeadControl');", HtmlID);
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

        /// <summary>
        ///     Получение выбранного условия поиска в текстовом виде
        /// </summary>
        /// <returns></returns>
        public string GetFilterClauseText()
        {
            if (String.IsNullOrEmpty(Description)) return "";
            switch ((DatePickerEnum) Convert.ToInt32(ValueDatePickerEnum))
            {
                case DatePickerEnum.Any:
                    return Description + ": " + Resx.GetString("lblAnyValue");
                case DatePickerEnum.Null:
                    return Description + ": " + Resx.GetString("dNull");
                case DatePickerEnum.Equal:
                    return ValueDate == null ? "" : Description + ": " + Resx.GetString("dEqual") + " " + Value;
                case DatePickerEnum.MoreOrEqual:
                    return ValueDate == null ? "" : Description + ": " + Resx.GetString("dMoreOrEqual") + " " + Value;
                case DatePickerEnum.LessOrEqual:
                    return ValueDate == null ? "" : Description + ": " + Resx.GetString("dLessOrEqual") + " " + Value;
                case DatePickerEnum.Interval:
                    var from = ((PeriodTimePicker) V4Page.V4Controls[ID + "PeriodDp"]).ValueDateFrom;
                    var to = ((PeriodTimePicker) V4Page.V4Controls[ID + "PeriodDp"]).ValueDateTo;
                    var sfrom = ((PeriodTimePicker) V4Page.V4Controls[ID + "PeriodDp"]).ValueFrom;
                    var sto = ((PeriodTimePicker) V4Page.V4Controls[ID + "PeriodDp"]).ValueTo;
                    if (from != null && to != null)
                    {
                        return Description + ": " + Resx.GetString("dInterval") + " " + Resx.GetString("lFrom") + " " +
                               sfrom + " " + Resx.GetString("lTo") + " " + sto;
                    }
                    break;
            }
            return "";
        }

        /// <summary>
        ///     Проверка на возможность использования значения контрола в запросе
        /// </summary>
        /// <returns>возможность использования значения контрола в запросе</returns>
        private bool CheckCanUse()
        {
            switch ((DatePickerEnum) Convert.ToInt32(ValueDatePickerEnum))
            {
                case DatePickerEnum.Any:
                case DatePickerEnum.Null:
                    return true;
                case DatePickerEnum.Equal:
                case DatePickerEnum.MoreOrEqual:
                case DatePickerEnum.LessOrEqual:
                    if (ValueDate != null) return true;
                    break;
                case DatePickerEnum.Interval:
                    var from = ((PeriodTimePicker) V4Page.V4Controls[ID + "PeriodDp"]).ValueDateFrom;
                    var to = ((PeriodTimePicker) V4Page.V4Controls[ID + "PeriodDp"]).ValueDateTo;
                    if (from != null && to != null) return true;
                    break;
            }
            return false;
        }

        private void RenderDatePicker(TextWriter w)
        {
            w.Write("<span id='{0}'>", HtmlID);
            w.Write(
                "<input type='text' value='{0}' id='{1}_0' class=\"v4d_datepicker\" style=\"width:80px;\" onchange=\"v4_ctrlChanged('{1}',true, true);\" onkeydown='v4d_keyDown(event, this);' {2}",
                Value, HtmlID, Visible ? "" : "style=\"display:none;\"");

            w.Write(" t='{0}' help='{1}'", HttpUtility.HtmlEncode(Value), HttpUtility.HtmlEncode(Help));
            
            w.Write(" isRequired={0}", IsRequired ? 1 : 0);
            
            if (IsDisabled)
                w.Write(" disabled ");

            if (TabIndex.HasValue)
                w.Write(" TabIndex={0} ", TabIndex);

            if (!string.IsNullOrEmpty(NextControl))
                w.Write(" nc='{0}'", IsUseCondition ? GetHtmlIdNextControl() : NextControl);
            w.Write(" />");

            w.Write("</span>");

            if (!V4Page.V4IsPostBack)
                w.Write("<script>v4_Datepicker.init('{0}_0', '{1}'); v4_replaceStyleRequired(gi('{0}_0'));</script>", HtmlID, V4Page.CurrentUser.Language);
        }

        /// <summary>
        ///     Связывание
        /// </summary>
        /// <param name="val">значение</param>
        /// <param name="type">тип</param>
        /// <param name="direction">направление</param>
        /// <returns>Признак успешного связывания</returns>
        public override bool DirectBind(ref object val, Type type, BindingDirection direction)
        {
            var changed = false;

            if (direction == BindingDirection.FromSource)
            {
                if (val == null)
                    Value = "";
                else if (type == typeof (DateTime))
                    Value = ((DateTime) val).ToString(_dateFormat);
                else if (type == typeof (DateTime?))
                    Value = ((DateTime?) val).Value.ToString(_dateFormat);
            }
            else
            {
                object newval = null;
                if (Value.Length > 0)
                    newval = DateTime.ParseExact(Value, _dateFormat, CultureInfo.GetCultureInfo("ru-RU"));

                if (newval == null && val != null)
                    changed = true;
                else if (newval != null)
                    changed = !newval.Equals(val);

                val = newval;
            }
            return changed;
        }

        /// <summary>
        ///     Инициализируем коллекцию условий и устанавливаем другие свойства составного контрола
        /// </summary>
        public override void V4OnInit()
        {
            if (!IsUseCondition) return;
            _list.Add(new Item(((int) DatePickerEnum.Equal).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("dEqual")));
            _list.Add(new Item(((int) DatePickerEnum.LessOrEqual).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("dLessOrEqual")));
            _list.Add(new Item(((int) DatePickerEnum.MoreOrEqual).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("dMoreOrEqual")));
            _list.Add(new Item(((int) DatePickerEnum.Interval).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("dInterval")));
            if (!IsNotUseEmpty)
            {
                _list.Add(new Item(((int) DatePickerEnum.Any).ToString(CultureInfo.InvariantCulture),
                    Resx.GetString("lblAnyValue")));
                _list.Add(new Item(((int) DatePickerEnum.Null).ToString(CultureInfo.InvariantCulture),
                    Resx.GetString("dNull")));
            }
            IsUseCondition = true;

            //TODO: Ограничение по мин и макс дате для TextBox
        }

        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="collection">Коллекция параметров</param>
        public override void ProcessCommand(NameValueCollection collection)
        {
            base.ProcessCommand(collection);
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
        ///     Отрисовка элемента управления
        /// </summary>
        /// <param name="w">Поток вывода</param>
        protected override void RenderControlBody(TextWriter w)
        {
            if (IsReadOnly)
            {
                w.Write(HttpUtility.HtmlEncode(Value));
                return;
            }
            if (IsUseCondition)
            {
                IsInterval = ValueDatePickerEnum ==
                             ((int) DatePickerEnum.Interval).ToString(CultureInfo.InvariantCulture);
                var isNovalue = (ValueDatePickerEnum ==
                                 ((int) DatePickerEnum.Null).ToString(CultureInfo.InvariantCulture) ||
                                 ValueDatePickerEnum ==
                                 ((int) DatePickerEnum.Any).ToString(CultureInfo.InvariantCulture));
                var period = new PeriodTimePicker
                {
                    HtmlID = ID + "PeriodDp",
                    ID = ID + "PeriodDp",
                    V4Page = V4Page,
                    IsOnlyPeriod = true,
                    TabIndex = TabIndex,
                    NextControl = NextControl,
                    Visible = IsInterval,
                    Help = Help
                };
                period.Changed += PeriodOnChanged;
                V4Page.V4Controls.Add(period);

                w.Write("<fieldset id=\"{0}_f\"><legend>", HtmlID);
                w.Write(@"<div id=""{1}"" class=""v4_selectClause"" 
onclick=""cmd('ctrl', '{2}', 'cmd', 'popupHead');"" 
onkeydown=""var key=v4_getKeyCode(event); if((key == 13 || key == 32) && !v4s_isPopupOpen) cmd('ctrl', '{2}', 'cmd', 'popupHead'); else v4s_keyDown(event);"" 
{3} help='{4}'>{0}</div>",
                    _list.Find(x => x.Code == ValueDatePickerEnum).Name,
                    ID + "HeadControl",
                    HtmlID,
                    TabIndex.HasValue ? " tabindex=" + TabIndex.Value : "",
                    HttpUtility.HtmlEncode(Help));
                w.Write("</legend><div id=\"{0}\">", ID + "Margin");

                IsDisabled = isNovalue;
                Value = isNovalue ? "" : Value;
                Visible = !IsInterval;

                RenderDatePicker(w);

                period.RenderControl(w);
                w.Write("</div></fieldset>");
                IsCanUseFilter = CheckCanUse();
            }
            else
            {
                RenderDatePicker(w);
            }
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            var setDisabled = PropertyChanged.Contains("IsDisabled");
            
            base.Flush();

            if (PropertyChanged.Contains("Value"))
            {
                if (ValueDate < _minDate && ValueDate != DateTime.MinValue) ValueDate = _minDate;
                if (ValueDate > _maxDate && _maxDate != DateTime.MinValue && ValueDate != DateTime.MinValue)
                    ValueDate = _maxDate;

                JS.Write("if(gi('{0}_0')){{gi('{0}_0').value='{1}';", HtmlID, HttpUtility.JavaScriptStringEncode(Value));
                JS.Write("gi('{0}_0').setAttribute('t','{1}');}}", HtmlID, HttpUtility.JavaScriptStringEncode(Value));
            }

            if (PropertyChanged.Contains("ListChanged"))
            {
                JS.Write("gi('" + ID + "HeadControl').innerHTML = '{0}';",
                    _list.Find(x => x.Code == ValueDatePickerEnum).Name);
                if (ValueDatePickerEnum == ((int) DatePickerEnum.Interval).ToString(CultureInfo.InvariantCulture))
                {
                    JS.Write("hi('{0}_0');", ID);
                    JS.Write("hi('{0}_1');", ID);
                    JS.Write("di('{0}PeriodDp');", ID);
                }
                else if ((ValueDatePickerEnum == ((int) DatePickerEnum.Null).ToString(CultureInfo.InvariantCulture)) ||
                         (ValueDatePickerEnum == ((int) DatePickerEnum.Any).ToString(CultureInfo.InvariantCulture)))
                {
                    JS.Write("gi('{0}_0').disabled=1;", ID);
                    JS.Write("gi('{0}_1').disabled=1;", ID);
                    JS.Write("di('{0}_0');", ID);
                    JS.Write("di('{0}_1');", ID);
                    JS.Write("hi('{0}PeriodDp');", ID);
                }
                else
                {
                    JS.Write("gi('{0}_0').disabled=null;", ID);
                    JS.Write("gi('{0}_1').disabled=null;", ID);
                    JS.Write("di('{0}_0');", ID);
                    JS.Write("di('{0}_1');", ID);
                    JS.Write("hi('{0}PeriodDp');", ID);
                }
            }

            if (IsReadOnly) return;

            JS.Write("v4_Datepicker.init('{0}_0', '{1}');", ID, V4Page.CurrentUser.Language);
                
            if (IsDisabled) IsRequired = false;
            JS.Write("gi('{0}_0').setAttribute('isRequired','{1}');", ID, IsRequired ? 1 : 0);
            JS.Write("v4_replaceStyleRequired(gi('{0}_0'));", ID);
        }

    }
}