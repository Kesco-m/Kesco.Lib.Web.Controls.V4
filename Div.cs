using System.IO;
using System.Web.UI;
using System.Text;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол div
    /// </summary>
    public class Div : V4Control
    {
        /// <summary>
        ///     Конструктор
        /// </summary>
        public Div()
        {
            CSSClass = "v5div";
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        public override void RenderControl(TextWriter w)
        {
            string display_style = Visible ? "" : "none";

            w.Write("<div id='{0}' class='{1}' style='display: {3}'>{2}", HtmlID, CSSClass, Value, display_style);
            RenderChildren(w as HtmlTextWriter);
            w.Write("</div>");
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            if (PropertyChanged.Contains("Visible"))
            {
                JS.Write("gi('{0}').style.display='{1}';", HtmlID, Visible ? "" : "none");
            }

            if (PropertyChanged.Contains("Value"))
            {
                var strWriter = new StringWriter();
                var htw = new HtmlTextWriter(strWriter);
                RenderChildren(htw);

                //Строка будет передана в Javascript код поэтому экранируем обратную косую черту, кавычки и переводы строки
                //HttpUtility.JavaScriptStringEncode() не заменяет \r\n, поэтому используем свой вариант
                StringBuilder sb = new StringBuilder(Value + strWriter);
                sb.Replace(@"\", @"\\");
                sb.Replace(@"'", @"\'");
                sb.Replace("\"", "\\\"");
                sb.Replace("\r", "\\\r");

                JS.Write("if(gi('{0}'))gi('{0}').innerHTML='{1}';", HtmlID, sb);
            }
        }
    }
}