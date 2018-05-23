using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.BaseExtention.Enums.Docs;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;
using Kesco.Lib.Entities.Corporate;
using Kesco.Lib.Entities.Documents;
using Kesco.Lib.Entities.Resources;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Settings;
using Unit = System.Web.UI.WebControls.Unit;
using Kesco.Lib.Web.Comet;

namespace Kesco.Lib.Web.Controls.V4.Common.DocumentPage
{
    /// <summary>
    ///     Базовый класс для стриниц документов
    ///     Все страницы, работающие с документами наследуют от него.
    /// </summary>
    /// <remarks>
    ///     Является абстрактным, т.к. содержит свойства, которые обязан переопределить на странице
    /// </remarks>
    public abstract class DocPage : EntityPage
    {
        /// <summary>
        ///     StringBuilder для общего пользования. Перед использованием выполняем Clear()
        /// </summary>
        private readonly StringBuilder _sb;

        /// <summary>
        ///     Функционал подписей
        /// </summary>
        private readonly SignsManager _signsManager;

        private TextBox _numberDoc;

        /// <summary>
        ///     Покупатель/Продавец по умолчанию (Для работы в Архиве)
        /// </summary>
        public string CurrentPerson;

        /// <summary>
        ///     Документ
        /// </summary>
        public Document Doc;

        /// <summary>
        ///     Дата документа в режиме редактирования
        /// </summary>
        public bool DocDateReadOnly = false;

        protected DocDirs Docdir = DocDirs.Undefined;

        /// <summary>
        ///     страница открыта в DocView
        /// </summary>
        public bool IsInDocView;

        /// <summary>
        /// Показать сохраняемые данные
        /// </summary>
        public bool ShowSaveData = false;

        /// <summary>
        ///     версия для печати
        /// </summary>
        /// <remarks>
        ///     страница Render'ится без скриптов (только HTML)
        /// </remarks>
        public bool IsPrintVersion;

        /// <summary>
        ///     Id следующего контрола для установки фокуса после даты
        /// </summary>
        public string NextControlAfterNumber;

        /// <summary>
        ///     не выводить блок с подписями на ЭФ
        /// </summary>
        public bool NoSign;

        /// <summary>
        ///     У документа нет номера - признак, связанный с соответствующим чек-боксом
        /// </summary>
        protected bool NumberNotExists;

        /// <summary>
        ///     Контрол редактирования номера ReadOnly
        /// </summary>
        protected bool NumberReadOnly;

        /// <summary>
        ///     Первоначальное состояние документа
        /// </summary>
        /// <remarks> у класса Document есть метод CompareToChanges</remarks>
        protected Document OriginalDoc;

        /// <summary>
        ///     Показывать кнопку копирования в меню
        /// </summary>
        public bool ShowCopyButton = true;

        /// <summary>
        ///     Показывать дату документа
        /// </summary>
        public bool ShowDocDate = true;

        /// <summary>
        ///     Показывать кнопку обновления в меню
        /// </summary>
        public bool ShowRefreshButton = true;

        /// <summary>
        ///     Показывать кнопку сохранения
        /// </summary>
        public bool ShowSaveButton;

        protected DocPage()
        {
            _signsManager = new SignsManager(this);
            _sb = new StringBuilder(100);
        }

        /// <summary>
        ///     Переданный в качестве параметра Id документа-основания
        /// </summary>
        public int DocId
        {
            get { return GetQueryStringIntParameter("DocId"); }
        }

        /// <summary>
        ///     ссылка на справку
        /// </summary>
        protected override string HelpUrl
        {
            get { return GetHelpUrl(); }
            set { Doc.DocType.HelpURL = value; }
        }

        /// <summary>
        ///     Признак  генерации номера документа
        /// </summary>
        public bool GenerateNumber
        {
            get { return Doc.GenerateNumber; }
            set { Doc.GenerateNumber = value; }
        }

        /// <summary>
        ///     Номер документа корректный
        /// </summary>
        public bool DocNumberIsCorrect
        {
            get
            {
                if (!NumberNotExists) return true;
                if (GenerateNumber) return true;
                if (Doc.Signed) return true;
                if (Doc.NumberIsDigital) return true;

                return false;
            }
        }

        /// <summary>
        ///     Обработчик события загрузки страницы
        /// </summary>
        /// <param name="e">Параметр события</param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (Request["CopyDoc"] != null)
                CopyDocMethod(Request["CopyDoc"]);
            else
            {
                DocumentInitialization();

                Doc.DocumentData.Id = Doc.Id = EntityId;

                var typeId = Doc.TypeID;
                LoadData(EntityId);


                InitControls();
                InitFields();

                if (typeId != Doc.TypeID)
                {
                    ShowMessage(Resx.GetString("msgWrongType"));
                    return;
                }

                // сообщаем что документ не доступен
                if (!EntityId.IsNullEmptyOrZero() && Doc.Unavailable)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    ShowMessage(string.Format(Resx.GetString("exDocUnavailable"), EntityId));
                    return;
                }
            }

            if (!Doc.IsNew)
                DocumentToControls();

            if (!PostRequest)
            {
                NoSign = Request.QueryString["NoSign"] == "1";
                IsPrintVersion = !string.IsNullOrEmpty(Request.QueryString["prn"]);
                IsInDocView = Request.QueryString["docview"] == "yes" ||
                              !string.IsNullOrEmpty(Request.QueryString["docview"]);
                CurrentPerson = Request.QueryString["currentperson"];

                var docdir = Request.QueryString["docdir"];
                if (docdir != null)
                    switch (docdir.ToLower())
                    {
                        case "in":
                            Docdir = DocDirs.In;
                            break;
                        case "out":
                            Docdir = DocDirs.Out;
                            break;
                        default:
                            Docdir = DocDirs.Undefined;
                            break;
                    }

                if (DocEditable)
                    ShowSaveButton = true;
                else
                    ShowRefreshButton = true;

                IsEditable = DocEditable;
            }
            else
            {
                Docdir = (DocDirs) Request.QueryString["docdir"].ToInt();
            }

            SetControlProperties();

            OriginalDoc = Doc.Clone();
        }

        /// <summary>
        ///     Инициализация полей документа при первой загрузке по коду
        ///     Костыль для правильного срабатывание Bindera
        /// </summary>
        private void InitFields()
        {
            foreach (var field in Doc.Fields.Values)
                field.ValueChangedEvent_Invoke(field.ValueString, "");
        }

        private void InitControls()
        {
            if (_numberDoc == null)
            {
                _numberDoc = new TextBox
                {
                    ID = "docNumberInp",
                    //  TabIndex = 1,
                    V4Page = this,
                    BindStringValue = Doc.NumberBind,
                    Value = Doc.Number ?? "",
                    Width = 150,
                    NextControl = NextControlAfterNumber
                };

                _numberDoc.Changed += NumberDocChanged;
                _numberDoc.V4Attributes.Add("onkeydown", "switch(event.keyCode){case 13: event.keyCode=9;break;}");
                _numberDoc.IsRequired = NumberRequired;
                if (!Doc.IsNew && string.IsNullOrEmpty(Doc.Number) && !NumberRequired)
                    _numberDoc.Visible = false;

                V4Controls.Add(_numberDoc);
            }
        }

        /// <summary>
        ///     Метод получения документа при копировании
        /// </summary>
        protected void CopyDocMethod(string guid)
        {
            var cachedDoc = Cache["CopyDoc" + guid];
            if (cachedDoc != null)
            {
                DocumentInitialization((Document) cachedDoc);
                Cache.Remove("CopyDoc" + guid);
            }
            else
            {
                // документ должен всегда быть инициализирован
                DocumentInitialization();

                var copyId = Request["CopyId"];

                // если кеш не получен получаем по ID
                if (!string.IsNullOrEmpty(copyId))
                {
                    Doc.DocumentData.Id = Doc.Id = copyId;

                    Doc.Load();
                    Doc.DocumentData.Load();

                    PrepareDocToCopy(Doc);
                }
            }

            DocumentToControls();
        }

        #region Методы необходимые или доступные для переопределения программисту

        /// <summary>
        ///     Инициализация конкретного типа документа
        /// </summary>
        /// <param name="copy">Параметр указывается если копируем документ</param>
        protected abstract void DocumentInitialization(Document copy = null);

        /// <summary>
        ///     Копирование данных документа на контролы.
        /// </summary>
        protected abstract void DocumentToControls();

        /// <summary>
        ///     Установить параметры контролов: параметры, дефолтные значения и т.д.
        /// </summary>
        protected abstract void SetControlProperties();

        /// <summary>
        ///     Проверка корректности вводимых полей
        /// </summary>
        /// <remarks>
        ///     Базовая валидация проходит только для полей со связью с колонками ДокументыДанные
        ///     Если валидация не требудется errors можно поставить null и вернуть true.
        /// </remarks>
        /// <param name="errors">Список ошибок, выходной параметр</param>
        /// <param name="exeptions">Исключения, Id поля для которого следует исключить валидацию</param>
        /// <returns>true - OK</returns>
        protected virtual bool ValidateDocument(out List<string> errors, params string[] exeptions)
        {
            errors = new List<string>();

            if (Doc.Date == DateTime.MinValue)
                errors.Add(Resx.GetString("DocDateNotExist"));

            if (Doc.DocType.NumberGenType != NumGenTypes.MustBeGenerated && !GenerateNumber &&
                string.IsNullOrWhiteSpace(Doc.Number))
                errors.Add(Resx.GetString("DocNumberNotExist"));

            if (Doc.Fields != null)
            {
                var msg = Resx.GetString("msgNoDataField");

                foreach (var f in Doc.Fields.Values)
                {
                    // если связь с документыДанные(DataColomnName) отсутствует, то не проверяем
                    if (f.IsMandatory && !f.DataColomnName.In("", "КодДокумента") && !f.Id.In(exeptions))
                    {
                        if (f.IsValueEmpty)
                        {
                            _sb.Clear();
                            _sb.Append(msg);
                            _sb.Append(": ");

                            if (IsRusLocal)
                                _sb.Append(f.DocumentField);
                            else if (IsEstLocal)
                                _sb.Append(f.DocumentFieldET);
                            else
                                _sb.Append(f.DocumentFieldEN);

                            errors.Add(_sb.ToString());
                        }
                    }
                }
            }

            if (errors.Count > 0)
                return false;

            return true;
        }

        /// <summary>
        ///     Метод вызываемый непосредственно перед сохранением документа
        /// </summary>
        protected virtual void OnBeforeSave()
        {
        }

        /// <summary>
        ///     Метод срабатывающий после сохранения документа
        /// </summary>
        protected virtual void OnDocumentSaved()
        {
        }

        /// <summary>
        ///     Метод срабатывающий при изменении подписей
        /// </summary>
        protected virtual void OnSignChanged()
        {
            // подписи не обновляем здесь из базы, это в зависимости от состояния DocView
            // 1. если архив открытлся, страница закрылась, обновлять подписи без надобности
            // 2. если открылась с ошибкой и это первая подпись, то обновляем всю страницу
            // 3. если открылась с ошибкой и это уже не первая подпись, то обновляем только подписи

            _signsManager.RefreshSigns(true);

            if (DocEditable)
            {
                ShowSaveButton = true;
            }
            else
            {
                ShowSaveButton = false;
                ShowRefreshButton = true;
            }

            SetDocMenuButtons();
            RefreshMenuButtons();
        }

        /// <summary>
        ///     Валидация перед подписанием документа
        /// </summary>
        /// <returns>true - OK</returns>
        protected virtual bool CheckBeforeSign(out List<string> li)
        {
            li = null;
            return true;
        }

        /// <summary>
        ///     Обновляет поля специфичные для данного документа(без полной перезагрузки страницы)
        /// </summary>
        protected virtual void RefreshCurrentDoc()
        {
            // в блоке RefreshDoc уже получили актуальные подписи
            _signsManager.RefreshSigns(false);

            RefreshDocTitle();
            RefreshNumber();
            RefreshNtf();
        }

        /// <summary>
        ///     Подготовка документа к копированию. Удаление всех лишних полей.
        /// </summary>
        protected virtual void PrepareDocToCopy(Document doc)
        {
            doc.Id = null;
            doc.Number = string.Empty;
            doc.DocumentData.Id = string.Empty;
            doc.ChangePersonID = 0;
            doc.DocumentData.ChangePersonID = 0;
            doc.ChangeDate = DateTime.MinValue;
            doc.DocumentData.ChangeDate = DateTime.MinValue;
            doc.Date = DateTime.MinValue;
            doc.DocSignsClear();
        }

        /// <summary>
        ///     Получение URL справки
        /// </summary>
        /// <remarks>
        ///     В случае если справка не доступна или трубуеся
        ///     другая справка, то можно метод переопределить
        /// </remarks>
        protected virtual string GetHelpUrl()
        {
            if (Doc.DocType != null)
                return Doc.DocType.HelpURL;

            return null;
        }

        /// <summary>
        ///     Загружает данные связаные с текущим документом
        /// </summary>
        protected virtual void LoadData(string id)
        {
            Doc.Load();
            Doc.DocumentData.Load();
        }

        #endregion

        /// <summary>
        ///     Получение целочисленного значения параметра из строки запроса
        /// </summary>
        /// <param name="name">параметр</param>
        /// <returns>целое значение либо 0 в случае неудачного преобразования к целому</returns>
        protected int GetQueryStringIntParameter(string name)
        {
            if (Request.QueryString[name] == null) return 0;
            if (!Regex.IsMatch(Request.QueryString[name], "^-?\\d+$", RegexOptions.IgnoreCase)) return 0;
            return int.Parse(Request.QueryString[name]);
        }

        /// <summary>
        ///     Формирует основыной функционал страницы документов: подписи, меню, заголовок, title
        /// </summary>
        public string RenderDocumentHeader()
        {
            using (var w = new StringWriter())
            {
                var titleText = GetPageTitle();
                w.Write("<script>document.title = '{0}'</script>", titleText);

                try
                {
                    SetDocMenuButtons();
                    RenderButtons(w);
                }
                catch (Exception e)
                {
                    var dex = new DetailedException("Не удалось сформировать кнопки формы: " + e.Message, e);
                    Logger.WriteEx(dex);
                    throw dex;
                }

                try
                {
                    _signsManager.RenderSigns(w);
                }
                catch (Exception ex)
                {
                    var dex = new DetailedException("Не удалось загрузить подписи документа: " + ex.Message, ex);
                    Logger.WriteEx(dex);
                    throw dex;
                }

                try
                {
                    RenderDocTitle(w, titleText);
                }
                catch (Exception e)
                {
                    var dex = new DetailedException("Не удалось получить заголовок страницы: " + e.Message, e);
                    Logger.WriteEx(dex);
                    throw dex;
                }

                return w.ToString();
            }
        }

        /// <summary>
        ///     Вспомогательный метод. Получение локализованого названия поля документа
        /// </summary>
        public string GetLocalizationFieldName(DocField field)
        {
            if (IsRusLocal)
                return field.DocumentField + ":";

            if (IsEstLocal)
                return field.DocumentFieldET + ":";

            return field.DocumentFieldEN + ":";
        }

        /// <summary>
        ///     Задание ограничений контрола по типам документов оснований, выбираемых в контроле
        /// </summary>
        /// <remarks>
        ///     Получаем значения для всех полей, кешируем, потом только выбираем из коллекции
        /// </remarks>
        public List<DocTypeParam> GetControlTypeFilter(int fieldId)
        {
            if (_docControlFilters == null)
            {
                _docControlFilters = DocTypeLink.GetControlFilter(Doc.TypeID);
            }

            // в фильтр нельзя передавать null
            if (_docControlFilters == null) return new List<DocTypeParam>();

            var filter = new List<DocTypeParam>();

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var f in _docControlFilters)
                if (fieldId == f.DocFieldId)
                    filter.Add(new DocTypeParam
                    {
                        DocTypeID = f.DocBasisId.ToString(),
                        QueryType = (DocTypeQueryType) f.BasisSearchMode
                    });

            return filter;
        }

        /// <summary>
        ///     Кеш ограничений контрола по типам документов
        /// </summary>
        private List<DocTypeLink> _docControlFilters;

        /// <summary>
        ///     Установить фокус на контрол по его ID
        /// </summary>
        /// <param name="contolId">HtmlID контрола</param>
        public override void V4SetFocus(string contolId)
        {
            if (contolId == "DocNumber")
            {
                if (Doc.IsNew)
                {
                    switch (Doc.DocType.NumberGenType)
                    {
                        case NumGenTypes.CanNotBeGenerated:
                            base.V4SetFocus("docNumberInp");
                            break;
                        case NumGenTypes.CanBeGenerated:
                            base.V4SetFocus(Doc.GenerateNumber ? "docNumberBtn" : "docNumberInp");
                            break;
                        case NumGenTypes.MustBeGenerated:
                            break;
                    }
                }
                else
                {
                    base.V4SetFocus("docNumberInp");
                }

                return;
            }

            base.V4SetFocus(contolId);
        }

        /// <summary>
        ///     Получить заголовок страницы документа
        /// </summary>
        private string GetPageTitle()
        {
            _sb.Clear();

            if (!Doc.IsNew && !string.IsNullOrEmpty(Doc.Name))
                _sb.Append(Doc.Name);
            else if (IsRusLocal && Doc.TypeID > 0 && !string.IsNullOrEmpty(Doc.DocType.TypeDocRu))
                _sb.Append(Doc.DocType.TypeDocRu);
            else if ((IsEstLocal || IsEngLocal) && Doc.TypeID > 0)
                _sb.Append(Doc.DocType.TypeDocEn);
            else if (!string.IsNullOrEmpty(Doc.DocType.TypeDocEn))
                _sb.Append(Doc.DocType.TypeDocEn);
            else
                _sb.Append(Resx.GetString("msgDoc"));

            if (Doc.IsNew)
                _sb.Append(string.Format(" ({0})", Resx.GetString("newDoc")));
            else
            {
                if (!string.IsNullOrWhiteSpace(Doc.Number))
                    _sb.Append(string.Format(" №{0}", Doc.Number));

                if (Doc.Date > DateTime.MinValue)
                    _sb.Append(string.Format(" {0} {1}", Resx.GetString("DateFrom"), Doc.Date.ToString("dd.MM.yyyy")));
            }

            return _sb.ToString();
        }

        protected override void ProcessCommand(string cmd, NameValueCollection param)
        {
            bool fDocSigned = Doc.Signed;

            switch (cmd)
            {
                // обновить подписи 
                case "RefreshSigns":
                    _signsManager.RefreshSigns();
                    break;

                // Добавить подпись
                case "AddSign":
                    AddSign();
                    break;
                // удаление подписи
                case "RemoveSign":
                    var ask = param["ask"] == "1";
                    var deleted = _signsManager.RemoveSign(param["IdSign"], ask);

                    if (deleted)
                    {
                        if (Doc.Signed)
                        {
                            OnSignChanged();
                        }
                        else
                            RefreshDocument();
                    }

                    break;
                case "RemoveSignsAll":
                    var rs = _signsManager.RemoveSignsAll();
                    if (rs)
                    {
                        if (param["Save"] != null && param["Save"].Equals("1")) SaveDocument(true);

                        HideDocSignDialog();
                       
                    }
                    break;
                case "CometSyncSigns":
                    CometSyncSigns(param["sign"], param["wuId"]);
                    break;
                // Сохранение документа
                case "SaveButton":
                    ShowSaveData = param["ShowSaveData"] == "1";
                    SaveDocument();

                    break;

                // Копировать на основании текущего документа
                case "DocCopy":
                    CopyDoc();
                    break;

                // Обновление документа
                case "RefreshDoc":

                    // Для корректной проверки DocEditable
                    // получаем актуальные данные
                    Doc.GetSignsFromDb();

                    if (!DocEditable)
                        RefreshDocument();
                    else
                        RefreshCurrentDoc();

                    break;
                // Показать текущий документ в Архиве документов
                case "ShowInDocView":
                    ShowDocumentInDocview(param["DocId"], false, param["openImage"] == "1");
                    break;

                case "GenerateNumber":
                    SetGenerateNumber(param["gType"] == "1");
                    break;

                default:
                    base.ProcessCommand(cmd, param);
                    break;
            }

            if (ItemId!=0)
                TranslatePageEvent(V4Request.Params, fDocSigned);
        }

       

        private void HideDocSignDialog()
        {
            JS.Write("$('#v4cometSignDivOuter').remove();");
        }

        private void CometSyncSigns(string sign, string wuId)
        {
            if (IsInDocView) return;

            var oldDocEditable = DocEditable;

            //если был не редактируемым
            if (!oldDocEditable)
            {
                //получаем подписи из БД
                Doc.GetSignsFromDb();
                //если остался нередактируемым - обновляем подписи
                if (!DocEditable)
                    _signsManager.RefreshSigns(false);
                else
                    RefreshDocument();

                return;
            }

            if (sign.Equals("-1"))
            {
                HideDocSignDialog();
            }

            // RenderChangeSignsConfirmDialog(wuId, "RemoveSignsAll", "Удалить все подписи с документа и продолжить редактирование;", null);
        }

        private void RenderChangeSignsConfirmDialog(string wuId, string action, string actionText,
            NameValueCollection args)
        {
            var employee = new Employee(wuId);
            var sb = new StringBuilder();
            var sbTextW = new StringBuilder();
            var sbTextB = new StringBuilder();

            var sbRun1 = new StringBuilder();
            var sbRun2 = new StringBuilder();
            var sbRun3 = new StringBuilder();

            var w = new StringWriter();

            var _args = args != null && args.Count > 0
                ? "," + string.Join(",", args
                    .AllKeys
                    .Select(key => "'" + key + "', '" + args[key] + "'")
                    .ToArray())
                : "";

            var title = "Сообщение!";

            RenderLinkEmployee(w, "", employee.Id, employee.FullName, NtfStatus.Empty);
            sbTextW.AppendFormat("{0} ", w);
            sbTextW.Append("подписал этот документ, в связи с этим документ стал не доступен для редактирования.");
            sbTextW.Append("<br/>");
            sbTextW.Append("Внесенные Вами данные не будут сохранены!");
            sbTextW.Append("<br/>");

            sbTextB.Append("Для продолжения работы выберите одно из следующих действий:");

            sbRun1.AppendFormat("<ul><li> <a href='#' onclick='window.location.reload();'>");
            sbRun1.AppendFormat("Получить актуальную версию документа");
            sbRun1.AppendFormat("</a>(все изменения будут утеряны!!!);</li>");

            sbRun1.AppendFormat(@"<li> <a href='#' onclick=""cmd('cmd', 'ShowInDocView', 'DocId','{0}');"">", EntityId);
            sbRun1.AppendFormat("Посмотреть актуальную версию документа;");
            sbRun1.AppendFormat("</a></li>");

            sbRun1.AppendFormat(@"<li> <a href='#' onclick=""cmd('cmd', '{0}'{1});"">", action, _args);
            sbRun1.AppendFormat(actionText);
            sbRun1.AppendFormat("</a></li></ul>");


            sb.Append("$('#v4cometSignDivOuter').remove();");
            sb.Append("$(\"body\").prepend(\"<div class='v4div-outer-container' id='v4cometSignDivOuter'></div>\");");
            sb.Append(
                "$(\"#v4cometSignDivOuter\").append(\"<div class='v4div-inner-container' id='v4cometSignDivInner'></div>\");");
            sb.Append(
                "$(\"#v4cometSignDivInner\").append(\"<div class='v4div-centered-content' style='width:500px; z-index:9000' id='v4cometSignDivContent'>");

            sb.Append("<table style='width:499px'>");

            sb.Append("<tr id='v4SignTrTitle'>");
            sb.Append("<td style='width:1px;cursor:move;' colspan=2>");
            sb.AppendFormat("<b>{0}</b>", title);
            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.Append("<td width='1%'>");
            sb.Append("<img src='/styles/Attention.gif' border=0>");
            sb.Append("</td>");
            sb.Append("<td>");
            sb.Append(HttpUtility.JavaScriptStringEncode(sbTextW.ToString()));
            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.Append("<td colspan=2>");
            sb.Append(HttpUtility.JavaScriptStringEncode(sbTextB.ToString()));
            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.Append("<td colspan=2>");
            sb.Append(HttpUtility.JavaScriptStringEncode(sbRun1.ToString()));
            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.Append("<td colspan=2>");
            sb.Append(HttpUtility.JavaScriptStringEncode(sbRun2.ToString()));
            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.Append("<td colspan=2>");
            sb.Append(HttpUtility.JavaScriptStringEncode(sbRun3.ToString()));
            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("</table>");

            sb.Append("</div>\");");

            sb.Append("$(\"#v4cometSignDivContent\").draggable({handle: '#v4SignTrTitle', containment: 'document'});");
            JS.Write(sb.ToString());
        }

        /// <summary>
        ///     Метод события копирования докумета
        /// </summary>
        protected void CopyDoc()
        {
            var clone = Doc.Clone();

            PrepareDocToCopy(clone);

            Cache["CopyDoc" + IDPage] = clone;
            var urlForCopy = V4Request.RawUrl.Substring(0, V4Request.RawUrl.IndexOf('?')) + "?CopyDoc=" + IDPage +
                             "&CopyId=" + Doc.Id;
            JS.Write("v4_windowOpen('{0}');", urlForCopy);
        }

        /// <summary>
        ///     Сформировать кнопки меню
        /// </summary>
        protected virtual void SetDocMenuButtons()
        {
            ClearMenuButtons();

            if (ShowSaveButton && !IsInDocView)
            {
                var btnSave = new Button
                {
                    ID = "btnSave",
                    V4Page = this,
                    Text = Resx.GetString("cmdSave"),
                    Title = Resx.GetString("titleSave"),
                    IconJQueryUI = ButtonIconsEnum.Save,
                    //Style = "BACKGROUND: buttonface url(/Styles/Save.gif) no-repeat left center;",
                    Width = 105,
                    OnClick = "cmd('cmd', 'SaveButton', 'ShowSaveData', v4_showSaveData(event));"
                };
                AddMenuButton(btnSave);
            }

            if (ShowRefreshButton)
            {
                var btnRefresh = new Button
                {
                    ID = "btnRefresh",
                    V4Page = this,
                    Text = Resx.GetString("cmdRefresh"),
                    Title = Resx.GetString("cmdRefreshDescription"),
                    //Style = "BACKGROUND: buttonface url(/Styles/refresh.gif) no-repeat left center;",
                    IconJQueryUI = ButtonIconsEnum.Refresh,
                    Width = 105,
                    OnClick = "cmd('cmd', 'RefreshDoc');"
                };

                AddMenuButton(btnRefresh);
            }

            if (ShowCopyButton)
            {
                var btnCopy = new Button
                {
                    ID = "btnCopy",
                    V4Page = this,
                    Text = Resx.GetString("cmdCopy"),
                    Title = Resx.GetString("cmdCopyTooltip"),
                    //Style = "BACKGROUND: buttonface url(/Styles/copy.gif) no-repeat left center;",
                    IconJQueryUI = ButtonIconsEnum.Copy,
                    Width = 105,
                    OnClick = "cmd('cmd', 'DocCopy');"
                };

                AddMenuButton(btnCopy);
            }
        }

        /// <summary>
        ///     Открытие в архиве документов
        /// </summary>
        /// <param name="id">Id Текущего документа</param>
        protected void ShowDocumentInDocview(string id, bool replicate, bool openImage)
        {
            if (string.IsNullOrEmpty(id))
                return;

            JS.Write(DocViewInterop.OpenDocument(id, openImage, replicate));

            
        }

        /// <summary>
        ///     Сформировать заглавие для страницы документа
        /// </summary>
        private void RenderDocTitle(StringWriter w, string titleText)
        {
            w.Write(@"<div id='divDocTitle' class=""v4FormContainer"">");

            if (!IsInDocView && !IsPrintVersion && !Doc.IsNew)
            {
                var showImige = Doc.ImageCode > 0 ? "1" : "0";
                w.Write(
                    "<a href=\"javascript:void(0);\" onclick=\"cmd('cmd', 'ShowInDocView', 'DocId', {0},'openImage', {1});\" title=\"{2}\">",
                    Doc.Id, showImige, Resx.GetString("cmdOpenDoc"));
                if (Doc.ImageCode > 0)
                    w.Write("<img src=\"/Styles/DocMain.gif\" border=\"0\" style=\"vertical-align:middle;\"/>");

                w.Write("<span style=\"font-weight: bold;\">" + titleText + "</span>");
                w.Write("</a>");
            }
            else
            {
                w.Write("<span style=\"font-weight: bold;\">" + titleText + "</span>");
            }

            w.Write("</div>");
        }

        /// <summary>
        ///     Обновить заглавие для страницы документа
        /// </summary>
        public void RefreshDocTitle()
        {
            using (var w = new StringWriter())
            {
                var titleText = GetPageTitle();

                w.Write("<script>document.title = '{0}'</script>", titleText);
                RenderDocTitle(w, titleText);
                JS.Write("if (gi('divDocTitle')) gi('divDocTitle').innerHTML={0};",
                    HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///     Коллекция для сохранения состояний между post запросами
        /// </summary>
        private readonly NameValueCollection sParam = new NameValueCollection();

        /// <summary>
        ///     Добавление подписи
        /// </summary>
        protected void AddSign()
        {
            var type = V4Request["type"] ?? "";
            var employeeInstead = string.IsNullOrEmpty(V4Request["id"]) ? 0 : Convert.ToInt32(V4Request["id"]);
            var saveAs = V4Request["saveAs"] ?? "";
            var isfirstSign = false;

            sParam.Add("callMethod", "AddSign");
            sParam.Add("type", type);
            sParam.Add("id", V4Request["id"]);
            sParam.Add("saveAs", saveAs);
            sParam.Add("checkSave", V4Request["checkSave"]);
            sParam.Add("checkNumber", V4Request["checkNumber"]);
            sParam.Add("checkSign", V4Request["checkSign"]);
            sParam.Add("checkSimilar", V4Request["checkSimilar"]);

            var sendMessage = DocViewParams.SignMessageWorkDone;

            if (!Doc.Signed && sParam["checkSave"] == null)
                if (!CheckBeforeSave())
                    return;

            if (!DocNumberIsCorrect && sParam["checkNumber"] == null)
                if (!CheckDocNumber(sParam, type))
                    return;

            if (type.Length > 0)
            {
                if (sParam["checkSign"] == null && !CheckBeforeSign())
                    return;

                if (employeeInstead == 0)
                {
                    if (!_signsManager.InquireSigner(type, ref sendMessage, ref employeeInstead))
                        return;
                }
            }

            //проверка на похожие документы !!!может смениться ID
            if (saveAs.Equals(""))
                if (!CheckForSimilarity(out saveAs))
                    return;

            if (!saveAs.Equals("0") || Doc.DocumentData.Unavailable)
                if (!CheckForRewriting(saveAs))
                    //проверка возможности добавления электронной формы к существующему документу
                    return;

            //проверка данных перед сохранением
            if (!Doc.IsNew && !Doc.Signed)
            {
                // if (!CheckForChanged()) return;
            }

            if (GenerateNumber || NumberNotExists) Doc.Number = null;

            if (DocEditable)
                EntityId = DocumentSave(saveAs);

            if (!EntityId.IsNullEmptyOrZero())
            {
                OnDocumentSaved();
                _signsManager.AddSignRecord(employeeInstead, type, out isfirstSign);
                OnSignChanged();
            }

            //далее определяем что-же делать дальше
            if (type.Equals("1") && sendMessage != DocViewParams.SignMessageWorkDone)
            {
                DocViewParams.SignMessageWorkDone = sendMessage;
                DocViewParams.SaveDVParameters();
            }

            AfterSave(type, sendMessage, employeeInstead, true, isfirstSign);
        }

        /// <summary>
        ///     Сохранение документа и восстановление курсора из асинхронного вызова
        /// </summary>
        protected virtual void SaveDocument()
        {
            SaveDocument(true);
        }

        /// <summary>
        ///     сохранение документа
        /// </summary>
        /// <param name="loadDocView">Открыть архив документов</param>
        protected bool SaveDocument(bool loadDocView)
        {
            var saveAs = V4Request["saveAs"] ?? "";

            var sendMessage = DocViewParams.SignMessageWorkDone;
            var employeeInstead = CurrentUser.EmployeeId;

            sParam.Add("callMethod", "SaveButton");
            sParam.Add("id", V4Request["id"]);
            sParam.Add("checkSave", V4Request["checkSave"]);
            sParam.Add("checkNumber", V4Request["checkNumber"]);
            sParam.Add("checkSimilar", V4Request["checkSimilar"]);

            if (!CheckBeforeSave())
                return false;

            //if (!DocNumberIsCorrect)
            //    if (!CheckDocNumber())
            //        return;

            //проверка на похожие документы !!!может смениться ID
            if (saveAs.Equals(""))
                if (!CheckForSimilarity(out saveAs))
                    return false;

            if (!saveAs.Equals("0") || Doc.DocumentData.Unavailable)
                if (!CheckForRewriting(saveAs))
                    //проверка возможности добавления электронной формы к существующему документу
                    return false;

            //проверка данных перед сохранением
            if (!Doc.IsNew && !Doc.Signed)
            {
                // if (!CheckForChanged()) return;
            }

            if (GenerateNumber || NumberNotExists) Doc.Number = null;
            
            EntityId = DocumentSave(saveAs);

            if (!EntityId.IsNullEmptyOrZero())
            {
                OnDocumentSaved();
            }

            if (!ShowSaveData)
            {
                if (loadDocView)
                    AfterSave("", sendMessage, employeeInstead, false, false);
                else
                    RefreshDocument();
            }
            ShowSaveData = false;
            return true;
        }

        /// <summary>
        ///     Действия после сохранения документа
        /// </summary>
        /// <param name="SignType">Тип подписи(финальная или обычная)</param>
        /// <param name="SendMessage">Отправить сообщение</param>
        /// <param name="EmployeeInsteadOf">Замещающий сотрудник</param>
        /// <param name="signChanged">true - изменена только подпись(для оптимизации)</param>
        private void AfterSave(string SignType, bool SendMessage, int EmployeeInsteadOf, bool signChanged,
            bool isfirstSign)
        {
            /*
            после нового документа его надо открыть в DocView, если он открыт не в DocView

            если сохраняли новый документ в DocView
            после сохранения документа в DocView

             1) при подписании документа в архиве документов не обновлять страницу,
                т.к. теряется фокус с формы послать сообщение (15.01.07,Serg)

            что делается после сохранения документа, зависит от:
            DocView	- документ открыт в DocView
            NewID	- сохранялся новый документ
            Signed	- имело место подписание документа (надо отправить сообщение)

            DocView NewID	Signed											dv	ms	rf
            0		0		0		обновляем										1
            0		0		1		шлем сообщение, обновляем					1	1
            0		1		0		открываем в DocView						1
            0		1		1		открываем в DocView, шлем сообщение		1	1
            1		0		0		ничего не делаем
            1		0		1		шлем сообщение								1
            1		1		0		обновляем										1
            1		1		1		шлем сообщение, обновляем					1	1

            rf - обновляем - перезагружаем страницу методом Get
            ms - шлем сообщение - шлем wm в DocView для открытия окна отправки сообщения по документу
            dv - открываем в DocView - пытаемся открыть в DocView, в новом окне (если неуспешно - обновляем, если успешно - закрываем окно)

            открываем в DocView, шлем сообщение, обновляем
            */

            var signed = SignType.Length > 0;
            var allowSendMsg = !"yes".Equals(V4Request["noSendMsg"]);
            var allowOpenInDocView = !"yes".Equals(V4Request["readonly"]);

            var dv = !IsInDocView && allowOpenInDocView;
            var ms = signed && SendMessage && allowSendMsg;

            var _signer = EmployeeInsteadOf.ToString();
            var msg = SignType.Equals("1") ? _signsManager.GetFinalSignMessage() : _signsManager.GetSignMessage();

            //Всегда закрываем форму и открываем в архиве
            if (!allowOpenInDocView && !allowSendMsg)
                Close();
            
            //ПРОВЕРИТЬ ЕСТЬ ЛИ АРХИВ НА КОМПЬЮТЕРЕ!!!!
            //if (signChanged)
            //{
            //    // если состояние изменилось - полностью обновляется страница
            //    if (isfirstSign)
            //        RefreshDocument();
            //    else
            //        _signsManager.RefreshSigns();
            //}
            //else
            //    RefreshDocument();

            if (ms)
                JS.Write(DocViewInterop.SendMessageDocument(Doc.Id, _signer, msg));
            else
                ShowDocumentInDocview(Doc.Id, false, false);

            if (dv)
                JS.Write("setTimeout(v4_dropWindow,1000);");

            sParam.Clear();
        }

        /// <summary>
        ///     Обновить страницу документа(полная перезагрузка страницы)
        /// </summary>
        /// <remarks>
        ///     Если документ не сохранен и не имеет Id, то введеные данные сотрутся
        /// </remarks>
        public void RefreshDocument()
        {
            if (_cometUsers != null)
                _cometUsers.DisposeComet();

            V4Navigate(Url4Reload);
        }

        public string Url4Reload
        {
            get
            {
                var sb = new StringBuilder();
                if (Doc.Id.Length > 0) sb.AppendFormat("&id={0}", Doc.Id);
                if (IsInDocView) sb.AppendFormat("&docview=yes");
                if (NoSign) sb.AppendFormat("&nosign=1");

                if (sb.Length > 0) sb[0] = '?';

                return HttpContext.Current.Request.Url.AbsolutePath + sb.ToString();
            }
        }

        /// <summary>
        ///     Сохранение документа
        /// </summary>
        private string DocumentSave(string saveAs)
        {
            OnBeforeSave();

            // пока упрощенная версия сохранения
            if (!saveAs.IsNullEmptyOrZero())
                Doc.Id = saveAs;

            List<DBCommand> cmds = null;
            if (ShowSaveData) cmds = new List<DBCommand>();
            
            Doc.Save(false, cmds);
            var doc = Doc as IDocumentWithPositions;
            if (doc != null)
                    doc.SaveDocumentPositions(false, cmds);
                
            
            if (ShowSaveData)
            {
                ShowMessage(GeterateTextFromSqlCmd(cmds),"SQL TRACE", MessageStatus.Information,"", 600, 600);
                cmds = null;

            }
            
            return Doc.Id;
        }

        private string GeterateTextFromSqlCmd(List<DBCommand> cmds)
        {
            StringBuilder sb = new StringBuilder();

            if (cmds.Count == 0)
            {
                sb.Append("Отсутствуют sql-команды для выполнения");
                return sb.ToString();
            }
            var inx = 0;
            var cnt = 0;
            cmds.ForEach(delegate(DBCommand cmd)
            {
                if (string.IsNullOrEmpty(cmd.Appointment))
                    sb.AppendLine("--<font color='red'>Цель выполнения запроса не определена!</font>");
                else
                    sb.AppendLine("--<b>" + cmd.Appointment + "</b>");
                sb.AppendLine("--" + cmd.ConnectionString);
                sb.AppendLine("<b>" + cmd.Text + "</b>");
                inx = 0;
                cnt = cmd.ParamsIn != null ? cmd.ParamsIn.Count : 0;
                if (cnt > 0)
                {
                    sb.AppendLine("--Входящие параметры:");
                    foreach (KeyValuePair<string, object> p in cmd.ParamsIn)
                    {
                        inx++;
                        sb.AppendLine(string.Format("<span style='margin-left:15px'>{0} = '{1}'{2}</span> ", p.Key,
                            p.Value, (inx < cnt) ? "," : ""));
                    }
                }
                inx = 0;
                cnt = cmd.ParamsOut != null ? cmd.ParamsOut.Count : 0;
                if (cnt > 0){
                    sb.AppendLine("--Исходящие параметры:");
                    foreach (KeyValuePair<string, object> p in cmd.ParamsOut)
                    {
                        inx++;
                        sb.AppendLine(string.Format("<span style='margin-left:15px'>{0} = '{1}'{2}</span> ", p.Key,
                            p.Value, (inx < cnt) ? "," : ""));
                    }
                }

                sb.AppendLine("=======================================");
                
            });

            return sb.ToString();
        }

        /// <summary>
        ///     сохранение с удалением подписей
        /// </summary>
        /// <returns>
        ///     true - OK
        /// </returns>
        private bool ForseSave()
        {
            Doc.GetSignsFromDb(); // кешируются в свойствах объекта
            if (!Doc.Finished)
            {
                foreach (var s in Doc.DocSigns)
                    DocSign.RemoveSign(s.Id);
            }
            else
            {
                ShowMessage("Удаление подписей с наличием завершающей подписи, невозможно");
                return false;
            }

            _signsManager.RefreshSigns();
            return true;
        }

        /// <summary>
        ///     Проверка на изменение документа
        /// </summary>
        private bool CheckForChanged()
        {
            var dt = Doc.ChangeDate;
            int usr;
            bool hasFinishSign;
            bool hasSign;

            Document.CheckForChanged(Doc.DocId, ref dt, out hasFinishSign, out hasSign, out usr);

            if (hasFinishSign || hasSign ||
                (dt != DateTime.MinValue && usr > 0 && !dt.Equals(Doc.ChangeDate) && usr != CurrentUser.EmployeeId))
            {
                RenderChangeSignsConfirmDialog(usr.ToString(), "RemoveSignsAll",
                    "Удалить все подписи с документа и сохранить свою версию документа;",
                    new NameValueCollection {{"Save", "1"}});
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Проверка на перезапись
        /// </summary>
        /// <returns>
        ///     true - OK
        /// </returns>
        private bool CheckForRewriting(string SaveAs)
        {
            if (SaveAs.Equals("0"))
                return true;

            var old = new Document(SaveAs);

            if (old.Signed)
            {
                // невозиожно сохраненить документ, т.к. такой документ уже существует и он подписан
                ShowMessage("Невозможно сохранить электронную форму, т.к. она уже существует и подписана");
                return false;
            }

            if (!old.DocumentData.Unavailable)
            {
                // невозиожно сохраненить документ, т.к. такой документ уже существует и он подписан
                ShowMessage(
                    "Невозможно добавить электронную форму документу, т.к. такой документ уже имеет электронную форму");
                return false;
            }

            var sequelDocsAll = DocLink.LoadChildDocsById(old.DocId);

            if (sequelDocsAll.Count > 0 && old.Is1SExported())
            {
                ShowMessage(
                    "Невозможно добавить эл. форму к документу, т.к. изображение уже использовалось как документ основание других документов и перенесено в 1С.\nПараметры переноса в 1С и создаваемой эл. формы могут несовпадать.\nВы сможете добавить эл. форму к изображению если: \n   1. Пометите на удаление в 1С документ (изображение) с кодом <" +
                    old.Id +
                    ">.\n   2. Снова нажмите кнопку сохранить на этой эл. форме.\n   3. Перевыбирете этот документ в вытекающих документах и перепроведёте их в 1С, если это необходимо.");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Проверка на похожесть документа
        /// </summary>
        /// <returns>true - OK</returns>
        private bool CheckForSimilarity(out string SaveAs)
        {
            SaveAs = "0";

            var old = DocPersons.LoadPersonsByDocId(Doc.DocId);
                //все лица по документу раньше (и сейчас до сохранения, пока тригер не обновит их)
            var oldData = DocPersons.LoadPersonsByDocId(Doc.DocId, true); //лица, которые раньше исп. в эл. форме

            var mustCheck = SimilarCheckRequired(old, oldData);

            if (!mustCheck)
                return true;

            var similarPersons = GetPersonsForSimilarCheck(old, oldData);
            var eqc = Doc.DocType.EquivCondition;
            var cnt = Document.SimilarCount(Doc.DocId, Doc.TypeID, Doc.Number, Doc.Date, similarPersons, eqc);
            
            if (cnt == 0)
                return true;
            
            JS.Write(DocViewInterop.CheckSimilarDocument(HttpContext.Current,
                Doc.IsNew ? "0" : Doc.Id,
                Doc.TypeID.ToString(),
                Doc.Date == DateTime.MinValue ? "" : Doc.Date.ToString("dd.MM.yyyy"),
                Doc.Number,
                similarPersons, 
                eqc.ToString()
                ));

            return false;
        }

        /// <summary>
        ///     Проверка на похожесть документов
        /// </summary>
        private bool SimilarCheckRequired(List<int> old, List<int> oldData)
        {
            if (Doc.IsNew)
                return true;

            var dps1 = new List<int?>(); //лица в документах данных сейчас (потом заполним, постепенно)

            //если все лица по документу не содержат лицо, то оно туда будет добавлено -> надо проверять
            if (Doc.DocumentData.PersonId1 > 0)
                if (!old.Contains(Doc.DocumentData.PersonId1 ?? 0)) return true;
                else dps1.Add(Doc.DocumentData.PersonId1);
            if (Doc.DocumentData.PersonId2 > 0)
                if (!old.Contains(Doc.DocumentData.PersonId2 ?? 0)) return true;
                else dps1.Add(Doc.DocumentData.PersonId2);
            if (Doc.DocumentData.PersonId3 > 0)
                if (!old.Contains(Doc.DocumentData.PersonId3 ?? 0)) return true;
                else dps1.Add(Doc.DocumentData.PersonId3);
            if (Doc.DocumentData.PersonId4 > 0)
                if (!old.Contains(Doc.DocumentData.PersonId4 ?? 0)) return true;
                else dps1.Add(Doc.DocumentData.PersonId4);
            if (Doc.DocumentData.PersonId5 > 0)
                if (!old.Contains(Doc.DocumentData.PersonId5 ?? 0)) return true;
                else dps1.Add(Doc.DocumentData.PersonId5);
            if (Doc.DocumentData.PersonId6 > 0)
                if (!old.Contains(Doc.DocumentData.PersonId6 ?? 0)) return true;
                else dps1.Add(Doc.DocumentData.PersonId6);

            //если хотябы одно лицо убрали из лиц в документах данных, оно будет удалено из всех лиц  -> надо проверять
            if (!dps1.Any()) return false;
            return oldData.Any(p => !dps1.Contains(Convert.ToInt32(p)));
        }

        /// <summary>
        /// </summary>
        private string GetPersonsForSimilarCheck(List<int> old, List<int> oldData)
        {
            foreach (var p in oldData) if (old.Contains(p)) old.Remove(p);
            var a = old.Select(p => (int?) p).ToList();

            if (Doc.DocumentData.PersonId1 > 0 && !a.Contains(Doc.DocumentData.PersonId1))
                a.Add(Doc.DocumentData.PersonId1);
            if (Doc.DocumentData.PersonId2 > 0 && !a.Contains(Doc.DocumentData.PersonId2))
                a.Add(Doc.DocumentData.PersonId2);
            if (Doc.DocumentData.PersonId3 > 0 && !a.Contains(Doc.DocumentData.PersonId3))
                a.Add(Doc.DocumentData.PersonId3);
            if (Doc.DocumentData.PersonId4 > 0 && !a.Contains(Doc.DocumentData.PersonId4))
                a.Add(Doc.DocumentData.PersonId4);
            if (Doc.DocumentData.PersonId5 > 0 && !a.Contains(Doc.DocumentData.PersonId5))
                a.Add(Doc.DocumentData.PersonId5);
            if (Doc.DocumentData.PersonId6 > 0 && !a.Contains(Doc.DocumentData.PersonId6))
                a.Add(Doc.DocumentData.PersonId6);

            a.Sort();
            var result = "";
            for (var i = 0; i < a.Count; i++)
            {
                if (i > 0) result += ",";
                result += a[i];
            }
            return result;
        }

        /// <summary>
        ///     Валидация номер документа
        /// </summary>
        /// <returns>
        ///     true - OK
        /// </returns>
        private bool CheckDocNumber(NameValueCollection p, string signType)
        {
            p["checkNumber"] = "0";

            if (!GenerateNumber && !NumberNotExists)
            {
                var url = "";
                if (!String.IsNullOrEmpty(Doc.DocType.URL))
                {
                    url = Doc.DocType.URL + (Doc.DocType.URL.Contains("?") ? "&" : "?") + "id=" + Doc.Id;
                }
                var dNum = Doc.Number == null ? "" : Doc.Number.Trim();
                JS.Write("ConfirmNumber.render('\"" + HttpUtility.HtmlEncode(dNum) + "\" " +
                         Resx.GetString("msgConfirm1") + "<br/>" +
                         Resx.GetString("msgConfirm2") + " \"" + HttpUtility.HtmlEncode(dNum) + "\"?','" + signType +
                         "','" + (!NumberReadOnly && IsEditable) + "','" +
                         (!Doc.Signed && !IsReadOnly) + "','" + url + "','" + Resx.GetString("msgOldTitle") + "', '" +
                         Resx.GetString("msgConfirm") + "', '" + Resx.GetString("msgBtn1") + "', '" +
                         String.Format(Resx.GetString("msgBtn2") + " \"{0}\"", HttpUtility.HtmlEncode(dNum)) + "', '" +
                         Resx.GetString("msgBtn3") + "');");
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Валидация
        /// </summary>
        /// <returns>
        ///     true - OK
        /// </returns>
        private bool CheckBeforeSign()
        {
            List<string> li;

            if (CheckBeforeSign(out li))
                return true;

            RenderErrors(li);

            return false;
        }

        /// <summary>
        ///     Проверка полей перед сохранением
        /// </summary>
        /// <returns>
        ///     true - OK
        /// </returns>
        private bool CheckBeforeSave()
        {
            List<string> li;

            if (ValidateDocument(out li))
                return true;

            RenderErrors(li);

            return false;
        }

        /// <summary>
        ///     Сформировать сообщение об ошибке
        /// </summary>
        public void RenderErrors(List<string> li, string text = null)
        {
            using (var w = new StringWriter())
            {
                foreach (var l in li)
                    w.Write("<div style='white-space: nowrap;'>{0}</div>", l);
                
                if (text == null)
                    text = "<br>" + (Doc.Signed ? "Подписание" : "Сохранение") + " документа невозможно.";

                ShowMessage(w + text, Resx.GetString("errIncorrectlyFilledField"), MessageStatus.Error,"", 500, 200);
                //else
                //{
                //    JS.Write("if(confirm({0}))", w + "\nВы уверены что хотите " + (Doc.Signed ? "подписать" : "сохранить") + " документ несмотря на предупреждения?");
                //sParam["checkSave"] = "0";
                //JS.Write("if(confirm({0}))", "'документ несмотря на предупреждения?'");
                //JS.Write(CreateJsCmdCommand(sParam));
                //}
            }
        }

        /// <summary>
        ///     Рендер ссылки открытия документа
        /// </summary>
        public void RenderLinkDoc(TextWriter w, string id)
        {
            w.Write("<A onclick='cmd('cmd', 'ShowInDocView', 'DocId', {0});' href='#'>", id);
        }

        /// <summary>
        ///     Рендер описания документа
        /// </summary>
        protected void RenderDocDescription(TextWriter w, int colspan)
        {
            RenderDocDescription(w, colspan > 1 ? "colspan=\"" + colspan + "\"" : "");
        }

        /// <summary>
        ///     Рендер описания документа из V3
        /// </summary>
        /// <param name="w"></param>
        /// <param name="colspan"></param>
        private void RenderDocDescription(TextWriter w, string colspan)
        {
            w.Write(@"
			<tr>
				<td vAlign=""top"">{0} :</td>
				<td width=""100%"" {1}>", Resx.GetString("lblDescription"), colspan);

            var doc = new TextArea
            {
                ID = "DocDescriptionArhiv",
                V4Page = this,
                Value = Doc.Description,
                Width = Unit.Empty,
                DisplayStyle = "block"
            };
            doc.Changed += DocOnChanged;
            V4Controls.Add(doc);
            doc.RenderControl(w);

            w.Write(@"</td>
			</tr>");
        }

        /// <summary>
        ///     Событие изменения описания документа в архиве
        /// </summary>
        private void DocOnChanged(object sender, ProperyChangedEventArgs p)
        {
            if (p.OldValue != p.NewValue)
                Doc.Description = p.NewValue;
        }

        /// <summary>
        ///     Начало рендера изменяемой части документа
        ///     Для поддержки существующих форм
        /// </summary>
        protected void StartRenderVariablePart(TextWriter w)
        {
            StartRenderVariablePart(w, 500, 334, 550);
        }

        /// <summary>
        ///     Начало рендера изменяемой части документа
        /// </summary>
        protected void StartRenderVariablePart(TextWriter w, int labelWidth, int fieldWidth, int fielsetWidth)
        {
            w.WriteLine("<fieldset style=\"width: {0}px; background-color: rgb(241, 241, 241);\">", fielsetWidth);
            w.WriteLine("<legend>{0}</legend>", Resx.GetString("lblVariablePartDoc"));
            w.WriteLine("<div style=\"padding: 5px; margin-left: 5px;\">");

            w.WriteLine(
                "<div class=\"ctlMargMain\"> <div style=\"vertical-align: top; width: {1}px; display: inline-table;\">{0}</div>",
                Resx.GetString("lblDescription") + ":", labelWidth);
            w.Write("<div style=\"display: inline-table; vertical-align: bottom;\">");

            var txaDesc = new TextArea
            {
                V4Page = this,
                Value = Doc.Description,
                MaxLength = 500,
                Width = fieldWidth, //334,
                ID = "txaDocDesc",
                BindStringValue = Doc.DescriptionBinder
            };


            txaDesc.Changed += txaDocDesc_OnChanged;
            V4Controls.Add(txaDesc);
            txaDesc.RenderControl(w);

            w.Write("</div></div>");
        }

        /// <summary>
        ///     Событие изменения описания документа в архиве
        /// </summary>
        private void txaDocDesc_OnChanged(object sender, ProperyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
                Doc.Description = e.NewValue;
        }

        /// <summary>
        ///     Окончание Рендера изменяемой части документа
        /// </summary>
        protected void EndRenderVariablePart(TextWriter w)
        {
            w.Write("</div></fieldset>");
        }

        // todo вытекающие документы скопированы из V3 нужна будет переделка(планируется перенести в контрол)

        #region Вытекающие документы 

        ///// <summary>
        /////  Вытекающие документы
        ///// </summary>
        ///// <remarks>
        ///// Временно на переделку
        ///// </remarks>
        //public void RenderLinkedDocsInfo(TextWriter w)
        //{
        //    if (Doc.IsNew) return;
        //    if (!Doc.Signed) return;

        //    var chTypes = DocTypeLink.GetAllChildTypes(Doc.TypeID);

        //    foreach (var c in chTypes)
        //    {
        //        //var dt = LoadSequelDocs( c.ChildDocId.ToString(),c.DocFieldId.ToString()); // пока не нужно
        //        var dt = new DataTable("notneed");
        //        RenderLinkedDocsInfoDetails(w, dt,
        //            Resx.GetString("listFollowType"),
        //            Resx.GetString("oneFollowType"),
        //            c.ChildDocId.ToString(),
        //            c.LinkType.ToString(), c.DocFieldId.ToString(),
        //            c.DocChildField, c.DocChildFieldEN);
        //    }
        //}

        /// <summary>
        ///     Временно на переделку
        /// </summary>
        private DataTable LoadSequelDocs(string type, string field)
        {
            if (Doc.IsNew) return new DataTable();
            var _sql = string.Format(@"
                SELECT D._КодДокумента КодДокумента, D.ДатаДокумента, D.КодТипаДокумента, D.КодРесурса1,ISNULL(D.Money1,0) Money1
                FROM vwСвязиДокументов S (nolock)
                			INNER JOIN vwДокументыДокументыДанные D (nolock) ON S.КодДокументаВытекающего=D.КодДокумента
                WHERE КодДокументаОснования={0} {1} {2}", Doc.Id,
                type.Length > 0 ? string.Format(" AND КодТипаДокумента IN ({0})", type) : "",
                field.Length > 0 ? string.Format(" AND КодПоляДокумента IN ({0})", field) : ""
                );

            return DBManager.GetData(_sql, Config.DS_document);
        }

//        /// <summary>
//        ///  Временно на переделку
//        /// </summary>
//        private void RenderLinkedDocsInfoDetails(TextWriter w, DataTable dt, string _title, string _oneTitle, string _type, string _linkType, string fieldId, string field, string fieldEn)
//        {
//            //DataView dv = new DataView
//            //{
//            //    Table = dt,
//            //    Sort = "КодРесурса1, ДатаДокумента"
//            //};

//            DocType dtp = new DocType(_type);
//            if (!dtp.Unavailable)
//            {
//                //w.Write("<table border=0 style='BORDER-COLLAPSE:collapse;'>");
//                //w.Write("<tr>");
//                //w.Write("<td {0}>", (dv.Count == 0 ? "style=\"padding-bottom:15px;\"" : ""));
//                w.Write("<div>");
//                if (_linkType == "21" || _linkType == "11") w.Write(_oneTitle);
//                else w.Write(_title);
//                w.Write(" \"" + (IsRusLocal ? dtp.Name : dtp.TypeDocEn) + "\"");
//                w.Write(" (" + Resx.GetString("LBL_ПоПолю") + " \"" + (IsRusLocal ? field : fieldEn) + "\")");

//                /* ссылку на создание (связывание с документом) выводим в случае, если тип связи
//                    (типы связи 12, 22 - означают, что может быть несколько вытекающих; 11, 21 - только 1 вытекающий)
//                    и количество уже связанных документов это допускают */
//                if (_linkType == "12" || _linkType == "22" )
//                {
//                    // для допускающих несколько вытекающих документов - выводим диалог (выбор или привязка к существующему)
//                    //if (_linkType == "12" || _linkType == "22")
//                    //{
//                    //    StringCollection col = new StringCollection();
//                    //    foreach (DataRow dr in dt.Rows)
//                    //        col.Add(dr["КодДокумента"].ToString());

//                    //    w.Write(" <a href=\"#\" onclick=\"return false;\"><img style=\"border:0;cursor:pointer;\" border=\"0\" src=\"/styles/new.gif\" title=\"{0}\"", Resx.GetString("listNewDoc"));
//                    //    w.Write(" onclick=\"window.showModalDialog('linkedDoc.ashx?type={0}&Id={1}&linkType={2}&basedate={3}&field={4}&linkedDocs={5}',null," +
//                    //        "'resizable:no;dialogHeight:140px;dialogWidth:520px;center:yes;status:no;help:no;');\"/></a>",
//                    //        _type, Doc.DocId, _linkType, Doc.Date.ToString("yyyyMMdd"), fieldId, Collection2Str(col));
//                    //}
//                    // для допускающих 1 вытекающий - сразу открываем создание нового документа на основе текущего
//                    //else
//                    //{
//                        string url = dtp.URL + (dtp.URL.IndexOf("?", StringComparison.Ordinal) > 0 ? "&" : "?") + "DocId=" + Doc.Id + "&fieldId=" + fieldId;

//                        w.Write(" <a href=\"#\" onclick=\"return false;\"><img style=\"border:0;cursor:pointer;\" border=\"0\" src=\"/styles/new.gif\" title=\"{0}\"", Resx.GetString("listNewDoc"));
        //                        w.Write(" onclick=\"v4_windowOpen('{0}');\"/></a>", url);
//                    //}
//                }
//                w.Write("</div>");
//                //w.Write("</td>");
//                //w.Write("</tr>");
//                //w.Write("</table>");
//            }
//        }

        /// <summary>
        ///     Преобразование коллекции к строке с разделителем ','
        /// </summary>
        public static string Collection2Str(StringCollection col)
        {
            return ConvertExtention.Convert.Collection2Str(col);
        }

        /// <summary>
        ///     Преобразование коллекции к строке с разделителем ','
        /// </summary>
        public static string Collection2Str(IEnumerable<string> col)
        {
            return ConvertExtention.Convert.Collection2Str(col);
        }


        public void RenderLinkedDocsInfo(TextWriter w)
        {
            if (Doc.IsNew) return;

            var dt = GetSettingsLinkedDocsInfo();
            if (dt.Rows.Count == 0) return;

            var dv = new DataView();
            dv.Table = dt;
            dv.Sort = "ПорядокВыводаВОсновании";
            dv.RowFilter = "КодТипаДокументаОснования=" + Doc.TypeID;
            GetLinkedDocsInfo(w, dv);
        }

        private DataTable GetSettingsLinkedDocsInfo()
        {
            var query = SQLQueries.SELECT_СвязиТиповДокументов_Вытекающие;
            var param = new Dictionary<string, object>
            {
                {"@id", new object[] {Doc.TypeID, DBManager.ParameterTypes.Int32}}
            };
            var dt = DBManager.GetData(query, Document.ConnString, CommandType.Text, param);
            return dt;
        }

        private void GetLinkedDocsInfo(TextWriter w, DataView dv)
        {
            for (var i = 0; i < dv.Count; i++)
            {
                var dt = Document.LoadSequelDocs(dv[i]["КодТипаДокументаВытекающего"].ToString(),
                    dv[i]["КодПоляДокумента"].ToString(), Doc.Id);
                RenderLinkedDocsInfoDetails(w, dt,
                    Resx.GetString("listFollowType"),
                    Resx.GetString("oneFollowType"),
                    dv[i]["КодТипаДокументаВытекающего"].ToString(), dv[i]["ТипСвязи"].ToString(),
                    dv[i]["КодПоляДокумента"].ToString(),
                    dv[i]["ПолеДокумента"].ToString(), dv[i]["ПолеДокументаEn"].ToString());
            }
        }

        private void RenderLinkedDocsInfoDetails(TextWriter w, DataTable dt, string _title, string _oneTitle,
            string _type, string _linkType, string fieldId, string field, string fieldEn)
        {
            var dvSys = new DataView();
            dvSys.Table = dt;

            var dv = new DataView();
            dv.Table = dt;
            dv.Sort = "КодРесурса1, ДатаДокумента";

            Document d = null;
            Currency cur = null;

            var curSml = "&nbsp;";
            var curScale = 2;
            decimal sum = 0;

            var dtp = new DocType(_type);
            if (!dtp.Unavailable)
            {
                w.Write("<table border=0 style='BORDER-COLLAPSE:collapse;'>");
                w.Write("<tr>");
                w.Write("<td {0}>", (dv.Count == 0 ? "style=\"padding-bottom:15px;\"" : ""));
                if (_linkType == "21" || _linkType == "11") w.Write(_oneTitle);
                else w.Write(_title);
                w.Write(" \"" + (IsRusLocal ? dtp.Name : dtp.TypeDocEn) + "\"");
                w.Write(" (" + Resx.GetString("LBL_ПоПолю") + " \"" + (IsRusLocal ? field : fieldEn) + "\")");

                /* ссылку на создание (связывание с документом) выводим в случае, если тип связи
                    (типы связи 12, 22 - означают, что может быть несколько вытекающих; 11, 21 - только 1 вытекающий)
                    и количество уже связанных документов это допускают */
                if (_linkType == "12" || _linkType == "22" || dv.Count == 0)
                {
                    // для допускающих несколько вытекающих документов - выводим диалог (выбор или привязка к существующему)
                    if (_linkType == "12" || _linkType == "22")
                    {
                        var col = new StringCollection();
                        foreach (DataRow dr in dt.Rows)
                            col.Add(dr["КодДокумента"].ToString());

                        w.Write(
                            " <a href=\"#\" onclick=\"return false;\"><img style=\"border:0;cursor:pointer;\" border=\"0\" src=\"/styles/new.gif\" title=\"{0}\"",
                            Resx.GetString("listNewDoc"));
                        w.Write(
                            " onclick=\"window.showModalDialog('linkedDoc.aspx?type={0}&Id={1}&linkType={2}&basedate={3}&field={4}&linkedDocs={5}',null," +
                            "'resizable:no;dialogHeight:140px;dialogWidth:520px;center:yes;status:no;help:no;');\"/></a>",
                            _type, Doc.Id, _linkType, Doc.Date.ToString("yyyyMMdd"), fieldId, Collection2Str(col));
                    }
                    // для допускающих 1 вытекающий - сразу открываем создание нового документа на основе текущего
                    else
                    {
                        var url = dtp.URL + (dtp.URL.IndexOf("?", StringComparison.Ordinal) > 0 ? "&" : "?") + "DocId=" +
                                  Doc.Id + "&fieldId=" + fieldId;

                        w.Write(
                            " <a href=\"#\" onclick=\"return false;\"><img style=\"border:0;cursor:pointer;\" border=\"0\" src=\"/styles/new.gif\" title=\"{0}\"",
                            Resx.GetString("listNewDoc"));
                        w.Write(
                            " onclick=\"v4_windowOpen('{0}');\"/></a>",
                            url);
                    }
                }
                w.Write(":</td>");
                w.Write("</tr>");
                w.Write("</table>");
            }

            if (dv.Count == 0) return;
            w.Write("<table border=0 style='BORDER-COLLAPSE:collapse;padding-bottom:15px;'>");
            for (var i = 0; i < dv.Count; i++)
            {
                d = new Document(dv[i]["КодДокумента"].ToString());
                if (d.Unavailable) continue;
                var res = d.DocumentData.ResourceId1;
                if (res.HasValue && res > 0)
                {
                    var test = res;
                    //todo
                    //cur = Currency.GetCurrency(res.Value);
                    //cur._PersonID = doc._Person1;
                    //if (cur.Unavailable || cur.ParentOf(V2.Resources.Resource._Money)) curSml = "&nbsp;";
                    //else
                    //{
                    //    curSml = cur.CurrencySymbol;
                    //    curScale = int.Parse(cur._UnitScale);
                    //}
                }
                else
                    curSml = "&nbsp;";

                w.Write("<tr>");
                w.Write("<td style='PADDING-LEFT:10px;' valign='top'>");
                RenderLinkDoc(w, d.Id);
                w.Write("<img src='/styles/DocMain.gif' border=0>");
                RenderLinkEnd(w);
                w.Write("</td>");
                w.Write("<td noWrap valign='top'>");
                RenderLinkDoc(w, d.Id);
                w.Write((d.Number.Length > 0) ? Resx.GetString("lN") + " " + d.Number + " " : "");
                if (d.Date != DateTime.MinValue) w.Write(Resx.GetString("lD") + " " + d.Date.ToString("dd.MM.yyyy"));
                RenderLinkEnd(w);
                w.Write("</td>");
                w.Write("<td noWrap valign='top'style='PADDING-LEFT:5px;'>");
                if (d.Finished) w.Write(Resx.GetString("listComplete"));
                else if (d.Signed) w.Write(Resx.GetString("listSigned"));
                else w.Write(Resx.GetString("listNotSigned"));
                w.Write("</td>");
                w.Write("<td align='right' noWrap valign='top' style='PADDING-LEFT:5px;'>");
                if (d.DocumentData.Money1.HasValue && d.DocumentData.Money1 > 0)
                    RenderNumber(w, d.DocumentData.Money1.Value.ToString(), curScale);
                else
                    w.Write("&nbsp;");
                w.Write("</td>");
                w.Write("<td valign='top'>");
                w.Write(curSml);
                w.Write("</td>");
                w.Write("<td valign='top' style='PADDING-LEFT:5px;'>");
                w.Write(d.Description.Length == 0 ? "&nbsp;" : d.Description);
                w.Write("</td>");
                w.Write("</tr>");

                if (!dv[i]["КодРесурса1"].Equals(DBNull.Value) &&
                    ((i < dv.Count - 1 && !dv[i + 1]["КодРесурса1"].Equals(dv[i]["КодРесурса1"]))
                     || i == dv.Count - 1))
                {
                    dvSys.RowFilter = "КодРесурса1=" + dv[i]["КодРесурса1"];
                    if (dvSys.Count > 1)
                    {
                        sum = Convert.ToDecimal(dt.Compute("SUM(Money1)", "КодРесурса1=" + dv[i]["КодРесурса1"]));
                        w.Write("<tr>");
                        w.Write("<td colspan=3 align='right'>");
                        w.Write(Resx.GetString("listTotal") + ":");
                        w.Write("</td>");
                        w.Write("<td align='right' noWrap style='PADDING-LEFT:5px;'>");
                        RenderNumber(w, ConvertExtention.Convert.Decimal2Str(sum, curScale), curScale);
                        w.Write("</td>");
                        w.Write("<td>");
                        w.Write(curSml);
                        w.Write("</td>");
                        w.Write("<td>");
                        w.Write("&nbsp;");
                        w.Write("</td>");
                        w.Write("</tr>");
                    }
                }
                curScale = 2;
                curSml = "&nbsp;";
            }

            w.Write("</table>");
        }

        #endregion

        /// <summary>
        ///     Отрисовать документ-основане
        /// </summary>
        protected void RenderBaseDocsHTML(TextWriter w, DocField f)
        {
            var arr = Doc.GetBaseDocs(f.DocFieldId);
            if (arr.Count == 0) return;

            w.Write("<table cellspacing=\"0\" cellpadding=\"0\">");
            foreach (var d in arr)
            {
                w.Write("<tr>");

                if (!IsPrintVersion && DocEditable)
                    w.Write(
                        @"<td><img src=""/styles/removefromList.gif"" style=""cursor:hand;"" alt=""убрать из списка"" onclick=""cmd('cmd','RemoveBaseDoc', 'docId','{0}','fieldId','{1}')""></td>",
                        d.Id, f == null ? "" : f.Id);

                w.Write("<td width='99%'>");

                w.Write("<a href=\"javascript:cmd('cmd', 'ShowInDocView', 'DocId', {0},'openImage', {1});\">", d.Id, 0);

                if (d.Unavailable) w.Write("#" + d.Id);
                else w.Write(d.FullDocName);
                RenderLinkEnd(w);

                w.Write("</td>");

                w.Write("</tr>");

                if (Doc.Date > DateTime.MinValue && d.Date > DateTime.MinValue && Doc.Date < d.Date)
                {
                    w.Write("<tr><td colspan='2'>");
                    RenderNtf(w, new List<string> {Resx.GetString("NTF_DateBaseDoc")});

                    w.Write("</td></tr>");
                }
            }
            w.Write("</table>");
        }

        #region номер документа

        /// <summary>
        ///     Обновление номера, даты и наименования документа
        /// </summary>
        protected void RefreshrDocNumDateNameRows()
        {
            using (var w = new StringWriter())
            {
                RenderDocNumDateNameRows(w);
                JS.Write("if(document.all('trDocNumDateName'))document.all('trDocNumDateName').innerHTML={0};",
                    HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///     Вывод номера, даты и наименования документа
        /// </summary>
        /// <param name="w"></param>
        protected void RenderDocNumDateNameRows(TextWriter w)
        {
            if (IsInDocView)
                return;

            if (!V4IsPostBack)
            {
                w.Write(@"<div id=""trDocNumDateName"">");
            }
            w.Write(@"<table cellspacing=""0"" cellPadding=""0"" border=""0"" width=""99.5%"">
			<tr>
				<td vAlign=""top"" width=""100px"">{0}:</td>
				<td>
					<table width=""100%"" cellSpacing=""0"" cellPadding=""0"">
						<tr>
							<td width=""50px"" vAlign=""top"" noWrap>", Resx.GetString("lblDocNumber"));
            RenderNumber(w);
            w.Write(@"</td>");

            if (ShowDocDate)
            {
                w.Write(@"<td width=""50px"" vAlign=""top"" noWrap align=""right"">{0}:&nbsp;</td>",
                    Resx.GetString("lblDate"));
                w.Write(@"<td vAlign=""top"" noWrap>");
                if (DocEditable && !IsPrintVersion)
                {
                    var docDate = new DatePicker
                    {
                        // TabIndex = 2,
                        ID = "DocDate",
                        V4Page = this,
                        IsRequired = true,
                        IsReadOnly = DocDateReadOnly,
                        Value = Doc.Date == DateTime.MinValue ? "" : Doc.Date.ToString("dd.MM.yyyy")
                    };

                    docDate.Changed += OnDocDateChanged;
                    V4Controls.Add(docDate);
                    docDate.RenderControl(w);
                }
                else
                {
                    w.Write(Doc.Date == DateTime.MinValue ? "" : Doc.Date.ToString("dd.MM.yyyy"));
                }
                w.Write(@"</td>");
            }


            w.Write(@"</tr>
					</table>
				</td>
			</tr>");

            // Строку с названием выводим в случае, если оно есть у документа или его можно задать (т.е. редактируем док-т)
            if (Doc.DocType.NameExist && ((DocEditable && !IsPrintVersion) || !string.IsNullOrEmpty(Doc.Name)))
            {
                w.Write(@"
				<tr>
					<td vAlign=""top"" noWrap>{0}:</td>
					<td vAlign=""top"">", Resx.GetString("lblDocName"));
                if (DocEditable && !IsPrintVersion)
                {
                    var docName = new TextBox
                    {
                        Width = Unit.Percentage(100),
                        //  TabIndex = 3,
                        ID = "docName",
                        V4Page = this,
                        Value = Doc.Name ?? ""
                    };
                    docName.Changed += DocNameChanged;
                    V4Controls.Add(docName);
                    docName.RenderControl(w);
                }
                else
                {
                    w.Write(Doc.Name ?? "");
                }
                w.Write("</td></tr>");
            }
            w.Write("</table>");
            if (!V4IsPostBack)
            {
                w.Write("</div>");
            }
        }

        /// <summary>
        ///     Рисуем контрол номера документа
        /// </summary>
        /// <param name="w"></param>
        public void RenderNumber(TextWriter w)
        {
            w.Write("<div id=\"docNumber\">");
            RenderNumberContent(w);
            w.Write("</div>");
        }

        /// <summary>
        ///     Документ редактируемый
        /// </summary>
        public bool DocEditable
        {
            get
            {
                var re = V4Request == null ? "" : V4Request["readonly"];
                return !Doc.Signed && !IsInDocView && re != "yes";
            }
        }

        /// <summary>
        ///     Документ подписан, хотябы один раз
        /// </summary>
        public bool DocSigned
        {
            get { return Doc.Signed; }
        }

        /// <summary>
        ///     Вывод ссылки на генерацию номера документа
        /// </summary>
        /// <param name="w"></param>
        protected void RenderNumberContent(TextWriter w)
        {
            if (!DocEditable || IsPrintVersion || NumberReadOnly)
            {
                if (!NumberNotExists)
                {
                    w.Write(HttpUtility.HtmlEncode(Doc.Number));
                }
                RenderNumberNotExistsInput(w);
                return;
            }

            if (Doc.DocType.NumberGenType == NumGenTypes.MustBeGenerated)
            {
                if (Doc.IsNew)
                {
                    w.Write("<nobr>{0}</nobr><br>", Resx.GetString("generatingNumberOfDocument"));
                }
                else
                {
                    w.Write("<nobr>{0}</nobr>", HttpUtility.HtmlEncode(Doc.Number));
                }
            }
            else if (Doc.DocType.NumberGenType == NumGenTypes.CanNotBeGenerated)
            {
                RenderNumberInput(w);
                RenderNumberNotExistsInput(w);
            }
            else
            {
                if (Doc.IsNumberGenerationAvailable)
                {
                    if (GenerateNumber)
                    {
                        w.Write("<nobr>{0}</nobr>", Resx.GetString("generatingNumberOfDocument"));
                        w.Write(
                            "<a id=\"docNumberBtn\" href=\"javascript:cmd('cmd', 'GenerateNumber', 'gType' , '0');\"><br>{0}</a>",
                            Resx.GetString("typeNumberOfDocument"));
                    }
                    else
                    {
                        RenderNumberInput(w);
                        w.Write(
                            "<a id=\"docNumberBtn\" href=\"javascript:cmd('cmd', 'GenerateNumber', 'gType', '1');\"><br>{0}</a>",
                            Resx.GetString("generateNumberOfDocument"));
                        RenderNumberNotExistsInput(w);
                    }
                }
                else
                {
                    RenderNumberInput(w);
                    RenderNumberNotExistsInput(w);
                }
            }
        }

        /// <summary>
        ///     Вывод чек-бокса - документ не имеет номера
        /// </summary>
        private void RenderNumberNotExistsInput(TextWriter w)
        {
            if (NumberRequired || (!DocEditable || IsPrintVersion) && !NumberNotExists) return;
            var cb = new CheckBox
            {
                ID = "cbNumberNotExists",
                V4Page = this,
                //TabIndex = 4,
                Checked = NumberNotExists
            };

            w.Write("<table cellspacing=\"0\" cellpadding=\"0\" border=\"0\"><tr><td>");
            if (!DocEditable || IsPrintVersion)
            {
                //w.Write("&nbsp;<img style=\"vertical-align:middle;\" src=\"" + Global.PathImg + "CheckBoxDisabled{0}.gif\"/>", NumberNotExists ? "Checked" : "Unchecked");
                cb.IsDisabled = true;
            }
            //else
            //{
            //    w.Write("<img id=\"numberNotExistsInp\" style=\"margin-top:3px;\" TabIndex=\"4\" src=\"" + Global.PathImg + "CheckBoxEnabled{0}.gif\" onclick=\"NotExists_swap('numberNotExistsInp', 0);\" onkeypress=\"NotExists_swap('numberNotExistsInp', 0);\" />", 
            //        NumberNotExists ? "Checked" : "Unchecked");
            //}
            cb.Changed += CbNumberNotExistsChanged;
            V4Controls.Add(cb);
            cb.RenderControl(w);
            w.Write("</td><td noWrap>&nbsp;{0}</td></tr></table>", Resx.GetString("numberNotExists"));
        }

        /// <summary>
        ///     Обновление номера документа
        /// </summary>
        public void RefreshNumber()
        {
            using (var w = new StringWriter())
            {
                RenderNumberContent(w);
                JS.Write("if(document.all('docNumber'))document.all('docNumber').innerHTML={0};",
                    HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///     Обязательность номера документа
        /// </summary>
        protected bool NumberRequired;

        /// <summary>
        ///     Обработчик изменения флага "у документа нет номера"
        /// </summary>
        /// <param name="notExist">true - флаг включен</param>
        protected void SetNumberNotExists(bool notExist)
        {
            NumberNotExists = notExist;
            NumberReadOnly = NumberNotExists && !NumberRequired;
            RefreshNumber();
        }

        /// <summary>
        ///     Изменение состояния чекбокса - наличия/отсутствия номера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void CbNumberNotExistsChanged(object sender, ProperyChangedEventArgs e)
        {
            SetNumberNotExists(((CheckBox) sender).Checked);
        }

        /// <summary>
        ///     Обработчик генерации номера
        /// </summary>
        /// <param name="gn">true - флаг включен</param>
        protected void SetGenerateNumber(bool gn)
        {
            GenerateNumber = gn;
            RefreshHtmlBlock("docNumber", RenderNumberContent);
        }

        /// <summary>
        ///     Вывод поля ввода номера документа
        /// </summary>
        /// <param name="w"></param>
        private void RenderNumberInput(TextWriter w)
        {
            _numberDoc.RenderControl(w);

            if (Doc.DocType.NumberGenType == NumGenTypes.CanBeGenerated ||
                Doc.DocType.NumberGenType == NumGenTypes.CanNotBeGenerated)
                _numberDoc.Focus();
        }

        /// <summary>
        ///     Сохранение номера документа в модели данных
        /// </summary>
        protected void NumberDocChanged(object sender, ProperyChangedEventArgs e)
        {
            Doc.Number = e.NewValue;
        }

        /// <summary>
        ///     Сохранение наименования документа в модели данных
        /// </summary>
        protected void DocNameChanged(object sender, ProperyChangedEventArgs e)
        {
            Doc.Name = e.NewValue;
        }

        /// <summary>
        ///     Сохранение даты документа в модели данных
        /// </summary>
        protected virtual void OnDocDateChanged(object sender, ProperyChangedEventArgs e)
        {
            DateTime dt;
            Doc.Date = DateTime.TryParse(e.NewValue, out dt) ? dt : DateTime.MinValue;
        }

        #endregion

        public void TranslatePageEvent(NameValueCollection Params, bool fDocSigned)
        {
            string cmd = Params["cmd"];
            switch (cmd)
            {
                case "RefreshDoc":
                case "RefreshSigns":
                    break;

                case "AddSign":
                    if (!fDocSigned)//Подписей до этого не было
                        cmd = "RefreshDoc";
                    else cmd = "RefreshSigns";
                    break;

                case "RemoveSign":
                    if (!Doc.Signed)//Подписей не осталось
                        cmd = "RefreshDoc";
                    else cmd = "RefreshSigns";
                    break;

                case "RemoveSignsAll":
                    cmd = "RefreshDoc";
                    break;

                default:
                    return;
            }

            CometMessage m = new CometMessage
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
                if (!(client.Page is DocPage)) return false;
                if (client.Id != this.ItemId) return false;
                if (((Page)client.Page).IDPage == IDPage) return false;

                DocPage dp = client.Page as DocPage;

                switch(cmd)
                {
                    case "RefreshDoc":
                        client.Start = DateTime.MinValue;
                        dp.RefreshDocument();
                        break;

                    case "RefreshSigns":
                        dp.V4Request = null;
                        dp._signsManager.RefreshSigns();
                        break;
                }
                return true;
            };

            CometServer.PushMessage(m, pred);
            CometServer.Process();
        }

        protected override void SetIdEntity(string id, string command)
        {
            switch (command)
            {
                case "checksimilar":
                    JS.Write("cmd('cmd', 'SaveButton', 'ShowSaveData', '', 'saveAs', '{0}');",
                        id == "-2" ? "0" : id);
                    break;
            }
        }
    }
}