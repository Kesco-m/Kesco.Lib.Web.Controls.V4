using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using Kesco.Lib.Web.Comet;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол Chat (Чат)
    /// </summary>
    public class Chat : V4Control
    {
        /// <summary>
        ///     Конструктор
        /// </summary>
        public Chat()
        {
            CSSClass = "v4chat";
            MaxHeight = 200;
        }

        /// <summary>
        ///     Максимальный размер, после которого появляется полоса прокрутки
        /// </summary>
        public int MaxHeight { get; set; }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        public override void RenderControl(TextWriter w)
        {
            w.Write("<div id='{0}_0' class='{1}' style='display:none'>", HtmlID, CSSClass);
            w.Write("<table>");
            w.Write(
                "<tr><td id='{0}_head' width='100%' style='background:darkgray; text-align: center'><font color=\"white\" style=\"font-weight: bold\">{1}</font><img align=\"left\" src=\"\\Styles\\Notes.gif\"/><img align=\"right\" src=\"\\Styles\\Cancel.gif\" border=\"0\" onclick='v4_cometChatHide(\"{0}\")'/></td><td></td></tr>",
                HtmlID, Resx.GetString("lblChat"));
            w.Write("<tr><td colspan=2>");
            w.Write("<div id='{0}_3' style='max-height:{1}px;max-width:400px; overflow:auto;'></div>", HtmlID, MaxHeight);
            w.Write("</td></tr>");
            w.Write("<tr><td colspan=2>");
            w.Write(
                "<input id='{0}_2' style='width: 98%; border-radius: 6px; box-shadow:0px 2px 10px 0px grey' onkeydown='return v4_cometChatKeyDown(event);' help='{1}' type='text'></input>",
                HtmlID, HttpUtility.HtmlEncode(Help));
            w.Write("</td></tr>");
            w.Write("</table>");
            w.Write("</div>");
            w.Write(
                "<img id='{0}_1' src=\"/Styles/chat.png\" border=\"0\"  onkeydown='return v4_cometChatShow(\"{0}\");' onclick='v4_cometChatShow(\"{0}\")' help='{1}' {2} />",
                HtmlID, HttpUtility.HtmlEncode(Help),
                TabIndex.HasValue ? " tabindex=" + TabIndex.Value : "");

            w.Write(
                "<script>$(\"#{0}_0\").draggable({{ containment: 'window', handle: '#{0}_head', cursor: 'move' }});</script>",
                HtmlID);
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
                JS.Write("if(gi('{0}_0'))gi('{0}_0').innerHTML='{1}';", HtmlID, Value);
            }
        }

        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="collection">Коллекция параметров</param>
        public override void ProcessCommand(NameValueCollection collection)
        {
            base.ProcessCommand(collection);
            if (collection["chat"] != null)
            {
                SendMessage(collection["chat"]);
            }
        }

        /// <summary>
        ///     Рассылка сообщений чата
        /// </summary>
        /// <param name="message">Сообщение</param>
        private void SendMessage(string message)
        {
            var s = "<js>v4_cometChatNewMessage('" + V4Page.V4Request.QueryString["ctrl"] + "', '" + message +
                    HttpUtility.JavaScriptStringEncode("<br />") +
                    "', '" + DateTime.Now.ToString("HH:mm:ss") + "', '" + V4Page.CurrentUser.FIO + "', '" +
                    V4Page.CurrentUser.Id + "');</js>";
            //CometServer.SendMessage(s, V4Page.ItemId, V4Page.IDPage);
        }
    }
}