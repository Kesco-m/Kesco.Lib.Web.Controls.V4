using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Management;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Entities;
using Kesco.Lib.Web.Comet;
using Kesco.Lib.Web.Controls.V4.Common;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол для совместной работы с документом
    /// </summary>
    public class Comet : V4Control
    {
        class CometClient
        {
            public string IdPage { get; set; }
            public string IdEmployee { get; set; }
            public string EmployeeLink { get; set; }
            public bool IsEditable { get; set; }
            
        }

        private readonly string _clientLocalization;
        private readonly string _cometTitle;
        private readonly string _lblBrowse;
        private readonly string _lblEditable;
        private readonly string _lblNewMessage;
        private readonly string _lblTogetherWork;
        private List<CometAsyncState> _entityActiveConnections;

        public bool CometDisplay { get; set; }

        /// <summary>
        ///     Конструктор
        /// </summary>
        public Comet(EntityPage page)
        {
            V4Page = page;
            CSSClass = "v4comet";
            _lblBrowse = Resx.GetString("lblBrowse");
            _lblEditable = Resx.GetString("lblEditable");
            _lblNewMessage = Resx.GetString("lblNewMessage");
            _lblTogetherWork = Resx.GetString("lblTogetherWork");
            _cometTitle = Resx.GetString("COMET_Title");
            _clientLocalization =
                string.Format(
                    @"v4Comet_clientLocalization = {{cmdClose:""{0}"",COMET_Title:""{1}"", lblChat:""{2}"", cmdSendMessage:""{3}"", lblWriteMessage:""{4}""}};",
                    Resx.GetString("cmdClose"),
                    _cometTitle,
                    Resx.GetString("lblChat"),
                    Resx.GetString("cmdSendMessage"),
                    Resx.GetString("lblWriteMessage"));

            CometServer.NotifyClients += CometServer_NotifyClients;
            CometServer.NotifyMessages += CometServer_NotifyMessages;
        }

        /// <summary>
        ///     Получение информаци об активных подключениях.
        /// </summary>
        /// <returns>Список активных подключения, за исключением текущего</returns>
        private List<CometAsyncState> GetEntityActiveConnections()
        {
            return
                CometServer.Connections.Where(
                    x =>
                        x.Tries > 0 && x.Start > DateTime.MinValue && x.ClientGuid != V4Page.IDPage && x.Id != 0 && 
                        x.Id == int.Parse(((EntityPage) V4Page).EntityId) &&
                        x.Name == ((EntityPage)V4Page).EntityName
                        ).ToList();
        }

        /// <summary>
        ///     Событие обработки подключения новых клиентов
        /// </summary>
        /// <param name="state">Объект с информацией о подключенном клиента</param>
        /// <param name="clientGuid">IDP клиента</param>
        /// <param name="status">0-подключился; 1-отключился</param>
        private void CometServer_NotifyClients(CometAsyncState state, string clientGuid = null, int status = 1)
        {
           if ((V4Page.IDPage == clientGuid || (state != null && V4Page.IDPage == state.ClientGuid))) return;

            _entityActiveConnections = GetEntityActiveConnections();

            var displayControl = false;
            var cometUsers = RenderBodyConrol(out displayControl, true);

            if (CometDisplay != displayControl)
            {
                CometDisplay = displayControl;

                CometServer.PushMessage(
                    new CometMessage
                    {
                        ClientGuid = V4Page.IDPage,
                        IsV4Script = true,
                        Message = cometUsers,
                        Status = 0,
                        UserName = ""
                    }, V4Page.IDPage);
            }
        }

        /// <summary>
        ///     Событие обработки получения сообщений чата
        /// </summary>
        /// <param name="clientGuid">IDP клиента</param>
        /// <param name="message">Сообщение</param>
        private void CometServer_NotifyMessages(string id, string name, string clientGuid, string message)
        {
            if (!(V4Page is EntityPage)) return;
            if (((EntityPage)V4Page).EntityId != id && ((EntityPage)V4Page).EntityName !=name) return;

            var jsMessage = RenderCometMessage(clientGuid, message);
            CometServer.PushMessage(
                new CometMessage
                {
                    ClientGuid = V4Page.IDPage,
                    IsV4Script = true,
                    Message = jsMessage,
                    Status = 0,
                    UserName = ""
                }, V4Page.IDPage);
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток вывода</param>
        public override void RenderControl(TextWriter w)
        {
            var displayControl = false;
            _entityActiveConnections = GetEntityActiveConnections();

            var cometUsers = RenderBodyConrol(out displayControl);

            Visible = displayControl;

            w.Write(
                "<img id='{0}_0' src=\"/Styles/chat.png\" border=\"0\" style='margin-left: 5px; margin-right: 5px; {3}' onclick='{4}v4_cometShowList(\"{0}\");' onmouseover=\"this.style.cursor='pointer';\" help='{1}' {2} title='{5}' />",
                HtmlID,
                HttpUtility.HtmlEncode(Help),
                TabIndex.HasValue ? " tabindex=" + TabIndex.Value : "",
                !displayControl ? "display:none" : "",
                HttpUtility.HtmlEncode(_clientLocalization),
                _cometTitle);
            w.Write("<img id='v4_imgCometNewMessage' src=\"/Styles/Notes.gif\" style='display:none' onclick='{2}v4_cometShowList(\"{1}\");' onmouseover=\"this.style.cursor='pointer';\" title='{0}'/>",
                _lblNewMessage,
                HtmlID,
                HttpUtility.HtmlEncode(_clientLocalization));
            w.Write(
                @"<div id = ""{0}_1"" style=""display: none;margin-bottom:5px; margin-top:5px;""><div><b>{2}</b></div><div id=""{0}_1_Body"" style=""margin-top:5px;background:whitesmoke;"">{1}</div></div>",
                HtmlID, cometUsers, _lblTogetherWork + ":");
        }

        /// <summary>
        ///     Отрисовка инфорации о наличии совместной работы
        /// </summary>
        /// <param name="displayControl">Если нет совместной работы, контрол не отображаем</param>
        /// <param name="commetMessage">Пришло ли сообщение через comet</param>
        /// <returns>Скрипт для выполнения на клиенте</returns>
        private string RenderBodyConrol(out bool displayControl, bool commetMessage = false)
        {
            var sb = new StringBuilder();
            var ret = "";
            displayControl = false;
            List<CometClient> cometClients = new List<CometClient>();
            foreach (
                var page in
                    _entityActiveConnections.OrderByDescending(x => x.Start)
                        .ToList()
                        .Select(state => V4Page.Application[state.ClientGuid])
                        .OfType<Page>())
            {
                using (var wr = new StringWriter())
                {
                    if (page.CurrentUser.Id == V4Page.CurrentUser.Id) continue;

                    var client = cometClients.FirstOrDefault(x => x.IdEmployee == page.CurrentUser.Id);

                    if (client != null)
                    {
                        if (!client.IsEditable && page.IsEditable) client.IsEditable = page.IsEditable;
                    }
                    else
                    {
                        V4Page.RenderLinkEmployee(wr, Guid.NewGuid().ToString(), page.CurrentUser, NtfStatus.Empty);
                        cometClients.Add(new CometClient { IdPage = page.IDPage, IdEmployee = page.CurrentUser.Id, EmployeeLink = wr.ToString(), IsEditable = page.IsEditable });
                    }
                }
                if (!displayControl) displayControl = true;
            }

            foreach (var x in cometClients)
            {
                sb.AppendFormat("<div>{0} - {1}</div>",
                     x.EmployeeLink,
                     x.IsEditable ? _lblEditable : _lblBrowse);
            }

            if (commetMessage)
            {
                var cometScript = "";
                ret = sb.ToString();
                if (string.IsNullOrEmpty(ret))
                {
                    cometScript = string.Format("$('#{0}_0').hide(); v4_cometShowImgMsg(0); v4_cometCloseUsersList();",
                        HtmlID);
                }
                else
                {
                    cometScript = string.Format("$('#{0}_0').show(); v4_setToolTip();", HtmlID);
                    displayControl = true;
                }
                ret = string.Format("<js>$('#{0}_1_Body').html('{1}');{2}</js>", HtmlID,
                    HttpUtility.JavaScriptStringEncode(ret),
                    cometScript);
            }
            else
                ret = sb.ToString();


            return ret;
        }

        /// <summary>
        ///     Отрисовка чата
        /// </summary>
        /// <param name="clientGuid">IDP страницы, приславшей сообщение</param>
        /// <param name="message">Текст сообщения</param>
        /// <returns>Скрипт для выполнения на клиенте</returns>
        private string RenderCometMessage(string clientGuid, string message)
        {
            var ret = "";
            using (var w = new StringWriter())
            {
                if (clientGuid == V4Page.IDPage)
                {
                    
                    w.Write(
                        "<div style='margin:2px;float:right;background:#C5FE98; min-width:225px; width:225px'>");
                }
                else
                {
                    w.Write(
                        "<div style='margin:2px;float:left;background:white; min-width:225px; width:225px;'>");

                    var p = (Page) V4Page.Application[clientGuid];
                    if (p != null)
                    {
                        w.Write("<div style=\"float:left\">");
                        V4Page.RenderLinkEmployee(w, Guid.NewGuid().ToString(), p.CurrentUser.Id, V4Page.IsRusLocal ? p.CurrentUser.FIO : p.CurrentUser.FIOEn,
                            NtfStatus.Information);
                        w.Write("</div>");
                    }
                }

                w.Write("<div style=\"float:right\">");
                    V4Page.RenderNtf(w,
                        new List<Notification>
                        {
                            new Notification
                            {
                                Message = DateTime.UtcNow.ToString("HH:mm"),
                                Status = NtfStatus.Information,
                                DashSpace = false
                            }
                        });

                w.Write("</div>");
                w.Write("<br>");
                w.Write(HttpUtility.HtmlEncode(message.Replace(@"\n", "<br/>")));
                
                w.Write("</div>");

                w.Write("<div style='clear: both;'></div>");
                
                ret =
                    string.Format(
                        "<js>{1}{2}v4_cometSetMessage('{0}');</js>",
                        HttpUtility.JavaScriptStringEncode(w.ToString()),
                        (clientGuid != V4Page.IDPage) ? "v4_cometShowImgMsg();" : "",
                        HttpUtility.HtmlEncode(_clientLocalization));
            }

            return ret;
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            if (PropertyChanged.Contains("Visible"))
            {
                JS.Write("if(gi('{0}')) gi('{0}').style.display='{1}';", HtmlID, Visible ? "" : "none");
            }
            if (PropertyChanged.Contains("Value"))
            {
                JS.Write("if(gi('{0}_0'))gi('{0}_0').innerHTML='{1}';", HtmlID, Value);
            }
        }

        /// <summary>
        ///     Отписываемся от событий
        /// </summary>
        public void DisposeComet()
        {
            CometServer.NotifyClients -= CometServer_NotifyClients;
            CometServer.NotifyMessages -= CometServer_NotifyMessages;
        }
    }
}