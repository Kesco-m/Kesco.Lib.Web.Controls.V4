using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.BindModels;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Entities;
using Kesco.Lib.Web.Comet;

namespace Kesco.Lib.Web.Controls.V4.Common
{
    /// <summary>
    ///     Базовый класс для стриниц сущностей
    ///     Все страницы, работающие с функционалом V4 и сущностями(Entities) наследуют от него.
    ///     Является абстрактным, т.к. содерщит свойства, которые обязан переопределить на странице
    /// </summary>
    public abstract class EntityPage : Page
    {
        /// <summary>
        ///     URL страницы
        /// </summary>
        protected string CurrentUrl{get; set;}

        protected void SetCurrentUrlParams(Dictionary<string, object> parameters)
        {
            if (parameters.Count == 0) return;
            var ub = new UriBuilder(CurrentUrl);
            var qs = HttpUtility.ParseQueryString(ub.Query);
            foreach (var p in parameters)
                qs.Set(p.Key, p.Value.ToString());

            ub.Query= string.Join("&",
                qs.AllKeys.Select(a => a + "=" + HttpUtility.UrlEncode(qs[a])));
            CurrentUrl = ub.Uri.AbsoluteUri;

        }

        /// <summary>
        /// Элемент управления для контроля совместной работы над сущностью
        /// </summary>
        protected Comet _cometUsers;

        /// <summary>
        ///     Конструктор
        /// </summary>
        protected EntityPage()
        {
            IsEditable = true;
        }

        /// <summary>
        ///     сущность страницы
        /// </summary>
        public Entity Entity;

        /// <summary>
        ///     идентификатор сущности страницы
        /// </summary>
        protected string id;

        /// <summary>
        ///     Первоначальное состояние сущности
        /// </summary>
        protected Entity OriginalEntity;

           /// <summary>
           /// Инициализация формы
           /// </summary>
           /// <param name="e">Аргументы</param>
        protected override void OnInit(EventArgs e)
        {
            if (RedirectPageByCondition()) return;

            CurrentUrl = Request.Url.AbsoluteUri;

            base.OnInit(e);

            if (!V4IsPostBack)
            {
                id = Request.QueryString["id"];
            }

            if (!V4IsPostBack) EntitySyncModifiedData();

            EntityInitialization();
            EntityFieldInit();
        }
        /// <summary>
        /// Синхронизация несохраненных изменений, сделанных другими пользователями в текущей сущности
        /// </summary>
        public void EntitySyncModifiedData()
        {
            if (ItemId != 0)
            {
                var list = CometServer.Connections.FindAll(u => u.Id.ToString() == EntityId && u.Name == EntityName && u.ClientGuid != IDPage && u.IsModified).AsEnumerable().OrderBy(o => o.Start);
                if (list.Any())
                {
                    var p = Application[list.First().ClientGuid] as EntityPage;
                    if (p != null) Entity = p.Entity;

                }
            }
        }

        /// <summary>
        ///     Инициализация конкретного типа сущности
        /// </summary>
        protected abstract void EntityInitialization(Entity copy = null);


        /// <summary>
        ///     Загружает данные связаные с текущей сущностью
        /// </summary>
        protected virtual void EntityLoadData(string idEntity)
        {
        }

        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="cmd">Команды</param>
        /// <param name="param">Параметры</param>
        protected override void ProcessCommand(string cmd, NameValueCollection param)
        {
            switch (cmd)
            {
                case "Refresh":
                    if (Entity.IsModified)
                    {
                        // Изменения не сохранены! Вы действительно хотите заново загрузить данные?
                        ShowConfirm(Resx.GetString("Confirm_msgRefreshNoSave"),
                        Resx.GetString("errDoisserWarrning"),
                        Resx.GetString("CONFIRM_StdCaptionYes"),
                        Resx.GetString("CONFIRM_StdCaptionNo"),
                        "cmdasync('cmd', 'RefreshNoSave');", null, null);
                    }
                    else
                    {
                        RefreshPage();
                    }
                    break;
                case "RefreshNoSave":
                    var list = CometServer.Connections.FindAll(u => u.Id.ToString() == EntityId && u.Name == EntityName && u.ClientGuid != IDPage && u.IsModified);
                    if (list.Any())
                    {

                        var clientGUIDList = list.Select(s => s.ClientGuid).ToList();
                        var clientList = new List<string>();
                        foreach (var user in clientGUIDList)
                        {
                            var p = Application[user] as Page;
                            clientList.Add(p == null ? "<не определен>" : p.CurrentUser.FIO);
                        }

                        // Данную форму сейчас редактируют сотрудники: {0}.
                        // В результате повторной загрузки данных все изменения сделанные ими на своих формах будут отменены.
                        // Выполнить обновление данных формы?
                        ShowConfirm(String.Format(Resx.GetString("Confirm_msgRefreshNoSaveUsers"), String.Join(",", clientList.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct())),
                            Resx.GetString("errDoisserWarrning"),
                            Resx.GetString("CONFIRM_StdCaptionYes"),
                            Resx.GetString("CONFIRM_StdCaptionNo"),
                            "cmdasync('cmd', 'RefreshNoUsersSave');", null, null);
                    }
                    else
                    {
                        RefreshPage();
                    }
                    break;
                case "RefreshNoUsersSave":
                    RefreshPage();
                    break;
                default:
                    base.ProcessCommand(cmd, param);
                    break;
            }

            //if (ItemId != 0)
            //    TranslatePageEvent(V4Request.Params);
        }

        public override void TranslatePageEvent(NameValueCollection Params)
        {
            var cmd = Params["cmd"];
            var m = new CometMessage
            {
                ClientGuid = IDPage,
                IsV4Script = true,
                Message = "<js>cmdasync();</js>",
                Status = 0,
                UserName = ""
            };

            Predicate<CometAsyncState> pred = (client) =>
            {
                if (client.Page == null) return false;
                if (client.Page == this) return false;
                
                if (client.Id != ItemId) return false;
                if (((Page)client.Page).IDPage == IDPage) return false;

                var p = client.Page as EntityPage;

                switch (cmd)
                {
                    case "RefreshForce":
                        ClearCacheObjects();
                        client.Start = DateTime.MinValue;
                        if (p != null)
                        {
                            //client.CompleteRequest();
                            p.JS.Write("location.href='{0}';", CurrentUrl);
                            //p.ShowMessage("!!!");
                        }

                        break;
                }
                return true;
            };

            CometServer.PushMessage(m, pred);
            CometServer.Process();
        }

        /// <summary>
        ///    Обновление страницы с повторной загрузкой данных из БД
        /// </summary>
        /// <returns></returns>
        protected void RefreshPage()
        {
            var connList = CometServer.Connections.FindAll(u =>
                u.Id.ToString() == EntityId && u.Name == EntityName && u.ClientGuid != IDPage);
            var hasOtherModified = false;
            foreach (var conn in connList)
            {
                if (!conn.IsModified) continue;
                hasOtherModified = true;
                conn.IsModified = false;
            }

            if (hasOtherModified) TranslatePageEvent(new NameValueCollection { { "cmd", "RefreshForce" } });

            V4Navigate(CurrentUrl);
        }

        ///// <summary>
        ///// Событие изменения сатуса страницы
        ///// </summary>
        //public override void EditingStatusChanged()
        //{
        //    if (Entity != null) Entity.IsModified = true;
        //}

        protected virtual void EntityFieldInit()
        {
            if (Entity != null)
            {
                foreach (var field in Entity.GetType().GetFields())
                {
                    if (field.FieldType.Name != "BinderValue") continue;
                    var method = field.FieldType.GetMethod("ValueChangedEvent_Invoke", new Type[] { typeof(string), typeof(string) });
                    if (method != null)
                    {
                        method.Invoke(((BinderValue)field.GetValue(Entity)), new object[] { ((BinderValue)field.GetValue(Entity)).Value, "" });
                    }
                }

                //var saveButton = MenuButtons?.Find(mb => mb.ID == "btnSave");
                //if (saveButton != null && Entity != null)
                //{
                //    saveButton.IsDisabled = !Entity.IsModified;
                //}
                var applyButton = MenuButtons.Find(mb => mb.ID == "btnApply");
                if (applyButton != null && Entity != null)
                {
                    applyButton.IsDisabled = !Entity.IsModified;
                }

            }
        }

        /// <summary>
        ///     Id сущности (если нужно int значение используй ItemId)
        /// </summary>
        public string EntityId
        {
            get { return ItemId.ToString(); }
            set { ItemId = value.ToInt(); }
        }

        /// <summary>
        /// Название сущности (свойство добавлено для поддержания единого подхода, можно использовать ItemName)
        /// </summary>
        public string EntityName
        {
            get { return ItemName; }
            set { ItemName = value; }
        }

        /// <summary>
        /// Переадресация страницы на указанную
        /// </summary>
        /// <param name="path">Путь для переадресации</param>
        /// <param name="withQueryParams">Добавлять к пути параметры текущей страницы</param>
        public void V4Redirect(string path, bool withQueryParams = true)
        {
            V4Dispose(true);
            Response.Redirect(path + (withQueryParams ? Request.Url.Query : ""), false);
            Response.Flush();
            Response.SuppressContent = true;
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            
        }

        /// <summary>
        ///     Освобождение ресурсов занятых страницей. Блокирует объект Application на время операции
        /// </summary>
        public override void V4Dispose(bool redirect = false)
        {
            if (_cometUsers != null)
                _cometUsers.DisposeComet();

            base.V4Dispose(redirect);
        }

        /// <summary>
        ///     Отрисовка контрола для совместной работы
        /// </summary>
        protected void RenderCometControl(StringWriter w)
        {
            _cometUsers = new Comet(this)
            {
                ID = "v4_cometUsersList",
                HtmlID = "v4_cometUsersList"
            };

            V4Controls.Add(_cometUsers);
            _cometUsers.RenderControl(w);
        }

        protected override void RenderAddControl(StringWriter w)
        {
            if (MenuButtons.Count > 0)
            {
                if (ItemId > 0)
                    RenderCometControl(w);
            }
        }

        /// <summary>
        ///     Сформировать кнопки меню
        /// </summary>
        public override void RenderButtons(StringWriter w)
        {
            base.RenderButtons(w);
        }

        /// <summary>
        ///     Возвращает разметку контрола label для отображения локальной даты
        /// </summary>
        /// <param name="dateTime">Дата и время для отображения</param>
        /// <param name="formatDateTime">Формат отображения даты</param>
        /// <param name="attributes">Атрибуты для отображаемого label</param>
        public string GetHtmlDateTimeLabel(DateTime dateTime, string formatDateTime = null, string attributes = null)
        {
            var formatedDateTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            return String.Format("<label class=localDT {0} {2}>{1}</label>",
                !String.IsNullOrEmpty(formatDateTime) ? String.Format("dtformat='{0}'", formatDateTime) : "",
                formatedDateTime, !String.IsNullOrEmpty(attributes) ? attributes : "");
        }

        /// <summary>
        ///     Возвращает разметку контрола label для отображения локальной даты
        /// </summary>
        /// <param name="dateTime">Дата и время для отображения</param>
        /// <param name="withoutTime">Отображать дату с/без даты</param>
        /// <param name="attributes">Атрибуты для отображаемого label</param>
        public string GetHtmlDateTimeLabel(DateTime dateTime, bool withoutTime, string attributes = null)
        {
            var formatedDateTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            return String.Format("<label class=localDT {0} {2}>{1}</label>", withoutTime ? "dtformat=dd.mm.yyyy" : "",
                formatedDateTime, !String.IsNullOrEmpty(attributes) ? attributes : "");
        }

        /// <summary>
        /// Переопределяемый метод присвоения идентификатора сущности
        /// </summary>
        /// <param name="id">Новый идентификатор сущности</param>
        /// <param name="command">Команда на присвоение</param>
        protected virtual void SetIdEntity(string id, string command)
        {
        }

        public override void ProcessRequest()
        {
            base.ProcessRequest();

            if (MenuButtons != null)
            {
                //var saveButton = MenuButtons.Find(mb => mb.ID == "btnSave");
                //if (saveButton != null && Entity != null)
                //{
                //    saveButton.IsDisabled = !Entity.IsModified;
                //}
                var applyButton = MenuButtons.Find(mb => mb.ID == "btnApply");
                if (applyButton != null && Entity != null)
                {
                    applyButton.IsDisabled = !Entity.IsModified;
                }
            }

            if (V4Request.Params.Count == 0)
            {
                return;
            }
            var key = V4Request.Params.Keys[1];
            switch (key)
            {
                case "setIdEntity":
                    SetIdEntity(V4Request.Params["setIdEntity"], V4Request.Params["command"]);
                    break;
            }
        }

        public virtual void CloseJQueryDialogForm(string idContainer)
        {
        }
    }
}