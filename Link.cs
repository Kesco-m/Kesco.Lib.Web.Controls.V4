using System.IO;
using System.Web;
using System.Web.UI.WebControls;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол - гиперссылка
    /// </summary>
    public class Link : V4Control
    {
        /// <summary>
        ///     Тут пишем клиентский скрипт, который выполнится на событии onclick
        /// </summary>
        public string OnClick { get; set; }

        /// <summary>
        ///     Тут пишем текст, который отобразится на кнопке
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        ///     Тут указываем адрес изображения, которое отобразится на ссылке
        /// </summary>
        public string ImgSrc { get; set; }

        /// <summary>
        ///     Тут указываем ширину изображения, которое отобразится на ссылке
        /// </summary>
        public Unit ImgWidth { get; set; }

        /// <summary>
        ///     Тут указываем высоту изображения, которое отобразится на ссылке
        /// </summary>
        public Unit ImgHeight { get; set; }

//<a href="http://www.w3schools.com">
//<img border="0" alt="W3Schools" src="logo_w3s.gif" width="100" height="100">
//</a>


        public override void RenderControl(TextWriter w)
        {
            w.Write("<a href=\"javascript:void(0);\" id=\"{0}\" style='{4}cursor:pointer;{1}' onclick=\"{2}\" {3}",
                HtmlID,
                Visible ? "" : "display:none;",
                OnClick,
                IsCaller && Value.Length > 0
                    ? " class='v4_callerControl' data-id='" + HttpUtility.UrlEncode(Value) + "' caller-type='" +
                      (int) CallerType + "'"
                    : "", Style);

            if (!string.IsNullOrEmpty(Title))
            {
                w.Write(" title='{0}'", HttpUtility.HtmlEncode(Title));
            }

            if (TabIndex.HasValue)
            {
                w.Write(" TabIndex={0} ", TabIndex);
            }

            w.Write(">{0}", Text);

            //В случае, если сушествует изображение
            if (!string.IsNullOrEmpty(ImgSrc))
            {
                w.Write("<img src='{0}' {1} {2}>", ImgSrc,
                    ImgWidth.IsEmpty ? "" : string.Concat("width='", ImgWidth, "'"),
                    ImgHeight.IsEmpty ? "" : string.Concat("height='", ImgHeight, "'"));
            }

            w.Write("</a>");
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
            else if (PropertyChanged.Contains("IsReadOnly"))
            {
                JS.Write("gi('{0}').disabled='{1}';", HtmlID, IsReadOnly ? "1" : "");
            }
        }
    }
}