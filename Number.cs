using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Kesco.Lib.BaseExtention.Enums.Controls;
using BindingDirection = Kesco.Lib.BaseExtention.Enums.Controls.BindingDirection;
using Utils = Kesco.Lib.ConvertExtention;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Статический класс переопределения метода ToString для контрола Number
    /// </summary>
    public static class NumberToString
    {
        /// <summary>
        ///     Метод decimal в string, с указанием точности
        /// </summary>
        /// <param name="val">значение</param>
        /// <param name="precision">точность</param>
        /// <param name="numberEndingZeros">количество 0 после запятой</param>
        /// <returns>значение в строковом виде</returns>
        public static string ToString(this decimal val, int precision, int numberEndingZeros)
        {
            var rez = val.ToString("N" + precision);
            var d = precision - numberEndingZeros;
            while (d > 0 && rez.EndsWith("0"))
            {
                rez = rez.Substring(0, rez.Length - 1);
                d--;
            }
            return rez.TrimEnd(',', '.');
        }

        /// <summary>
        ///     Метод decimal в string, с указанием количества 0 после запятой
        /// </summary>
        /// <param name="val">значение</param>
        /// <param name="numberEndingZeros">количество 0 после запятой</param>
        /// <returns>значение в строковом виде</returns>
        public static string ToString(this decimal val, int numberEndingZeros)
        {
            var rez = val.ToString(CultureInfo.InvariantCulture).TrimEnd(',', '.');
            var start = rez.IndexOf(".", StringComparison.Ordinal);
            if (start == -1) return val.ToString("N" + numberEndingZeros).TrimEnd(',', '.');
            for (var i = 0; i < numberEndingZeros; i++)
            {
                start++;
                if (rez.Length <= start)
                {
                    rez += "0";
                }
            }
            return rez.TrimEnd(',', '.');
        }
    }


    /// <summary>
    ///     Класс для работы с числами
    /// </summary>
    [DefaultProperty("Value")]
    [ToolboxData("<{0}:Number runat=server />")]
    public class Number : V4Control
    {
        /// <summary>
        ///     Коллекция условий
        /// </summary>
        private readonly List<Item> _list = new List<Item>();

        /// <summary>
        ///     Максимальное значение
        /// </summary>
        private decimal _maxValue = decimal.MinValue;

        /// <summary>
        ///     Минимальное значение
        /// </summary>
        private decimal _minValue = decimal.MinValue;

        /// <summary>
        ///     Точность, количество знаков после запятой
        /// </summary>
        private int _precision;

        /// <summary>
        ///     Первое значение
        /// </summary>
        private string _value1 = "";

        /// <summary>
        ///     Второе значение
        /// </summary>
        private string _value2 = "";

        /// <summary>
        ///     Значение выбранного условия (из перечисления)
        /// </summary>
        public string ValueNumbersEnum = "0";

        /// <summary>
        ///     Конструктор
        /// </summary>
        public Number()
        {
            CSSClass = "v4n";
            Changed += NumberChanged;
        }

        /// <summary>
        ///     Нормализировать текстовые строки
        /// </summary>
        public bool NormalizeString { get; set; }

        /// <summary>
        ///     Сообщение валидации
        /// </summary>
        public string ValidationMessage { get; set; }

        /// <summary>
        ///     Количество 0 после запятой
        /// </summary>
        public int NumberEndingZeros { get; set; }

        /// <summary>
        ///     Точность, количество знаков после запятой
        /// </summary>
        public int Precision
        {
            set { _precision = value > 10 ? 10 : value; }
            get { return _precision; }
        }

        /// <summary>
        ///     Минимальное значение
        /// </summary>
        public decimal MinValue
        {
            set { _minValue = value; }
            get
            {
                if (_minValue == decimal.MinValue) return decimal.MinValue;
                var v = _minValue.ToString("N" + Precision);
                return decimal.Parse(v);
            }
        }

        /// <summary>
        ///     Максимальное значение
        /// </summary>
        public decimal MaxValue
        {
            set { _maxValue = value; }
            get
            {
                if (_maxValue == decimal.MinValue) return decimal.MinValue;
                var v = _maxValue.ToString("N" + Precision);
                return decimal.Parse(v);
            }
        }

        /// <summary>
        ///     Значение Decimal
        /// </summary>
        public decimal? ValueDecimal
        {
            get
            {
                decimal rez;
                if (decimal.TryParse(Value.Replace(".", ","), out rez))
                    return rez;
                return null;
            }
            set { Value = value.HasValue ? value.Value.ToString(NumberEndingZeros) : ""; }
        }

        /// <summary>
        ///     Значение Float
        /// </summary>
        public float? ValueFloat
        {
            get
            {
                float rez;
                if (float.TryParse(Value.Replace(".", ","), out rez))
                    return rez;
                return null;
            }
            set { Value = value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : ""; }
        }

        /// <summary>
        ///     Значение Int
        /// </summary>
        public int? ValueInt
        {
            get
            {
                int rez;
                if (int.TryParse(Regex.Replace(Value.Replace(",", "").Replace(".",""), @"\s+", ""), out rez))
                    return rez;
                return null;
            }
            set
            {
                if (Precision == 0)
                    Value = value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "";
                else
                    ValueDecimal = value;
            }
        }

        /// <summary>
        ///     Прямое связывание
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
                else if (type == typeof (decimal))
                    ValueDecimal = (decimal) val;
                else if (type == typeof (int))
                    ValueInt = (int) val;
                else
                    Value = val.ToString();
            }
            else
            {
                object newval = null;
                if (type == typeof (string))
                    newval = Value;
                else if (type == typeof (int) || type == typeof (int?))
                {
                    if (Value.Length > 0)
                        newval = ValueInt;
                }
                else if (type == typeof (short) || type == typeof (short?))
                {
                    if (Value.Length > 0)
                        newval = short.Parse(Value, NumberStyles.Any);
                }
                else if (type == typeof (decimal))
                    newval = ValueDecimal;
                else if (type == typeof (decimal?))
                {
                    if (Value.Length > 0)
                        newval = ValueDecimal;
                }
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
            _list.Add(new Item(((int) NumbersEnum.Equally).ToString(CultureInfo.InvariantCulture),
                "(=) " + Resx.GetString("cEqually")));
            _list.Add(new Item(((int) NumbersEnum.More).ToString(CultureInfo.InvariantCulture),
                "(>) " + Resx.GetString("cMore")));
            _list.Add(new Item(((int) NumbersEnum.Less).ToString(CultureInfo.InvariantCulture),
                "(<) " + Resx.GetString("cLess")));
            _list.Add(new Item(((int) NumbersEnum.MoreOrEqual).ToString(CultureInfo.InvariantCulture),
                "(>=) " + Resx.GetString("cMoreOrEqual")));
            _list.Add(new Item(((int) NumbersEnum.LessOrEqual).ToString(CultureInfo.InvariantCulture),
                "(<=) " + Resx.GetString("cLessOrEqual")));
            _list.Add(new Item(((int) NumbersEnum.NotEqual).ToString(CultureInfo.InvariantCulture),
                "(<>) " + Resx.GetString("cNotEqual")));
            _list.Add(new Item(((int) NumbersEnum.Interval).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("dInterval")));
            if (!IsNotUseEmpty)
            {
                _list.Add(new Item(((int) NumbersEnum.Any).ToString(CultureInfo.InvariantCulture),
                    Resx.GetString("lblAnyValue")));
                _list.Add(new Item(((int) NumbersEnum.NoValue).ToString(CultureInfo.InvariantCulture),
                    Resx.GetString("lblNotValue")));
            }
            IsUseCondition = true;
        }

        /// <summary>
        ///     Отрисовка тела элемента управления
        /// </summary>
        /// <param name="w">Объект для записи HTML-разметки</param>
        protected override void RenderControlBody(TextWriter w)
        {
            if (IsReadOnly)
            {
                w.Write(HttpUtility.HtmlEncode(IsUseCondition ? _value1 : Value));
            }
            else
            {
                if (IsUseCondition)
                {
                    IsInterval = ValueNumbersEnum == ((int) NumbersEnum.Interval).ToString(CultureInfo.InvariantCulture);
                    var isNovalue = (ValueNumbersEnum ==
                                     ((int) NumbersEnum.NoValue).ToString(CultureInfo.InvariantCulture) ||
                                     ValueNumbersEnum == ((int) NumbersEnum.Any).ToString(CultureInfo.InvariantCulture));
                    var number = new Number
                    {
                        HtmlID = ID + "Number",
                        ID = ID + "Number",
                        V4Page = V4Page,
                        Width = (Unit) (IsInterval ? Width.Value - 17 : Width.Value),
                        TabIndex = TabIndex,
                        Value = isNovalue ? "" : _value1,
                        NextControl = IsInterval ? ID + "Number2_0" : NextControl,
                        IsDisabled = isNovalue,
                        Help = Help,
                        NumberEndingZeros = NumberEndingZeros
                    };
                    number.Changed += NumberAltChanged;
                    V4Page.V4Controls.Add(number);
                    var number2 = new Number
                    {
                        HtmlID = ID + "Number2",
                        ID = ID + "Number2",
                        V4Page = V4Page,
                        Width = (Unit) (IsInterval ? Width.Value - 17 : Width.Value),
                        TabIndex = TabIndex,
                        Value = isNovalue ? "" : _value2,
                        NextControl = NextControl,
                        Help = Help,
                        NumberEndingZeros = NumberEndingZeros
                    };
                    number2.Changed += Number2AltChanged;
                    V4Page.V4Controls.Add(number2);

                    w.Write("<fieldset><legend>");
                    w.Write(@"<div id=""{1}"" class=""v4_selectClause"" 
onclick=""cmd('ctrl', '{2}', 'cmd', 'popupHead');"" 
onkeydown=""var key=v4_getKeyCode(event); if((key == 13 || key == 32) && !v4s_isPopupOpen) cmd('ctrl', '{2}', 'cmd', 'popupHead'); else v4s_keyDown(event);"" 
{3} help='{4}'>{0}</div>",
                        _list.Find(x => x.Code == ValueNumbersEnum).Name,
                        ID + "HeadControl",
                        HtmlID,
                        TabIndex.HasValue ? " tabindex=" + TabIndex.Value : "",
                        HttpUtility.HtmlEncode(Help));
                    w.Write("</legend><div id=\"{0}\">", ID + "Margin");

                    w.Write("<table cellspacing=\"0\" cellpadding=\"0\"><tr>");
                    w.Write("<td id=\"" + ID + "lFrom\" style=\"display:" + (IsInterval ? "" : "none") + "\">" +
                            Resx.GetString("lFrom") + "&nbsp;</td>");
                    w.Write("<td>");
                    number.RenderControl(w);
                    w.Write("</td></tr>");

                    w.Write("<tr id=\"" + ID + "lTo\" style=\"display:" + (IsInterval ? "" : "none") +
                            "\"><td style=\"padding-top:2px;\">" + Resx.GetString("lTo") + "&nbsp;</td>");
                    w.Write("<td style=\"padding-top:2px;\">");
                    number2.RenderControl(w);
                    w.Write("</td></tr>");
                    w.Write("</table></div></fieldset>");
                    IsCanUseFilter = CheckCanUse();
                }
                else
                {
                    var addClass = CSSClass;
                    w.Write("<input  style='width:{0};{1};' id='{2}_0' value='{3}' type='Text' ",
                        Width.IsEmpty ? "100%" : Width.ToString(),
                        Height.IsEmpty ? "" : "height:" + Height,
                        HtmlID, HttpUtility.HtmlEncode(Value));
                    if (IsDisabled)
                        w.Write(" disabled='true'");

                    w.Write(" t='{0}' help='{1}'", HttpUtility.HtmlEncode(Value), HttpUtility.HtmlEncode(Help));

                    if (!string.IsNullOrEmpty(NextControl))
                    {
                        //w.Write(" nc='{0}'", GetHtmlIdNextControl());
                        w.Write(" nc='{0}'", NextControl);
                    }
                    w.Write(" isRequired={0}", IsRequired ? 1 : 0);
                    w.Write(" onkeydown=\"return v4t_keyDown(event);");
                    if (IsRequired)
                        w.Write("v4_replaceStyleRequired(this);");
                    w.Write("\"");
                    w.Write(" onchange=\"v4_ctrlChanged('{0}',true);\"", HtmlID);

                    if (IsRequired && Value.Length == 0)
                    {
                        addClass += " v4s_required";
                        addClass = addClass.Trim();
                    }

                    if (addClass.Length > 0)
                        w.Write(" class=\"{0}\"", addClass);
                    
                    
                    if (Title.Length > 0)
                        w.Write(" title='{0}'", HttpUtility.HtmlEncode(Title));

                    if (TabIndex.HasValue)
                        w.Write(" TabIndex={0} ", TabIndex);

                    w.Write(" />");
                }
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
                if (IsReadOnly)
                    JS.Write("if(gi('{0}_0')) gi('{0}').innerText='{1}';", HtmlID, HttpUtility.JavaScriptStringEncode(Value));
                else
                {
                    JS.Write("if(gi('{0}_0')){{gi('{0}_0').value='{1}';", HtmlID, HttpUtility.JavaScriptStringEncode(Value));
                    JS.Write("gi('{0}_0').setAttribute('t','{1}');}}", HtmlID, Value.Length == 0 ? "" : HttpUtility.JavaScriptStringEncode(Value));
                    if (IsRequired)
                        JS.Write("v4_replaceStyleRequired(gi('{0}_0'));", HtmlID);
                }
            }
            else if (PropertyChanged.Contains("IsRequired"))
            {
                JS.Write("if(gi('{0}_0')) gi('{0}_0').setAttribute('isRequired','{1}');", HtmlID, IsRequired ? 1 : 0);
                JS.Write("v4_replaceStyleRequired(gi('{0}_0'));", HtmlID);
            }
            if (!IsReadOnly && PropertyChanged.Contains("IsDisabled"))
            {
                JS.Write("if(gi('{0}_0')) gi('{0}_0').disabled={1};", HtmlID, IsDisabled ? "1" : "null");
            }
            if (PropertyChanged.Contains("ListChanged"))
            {
                JS.Write("gi('" + ID + "HeadControl').innerHTML = '{0}';",
                    _list.Find(x => x.Code == ValueNumbersEnum).Name);
                if (ValueNumbersEnum == ((int) NumbersEnum.Interval).ToString(CultureInfo.InvariantCulture))
                {
                    JS.Write("di('" + ID + "lFrom');");
                    JS.Write("di('" + ID + "lTo');");
                    JS.Write("var w = " + Width.Value + " - $('td#" + ID + "lFrom').width();");
                    JS.Write("gi('" + ID + "Number').style.width = w+'px';");
                    JS.Write("gi('" + ID + "Number2').style.width = w+'px';");
                    JS.Write("gi('" + ID + "Number_0').style.width = w+'px';");
                    JS.Write("gi('" + ID + "Number2_0').style.width = w+'px';");
                    JS.Write("gi('" + ID + "Number_0').setAttribute('nc','{0}');", ID + "Number2_0");
                    JS.Write("gi('" + ID + "Margin').style.marginBottom = '0px';");
                }
                else
                {
                    JS.Write("hi('" + ID + "lFrom');");
                    JS.Write("hi('" + ID + "lTo');");
                    JS.Write("gi('" + ID + "Number').style.width = '{0}px';", Width.Value);
                    JS.Write("gi('" + ID + "Number2').style.width = '{0}px';", Width.Value);
                    JS.Write("gi('" + ID + "Number_0').style.width = '{0}px';", Width.Value);
                    JS.Write("gi('" + ID + "Number2_0').style.width = '{0}px';", Width.Value);
                    JS.Write("gi('" + ID + "Number_0').setAttribute('nc','{0}');", NextControl);
                    JS.Write("gi('" + ID + "Margin').style.marginBottom = '-2px';");
                }
                if (ValueNumbersEnum == ((int) NumbersEnum.NoValue).ToString(CultureInfo.InvariantCulture) ||
                    ValueNumbersEnum == ((int) NumbersEnum.Any).ToString(CultureInfo.InvariantCulture))
                {
                    JS.Write("gi('" + ID + "Number_0').disabled=\"true\";");
                    JS.Write("gi('" + ID + "Number_0').value=\"\";");
                    JS.Write("gi('" + ID + "Number2_0').value=\"\";");
                }
                else
                {
                    JS.Write("gi('" + ID + "Number_0').disabled=\"\";");
                }
            }
            if (PropertyChanged.Contains("IntervalChanged"))
            {
                JS.Write("gi('{0}Number_0').value=\"{1}\";", ID, _value1);
                JS.Write("gi('{0}Number2_0').value=\"{1}\";", ID, _value2);
            }
        }

        /// <summary>
        ///     Установка выбранного условия
        /// </summary>
        /// <param name="val">выбранное условие</param>
        private void SetControlClause(object val)
        {
            if (val != null)
            {
                ValueNumbersEnum = val.ToString();
                if (ValueNumbersEnum == ((int) NumbersEnum.NoValue).ToString(CultureInfo.InvariantCulture) ||
                    ValueNumbersEnum == ((int) NumbersEnum.Any).ToString(CultureInfo.InvariantCulture))
                {
                    _value1 = _value2 = "";
                    FocusToNextCtrl();
                }
                IsInterval = ValueNumbersEnum == ((int) NumbersEnum.Interval).ToString(CultureInfo.InvariantCulture);
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

            JS.Write("v4f_showPopup('{0}Number', '{0}HeadControl');", HtmlID);
        }

        /// <summary>
        ///     Отрисовка списка условий в виде попап дива
        /// </summary>
        /// <param name="w">Ответ сервера</param>
        /// <param name="data">Коллекция условий</param>
        public virtual void RenderPopupWindowClause(TextWriter w, List<Item> data)
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
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="collection">Коллекция параметров</param>
        public override void ProcessCommand(NameValueCollection collection)
        {
            base.ProcessCommand(collection);
            if (collection["vn"] != null)
            {
                var oldVal = Value;
                Value = collection["vn"];
                OnChanged(new ProperyChangedEventArgs(oldVal, Value));
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
        ///     Получение выбранного условия поиска в текстовом виде
        /// </summary>
        /// <returns></returns>
        public string GetFilterClauseText()
        {
            if (String.IsNullOrEmpty(Description)) return "";
            switch ((NumbersEnum) Convert.ToInt32(ValueNumbersEnum))
            {
                case NumbersEnum.Any:
                    return Description + ": " + Resx.GetString("lblAnyValue");
                case NumbersEnum.NoValue:
                    return Description + ": " + Resx.GetString("lblNotValue");
                case NumbersEnum.Equally:
                    return String.IsNullOrEmpty(_value1)
                        ? ""
                        : Description + ": " + Resx.GetString("cEqually") + " " + _value1;
                case NumbersEnum.More:
                    return String.IsNullOrEmpty(_value1)
                        ? ""
                        : Description + ": " + Resx.GetString("cMore") + " " + _value1;
                case NumbersEnum.Less:
                    return String.IsNullOrEmpty(_value1)
                        ? ""
                        : Description + ": " + Resx.GetString("cLess") + " " + _value1;
                case NumbersEnum.MoreOrEqual:
                    return String.IsNullOrEmpty(_value1)
                        ? ""
                        : Description + ": " + Resx.GetString("cMoreOrEqual") + " " + _value1;
                case NumbersEnum.LessOrEqual:
                    return String.IsNullOrEmpty(_value1)
                        ? ""
                        : Description + ": " + Resx.GetString("cLessOrEqual") + " " + _value1;
                case NumbersEnum.NotEqual:
                    return String.IsNullOrEmpty(_value1)
                        ? ""
                        : Description + ": " + Resx.GetString("cNotEqual") + " " + _value1;
                case NumbersEnum.Interval:
                    if (!String.IsNullOrEmpty(_value1) && !String.IsNullOrEmpty(_value2))
                    {
                        return Description + ": " + Resx.GetString("dInterval") + " " + Resx.GetString("lFrom") + " " +
                               _value1 + " " + Resx.GetString("lTo") + " " + _value2;
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
            switch ((NumbersEnum) Convert.ToInt32(ValueNumbersEnum))
            {
                case NumbersEnum.Any:
                case NumbersEnum.NoValue:
                    return true;
                case NumbersEnum.Equally:
                case NumbersEnum.More:
                case NumbersEnum.Less:
                case NumbersEnum.MoreOrEqual:
                case NumbersEnum.LessOrEqual:
                case NumbersEnum.NotEqual:
                    if (ValueDecimal1 != null) return true;
                    break;
                case NumbersEnum.Interval:
                    if (ValueDecimal1 != null && ValueDecimal2 != null) return true;
                    break;
            }
            return false;
        }

        /// <summary>
        ///     Событие Изменение значения контрола
        /// </summary>
        /// <param name="sender">контрол</param>
        /// <param name="e">аргументы события</param>
        private void NumberChanged(object sender, ProperyChangedEventArgs e)
        {
            var v = e.NewValue;
            if (string.IsNullOrEmpty(v))
            {
                if (IsRequired)
                    ValueInt = null;
                else
                    Value = "";
            }
            else
            {
                decimal rez;
                float rez2;
                if (decimal.TryParse(v.Replace(".", ","), out rez))
                {
                    if (_minValue != decimal.MinValue && rez < MinValue)
                    {
                        JS.Write("Alert.render('Minimal value: {0}');", MinValue);
                        Value = e.OldValue;
                    }
                    else if (_maxValue != decimal.MinValue && rez > MaxValue)
                    {
                        JS.Write("Alert.render('Maximal value: {0}');", MaxValue);
                        Value = e.OldValue;
                    }
                    else
                    {
                        Value = NormalizeString ? rez.ToString() : rez.ToString(NumberEndingZeros);
                    }
                }
                else if (float.TryParse(v.Replace(".", ","), out rez2))
                {
                    Value = rez2.ToString(CultureInfo.InvariantCulture);
                }
                else
                    Value = e.OldValue;
            }
        }

        /// <summary>
        ///     Форматирование значения
        /// </summary>
        /// <param name="n">Исходная строка</param>
        /// <param name="minScale">минимальный масштаб</param>
        /// <param name="maxScale">максимальный масштаб</param>
        /// <param name="groupSeparator">разделитель</param>
        /// <returns>форматированная строка</returns>
        public static string FormatNumber(string n, int minScale, int maxScale, string groupSeparator)
        {
            if (n.Length == 0) return "";
            var d = Utils.Convert.Str2Decimal(n);
            var nfi = (NumberFormatInfo) NumberFormatInfo.CurrentInfo.Clone();
            nfi.CurrencySymbol = "";
            nfi.CurrencyDecimalDigits = maxScale;
            nfi.CurrencyGroupSeparator = groupSeparator;
            var s = d.ToString("C", nfi);
            s = Regex.Replace(s, "[0]{0," + (maxScale - minScale) + "}$", "");
            s = Regex.Replace(s, ",$", "");
            return s;
        }

        #region setters

        /// <summary>
        ///     Первое значение типа 128-битное число
        /// </summary>
        public decimal? ValueDecimal1
        {
            get
            {
                decimal rez;
                if (decimal.TryParse(_value1.Replace(".", ","), out rez))
                    return rez;
                if (ValueFloat1.HasValue)
                {
                    try
                    {
                        return (decimal) ValueFloat1;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
                return null;
            }
            set { _value1 = value.HasValue ? value.Value.ToString(NumberEndingZeros) : ""; }
        }

        /// <summary>
        ///     Первое значение типа число с плавающей запятой
        /// </summary>
        public float? ValueFloat1
        {
            get
            {
                float rez;
                if (float.TryParse(_value1.Replace(".", ","), out rez))
                    return rez;
                return null;
            }
            set { _value1 = value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : ""; }
        }

        /// <summary>
        ///     Первое значение типа целое число
        /// </summary>
        public int? ValueInt1
        {
            get
            {
                int rez;
                if (int.TryParse(Regex.Replace(_value1.Replace(",", ""), @"\s+", ""), out rez))
                    return rez;
                return null;
            }
            set
            {
                if (Precision == 0)
                    _value1 = value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "";
                else
                    ValueDecimal1 = value;
            }
        }

        /// <summary>
        ///     Значение для второго числа диапазона типа 128-битное число
        /// </summary>
        public decimal? ValueDecimal2
        {
            get
            {
                decimal rez;
                if (decimal.TryParse(_value2.Replace(".", ","), out rez))
                    return rez;
                if (ValueFloat2.HasValue)
                {
                    try
                    {
                        return (decimal) ValueFloat2;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
                return null;
            }
            set { _value2 = value.HasValue ? value.Value.ToString(NumberEndingZeros) : ""; }
        }

        /// <summary>
        ///     Значение для второго числа диапазона типа число с плавающей запятой
        /// </summary>
        public float? ValueFloat2
        {
            get
            {
                float rez;
                if (float.TryParse(_value2.Replace(".", ","), out rez))
                    return rez;
                return null;
            }
            set { _value2 = value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : ""; }
        }

        /// <summary>
        ///     Значение для второго числа диапазона типа целое число
        /// </summary>
        public int? ValueInt2
        {
            get
            {
                int rez;
                if (int.TryParse(Regex.Replace(_value2.Replace(",", ""), @"\s+", ""), out rez))
                    return rez;
                return null;
            }
            set
            {
                if (Precision == 0)
                    _value2 = value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "";
                else
                    ValueDecimal2 = value;
            }
        }

        #endregion

        #region handlers

        /// <summary>
        ///     Событие Изменение значения первого контрола
        /// </summary>
        /// <param name="sender">контрол</param>
        /// <param name="e">аргументы события</param>
        private void NumberAltChanged(object sender, ProperyChangedEventArgs e)
        {
            var v = e.NewValue;
            if (string.IsNullOrEmpty(v))
            {
                if (IsRequired)
                    ValueInt1 = null;
                else
                    _value1 = "";
            }
            else
            {
                decimal rez;
                float rez2;
                if (decimal.TryParse(v.Replace(".", ","), out rez))
                {
                    _value1 = NormalizeString ? rez.ToString() : rez.ToString(NumberEndingZeros);

                    if (rez > ValueDecimal2)
                        ValueDecimal2 = ValueDecimal1;
                }
                else if (float.TryParse(v.Replace(".", ","), out rez2))
                {
                    _value1 = rez2.ToString(CultureInfo.InvariantCulture);
                    if (rez2 > ValueFloat2)
                        ValueFloat2 = ValueFloat1;
                }
                else
                    _value1 = e.OldValue;
            }
            SetPropertyChanged("IntervalChanged");
            OnChanged(new ProperyChangedEventArgs(Value, Value));
        }

        /// <summary>
        ///     Событие Изменение значения второго контрола
        /// </summary>
        /// <param name="sender">контрол</param>
        /// <param name="e">аргументы события</param>
        private void Number2AltChanged(object sender, ProperyChangedEventArgs e)
        {
            var v = e.NewValue;
            if (string.IsNullOrEmpty(v))
            {
                if (IsRequired)
                    ValueInt2 = null;
                else
                    _value2 = "";
            }
            else
            {
                decimal rez;
                float rez2;
                if (decimal.TryParse(v.Replace(".", ","), out rez))
                {
                    _value2 = NormalizeString ? rez.ToString() : rez.ToString(NumberEndingZeros);
                    if (rez < ValueDecimal1)
                        ValueDecimal1 = ValueDecimal2;
                }
                else if (float.TryParse(v.Replace(".", ","), out rez2))
                {
                    _value2 = rez2.ToString(CultureInfo.InvariantCulture);
                    if (rez2 < ValueFloat1)
                        ValueFloat1 = ValueFloat2;
                }
                else
                    _value2 = e.OldValue;
            }
            SetPropertyChanged("IntervalChanged");
            OnChanged(new ProperyChangedEventArgs(Value, Value));
        }

        #endregion
    }
}