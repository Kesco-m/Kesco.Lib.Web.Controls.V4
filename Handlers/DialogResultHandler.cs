using System;
using System.Reflection;
using System.Web;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Controls.V4.Common;
using Kesco.Lib.Web.Settings;
using Kesco.Lib.Web.SignalR;
//using Kesco.Lib.Web.Signal;

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
            var control = context.Request.Form["control"] ??
                          context.Request.Form["control"] ?? context.Request.QueryString["control"];
            var clientName = context.Request.Form["clientname"] ?? context.Request.QueryString["clientname"];
            var command = context.Request.Form["command"] ?? context.Request.QueryString["command"];


            //Реализуем принудительное закрытие signal-соединения.
            if (control == "window" && command == "pageclose")
            {
                var p = KescoHub.GetPage(callbackKey) as Page;
                if (p != null)
                {
                    p.SaveParameters();
                    p.V4Dispose();
                }

                return;
            }

            //Реализуем принудительное закрытие signal-соединения.
            if (control == "window" && command == "refreshForce")
            {
                var p = KescoHub.GetPage(callbackKey) as Page;
                if (p != null)
                {
                }

                return;
            }


            //Реализуем отправки сообщения о клиентской ошибке
            if (control == "window" && command == "error")
            {
                var message = HttpUtility.UrlDecode(context.Request["messageError"]);
                var type = int.Parse(context.Request["typeError"] ?? "1");
                var p = (Priority) type;

                Logger.WriteEx(new LogicalException
                (message,
                    "Message: " + message + ";\n" +
                    (context.Request["linenumberError"].Length > 0
                        ? "Line: " + context.Request["linenumberError"] + ";\n"
                        : "") +
                    (context.Request["urlError"].Length > 0
                        ? "Url: " + HttpUtility.UrlDecode(context.Request["urlError"])
                        : ""),
                    Assembly.GetExecutingAssembly().GetName(),
                    "window.onError",
                    p));

                return;
            }

            control = context.Request.Form["control"] ??
                      context.Request.Form["control"] ?? context.Request.QueryString["callbackKey"];

            if (string.IsNullOrEmpty(callbackKey) || string.IsNullOrEmpty(control))
                throw new LogicalException("Ошибка передачи параметров",
                    "Не переданы обязательные параметры callbackKey|control",
                    Assembly.GetExecutingAssembly().GetName());

            var page = KescoHub.GetPage(callbackKey) as Page;
            if (page == null) throw new Exception("Ошибка -> " + callbackKey);

            if (!string.IsNullOrEmpty(clientName))
            {
                KescoHub.SendMessage(new SignalMessage
                {
                    PageId = callbackKey,
                    IsV4Script = true,
                    Message = $"<js>v4_clientName = '{clientName}'; v4_setToolTip();</js>"
                });
                return;
            }


            var value = context.Request.Form["value"];
            var valueText = context.Request.Form["valueText"];
            var escaped = context.Request.Form["escaped"];
            var multiReturn = context.Request.Form["multiReturn"];

            if (escaped.Equals("1"))
            {
                value = HttpUtility.UrlDecode(value);
                valueText = HttpUtility.UrlDecode(valueText ?? "");
            }

            if (value == null || value.Equals("0"))
            {
                if (!string.IsNullOrEmpty(control))
                    KescoHub.SendMessage(new SignalMessage
                    {
                        PageId = callbackKey,
                        IsV4Script = true,
                        Message =
                            $"<js>var suff= gi('{control}_0')!=null?'_0':''; $(window).focus(); setTimeout(function () {{$('#{control}' + suff).focus();}},10);</js>"
                    });
            }
            else
            {
                if (callbackKey == control && command.ToLower().Equals("checksimilar"))
                    KescoHub.SendMessage(new SignalMessage
                    {
                        PageId = callbackKey,
                        IsV4Script = true,
                        Message =
                            $"<js>$(window).focus(); v4_setIdEntity('{HttpUtility.JavaScriptStringEncode(value ?? "")}', '{HttpUtility.JavaScriptStringEncode(command ?? "CHECKSIMILAR")}');</js>"
                    });
                else

                    KescoHub.SendMessage(new SignalMessage
                    {
                        PageId = callbackKey,
                        IsV4Script = true,
                        Message =
                            $"<js>$(window).focus(); v4s_setSelectedValue('{control}', '{callbackKey}', JSON.parse('{HttpUtility.JavaScriptStringEncode(value ?? "")}'), '{(multiReturn == "2" ? "True" : "False")}');</js>"
                    });
            }
        }

        private void ProcessRequestByProxyWindow(HttpContext context)
        {
            var returnValue = HttpUtility.UrlDecode(context.Request.Form["value"]);

            context.Response.Write("<html>");
            context.Response.Write("<head>");
            context.Response.Write(
                string.Format(
                    "<script src='/Styles/Kesco.V4/JS{0}/jquery-1.12.4.min.js' type='text/javascript'></script>",
                    Config.versionV4js));
            context.Response.Write(string.Format(
                "<script src='/Styles/Kesco.V4/JS{0}/kesco.Dialog.js' type='text/javascript'></script>",
                Config.versionV4js));

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
        public bool IsReusable => false;

        #endregion
    }
}