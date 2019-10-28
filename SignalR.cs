using System.IO;
using System.Web;
using Kesco.Lib.Web.Controls.V4.Common;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол оповещения о совместной работы с сущностью
    /// </summary>
    public class SignalR : V4Control
    {
        private readonly string _clientLocalization;
        private readonly string _lblBrowse;
        private readonly string _lblEditable;
        private readonly string _lblNewMessage;
        private readonly string _lblTogetherWork;
        private readonly string _signalTitle;

        /// <summary>
        ///     Конструктор
        /// </summary>
        public SignalR(EntityPage page)
        {
            V4Page = page;
            Visible = false;

            CSSClass = "v4signal";
            _lblBrowse = Resx.GetString("lblBrowse");
            _lblEditable = Resx.GetString("lblEditable");
            _lblNewMessage = Resx.GetString("lblNewMessage");
            _lblTogetherWork = Resx.GetString("lblTogetherWork");
            _signalTitle = Resx.GetString("COMET_Title");
            _clientLocalization =
                string.Format(
                    @"v4Signal_clientLocalization = {{cmdClose:""{0}"",COMET_Title:""{1}"", lblChat:""{2}"", cmdSendMessage:""{3}"", lblWriteMessage:""{4}""}};",
                    Resx.GetString("cmdClose"),
                    _signalTitle,
                    Resx.GetString("lblChat"),
                    Resx.GetString("cmdSendMessage"),
                    Resx.GetString("lblWriteMessage"));
        }

        public sealed override bool Visible
        {
            get { return base.Visible; }
            set { base.Visible = value; }
        }


        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток вывода</param>
        public override void RenderControl(TextWriter w)
        {
            w.Write(
                "<img id='{0}_0' src=\"/Styles/chat.png\" border=\"0\" style='margin-left: 5px; margin-right: 5px; {3}' onclick='{4}v4_signalShowList(\"{0}\");' onmouseover=\"this.style.cursor='pointer';\" help='{1}' {2} title='{5}' />",
                HtmlID,
                HttpUtility.HtmlEncode(Help),
                TabIndex.HasValue ? " tabindex=" + TabIndex.Value : "",
                !Visible ? "display:none" : "",
                HttpUtility.HtmlEncode(_clientLocalization),
                _signalTitle);
            w.Write(
                @"<div id = ""{0}_1"" style=""display: none;margin-bottom:5px; margin-top:5px;""><div><b>{2}</b></div><div id=""{0}_1_Body"" style=""margin-top:5px;background:whitesmoke;"">{1}</div></div>",
                HtmlID, "", _lblTogetherWork + ":");
        }
    }
}