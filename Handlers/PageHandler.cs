﻿using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.UI;
using Kesco.Lib.Entities.Corporate;
using Kesco.Lib.Localization;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Settings;
using Kesco.Lib.Web.SignalR;
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
                    p = context.Handler as Page;
                    if (p != null)
                    {
                        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture =
                            CorporateCulture.GetCorporateCulture(p.CurrentUser.Language);

                        p.Data["StartDate"] = DateTime.UtcNow;
                        p.Data["Url"] = context.Request.Url.PathAndQuery;
                        p.CurrentUser.Host = context.Server.MachineName;


                        p.IDPage = Guid.NewGuid().ToString();
                        p.V4Request = context.Request;
                        p.V4Response = context.Response;
                        
                        KescoHub.AddPage(p.IDPage, p);

                        var info =
                            $"{DateTime.Now:dd.MM.yy HH:mm:ss} -> В KescoHub зарегистрирована новая страница [{p.IDPage}]";
                        KescoHub.RefreshSignalViewInfo(new KescoHubTraceInfo {TraceInfo = info});
                    }

                    if (context.Handler != null) context.Handler.ProcessRequest(context);
                }
                else
                {
                    var qsidp = context.Request.QueryString["idp"];

                    p = KescoHub.GetPage(qsidp) as Page;

                    if (p == null)
                    {
                        RenderPageReload(context);
                        var info =
                            $"{DateTime.Now.ToString("dd.MM.yy HH:mm:ss")} -> Станица {qsidp} не найдена в KescoHub. Страница, отправившая запрос с левым idp - будут перезагружена";

                        KescoHub.RefreshSignalViewInfo(new KescoHubTraceInfo {TraceInfo = info});
                    }
                    else
                    {
                        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture =
                            CorporateCulture.GetCorporateCulture(p.CurrentUser.Language);

                        p.V4IsPostBack = true;
                        p.IDPostRequest = Guid.NewGuid().ToString();
                        p.V4Request = context.Request;
                        p.V4Response = context.Response;
                        p.ProcessRequest();

                        p.Flush();
                        var requestParams = GetRequestV4Params(context.Request.Params);
                        var info = $"{DateTime.Now:dd.MM.yy HH:mm:ss} -> Перехват события.{requestParams}";
                        KescoHub.RefreshSignalViewInfo(new KescoHubTraceInfo {TraceInfo = info});
                    }
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                var qsidp = context.Request.QueryString["idp"];
                if (!string.IsNullOrEmpty(qsidp))
                {
                    try
                    {
                        p = KescoHub.GetPage(qsidp) as Page;

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

                    var info =
                        $"{DateTime.Now:dd.MM.yy HH:mm:ss} -> На странице [{qsidp}] возникла ошибка {ex.Message}";
                    KescoHub.RefreshSignalViewInfo(new KescoHubTraceInfo {TraceInfo = info});
                }
                else
                {
                    Logger.WriteEx(ex);
                    RenderDialogException(sb, ex, false);
                    RenderErrorPage(context, sb);

                    var info = $"{DateTime.Now:dd.MM.yy HH:mm:ss} -> В приложении возникла ошибка {ex.Message}";
                    KescoHub.RefreshSignalViewInfo(new KescoHubTraceInfo {TraceInfo = info});
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
        public bool IsReusable => false;


        private string GetRequestV4Params(NameValueCollection requestParams)
        {
            var w = new StringWriter();
            w.Write(Environment.NewLine);
            foreach (string key in requestParams)
                switch (key.ToLower())
                {
                    case "idp":
                    case "cmd":
                    case "page":
                    case "ctrl":
                        w.Write($"{key}={requestParams[key]}; ");
                        break;
                }

            return w.ToString();
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
        public override IHttpHandler GetHandler(HttpContext context, string requestType, string virtualPath,
            string path)
        {
            if (context != null)
            {
                var idp = context.Request.QueryString["idp"];
                if (!string.IsNullOrEmpty(idp))
                    return (IHttpHandler) KescoHub.GetPage(idp);
            }

            return base.GetHandler(context, requestType, virtualPath, path);
        }


        private void RenderPageReload(HttpContext context)
        {
            var сEmployee = new Employee(true);
            var culture = "en";
            if (!сEmployee.Unavailable)
                culture = сEmployee.Language;

            Thread.CurrentThread.CurrentUICulture =
                CultureInfo.CreateSpecificCulture(culture);

            var sb = new StringBuilder();
            sb.Append("<f><js>");


            //var dex = new LogicalException(Resx.GetString("lSession"),
            //    Resx.GetString("lSession"),
            //    Assembly.GetExecutingAssembly().GetName(), Priority.ExternalError);
            //Logger.WriteEx(dex);

            sb.AppendFormat(
                @"v4_isConfirmDelete = false; location.href = location.href;");


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
            sb0.Append($"<script src='{Config.styles_js}jquery-1.12.4.min.js' type='text/javascript'></script>");
            sb0.Append($"<script src='{Config.styles_js}jquery-ui.js' type='text/javascript'></script>");
            sb0.AppendFormat($"<link href='{Config.styles_css}Kesco.css' rel='stylesheet' type='text/css'/>");
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
            sb.Append("$(\"body\").prepend(\"<div class='v4div-outer-container' id='v4DivErrorOuter'></div>\");");
            sb.Append(
                "$(\"#v4DivErrorOuter\").append(\"<div class='v4div-inner-container' id='v4DivErrorInner'></div>\");");
            sb.Append(
                "$(\"#v4DivErrorInner\").append(\"<div class='v4div-centered-content' style='width:500px;background:#D5D5D5;' id='v4DivErrorContent'>");

            sb.Append("<table style='width:499px'>");

            sb.Append("<tr id='v4TrErrorTitle'>");
            sb.Append("<td style='width:1px;cursor:move'>");
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
                errorMessage =
                    HttpUtility.JavaScriptStringEncode(HttpUtility.HtmlEncode(((DetailedException) ex).CustomMessage));
            else
                errorMessage =
                    HttpUtility.JavaScriptStringEncode(HttpUtility.HtmlEncode(ex.GetBaseException().Message));

            sb.Append(errorMessage);

            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.Append("<td>");

            sb.Append("<u id='v4BtnErrDetails' style='cursor:pointer;'>подробно об ошибке</u>");
            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.Append("<td colspan=2>");
            sb.Append(
                "<div id='v4DivStackTrace' style='display:none;background-color:#fafad2;height:150px; max-width:480px; overflow: auto'>");
            sb.Append("<pre>");

            try
            {
                var exception = ex as DetailedException;
                if (exception != null)
                    sb.Append(
                        HttpUtility.JavaScriptStringEncode(HttpUtility.HtmlEncode(exception.GetExtendedDetails())));
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