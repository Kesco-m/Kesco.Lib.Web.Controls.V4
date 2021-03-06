﻿using System;
using System.Web;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Controls.V4.Common;
using Kesco.Lib.Web.Settings;

namespace Kesco.Lib.Web.Controls.V4.Globals
{
    /// <summary>
    ///     Базовый класс для Global.asax
    /// </summary>
    public abstract class GlobalBase : HttpApplication
    {
        public static string ToolTip = Config.contacts;
        public static string Caller = Config.contacts_caller;
        public static bool Redirect = true;

        /// <summary>
        ///     Текущий домен приложения
        /// </summary>
        public static string Domain
        {
            get
            {
                var d = Config.domain;
                if (string.IsNullOrEmpty(d))
                    throw new Exception("В конфигурационном файле не указан параметр Domain");

                return d;
            }
        }

        /// <summary>
        ///     Процедура, вызывается автоматически системой один раз при старте приложения
        /// </summary>
        /// <param name="sender">Текущее приложение</param>
        /// <param name="e">Параметры</param>
        protected virtual void Application_Start(object sender, EventArgs e)
        {
            PageManager.Start();

            var log = new LogModule(Config.appName);
            log.Init(Config.smtpServer, Config.email_Support);
            log.OnDispose += log_OnDispose;
            Logger.Init(log);
        }

        /// <summary>
        ///     Процедура, вызывается автоматически системой один раз при выгрузке приложения
        /// </summary>
        /// <param name="sender">Текущее приложение</param>
        /// <param name="e">Параметры</param>
        protected virtual void Application_End(object sender, EventArgs e)
        {
        }

        private void AddOriginHeader()
        {
            var rq = HttpContext.Current?.Request;
            if (rq == null) return;
            var origin = rq.Headers["Origin"];
                        
            if (string.IsNullOrEmpty(origin))
                HttpContext.Current?.Response?.AddHeader("Access-Control-Allow-Origin", "*");
            else
            {
                HttpContext.Current?.Response?.AddHeader("Access-Control-Allow-Origin", origin);
                HttpContext.Current?.Response?.AddHeader("Vary", "Origin");                 
            }
        }


        /// <summary>
        ///     Вызывается первым каждый раз при получении нового запроса от пользователя.
        ///     Осуществляет перенаправление запроса на полное имя (server.domain).
        ///     Это необходимо для поддержания совместимости с приложениями V3, работающими с куками
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Параметры</param>
        protected virtual void Application_BeginRequest(object sender, EventArgs e)
        {
            AddOriginHeader();
            HttpContext.Current?.Response?.AddHeader("Access-Control-Allow-Credentials", "true");

            if (Request.Url.Host.IndexOf(Domain, StringComparison.Ordinal) != -1) return;

            var uriBuilder = new UriBuilder(Request.Url);
            var authority = HttpContext.Current?.Request.Url.Authority;
            if (string.IsNullOrEmpty(authority)) return;

            if (authority == "localhost" || authority?.IndexOf(":", StringComparison.InvariantCulture) > -1)
                authority = Server.MachineName;
            
            if (Redirect)
            {
                uriBuilder.Host = authority + "." + Domain;
                Response.Redirect(uriBuilder.Uri.ToString(), false);
            }
        }

        /// <summary>
        ///     Процедура глобального перехвата исключительных ситуаций.
        ///     Получает описание последней ошибки в объекте Server, записывает ошибку в Application и отсылает в службу поддержки
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Параметры</param>
        protected virtual void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            if (Application["Error"] == null)
                Application["Error"] = ex;
            Logger.WriteEx(ex);
        }

        /// <summary>
        ///     Вызывается после аутентификации пользователя (установления его виртуальной личности, включая статус
        ///     "незарегистрированный пользователь").
        ///     Данный метод можно использовать для проведения дополнительных проверок его статуса.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Параметры</param>
        protected virtual void Application_AuthenticateRequest(object sender, EventArgs e)
        {
        }

        /// <summary>
        ///     Вызывается единожды для каждого клиента (посетителя) в начале его сессии.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Параметры</param>
        protected virtual void Session_Start(object sender, EventArgs e)
        {
        }

        /// <summary>
        ///     Вызывается единожды для каждого клиента (посетителя) при завершении его сессии (закрытии или истечении времени
        ///     таймаута).
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Параметры</param>
        protected virtual void Session_End(object sender, EventArgs e)
        {
        }

        /// <summary>
        ///     Реинициализация Log.dll, вызывается из сборки Log.dll в случае неудачной отправки сообщения в службу поддержки
        /// </summary>
        /// <param name="sender">Экземпляр обяъекта Log</param>
        private void log_OnDispose(LogModule sender)
        {
            sender.Init(Config.smtpServer, Config.email_Support);
        }
    }
}