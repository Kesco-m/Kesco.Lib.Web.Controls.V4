using System;
using System.IO;
using System.Web;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол для работы с временем
    /// </summary>
    public class Time : V4Control
    {
        private string _value = "";

        /// <summary>
        ///     Формат фремени
        /// </summary>
        public string TimeFormat = "HH:mm:ss";

        /// <summary>
        ///     Конструктор
        /// </summary>
        public Time()
        {
            CSSClass = "v3dt";
        }

        /// <summary>
        ///     Значение
        /// </summary>
        public DateTime? ValueTime
        {
            get
            {
                if (string.IsNullOrEmpty(Value))
                    return null;
                return DateTime.ParseExact(Value, TimeFormat, null);
            }
            set { Value = value.HasValue ? value.Value.ToString(TimeFormat) : 0.ToString(TimeFormat); }
        }

        /// <summary>
        ///     Значение контрола
        /// </summary>
        public override string Value
        {
            get
            {
                if (string.IsNullOrEmpty(_value))
                    return 0.ToString(TimeFormat);
                return _value;
            }
            set
            {
                if (!_value.Equals(value)) SetPropertyChanged("Value");
                value = value.Trim();
                _value = value;
            }
        }

        /// <summary>
        ///     Отрисовка тела элемента управления
        /// </summary>
        /// <param name="w">Объект для записи HTML-разметки</param>
        protected override void RenderControlBody(TextWriter w)
        {
            if (IsReadOnly)
            {
                w.Write(HttpUtility.HtmlEncode(Value));
            }
            else
            {
                w.Write(
                    "<INPUT type='text' style='width:{2};' value='{0}' id='{1}_0' onkeydown='return v4tm_keyDown(event, \"{3}\");' onblur='v4tm_changed(\"{1}\");'",
                    Value, HtmlID, Width, TimeFormat == "HH:mm:ss");
                w.Write(" t='{0}' help='{1}'", HttpUtility.HtmlEncode(Value), HttpUtility.HtmlEncode(Help));
                w.Write(" isRequired={0}", IsRequired ? 1 : 0);
                if (IsRequired && Value.Length == 0)
                    w.Write(" class='v4s_required v3dt'");
                if (IsDisabled)
                    w.Write(" disabled='true'");
                if (!string.IsNullOrEmpty(NextControl))
                    w.Write(" nc='{0}'", GetHtmlIdNextControl());
                if (TabIndex.HasValue)
                    w.Write(" TabIndex={0} ", TabIndex);
                w.Write(" />");
            }
        }
    }
}