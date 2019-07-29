using System;
using System.Net.Mime;
using System.Reflection;
using System.Web;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Comet;
using Kesco.Lib.Web.Controls.V4.Common;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    /// <summary>
    ///     HTTP-хендлер для получения значений из других приложений, использовать вместо Cookie
    /// </summary>
    public class DialogResultHandler : IHttpHandler
    {
        #region IHttpHandler Members

        /// <summary>
        ///     Обработка результатов запроса
        /// </summary>
        /// <param name="context">Текущий контекст</param>
        public void ProcessRequest(HttpContext context)
        {
            if (context.Request.Form["callbackKey"] == null && context.Request.QueryString["callbackKey"] == null)
                ProcessRequestByProxyWindow(context);
            else
                ProcessRequestByIdPage(context);

        }

        private void ProcessRequestByIdPage(HttpContext context)
        {
            
            var callbackKey = context.Request.Form["callbackKey"] ?? context.Request.QueryString["callbackKey"];
            var control = context.Request.Form["control"] ?? context.Request.Form["control"] ?? context.Request.QueryString["control"];
            var clientName = context.Request.Form["clientname"] ?? context.Request.QueryString["clientname"];
            var command = context.Request.Form["command"] ?? context.Request.QueryString["command"];


            //Реализуем принудительное закрытие comet-соединения.
            if (control == "window" && command == "pageclose")
            {
                var p = context.Application[callbackKey] as Page;
                if (p != null)
                {
                    p.SaveParameters();
                    p.V4Dispose();
                }
                return;
            }

            //Реализуем принудительное закрытие comet-соединения.
            if (control == "window" && command == "refreshForce")
            {
                var p = context.Application[callbackKey] as Page;
                if (p != null)
                {
                }
                return;
            }


            //Реализуем отправки сообщения о клиентской ошибке
            if (control == "window" && command == "error")
            {
                string message = HttpUtility.UrlDecode(context.Request["messageError"]);
                int type = int.Parse(context.Request["typeError"] ?? "1");
                Priority p = (Priority)type;

                Logger.WriteEx(new LogicalException
                (message,
                    "Message: " + message + ";\n" +
                    (context.Request["linenumberError"].Length > 0 ? "Line: " + context.Request["linenumberError"] + ";\n" : "") +
                    (context.Request["urlError"].Length > 0 ? "Url: " + HttpUtility.UrlDecode(context.Request["urlError"]) : ""),
                    Assembly.GetExecutingAssembly().GetName(),
                    "window.onError",
                    p));

                return;
            }

            control = context.Request.Form["control"] ?? context.Request.Form["control"] ?? context.Request.QueryString["callbackKey"];

            if (string.IsNullOrEmpty(callbackKey) || string.IsNullOrEmpty(control))
            {
                throw new Log.LogicalException("Ошибка передачи параметров",
                    "Не переданы обязательные параметры callbackKey|control",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName());
            }
            
            var page = context.Application[callbackKey] as Page;
            if (page == null)
            {
                throw new Exception("Ошибка -> " + callbackKey);
            }

            if (!string.IsNullOrEmpty(clientName))
            {
                CometServer.PushMessage(new CometMessage
                {
                    Message =
                        string.Format(
                            "<js>v4_clientName = '{0}'; v4_setToolTip();</js>",
                            clientName),
                    ClientGuid = callbackKey,
                    Status = 0,
                    UserName = context.User.Identity.Name,
                    IsV4Script = true
                }, callbackKey);
                CometServer.Process();
                return;
            }
        

            var value = context.Request.Form["value"];
            var valueText = context.Request.Form["valueText"];
            var escaped = context.Request.Form["escaped"];
            var multiReturn = context.Request.Form["multiReturn"];

            if (escaped.Equals("1"))
            {
                value = HttpUtility.UrlDecode(value);
                valueText = HttpUtility.UrlDecode(valueText??"");
            }
            if (value == null || value.Equals("0"))
            {
                if (!string.IsNullOrEmpty(control))
                    CometServer.PushMessage(new CometMessage
                    {
                        Message =
                            string.Format(
                                "<js>var suff= gi('{0}_0')!=null?'_0':''; $(window).focus(); setTimeout(function () {{$('#{0}' + suff).focus();}},10);</js>",
                                control),
                        ClientGuid = callbackKey,
                        Status = 0,
                        UserName = context.User.Identity.Name,
                        IsV4Script = true
                    }, callbackKey);
                else
                    CometServer.PushMessage(
                        new CometMessage
                        {
                            Message = "<js>alert('Нет значения и нет контрола');</js>",
                            ClientGuid = callbackKey,
                            Status = 0,
                            UserName = context.User.Identity.Name,
                            IsV4Script = true

                        }, callbackKey);

            }
            else
            {
                if (callbackKey == control && command.ToLower().Equals("checksimilar"))

                    CometServer.PushMessage(new CometMessage()
                    {
                        Message =
                            string.Format("<js>$(window).focus(); v4_setIdEntity('{0}', '{1}');</js>",
                                HttpUtility.JavaScriptStringEncode(value ?? ""),
                                HttpUtility.JavaScriptStringEncode(command ?? "CHECKSIMILAR")),
                        ClientGuid = callbackKey,
                        Status = 0,
                        UserName = context.User.Identity.Name,
                        IsV4Script = true
                    },
                        callbackKey);
                else
                    CometServer.PushMessage(new CometMessage()
                    {
                        Message =
                            string.Format("<js>$(window).focus(); v4s_setSelectedValue('{0}', '{1}', JSON.parse('{2}'), '{3}');</js>",
                                control,
                                callbackKey,
                                HttpUtility.JavaScriptStringEncode(value ?? ""),
                                multiReturn=="2"?"True":"False"),
                        ClientGuid = callbackKey,
                        Status = 0,
                        UserName = context.User.Identity.Name,
                        IsV4Script = true
                    },
                        callbackKey);

                CometServer.Process();
            }
        }

        private void ProcessRequestByProxyWindow(HttpContext context)
        {
            var returnValue = HttpUtility.UrlDecode(context.Request.Form["value"]);

            context.Response.Write("<html>");
            context.Response.Write("<head>");
            context.Response.Write(
                string.Format("<script src='/Styles/Kesco.V4/JS{0}/jquery-1.12.4.min.js' type='text/javascript'></script>", Settings.Config.versionV4js));
            context.Response.Write(string.Format("<script src='/Styles/Kesco.V4/JS{0}/kesco.Dialog.js' type='text/javascript'></script>", Settings.Config.versionV4js));

            context.Response.Write(@"
<script>
function ReturnResult(r) {
	        var w = window.opener || window.parent.opener
	        var wHost = window.parent || window.self;
	        try {
	            if (w && !w.closed && w.$ && w.$.v4_windowManager) {                 
	                w.$.v4_windowManager.closeDialogEx(wHost, r);
	            }                
	        } catch (e) {
	            alert(e.Description);
	        }
	        if (!wHost.closed) {               
                    wHost.close();	            
	        }
	    }
</script>");
            context.Response.Write("</head>");
            context.Response.Write("<body>");
            context.Response.Write(string.Format(@"
<script>
ReturnResult('{0}');
</script>
",
                HttpUtility.JavaScriptStringEncode(returnValue)));
            context.Response.Write("</body></html>");
        }

        /// <summary>
        ///     Может ли другой запрос использовать экземпляр класса
        /// </summary>
        public bool IsReusable
        {
            get { return false; }
        }

        #endregion
    }
}