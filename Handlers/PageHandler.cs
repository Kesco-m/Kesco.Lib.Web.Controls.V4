using System;
using System.Globalization;
using System.Resources;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.UI;
using Kesco.Lib.Entities.Corporate;
using Kesco.Lib.Localization;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Controls.V4.Common;
using Page = Kesco.Lib.Web.Controls.V4.Common.Page;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    /// <summary>
    ///     Перехват запросов к странице
    /// </summary>
    public class PageHandler : PageHandlerFactory, IHttpHandler
    {
        /// <summary>
        ///     Локализация
        /// </summary>
        public ResourceManager Resx = Resources.Resx;

        /// <summary>
        ///     Разрешает обработку веб-запросов НТТР для пользовательского элемента HttpHandler, который реализует интерфейс
        ///     <see cref="T:System.Web.IHttpHandler" />.
        /// </summary>
        /// <param name="context">
        ///     Объект <see cref="T:System.Web.HttpContext" />, предоставляющий ссылки на внутренние серверные
        ///     объекты (например, Request, Response, Session и Server), используемые для обслуживания HTTP-запросов.
        /// </param>
        public void ProcessRequest(HttpContext context)
        {
            context.Response.CacheControl = "no-cache";
            context.Response.AppendHeader("X-UA-Compatible", "IE=Edge");

            Page p;

            try
            {
                if (string.IsNullOrEmpty(context.Request.QueryString["idp"]))
                {
                    context.Handler = base.GetHandler(context, context.Request.RequestType,
                        context.Request.CurrentExecutionFilePath, context.Request.PhysicalApplicationPath);
                    p = (context.Handler as Page);
                    if (p != null)
                    {
                        Thread.CurrentThread.CurrentUICulture =
                            CultureInfo.CreateSpecificCulture(p.CurrentUser.Language);

                        p.Data["StartDate"] = DateTime.UtcNow;
                        p.Data["Url"] = context.Request.Url.PathAndQuery;
                        p.CurrentUser.Host = context.Server.MachineName;
                        context.Application.Lock();
                        p.IDPage = Guid.NewGuid().ToString();
                        p.V4Request = context.Request;
                        p.V4Response = context.Response;

                        context.Application[p.IDPage] = p;
                        context.Application.UnLock();
                    }

                    if (context.Handler != null)
                    {

                        context.Handler.ProcessRequest(context);
                    }
                }
                else
                {
                    p = context.Application[context.Request.QueryString["idp"]] as Page;

                    if (p == null)
                    {
                        RenderPageReload(context);
                    }
                    else
                    {
                        Thread.CurrentThread.CurrentUICulture =
                            CultureInfo.CreateSpecificCulture(p.CurrentUser.Language);
                        p.V4IsPostBack = true;
                        p.V4Request = context.Request;
                        p.V4Response = context.Response;
                        p.ProcessRequest();
                        p.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                if (context.Request.QueryString["idp"] != null
                    && context.Request.QueryString["idp"] != "")
                {
                    try
                    {
                        p = context.Application[context.Request.QueryString["idp"]] as Page;
                        p.V4Request = context.Request;
                        p.V4Response = context.Response;
                        RenderDialogException(sb, ex, true);
                        p.JS.Write(sb.ToString());
                        p.Flush();
                    }
                    catch
                    {
                        Logger.WriteEx(ex);
                        RenderDialogException(sb, ex, false);
                        RenderErrorPage(context, sb);
                    }
                }
                else
                {
                    Logger.WriteEx(ex);
                    RenderDialogException(sb, ex, false);
                    RenderErrorPage(context, sb);
                }
            }
        }

        /// <summary>
        ///     Возвращает значение, позволяющее определить, может ли другой запрос использовать экземпляр класса
        ///     <see cref="T:System.Web.IHttpHandler" />.
        /// </summary>
        /// <returns>
        ///     Значение true, если экземпляр <see cref="T:System.Web.IHttpHandler" /> доступен для повторного использования; в
        ///     противном случае — значение false.
        /// </returns>
        public bool IsReusable
        {
            get { return false; }
        }

        /// <summary>
        ///     Возвращает экземпляр интерфейса <see cref="T:System.Web.IHttpHandler" /> для обработки требуемого ресурса.
        /// </summary>
        /// <returns>
        ///     Новый обработчик <see cref="T:System.Web.IHttpHandler" />, обрабатывающий запрос; в противном случае — null.
        /// </returns>
        /// <param name="context">
        ///     Экземпляр класса <see cref="T:System.Web.HttpContext" />, который предоставляет ссылки на
        ///     внутренние объекты сервера (например, Запрос, Ответ. Сеанс и Сервер), используемые для обслуживания HTTP-запросов.
        /// </param>
        /// <param name="requestType">Метод http для передачи данных (GET или POST), используемый клиентом.</param>
        /// <param name="virtualPath">Виртуальный путь к требуемому ресурсу.</param>
        /// <param name="path">Свойство <see cref="P:System.Web.HttpRequest.PhysicalApplicationPath" /> требуемого ресурса.</param>
        public override IHttpHandler GetHandler(HttpContext context, string requestType, string virtualPath, string path)
        {
            if (context != null && !string.IsNullOrEmpty(context.Request.QueryString["idp"]))
                return (IHttpHandler) context.Application[context.Request.QueryString["idp"]];
            return base.GetHandler(context, requestType, virtualPath, path);
        }

      
        private void RenderPageReload(HttpContext context)
        {
            Employee сEmployee = new EmployeeCurrent();
            var culture = "en";
            if (!сEmployee.Unavailable)
                culture = сEmployee.Language;

            Thread.CurrentThread.CurrentUICulture =
                CultureInfo.CreateSpecificCulture(culture);

            var sb = new StringBuilder();
            sb.Append("<f><js>");

            sb.AppendFormat(
                @"v4_isConfirmDelete = true; ConfirmReload.render('{0}', '{1}', '{2}', '{3}', 'false');",
                Resx.GetString("msgOsnAttention0"), Resx.GetString("lSession"), Resx.GetString("lCont"),
                Resx.GetString("lEnd"));

            sb.Append("</js></f>");
            context.Response.Write(sb.ToString());
        }

        private void RenderErrorPage(HttpContext context, StringBuilder sb)
        {
            var sb0 = new StringBuilder();

            //sb0.Append("<!DOCTYPE>");
            sb0.Append("<html>");
            sb0.Append("<head>");
            sb0.Append("<title>");
            sb0.Append("Ошибка!");
            sb0.Append("</title>");
            sb0.Append("<script src='/Styles/Kesco.V4/JS/jquery-1.11.3.min.js' type='text/javascript'></script>");
            sb0.Append("<script src='/Styles/Kesco.V4/JS/jquery-ui.js' type='text/javascript'></script>");
            sb0.Append("<script src='/Styles/Kesco.V4/JS/Kesco.Confirm.js' type='text/javascript'></script>");
            sb0.Append("<link href='/Styles/Kesco.V4/CSS/Kesco.V4.css' rel='stylesheet' type='text/css'/>");
            sb0.Append("</head>");
            sb0.Append("<body>");

            sb0.Append("</body>");
            sb0.Append("<script>");

            sb0.Append(sb);

            sb0.Append("</script>");
            sb0.Append("</html>");

            context.Response.Write(sb0.ToString());
            context.Response.End();
        }

        private void RenderDialogException(StringBuilder sb, Exception ex, bool close)
        {
            sb.Append("Wait.render(false);");//Восстановление курсора по-умолчанию после асинхронного вызова

            sb.Append("$(\"body\").prepend(\"<div class='v4div-outer-container' id='v4DivErrorOuter'></div>\");");
            sb.Append(
                "$(\"#v4DivErrorOuter\").append(\"<div class='v4div-inner-container' id='v4DivErrorInner'></div>\");");
            sb.Append(
                "$(\"#v4DivErrorInner\").append(\"<div class='v4div-centered-content' style='width:500px;background:#D5D5D5;' id='v4DivErrorContent'>");

            sb.Append("<table style='width:499px'>");

            sb.Append("<tr id='v4TrErrorTitle'>");
            sb.Append("<td style='width:1px;cursor:move''>");
            sb.Append("<b>Ошибка!</b>");
            sb.Append("</td>");
            sb.Append("<td style='text-align:right;cursor:move'>");
            if (close) sb.Append("<img id='v4BtnErrClose' style='cursor:pointer' src='/styles/cancel.gif'>");
            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.Append("<td rowspan=2>");
            sb.Append("<img src='/styles/ErrorHand.gif' border=0>");
            sb.Append("</td>");
            sb.Append("<td>");

            string errorMessage;
            if (ex is DetailedException)
                errorMessage = HttpUtility.JavaScriptStringEncode(HttpUtility.HtmlEncode(((DetailedException)ex).CustomMessage));
            else
                errorMessage = HttpUtility.JavaScriptStringEncode(HttpUtility.HtmlEncode(ex.GetBaseException().Message));
            
            sb.Append(errorMessage);

            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.Append("<td>");

            sb.Append("<u id='v4BtnErrDetails' style='cursor:pointer;'>подробно об ошибке</u>");
            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.Append("<td style='font-size:7pt;' colspan=2>");
            sb.Append(
                "<div id='v4DivStackTrace' style='display:none;background-color:#fafad2;height:150px; max-width:480px; overflow: auto'>");
            sb.Append("<pre>");

            try
            {
                var exception = ex as DetailedException;
                if (exception != null)
                    sb.Append(HttpUtility.JavaScriptStringEncode(HttpUtility.HtmlEncode(exception.GetExtendedDetails())));
                else
                    sb.Append(HttpUtility.JavaScriptStringEncode(ex.GetBaseException().StackTrace));
            }
            catch
            {
            }
            sb.Append("</pre>");
            sb.Append("</div>");

            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("</table>");
            sb.Append("</div>\");");

            sb.Append(
                "$(\"#v4BtnErrDetails\").click(function() {var v4ErrObj=$('#v4DivStackTrace'); if (v4ErrObj && v4ErrObj.css('display')=='block') v4ErrObj.css('display','none'); else v4ErrObj.css('display','block');});");
            sb.Append("$(\"#v4BtnErrClose\").click(function() {$('#v4DivErrorOuter').remove();});");
            sb.Append("$(\"#v4DivErrorContent\").draggable({handle: '#v4TrErrorTitle', containment: 'document'});");
        }
    }
}