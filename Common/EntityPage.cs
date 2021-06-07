using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.BindModels;
using Kesco.Lib.Entities;
using Kesco.Lib.Web.Controls.V4.Common.DocumentPage;
using Kesco.Lib.Web.SignalR;

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
        ///     Коллекция для сохранения состояний между post запросами
        /// </summary>
        private readonly NameValueCollection sParam = new NameValueCollection();

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
        ///     Элемент управления для контроля совместной работы над сущностью
        /// </summary>
        protected SignalR SignalRUsers;

        /// <summary>
        ///     Конструктор
        /// </summary>
        protected EntityPage()
        {
            IsEditable = true;
        }


        /// <summary>
        ///     Id сущности (если нужно int значение используй ItemId)
        /// </summary>
        public string EntityId
        {
            get => ItemId.ToString();
            set => ItemId = value.ToInt();
        }

        /// <summary>
        ///     Название формы сущности (свойство добавлено для поддержания единого подхода, можно использовать ItemName)
        /// </summary>
        public string EntityPageName
        {
            get => ItemName;
            set => ItemName = value;
        }


        /// <summary>
        ///     Инициализация формы
        /// </summary>
        /// <param name="e">Аргументы</param>
        protected override void OnInit(EventArgs e)
        {
            if (RedirectPageByCondition()) return;

            base.OnInit(e);

            if (!V4IsPostBack)
            {
                id = Request.QueryString["id"];
                EntitySyncModifiedData();
            }

            EntityInitialization();
            
            if (!(this is DocPage) && (Entity == null || string.IsNullOrEmpty(Entity.Id) && !string.IsNullOrEmpty(id))) EntityLoadData(id);
            EntityFieldInit();
        }

        /// <summary>
        ///     Синхронизация несохраненных изменений, сделанных другими пользователями в текущей сущности
        /// </summary>
        private void EntitySyncModifiedData()
        {
            if (string.IsNullOrEmpty(EntityId)) return;

            var pages = KescoHub.GetAllPages().ToList();
            pages.ForEach(x =>
            {
                if (!(x is EntityPage)) return;
                var ep = (EntityPage) x;
                if (ep.Entity == null) return;

                if (ep.EntityId == EntityId && ep.EntityPageName == EntityPageName && ep.IDPage != IDPage && ep.Entity.IsModified)
                {
                    Entity = ep.Entity;
                    OriginalEntity = ep.OriginalEntity;
                    return;
                }
            });
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
            if (string.IsNullOrEmpty(idEntity) || idEntity == "0") return;
            if (Entity == null || Entity.IsMultiId) return;

            Entity.Id = idEntity;
            Entity.Load();
        }

        /// <summary>
        ///     Процедура перепроверки введенных данных
        /// </summary>
        protected virtual void RefreshNotification()
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
                case "RefreshNotification":
                    RefreshNotification();
                    break;
                case "Cancel":
                    if (Entity != null && (Entity.IsModified))
                    {
                        ShowConfirm(Resx.GetString("Confirm_msgCancel"),
                            Resx.GetString("errDoisserWarrning"),
                            Resx.GetString("QSBtnYes"),
                            Resx.GetString("QSBtnNo"),
                            "cmdasync('cmd', 'CancelYes');",
                            null, null);
                    }
                    else
                    {
                        V4DropWindow();
                    }
                    break;
                case "CancelYes":
                    V4DropWindow();
                    break;
                case "Refresh":

                    //если экземпляр сущности новый, перепроверяем данные
                    if (Entity != null && Entity.IsNew)
                    {
                        RefreshNotification();
                        break;
                    }

                    // если объект изменили, то спрашиваем
                    if (Entity != null && Entity.IsModified)
                    {
                        ShowConfirm(Resx.GetString("Confirm_msgRefreshNoSave"),
                            Resx.GetString("errDoisserWarrning"),
                            Resx.GetString("Confirm_msgRefresh_captionYes"),
                            Resx.GetString("Confirm_msgRefresh_captionNo"),
                            "cmdasync('cmd', 'RefreshNoSave');",
                            "cmdasync('cmd', 'RefreshNotification');",
                            null, null, null, 407, null, false, false,
                            Resx.GetString("Confirm_msgRefresh_captionCancel"), "");

                        break;
                    }

                    RefreshPage();
                    break;
                case "RefreshNoSave":
                    var userList = new List<string>();
                    var cuId = CurrentUser.Id;
                    var pages = KescoHub.GetAllPages().ToList();
                    pages.ForEach(p =>
                    {
                        if (p == null) return;
                        if (!(p is EntityPage)) return;
                        var ep = (EntityPage) p;
                        var epCuId = ep.CurrentUser.Id;
                        //todo: Добавить проверку на наличие изменений в объекте
                        if (cuId == epCuId || ep.ItemId == 0 || ep.ItemId != ItemId || ep.ItemName != ItemName ||
                            ep.IDPage == IDPage) return;
                        userList.Add(ep.CurrentUser.FIO);
                    });

                    if (userList.Count > 0)
                        ShowConfirm(
                            string.Format(Resx.GetString("Confirm_msgRefreshNoSaveUsers"),
                                string.Join(",", userList.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct())),
                            Resx.GetString("errDoisserWarrning"),
                            Resx.GetString("CONFIRM_StdCaptionYes"),
                            Resx.GetString("CONFIRM_StdCaptionNo"),
                            "cmdasync('cmd', 'RefreshNoUsersSave');", null, null);
                    else
                        RefreshPage();

                    break;
                case "RefreshNoUsersSave":
                    RefreshPage();
                    break;
                default:
                    base.ProcessCommand(cmd, param);
                    break;
            }
        }

        public override void TranslatePageEvent(NameValueCollection Params)
        {
            if (ItemId == 0) return;
            var cmd = Params["cmd"];

            var existsMessage = false;
            var clearCache = false;
            var pages = KescoHub.GetAllPages().ToList();
            pages.ForEach(p =>
            {
                if (!(p is EntityPage)) return;
                var ep = (EntityPage) p;

                //todo: Добавить проверку на наличие изменений в объекте
                if (ep.ItemId == 0 || ep.ItemId != ItemId || ep.ItemName != ItemName || ep.IDPage == IDPage) return;
                existsMessage = true;
                switch (cmd)
                {
                    case "RefreshForce":
                        if (!clearCache)
                        {
                            ClearCacheObjects();
                            clearCache = true;
                        }

                        ep.V4Navigate(CurrentUrl);
                        break;
                }
            });

            if (!existsMessage) return;
            SendCmdAsyncMessage();
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
            EntityName = Entity?.GetType().Name;

            if (Entity != null)
            {
                foreach (var field in Entity.GetType().GetFields())
                {
                    if (field.FieldType.Name != "BinderValue") continue;
                    var method = field.FieldType.GetMethod("ValueChangedEvent_Invoke",
                        new[] {typeof(string), typeof(string)});
                    if (method != null)
                        method.Invoke((BinderValue) field.GetValue(Entity),
                            new object[] {((BinderValue) field.GetValue(Entity)).Value, ""});
                }
                               
                var applyButton = MenuButtons.Find(mb => mb.ID == "btnApply");
                if (applyButton != null && Entity != null) applyButton.IsDisabled = !Entity.IsModified;

                var saveButton = MenuButtons.Find(mb => mb.ID == "btnSave");
                if (saveButton != null && Entity != null) saveButton.IsDisabled = !Entity.IsModified;

                var recheckButton = MenuButtons.Find(mb => mb.ID == "btnReCheck");
                if (recheckButton != null && Entity != null) recheckButton.IsDisabled = !Entity.IsModified;

            }
        }

        /// <summary>
        ///     Переадресация страницы на указанную
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
            base.V4Dispose(redirect);
        }

        /// <summary>
        ///     Отрисовка контрола для совместной работы
        /// </summary>
        protected void RenderSignalControl(StringWriter w)
        {
            SignalRUsers = new SignalR(this)
            {
                ID = "v4_signalUsersList",
                HtmlID = "v4_signalUsersList"
            };

            V4Controls.Add(SignalRUsers);
            SignalRUsers.RenderControl(w);
        }

        protected override void RenderAddControl(StringWriter w)
        {
            if (MenuButtons.Count > 0)
                if (ItemId > 0)
                    RenderSignalControl(w);
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
            return string.Format("<label class=localDT {0} {2}>{1}</label>",
                !string.IsNullOrEmpty(formatDateTime) ? string.Format("dtformat='{0}'", formatDateTime) : "",
                formatedDateTime, !string.IsNullOrEmpty(attributes) ? attributes : "");
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
            return string.Format("<label class=localDT {0} {2}>{1}</label>", withoutTime ? "dtformat=dd.mm.yyyy" : "",
                formatedDateTime, !string.IsNullOrEmpty(attributes) ? attributes : "");
        }

        /// <summary>
        ///     Переопределяемый метод присвоения идентификатора сущности
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
                var applyButton = MenuButtons.Find(mb => mb.ID == "btnApply");
                if (applyButton != null && Entity != null) applyButton.IsDisabled = !Entity.IsModified;

                
                var recheckButton = MenuButtons.Find(mb => mb.ID == "btnReCheck");
                if (recheckButton != null && Entity != null) recheckButton.IsDisabled = !Entity.IsModified;

            }

            if (V4Request.Params.Count == 0) return;
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


        /// <summary>
        ///     Сохранение сущности и восстановление курсора из асинхронного вызова
        /// </summary>
        protected virtual bool SaveEntity()
        {
            // добавить необходимые проверки перед сохранением сущности
            // при невыполнении return false;
            RefreshPage();
            return true;
        }
    }
}