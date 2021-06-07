using System;
using System.Linq;
using System.Web;
using Kesco.Lib.Entities.Corporate;
using Kesco.Lib.Web.Controls.V4.Common;
using Kesco.Lib.Web.Settings;
using Kesco.Lib.Web.SignalR;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    public class SignalRHubHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.CacheControl = "no-cache";

            var appRoot = Page.GetWebAppRoot();
            var cuser = new Employee(true);
            var w = context.Response;
            w.Write(Environment.NewLine);
            w.Write("<!DOCTYPE html>");
            w.Write(Environment.NewLine);
            w.Write("<html>");
            w.Write(Environment.NewLine);
            w.Write("<head>");
            w.Write(Environment.NewLine);
            w.Write("<title>Активные соединения</title>");
            w.Write(Environment.NewLine);
            w.Write("<meta http-equiv='X-UA-Compatible' content='IE=edge'/>");
            w.Write(Environment.NewLine);
            w.Write($"<link href='{Config.styles_css}Kesco.css' rel='stylesheet' type='text/css'/>");
            w.Write(Environment.NewLine);
            w.Write($"<script src='{Config.styles_js}jquery-1.12.4.min.js' type ='text/javascript'></script>");
            w.Write(Environment.NewLine);
            w.Write(
                $"<script src='{Config.styles_js}jquery.signalR-2.4.1.min.js' type ='text/javascript'></script>");
            w.Write(Environment.NewLine);
            w.Write($"<script src='{Config.styles_js}kesco.js' type ='text/javascript'></script>");

            w.Write(Environment.NewLine);
            w.Write($"<script src='{appRoot}/signalr/hubs' type ='text/javascript'></script>");
            w.Write(Environment.NewLine);

            w.Write(@"<script>");
            w.Write(Environment.NewLine);
            w.Write($"var v4_userId = {cuser.Id}");
            w.Write(Environment.NewLine);
            w.Write(@"
var v4_kescoHub = null;
var v4_tryingToReconnect = false;
$(document).ready(function () {
 
    v4_kescoHub = $.connection.kescoSignalHub;
    registerClientMethods();

    $.connection.hub.connectionSlow = function () { 
       // console.log('[' + (new Date()).toLocaleTimeString() + '] KescoHub Медленное подключение'); 
    };

     $.connection.hub.reconnecting(function() {
                v4_tryingToReconnect = true;
                //console.log('[' + (new Date()).toLocaleTimeString() + '] KescoHub Восстановление соединения');
                var msg = 'Связь с сервером потеряна!';                
                $('#divConnections').html('<strong>' + msg + '</strong>');                
                $('#divAllConnections').html('');                
                setTimeout(v4_brokenConnection, 15000);
            });

    $.connection.hub.reconnected(function() {
        v4_tryingToReconnect = false;
        // console.log('[' + (new Date()).toLocaleTimeString() + '] KescoHub Соединение восстановлено');
        location.href = location.href;
    });


    $.connection.hub.disconnected(function() {
        // console.log('[' + (new Date()).toLocaleTimeString() + '] KescoHub Подключение потеряно');
            if ($.connection.hub.lastError) {
                // console.log('[' + (new Date()).toLocaleTimeString() + '] KescoHub Ошибка:' + $.connection.hub.lastError.message);
            }

            if (!v4_tryingToReconnect)
            {
                setTimeout(function() {
                    $.connection.hub.start().done(function() {
                        // console.log('[' + (new Date()).toLocaleTimeString() + '] KescoHub Попытка восстановления подключения');
                    }).fail(function() { 
                        // console.log('[' + (new Date()).toLocaleTimeString() + '] KescoHub Ошибка при попытке восстановления подключения'); 
                    });

                }, 10);
            }
        });

    $.connection.hub.stateChanged = function() { 
        // console.log('[' + (new Date()).toLocaleTimeString() + '] KescoHub Изменение состояние подключения') 
    };

    // $.connection.hub.logging = true;

    $.connection.hub.start().done(function ()
    {
        // console.log('[' + (new Date()).toLocaleTimeString() + '] KescoHub Соединение установлено'); 
    }).fail(function () { 
        // console.log('[' + (new Date()).toLocaleTimeString() + '] KescoHub Ошибка установке соединения'); 
    });
   
 
});

function registerClientMethods() {
     v4_kescoHub.client.onPageConnected = function () {            
            v4_kescoHub.server.onPageRegistered('signalview','',v4_userId,'','','0','signalview_ashx', '', false,false);        
    }

    v4_kescoHub.client.refreshActivePagesInfo = function (pages) {        
        v4_refreshListActivePages(pages);
    }

    v4_kescoHub.client.refreshSignalViewInfo = function (info) {        
        $('#divParamsInApp').html(info.CountPages);        
        var trace = $('#divTrace').html();
        if (trace.length > 4000) trace = '';
        $('#divTrace').html(info.TraceInfo + trace);
    }
}
   
function v4_refreshListActivePages(pages){
     
        var cnt = pages.length;
        $('#divAllConnections').html(""Всего активных соединений: "" + cnt);
        var tblConnections = ""<table class='grid'>"";
        tblConnections +=""<thead>"";

        tblConnections +=""<tr class='gridHeader'>"";
        tblConnections +=""<td>Транспорт</td>"";   
        tblConnections +=""<td>Пользователь</td>"";
        tblConnections +=""<td>Идентификатор страницы</td>"";
        tblConnections +=""<td>Страница</td>"";
        tblConnections +=""<td>Код объекта</td>"";
        tblConnections +=""<td>Что делает</td>"";
        tblConnections +=""<td>Когда подключился</td>"";
        tblConnections +=""</tr>"";

        tblConnections +=""</thead>"";

        tblConnections +=""<tbody>"";
        $.each(pages,
        function() { 
            
            tblConnections +=""<tr>"";

            tblConnections +=""<tr>"";
            tblConnections +=""<td>"" + this.TransportSignalR + ""</td>"";
            tblConnections +=""<td>"" + this.UserName + ""</td>"";
            tblConnections +=""<td>"" + this.PageId + ""</td>"";
            tblConnections +=""<td>"" + this.ItemName + ""</td>"";
            tblConnections +=""<td>"" + this.EntityId + ""</td>"";
            tblConnections +=""<td>"" + (this.IsEditable?""Редактирует"":""Просматривает"") + ""</td>"";
            tblConnections +=""<td>"" + Kesco.toLocalTime(this.StartTimeFormat) + ""</td>"";

            tblConnections +=""</tr>"";             

        });
        tblConnections +=""</tbody>"";
        if (cnt>0) {                                                            
            tblConnections += '</table>';                    
        } else {
            tblConnections="""";                    
        }

        $('#divConnections').html(tblConnections);
        
}

function v4_brokenConnection() {
    if (window.status == v4_signalStatus)
    {
        v4_tryingToReconnect = true;
        $.connection.hub.stop();
        v4_hubImpossibleConnect();
    }
}
");
            w.Write(Environment.NewLine);
            w.Write("</script>");
            w.Write(Environment.NewLine);

            w.Write("</head>");
            w.Write(Environment.NewLine);
            w.Write("<body style=\"margin:8px;\">");
            w.Write(Environment.NewLine);
            w.Write("<div id='divConnections'></div>");
            w.Write("<div id='divAllConnections'></div>");


            //#########################################################################

            var info =
                $"{DateTime.Now:dd.MM.yy HH:mm:ss} -> При загрузке получили актуальную информацию о состоянии KescoHub";
            var cnt = KescoHub.GetAllPages().Count();

            w.Write(
                $"<br/><div class=\"v4DivInline\">Страниц в KescoHub:&nbsp;</div><div class=\"v4DivInline\" id=\"divParamsInApp\">{cnt}</div>");
            w.Write($"<div>Действия: </div><div id=\"divTrace\"><div>{info}</div></div>");


            if (context.Application.AllKeys.Contains("Error"))
            {
                // эти ошибки ловятся в Global.asax в методе Application_Error
                w.Write("</br>");
                w.Write("</br>");
                w.Write("<div>В Application обнаружен объект ошибки:</div>");
                var exObj = context.Application["Error"] as Exception;
                if (exObj != null)
                {
                    w.Write(exObj.Message);
                    w.Write(Environment.NewLine);
                    w.Write(exObj.StackTrace);
                }
            }

            //#########################################################################

            w.Write(Environment.NewLine);
            w.Write("</body>");
            w.Write(Environment.NewLine);
            w.Write("</html>");
        }

        public bool IsReusable => false;
    }
}