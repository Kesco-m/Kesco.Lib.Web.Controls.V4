using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI.WebControls;
using Kesco.Lib.BaseExtention.Enums.Controls;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол выбора периода
    /// </summary>
    public class PeriodTimePicker : V4Control
    {
        private string _dateFormat = "dd.MM.yyyy";
        private bool _showTime;
        private string _valueFrom = "";
        private string _valuePeriod = "";
        private string _valueTo = "";

        /// <summary>
        ///     Признак отображения контрола - только дата начала и конца (без выбора периода)
        /// </summary>
        public bool IsOnlyPeriod;

        /// <summary>
        ///     Коллекция возможных периодов
        /// </summary>
        public List<Item> List = new List<Item>();

        /// <summary>
        ///     Конструктор контрола выбора периода
        /// </summary>
        public PeriodTimePicker()
        {
            CSSClass = "v4d"; //не класть в V4OnInit !!!
            IsUseCondition = true;
        }

        /// <summary>
        ///     Текстовое представление даты начала периода
        /// </summary>
        public string ValueFrom
        {
            get { return _valueFrom; }
            set
            {
                value = value.Trim();
                if (!_valueFrom.Equals(value))
                {
                    SetPropertyChanged("ValueFrom");
                }
                _valueFrom = value;
            }
        }

        /// <summary>
        ///     Текстовое представление даты начала периода (в виде "yyyyMMddHHmmss")
        /// </summary>
        public string ValueFromODBC
        {
            get
            {
                return ValueDateFrom.HasValue
                    ? ValueDateFrom.Value.ToString("yyyyMMdd" + (ShowTime ? "HHmmss" : ""))
                    : "";
            }
        }

        /// <summary>
        ///     Текстовое представление даты окончания периода (в виде "yyyyMMddHHmmss")
        /// </summary>
        public string ValueToODBC
        {
            get
            {
                return ValueDateTo.HasValue ? ValueDateTo.Value.ToString("yyyyMMdd" + (ShowTime ? "HHmmss" : "")) : "";
            }
        }

        /// <summary>
        ///     Текстовое представление даты окончания периода
        /// </summary>
        public string ValueTo
        {
            get { return _valueTo; }
            set
            {
                value = value.Trim();
                if (!_valueTo.Equals(value))
                {
                    SetPropertyChanged("ValueTo");
                }
                _valueTo = value;
            }
        }

        /// <summary>
        ///     Текстовое представление выбранного периода
        /// </summary>
        public string ValuePeriod
        {
            get { return _valuePeriod; }
            set
            {
                value = value.Trim();
                if (!_valuePeriod.Equals(value))
                {
                    SetPropertyChanged("ValuePeriod");
                }
                _valuePeriod = value;
            }
        }

        /// <summary>
        ///     Признак отображения времени
        /// </summary>
        public bool ShowTime
        {
            get { return _showTime; }
            set
            {
                _showTime = value;
                _dateFormat = _showTime ? "dd.MM.yyyy HH:mm" : "dd.MM.yyyy";
                CSSClass = _showTime ? "v3dt" : "v4d";
            }
        }

        /// <summary>
        ///     Дата начала периода
        /// </summary>
        public DateTime? ValueDateFrom
        {
            get
            {
                if (string.IsNullOrEmpty(ValueFrom))
                    return null;
                return DateTime.ParseExact(ValueFrom, _dateFormat, null);
            }
            set { ValueFrom = value.HasValue ? value.Value.ToString(_dateFormat) : ""; }
        }

        /// <summary>
        ///     Дата окончания периода
        /// </summary>
        public DateTime? ValueDateTo
        {
            get
            {
                if (string.IsNullOrEmpty(ValueTo))
                    return null;
                return DateTime.ParseExact(ValueTo, _dateFormat, null);
            }
            set { ValueTo = value.HasValue ? value.Value.ToString(_dateFormat) : ""; }
        }

        /// <summary>
        ///     Инициализация коллекции возможных периодов
        /// </summary>
        public override void V4OnInit()
        {
            List.Add(new Item(((int) PeriodsEnum.Day).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("iPTypeDay")));
            List.Add(new Item(((int) PeriodsEnum.Week).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("iPTypeWeek")));
            List.Add(new Item(((int) PeriodsEnum.Mounth).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("iPTypeMounth")));
            List.Add(new Item(((int) PeriodsEnum.Quarter).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("iPTypeQuarter")));
            List.Add(new Item(((int) PeriodsEnum.Year).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("iPTypeYear")));
            List.Add(new Item(((int) PeriodsEnum.Custom).ToString(CultureInfo.InvariantCulture),
                Resx.GetString("iPCustom")));
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        protected override void RenderControlBody(TextWriter w)
        {
            if (IsReadOnly)
            {
                w.Write(Resx.GetString("lFrom") + "&nbsp;" + HttpUtility.HtmlEncode(ValueFrom) + "&nbsp;" + Resx.GetString("lTo") +
                        "&nbsp;" + HttpUtility.HtmlEncode(ValueTo));
            }
            else
            {
                var isDay = InitPeriod(ValuePeriod);
                if (!IsOnlyPeriod)
                {
                    var period = new ComboBox
                    {
                        HtmlID = ID + "Select",
                        ID = ID + "Select",
                        Width = Unit.Pixel(80),
                        V4Page = V4Page,
                        ValueField = "Name",
                        KeyField = "Code",
                        TabIndex = TabIndex,
                        NextControl = NextControl,
                        IsRequired = IsRequired,
                        EmptyValueExist = false,
                        Help = Help,
                        Value = ValuePeriod
                    };
                    FillComboBox(period);
                    period.Changed += PeriodChanged;
                    V4Page.V4Controls.Add(period);
                    w.Write("<table cellspacing=\"0\" cellpadding=\"0\" class='{0}'>", CSSClass);
                    w.Write("<tr>");
                    w.Write("<td style=\"padding-bottom:3px\">");
                    w.Write(Resx.GetString("lPeriod") + ":&nbsp;</td><td Width={0}px>", period.Width.Value);
                    period.RenderControl(w);

                    w.Write("</td><td>");
                    if (ValuePeriod == ((int) PeriodsEnum.Custom).ToString(CultureInfo.InvariantCulture))
                    {
                        w.Write(
                            @"<img id=""periodTimePicker_datePrev_{0}"" src=""/STYLES/PagePrev.gif"" help=""{1}"" onclick=""return false;"" onmouseover=""return false;"" />",
                            ID, HttpUtility.HtmlEncode(Help));
                    }
                    else
                    {
                        w.Write(@"<img onclick=""cmdasync('ctrl', '{0}' ,'cmd', 'prev');"" onmouseover=""v4ptp_mouseOver();""
id=""periodTimePicker_datePrev_{0}"" src=""/STYLES/PagePrevActive.gif"" tabindex=""{1}"" help=""{2}"" />", ID, TabIndex,
                            HttpUtility.HtmlEncode(Help));
                    }
                    w.Write("</td>");
                }
                else
                {
                    w.Write("<table cellspacing=\"0\" cellpadding=\"0\" class='{0}'>", CSSClass);
                    w.Write("<tr>");
                }
                w.Write("<td id=\"" + ID + "lFrom\" style=\"padding-bottom:3px\">");
                w.Write(Resx.GetString("lFrom") + "&nbsp;");
                w.Write("</td><td>");

                var dpFrom = new DatePicker
                {
                    HtmlID = ID + "From",
                    ID = ID + "From",
                    V4Page = V4Page,
                    IsDisabled = IsDisabled,
                    TabIndex = TabIndex,
                    Value = ValueFrom,
                    Help = Help
                };
                var dpTo = new DatePicker
                {
                    HtmlID = ID + "To",
                    ID = ID + "To",
                    V4Page = V4Page,
                    IsDisabled = IsDisabled,
                    TabIndex = TabIndex,
                    NextControl = NextControl,
                    Value = ValueTo,
                    Help = Help
                };
                dpFrom.NextControl = dpTo.ID;
                dpFrom.Changed += DpFromChanged;
                V4Page.V4Controls.Add(dpFrom);
                dpFrom.RenderControl(w);

                w.Write("</td>");
                w.Write("<td id=\"" + ID + "lTo\" style=\"padding-bottom:3px\">&nbsp;" + Resx.GetString("lTo") +
                        "&nbsp;</td>");
                w.Write("<td id=\"" + ID + "dpTo\">");

                dpTo.Changed += DpToChanged;
                V4Page.V4Controls.Add(dpTo);
                dpTo.RenderControl(w);

                w.Write("</td>");
                if (!IsOnlyPeriod)
                {
                    w.Write("<td>");
                    if (ValuePeriod == ((int) PeriodsEnum.Custom).ToString(CultureInfo.InvariantCulture))
                    {
                        w.Write(
                            @"<img id=""periodTimePicker_dateNext_{0}"" src=""/STYLES/PageNext.gif"" help=""{1}"" onclick=""return false;"" onmouseover=""return false;"" />",
                            ID, HttpUtility.HtmlEncode(Help));
                    }
                    else
                    {
                        w.Write(@"<img onclick=""cmdasync('ctrl', '{0}' ,'cmd', 'next');"" onmouseover=""v4ptp_mouseOver();""
id=""periodTimePicker_dateNext_{0}"" src=""/STYLES/PageNextActive.gif"" tabindex=""{1}"" help=""{2}"" />", ID, TabIndex,
                            HttpUtility.HtmlEncode(Help));
                    }
                    w.Write("</td>");
                }
                w.Write("</tr></table>");
                if (isDay)
                    w.Write("<script type=\"text/javascript\">hi('" + ID + "lFrom');hi('" + ID + "lTo');hi('" + ID +
                            "dpTo');</script>");
            }
        }

        /// <summary>
        ///     Событие изменения даты начала периода
        /// </summary>
        /// <param name="sender">контрол даты</param>
        /// <param name="e">аргумент</param>
        internal void DpFromChanged(object sender, ProperyChangedEventArgs e)
        {
            ValueFrom = e.NewValue;

            if (ValuePeriod != ((int) PeriodsEnum.Day).ToString(CultureInfo.InvariantCulture))
            {
                if (ValueDateFrom > ValueDateTo)
                {
                    ValueTo = ValueFrom;
                }
            }
            else
                ValueTo = ValueFrom;

            //Вызывается после возможного изменения ValueTo, иначе в обработчике события ValueTo неактуально
            OnChanged(new ProperyChangedEventArgs(e.OldValue, e.NewValue));

            CheckPeriod();
        }

        /// <summary>
        ///     Событие изменения даты окончания периода
        /// </summary>
        /// <param name="sender">контрол даты</param>
        /// <param name="e">аргумент</param>
        internal void DpToChanged(object sender, ProperyChangedEventArgs e)
        {
            ValueTo = e.NewValue;

            if (ValuePeriod != ((int) PeriodsEnum.Day).ToString(CultureInfo.InvariantCulture))
            {
                if (ValueDateFrom > ValueDateTo)
                {
                    ValueFrom = ValueTo;
                }
            }
            else
                ValueFrom = ValueTo;

            //Вызывается после возможного изменения ValueFrom, иначе в обработчике события ValueFrom неактуально
            OnChanged(new ProperyChangedEventArgs(e.OldValue, e.NewValue));

            CheckPeriod();
        }

        /// <summary>
        ///     Метод возвращающий порядковый номер квартала (римскими цифрами)
        /// </summary>
        /// <returns>номер квартала</returns>
        private string GetNumberPeriod()
        {
            switch (int.Parse(ValuePeriod))
            {
                //case (int) PeriodsEnum.Day:
                //    return ValueDateFrom.Value.DayOfYear.ToString(CultureInfo.InvariantCulture);
                //case (int) PeriodsEnum.Week:
                //    return Math.Ceiling((decimal)ValueDateFrom.Value.DayOfYear / 7).ToString(CultureInfo.InvariantCulture);
                //case (int) PeriodsEnum.Mounth:
                //    return ValueDateFrom.Value.Month.ToString(CultureInfo.InvariantCulture);
                case (int) PeriodsEnum.Quarter:
                    if (ValueDateFrom.HasValue)
                    {
                        var q = Math.Ceiling((decimal) ValueDateFrom.Value.Month/3);
                        switch (Convert.ToInt32(q))
                        {
                            case 1:
                                return "I";
                            case 2:
                                return "II";
                            case 3:
                                return "III";
                            case 4:
                                return "IV";
                        }
                    }
                    break;
            }
            return "";
        }

        /// <summary>
        ///     Установка периода "Произвольно", при любом изменении даты начала или окончания периода
        ///     Исключение составляет период - День.
        /// </summary>
        private void CheckPeriod()
        {
            if (ValuePeriod == ((int)PeriodsEnum.Day).ToString(CultureInfo.InvariantCulture)) return;
            ValuePeriod = ((int) PeriodsEnum.Custom).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Биндим контрол Select нашими данными
        /// </summary>
        /// <param name="searchParam">параметр поиска (в данном случае не используется)</param>
        /// <returns>Коллекция периодов</returns>
        public IEnumerable FillSelect(string searchParam)
        {
            return List;
        }

        private void FillComboBox(ComboBox period)
        {
            foreach (var item in List)
            {
                period.Items.Add(item.Code, item.Name);
            }
        }

        /// <summary>
        ///     Получение элемента списка по коду
        /// </summary>
        /// <param name="id">ID элемента</param>
        /// <returns>элемент</returns>
        public object GetObjectById(string id)
        {
            var item = new Item(id, List.Find(x => x.Code == id).Name);
            if (item.Code == "4")
                item.Name += " " + GetNumberPeriod();
            return item;
        }

        /// <summary>
        ///     Событие изменения периода
        /// </summary>
        /// <param name="sender">контрол периода</param>
        /// <param name="e">аргумент</param>
        internal void PeriodChanged(object sender, ProperyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.NewValue))
            {
                SetPeriod(e.NewValue);
            }
            else
            {
                e.NewValue = e.OldValue;
                ((ComboBox) sender).Value = e.NewValue;
            }
            ValuePeriod = ((ComboBox) sender).Value;
            //((ComboBox)sender).ValueText = List.Find(x => x.Code == ValuePeriod).Name + " " + GetNumberPeriod();
            OnChanged(new ProperyChangedEventArgs(e.OldValue, e.NewValue));
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            base.Flush();
            if (PropertyChanged.Contains("ValueFrom"))
            {
                JS.Write("gi('{0}_0').value='{1}';", ID + "From", HttpUtility.JavaScriptStringEncode(ValueFrom));
                JS.Write("gi('{0}_0').setAttribute('t','{1}');", ID + "From",
                    HttpUtility.JavaScriptStringEncode(ValueFrom));
                //if (!IsOnlyPeriod)
                //    ((Select)V4Page.V4Controls[ID + "Select"]).ValueText = List.Find(x => x.Code == ValuePeriod).Name + " " + GetNumberPeriod();
            }
            if (PropertyChanged.Contains("ValueTo"))
            {
                JS.Write("gi('{0}_0').value='{1}';", ID + "To", HttpUtility.JavaScriptStringEncode(ValueTo));
                JS.Write("gi('{0}_0').setAttribute('t','{1}');", ID + "To", HttpUtility.JavaScriptStringEncode(ValueTo));
            }
            //if (!IsOnlyPeriod)
            //    ((Select)V4Page.V4Controls[ID + "Select"]).ValueText = List.Find(x => x.Code == ValuePeriod).Name + " " + GetNumberPeriod();
            if (!IsOnlyPeriod && PropertyChanged.Contains("ValuePeriod"))
            {
                JS.Write("gi('{0}_0').value='{1}';", ID + "Select", HttpUtility.JavaScriptStringEncode(ValuePeriod));
                if (ValuePeriod == ((int) PeriodsEnum.Custom).ToString(CultureInfo.InvariantCulture))
                {
                    DisableListing(true);
                }
                else
                {
                    DisableListing(false);
                }
            }
        }

        /// <summary>
        ///     Включение/отключение листинга периода
        /// </summary>
        public void DisableListing(bool disable)
        {
            if (disable)
            {
                JS.Write("gi('periodTimePicker_datePrev_{0}').src='/STYLES/PagePrev.gif';", ID);
                JS.Write("gi('periodTimePicker_datePrev_{0}').onclick={1};", ID, "function(){return false;}");
                JS.Write("gi('periodTimePicker_datePrev_{0}').onmouseover={1};", ID, "function(){return false;}");
                JS.Write("gi('periodTimePicker_datePrev_{0}').tabIndex='';", ID);
                JS.Write("gi('periodTimePicker_dateNext_{0}').src='/STYLES/PageNext.gif';", ID);
                JS.Write("gi('periodTimePicker_dateNext_{0}').onclick={1};", ID, "function(){return false;}");
                JS.Write("gi('periodTimePicker_dateNext_{0}').onmouseover={1};", ID, "function(){return false;}");
                JS.Write("gi('periodTimePicker_dateNext_{0}').tabIndex='';", ID);
            }
            else
            {
                JS.Write("gi('periodTimePicker_datePrev_{0}').src='/STYLES/PagePrevActive.gif';", ID);
                JS.Write("gi('periodTimePicker_datePrev_{0}').onclick={1};", ID,
                    "function(){cmdasync(\"ctrl\", \"" + ID + "\" ,\"cmd\", \"prev\");}");
                JS.Write("gi('periodTimePicker_datePrev_{0}').onmouseover={1};", ID, "function(){v4ptp_mouseOver();}");
                JS.Write("gi('periodTimePicker_datePrev_{0}').tabIndex='{1}';", ID, TabIndex);
                JS.Write("gi('periodTimePicker_dateNext_{0}').src='/STYLES/PageNextActive.gif';", ID);
                JS.Write("gi('periodTimePicker_dateNext_{0}').onclick={1};", ID,
                    "function(){cmdasync(\"ctrl\", \"" + ID + "\" ,\"cmd\", \"next\");}");
                JS.Write("gi('periodTimePicker_dateNext_{0}').onmouseover={1};", ID, "function(){v4ptp_mouseOver();}");
                JS.Write("gi('periodTimePicker_dateNext_{0}').tabIndex='{1}';", ID, TabIndex);
            }
        }

        /// <summary>
        ///     Установка даты начала и окончания периода в зависимости от выбранного периода
        /// </summary>
        /// <param name="val">значение периода</param>
        public void SetPeriod(string val)
        {
            if (String.IsNullOrEmpty(val)) return;
            JS.Write("di('" + ID + "lFrom');");
            JS.Write("di('" + ID + "lTo');");
            JS.Write("di('" + ID + "dpTo');");
            switch (int.Parse(val))
            {
                case (int) PeriodsEnum.Day:
                    //if (ValueDateFrom != null)
                    //{
                    //    ValueDateTo = ValueDateFrom;
                    //}
                    //else
                    //{
                    //    ValueDateFrom = ValueDateTo = ((DatePicker) V4Page.V4Controls[ID + "From"]).ValueDate = DateTime.Now;
                    //}
                    ValueDateFrom = ValueDateTo = ((DatePicker) V4Page.V4Controls[ID + "From"]).ValueDate = DateTime.Now;
                    JS.Write("hi('" + ID + "lFrom');");
                    JS.Write("hi('" + ID + "lTo');");
                    JS.Write("hi('" + ID + "dpTo');");
                    break;
                case (int) PeriodsEnum.Week:
                    if (ValueDateFrom == null)
                    {
                        ValueDateFrom =
                            ((DatePicker) V4Page.V4Controls[ID + "From"]).ValueDate =
                                DateTime.Now.AddDays(-1*(int) DateTime.Now.DayOfWeek + 1);
                        ValueDateTo =
                            ((DatePicker) V4Page.V4Controls[ID + "To"]).ValueDate =
                                DateTime.Now.AddDays(7 - (int) DateTime.Now.DayOfWeek);
                    }
                    else
                    {
                        ValueDateTo =
                            ((DatePicker) V4Page.V4Controls[ID + "To"]).ValueDate =
                                ValueDateFrom.Value.AddDays(7 - (int) ValueDateFrom.Value.DayOfWeek);
                        ValueDateFrom =
                            ((DatePicker) V4Page.V4Controls[ID + "From"]).ValueDate =
                                ValueDateFrom.Value.AddDays(-1*(int) ValueDateFrom.Value.DayOfWeek + 1);
                    }
                    break;
                case (int) PeriodsEnum.Mounth:
                    if (ValueDateFrom == null)
                    {
                        ValueDateFrom =
                            ((DatePicker) V4Page.V4Controls[ID + "From"]).ValueDate =
                                new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        if (DateTime.Now.Month == 12)
                        {
                            ValueDateTo =
                                ((DatePicker) V4Page.V4Controls[ID + "To"]).ValueDate =
                                    new DateTime(DateTime.Now.Year + 1, 1, 1).AddDays(-1);
                        }
                        else
                        {
                            ValueDateTo =
                                ((DatePicker) V4Page.V4Controls[ID + "To"]).ValueDate =
                                    new DateTime(DateTime.Now.Year, DateTime.Now.Month + 1, 1).AddDays(-1);
                        }
                    }
                    else
                    {
                        ValueDateFrom =
                            ((DatePicker) V4Page.V4Controls[ID + "From"]).ValueDate =
                                new DateTime(ValueDateFrom.Value.Year, ValueDateFrom.Value.Month, 1);
                        if (ValueDateFrom.Value.Month == 12)
                        {
                            ValueDateTo =
                                ((DatePicker) V4Page.V4Controls[ID + "To"]).ValueDate =
                                    new DateTime(ValueDateFrom.Value.Year + 1, 1, 1).AddDays(-1);
                        }
                        else
                        {
                            ValueDateTo =
                                ((DatePicker) V4Page.V4Controls[ID + "To"]).ValueDate =
                                    new DateTime(ValueDateFrom.Value.Year, ValueDateFrom.Value.Month + 1, 1).AddDays(-1);
                        }
                    }
                    break;
                case (int) PeriodsEnum.Quarter:
                    if (ValueDateFrom == null)
                    {
                        var q = (DateTime.Now.Month - 1)/3;
                        ValueDateFrom =
                            ((DatePicker) V4Page.V4Controls[ID + "From"]).ValueDate =
                                new DateTime(DateTime.Now.Year, q*3 + 1, 1);
                        ValueDateTo =
                            ((DatePicker) V4Page.V4Controls[ID + "To"]).ValueDate =
                                new DateTime(DateTime.Now.Year, (q + 1)*3, 1).AddMonths(1).AddDays(-1);
                    }
                    else
                    {
                        var q = (ValueDateFrom.Value.Month - 1)/3;
                        ValueDateFrom =
                            ((DatePicker) V4Page.V4Controls[ID + "From"]).ValueDate =
                                new DateTime(ValueDateFrom.Value.Year, q*3 + 1, 1);
                        ValueDateTo =
                            ((DatePicker) V4Page.V4Controls[ID + "To"]).ValueDate =
                                new DateTime(ValueDateFrom.Value.Year, (q + 1)*3, 1).AddMonths(1).AddDays(-1);
                    }
                    break;
                case (int) PeriodsEnum.Year:
                    if (ValueDateFrom == null)
                    {
                        ValueDateFrom =
                            ((DatePicker) V4Page.V4Controls[ID + "From"]).ValueDate =
                                new DateTime(DateTime.Now.Year, 1, 1);
                        ValueDateTo =
                            ((DatePicker) V4Page.V4Controls[ID + "To"]).ValueDate =
                                new DateTime(DateTime.Now.Year + 1, 1, 1).AddDays(-1);
                    }
                    else
                    {
                        ValueDateFrom =
                            ((DatePicker) V4Page.V4Controls[ID + "From"]).ValueDate =
                                new DateTime(ValueDateFrom.Value.Year, 1, 1);
                        ValueDateTo =
                            ((DatePicker) V4Page.V4Controls[ID + "To"]).ValueDate =
                                new DateTime(ValueDateFrom.Value.Year + 1, 1, 1).AddDays(-1);
                    }
                    break;
            }
        }

        /// <summary>
        ///     Начальная установка даты начала и окончания периода в зависимости от выбранного периода
        /// </summary>
        /// <param name="val">значение периода</param>
        private bool InitPeriod(string val)
        {
            if (String.IsNullOrEmpty(val)) return false;
            switch (int.Parse(val))
            {
                case (int) PeriodsEnum.Day:
                    //if (ValueDateFrom == null)
                    //{
                    //    ValueDateFrom = ValueDateTo = DateTime.Now;
                    //}
                    //else
                    //{
                    //    ValueDateTo = ValueDateFrom;
                    //}
                    ValueDateFrom = ValueDateTo = DateTime.Now;
                    return true;
                case (int) PeriodsEnum.Week:
                    if (ValueDateFrom == null)
                    {
                        ValueDateFrom = DateTime.Now.AddDays(-1*(int) DateTime.Now.DayOfWeek + 1);
                        ValueDateTo = DateTime.Now.AddDays(7 - (int) DateTime.Now.DayOfWeek);
                    }
                    else
                    {
                        ValueDateFrom = ValueDateFrom.Value.AddDays(-1*(int) ValueDateFrom.Value.DayOfWeek + 1);
                        ValueDateTo = ValueDateFrom.Value.AddDays(7 - (int) ValueDateFrom.Value.DayOfWeek);
                    }
                    return false;
                case (int) PeriodsEnum.Mounth:
                    if (ValueDateFrom == null)
                    {
                        ValueDateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        if (DateTime.Now.Month == 12)
                        {
                            ValueDateTo = new DateTime(DateTime.Now.Year + 1, 1, 1).AddDays(-1);
                        }
                        else
                        {
                            ValueDateTo = new DateTime(DateTime.Now.Year, DateTime.Now.Month + 1, 1).AddDays(-1);
                        }
                    }
                    else
                    {
                        ValueDateFrom = new DateTime(ValueDateFrom.Value.Year, ValueDateFrom.Value.Month, 1);
                        if (ValueDateFrom.Value.Month == 12)
                        {
                            ValueDateTo = new DateTime(ValueDateFrom.Value.Year + 1, 1, 1).AddDays(-1);
                        }
                        else
                        {
                            ValueDateTo =
                                new DateTime(ValueDateFrom.Value.Year, ValueDateFrom.Value.Month + 1, 1).AddDays(-1);
                        }
                    }
                    return false;
                case (int) PeriodsEnum.Quarter:
                    if (ValueDateFrom == null)
                    {
                        var q = (DateTime.Now.Month - 1)/3;
                        ValueDateFrom = new DateTime(DateTime.Now.Year, q*3 + 1, 1);
                        ValueDateTo = new DateTime(DateTime.Now.Year, (q + 1)*3, 1).AddMonths(1).AddDays(-1);
                    }
                    else
                    {
                        var q = (ValueDateFrom.Value.Month - 1)/3;
                        ValueDateFrom = new DateTime(ValueDateFrom.Value.Year, q*3 + 1, 1);
                        ValueDateTo = new DateTime(ValueDateFrom.Value.Year, (q + 1)*3, 1).AddMonths(1).AddDays(-1);
                    }
                    return false;
                case (int) PeriodsEnum.Year:
                    if (ValueDateFrom == null)
                    {
                        ValueDateFrom = new DateTime(DateTime.Now.Year, 1, 1);
                        ValueDateTo = new DateTime(DateTime.Now.Year + 1, 1, 1).AddDays(-1);
                    }
                    else
                    {
                        ValueDateFrom = new DateTime(ValueDateFrom.Value.Year, 1, 1);
                        ValueDateTo = new DateTime(ValueDateFrom.Value.Year + 1, 1, 1).AddDays(-1);
                    }
                    return false;
            }
            return false;
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
                    case "prev":
                        SetPrevPeriod();
                        OnChanged(new ProperyChangedEventArgs("cmd", "prev"));
                        break;
                    case "next":
                        SetNextPeriod();
                        OnChanged(new ProperyChangedEventArgs("cmd", "next"));
                        break;
                }
            }
        }

        /// <summary>
        ///     Установка предыдущего периода
        /// </summary>
        private void SetPrevPeriod()
        {
            if (String.IsNullOrEmpty(ValuePeriod)) return;
            switch (int.Parse(ValuePeriod))
            {
                case (int) PeriodsEnum.Day:
                    if (!ValueDateFrom.HasValue) return;
                    ValueDateFrom = ValueDateTo = ValueDateFrom.Value.AddDays(-1);
                    break;
                case (int) PeriodsEnum.Week:
                    if (!ValueDateFrom.HasValue) return;
                    ValueDateFrom = ValueDateFrom.Value.AddDays(-7);
                    if (!ValueDateTo.HasValue) return;
                    ValueDateTo = ValueDateTo.Value.AddDays(-7);
                    break;
                case (int) PeriodsEnum.Mounth:
                    if (!ValueDateFrom.HasValue) return;
                    ValueDateFrom = ValueDateFrom.Value.AddMonths(-1);
                    if (!ValueDateTo.HasValue) return;
                    ValueDateTo = ValueDateFrom.Value.AddMonths(1).AddDays(-1);
                    break;
                case (int) PeriodsEnum.Quarter:
                    if (!ValueDateFrom.HasValue) return;
                    ValueDateFrom = ValueDateFrom.Value.AddMonths(-3);
                    if (!ValueDateTo.HasValue) return;
                    ValueDateTo = ValueDateFrom.Value.AddMonths(3).AddDays(-1);
                    break;
                case (int) PeriodsEnum.Year:
                    if (!ValueDateFrom.HasValue) return;
                    ValueDateFrom = ValueDateFrom.Value.AddYears(-1);
                    if (!ValueDateTo.HasValue) return;
                    ValueDateTo = ValueDateTo.Value.AddYears(-1);
                    break;
            }
        }

        /// <summary>
        ///     Установка следующего периода
        /// </summary>
        private void SetNextPeriod()
        {
            if (String.IsNullOrEmpty(ValuePeriod)) return;
            switch (int.Parse(ValuePeriod))
            {
                case (int) PeriodsEnum.Day:
                    if (!ValueDateFrom.HasValue) return;
                    ValueDateFrom = ValueDateTo = ValueDateFrom.Value.AddDays(1);
                    break;
                case (int) PeriodsEnum.Week:
                    if (!ValueDateFrom.HasValue) return;
                    ValueDateFrom = ValueDateFrom.Value.AddDays(7);
                    if (!ValueDateTo.HasValue) return;
                    ValueDateTo = ValueDateTo.Value.AddDays(7);
                    break;
                case (int) PeriodsEnum.Mounth:
                    if (!ValueDateFrom.HasValue) return;
                    ValueDateFrom = ValueDateFrom.Value.AddMonths(1);
                    if (!ValueDateTo.HasValue) return;
                    ValueDateTo = ValueDateFrom.Value.AddMonths(1).AddDays(-1);
                    break;
                case (int) PeriodsEnum.Quarter:
                    if (!ValueDateFrom.HasValue) return;
                    ValueDateFrom = ValueDateFrom.Value.AddMonths(3);
                    if (!ValueDateTo.HasValue) return;
                    ValueDateTo = ValueDateFrom.Value.AddMonths(3).AddDays(-1);
                    break;
                case (int) PeriodsEnum.Year:
                    if (!ValueDateFrom.HasValue) return;
                    ValueDateFrom = ValueDateFrom.Value.AddYears(1);
                    if (!ValueDateTo.HasValue) return;
                    ValueDateTo = ValueDateTo.Value.AddYears(1);
                    break;
            }
        }
    }

    /// <summary>
    ///     Бизнес-объект - элемент
    /// </summary>
    public class Item
    {
        /// <summary>
        ///     Элемент коллекции
        /// </summary>
        /// <param name="code">Ключ</param>
        /// <param name="name">Наименование</param>
        public Item(string code, string name)
        {
            Code = code;
            Name = name;
        }

        /// <summary>
        ///     Код
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        ///     Наименование
        /// </summary>
        public string Name { get; set; }
    }
}