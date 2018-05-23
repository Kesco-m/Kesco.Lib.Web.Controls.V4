using System;
using System.IO;
using System.Web;
using System.Web.UI.WebControls;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол Button (Кнопка)
    /// </summary>
    public class Button : V4Control
    {
        private string _text;

        /// <summary>
        ///     Конструктор
        /// </summary>
        public Button()
        {
            Width = Unit.Empty;
            IsUseCondition = true;
        }

        /// <summary>
        ///     Тут можно указать атрибут Style
        /// </summary>
        public string Style { get; set; }

        /// <summary>
        ///     Тут пишем клиентский скрипт, который выполнится на событии onclick
        /// </summary>
        public string OnClick { get; set; }

        /// <summary>
        ///     Тут пишем текст, который отобразится на кнопке
        /// </summary>
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    SetPropertyChanged("Text");
                    _text = value;
                }
            }
        }

        /// <summary>
        /// Добавляем иконку из JQUI
        /// </summary>
        public string IconJQueryUI { get; set; }
        /// <summary>
        /// Добавляем иконку из Styles
        /// </summary>
        public string IconKesco { get; set; }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        public override void RenderControl(TextWriter w)
        {
            w.Write("<button");
            w.Write(" id='{0}'", HtmlID);
           
            if (!string.IsNullOrEmpty(Value))
                w.Write(" value=\"{0}\"", HttpUtility.HtmlEncode(Value));

            

            w.Write(" onclick=\"{0}\"", OnClick);
            w.Write(" style = \"");
            if (!Visible)
                w.Write("display:none;");
            if (Width != Unit.Empty)
                w.Write("width:{0};", Width);
            w.Write("\"");

            if (!string.IsNullOrEmpty(Title))
                w.Write(" title='{0}'", HttpUtility.HtmlEncode(Title));

            if (TabIndex.HasValue)
                w.Write(" TabIndex={0} ", TabIndex);
            
            w.Write(">");
            w.Write(Text);
            w.Write("</button>");

            if (!string.IsNullOrEmpty(IconJQueryUI))
                w.Write("<script>$('#{0}').button({{icons: {{primary: {1}}}}});</script>", HtmlID, IconJQueryUI);
            else
                if (!string.IsNullOrEmpty(IconKesco))
                    w.Write("<script>$('#{0}').prepend(\"<img src='{1}'/>\").button();</script>", HtmlID, IconKesco);
                else
                    w.Write("<script>$('#{0}').button();</script>", HtmlID);
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            if (PropertyChanged.Contains("Visible"))
            {
                JS.Write("if(gi('{0}'))gi('{0}').style.display='{1}';", HtmlID, Visible ? "" : "none");
            }
            else if (PropertyChanged.Contains("IsReadOnly"))
            {
                JS.Write("if(gi('{0}'))gi('{0}').disabled='{1}';", HtmlID, IsReadOnly ? "1" : "");
            }

            if (PropertyChanged.Contains("Text"))
            {
                JS.Write("if(gi('{0}'))gi('{0}').innerText='{1}';", HtmlID, Text);
            }
            
        }
    }
}