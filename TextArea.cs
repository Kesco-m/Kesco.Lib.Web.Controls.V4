using System.ComponentModel;
using System.IO;
using System.Web;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол многострочный текст
    /// </summary>
    [DefaultProperty("Text")]
    public class TextArea : V4Control
    {
        /// <summary>
        ///     Конструктор
        /// </summary>
        public TextArea()
        {
            CSSClass = "";
        }

        /// <summary>
        ///     Свойство количество строк
        /// </summary>
        public int Rows { get; set; }

        /// <summary>
        ///     Устанавливает максимальное число символов, которое может быть введено пользователем в текстовом поле. O - не
        ///     ограничено
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        protected override void RenderControlBody(TextWriter w)
        {
            if (IsReadOnly)
            {
                w.Write(HttpUtility.HtmlEncode(Value));
            }
            else
            {
                w.Write("<textarea id='{0}_0' style='{1}{2}' {3}", HtmlID,
                    string.Concat("width:", Width.IsEmpty ? "100%;" : string.Concat(Width.ToString(), ";")),
                    Height.IsEmpty ? "" : string.Concat("height:", Height.ToString(), ";"),
                    MaxLength > 0 ? string.Concat("maxlength='", MaxLength, "'") : string.Empty);

                if (IsRequired)
                {
                    w.Write(" onkeyup='v4_replaceStyleRequired(this)'");
                }
                w.Write(" onkeydown='v4t_keyDown(event);'");
                if (IsDisabled)
                {
                    w.Write(" disabled='true'");
                }
                if (!string.IsNullOrEmpty(NextControl))
                    w.Write(" nc='{0}'", GetHtmlIdNextControl());
                if (TabIndex.HasValue)
                    w.Write(" TabIndex={0} ", TabIndex);
                w.Write(" onchange='v4t_changed();'");

                if (IsRequired && Value.Length == 0)
                {
                    w.Write(" class='v4s_required'");
                }
                if (Title.Length > 0)
                {
                    w.Write(" title='{0}'", HttpUtility.HtmlEncode(Title));
                }
                if (Rows > 0)
                {
                    w.Write(" rows='{0}'", HttpUtility.HtmlEncode(Rows));
                }
                w.Write(" >{0}</textarea>", HttpUtility.HtmlEncode(Value));
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
                {
                    JS.Write("if(gi('{0}'))gi('{0}').innerText='{1}';", HtmlID,
                        HttpUtility.JavaScriptStringEncode(Value));
                }
                else
                {
                    JS.Write("gi('{0}_0').value='{1}';", HtmlID, HttpUtility.JavaScriptStringEncode(Value));
                    if (IsRequired)
                    {
                        JS.Write("v4_replaceStyleRequired(gi('{0}_0'));", HtmlID);
                    }
                }
            }
            else
            {
                JS.Write("if(gi('{0}_0'))gi('{0}_0').value='{1}';", HtmlID, HttpUtility.JavaScriptStringEncode(Value));
                JS.Write("if(gi('{0}_0'))gi('{0}_0').setAttribute('isRequired','{1}');", HtmlID, IsRequired ? 1 : 0);
                if (IsRequired)
                {
                    JS.Write("v4_replaceStyleRequired(gi('{0}_0'));", HtmlID);
                }
            }

            if (PropertyChanged.Contains("IsRequired"))
            {
                JS.Write("gi('{0}_0').setAttribute('isRequired','{1}');", HtmlID, IsRequired ? 1 : 0);
                JS.Write("v4_replaceStyleRequired(gi('{0}_0'));", HtmlID);
            }
        }
    }
}