using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.BindModels;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.BaseExtention.Enums.Docs;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;
using Kesco.Lib.Entities.Corporate;
using Kesco.Lib.Entities.Documents;
using Kesco.Lib.Entities.Documents.EF.Dogovora;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Settings;
using Kesco.Lib.Web.SignalR;

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

        /// <summary>
        ///     Коллекция для сохранения состояний между post запросами
        /// </summary>
        private readonly NameValueCollection sParam = new NameValueCollection();

        private DatePicker _dateDoc;

        /// <summary>
        ///     Кеш ограничений контрола по типам документов
        /// </summary>
        private List<DocTypeLink> _docControlFilters;

        private TextBox _numberDoc;

        /// <summary>
        ///     Покупатель/Продавец по умолчанию (Для работы в Архиве)
        /// </summary>
        public string CurrentPerson;

        /// <summary>
        ///     Дата документа в режиме редактирования
        /// </summary>
        public bool DocDateReadOnly = false;

        protected DocDirs Docdir = DocDirs.Undefined;

        /// <summary>
        ///     Словарь привязки клиентских контролов к полям документа
        /// </summary>
        public Dictionary<V4Control, DocField> FieldsToControlsMapping = null;

        /// <summary>
        /// Отрисовывать заголовок формы, если нет названия документа
        /// </summary>
        public bool IsRenderDocTitle { get; set; } = true;

        /// <summary>
        ///     страница открыта в DocView
        /// </summary>
        public bool IsInDocView;

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
        public string NextControlAfterDate;

        /// <summary>
        ///     Id следующего контрола для установки фокуса после описания
        /// </summary>
        public string NextControlAfterDocDesc;

        /// <summary>
        ///     Id следующего контрола для установки фокуса после номера договора
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
        ///     Показывать кнопку копирования в меню
        /// </summary>
        public bool ShowCopyButton = true;

        /// <summary>
        ///     Показывать дату документа
        /// </summary>
        public bool ShowDocDate = true;

        /// <summary>
        ///     Показывать кнопку редактирования
        /// </summary>
        public bool ShowEditButton = true;

        /// <summary>
        ///     Показывать кнопку обновления в меню
        /// </summary>
        public bool ShowRefreshButton = true;

        /// <summary>
        ///     Показывать кнопку сохранения
        /// </summary>
        public bool ShowSaveButton;

        /// <summary>
        ///     Показать сохраняемые данные
        /// </summary>
        public bool ShowSaveData;

        protected DocPage()
        {
            _signsManager = new SignsManager(this);
            _sb = new StringBuilder(100);
        }

        /// <summary>
        ///     Документ
        /// </summary>
        public Document Doc
        {
            get { return (Document) Entity; }
            set { Entity = value; }
        }

        /// <summary>
        ///     Переданный в качестве параметра Id документа-основания
        /// </summary>
        public int DocId => GetQueryStringIntParameter("DocId");

        /// <summary>
        ///     ссылка на справку
        /// </summary>
        public override string HelpUrl
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

        public string CorrId { get; set; }

        public string CopyId
        {
            get
            {
                var copyid = "";
                try
                {
                    copyid = V4Request["CopyId"];
                }
                catch
                {
                    // ignored
                }

                return string.IsNullOrEmpty(CorrId) ? copyid : CorrId;
            }
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

        public string Url4Reload
        {
            get
            {
                var sb = new StringBuilder();
                if (Doc.Id.Length > 0) sb.AppendFormat("&id={0}", Doc.Id);
                if (IsInDocView) sb.AppendFormat("&docview=yes");
                if (NoSign) sb.AppendFormat("&nosign=1");

                if (sb.Length > 0) sb[0] = '?';

                return HttpContext.Current.Request.Url.AbsolutePath + sb;
            }
        }

        /// <summary>
        ///     Форма открыта в режиме редактирования
        /// </summary>
        /// <param name="request">Текущий request</param>
        /// <returns></returns>
        public static bool IsOpenInEditableMode(HttpRequest request)
        {
            if (string.IsNullOrEmpty(request.QueryString["id"])) return true;
            if (int.Parse(request.QueryString["id"]) == 0) return true;

            var parameters = new Dictionary<string, object>
                {{"@КодДокумента", int.Parse(request.QueryString["id"])}, {"@EF", 1}};
            using (var dbReader = new DBReader(SQLQueries.SELECT_ПодписиДокумента, CommandType.Text, Config.DS_document,
                parameters))
            {
                if (dbReader.HasRows) return false;
            }

            return string.IsNullOrEmpty(request.QueryString["docview"]);
        }

        /// <summary>
        ///     Обработчик события загрузки страницы
        /// </summary>
        /// <param name="e">Параметр события</param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (Request["CopyDoc"] != null)
            {
                CopyDocMethod(Request["CopyDoc"]);
                InitControls();
                InitFields();
            }
            else
            {
                if (Doc == null) return; //происходит редирект

                Doc.Id = EntityId;
                Doc.DocumentData.Id = EntityId;

                var typeId = Doc.TypeId;

                EntityLoadData(EntityId);

                InitControls();
                InitFields();

                // сообщаем что документ не доступен
                if (!EntityId.IsNullEmptyOrZero() && Doc.Unavailable)
                {
                    V4Dispose();
                    Response.Write(string.Format(Resx.GetString("exDocUnavailable"), EntityId));
                    Response.Flush();
                    Response.SuppressContent = true;
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                    //ShowMessage(string.Format(Resx.GetString("exDocUnavailable"), EntityId), Resx.GetString("alertMessage"));
                    return;
                }

                if (typeId != Doc.TypeId)
                {
                    Logger.WriteEx(new LogicalException(
                        $"URL формы не соответсвует типу документа. Тип должен быть: {typeId}, а пытаются открыть: {Doc.TypeId}",
                        Resx.GetString("msgWrongType"), Assembly.GetExecutingAssembly().GetName(),
                        Priority.ExternalError));
                    V4Dispose();
                    Response.Write(Resx.GetString("msgWrongType"));
                    Response.Flush();
                    Response.SuppressContent = true;
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                    return;
                }
            }

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


                ShowSaveButton = DocEditable;
                ShowRefreshButton = true;

                IsEditable = DocEditable;
            }
            else
            {
                Docdir = (DocDirs) Request.QueryString["docdir"].ToInt();
            }

            DocumentToControls();
            SetControlProperties();

            if (Doc.IsNew) SetBaseDocTypeFilter();
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
                    OriginalValue = OriginalEntity == null ? "" : ((Document) OriginalEntity).Number,
                    Width = 150,
                    NextControl = NextControlAfterNumber
                };

                _numberDoc.Changed += NumberDocChanged;
                //_numberDoc.V4Attributes.Add("onkeydown", "switch(event.keyCode){case 13: event.keyCode=9;break;}");
                _numberDoc.IsRequired = NumberRequired;
                //if (!Doc.IsNew && string.IsNullOrEmpty(Doc.Number) && !NumberRequired)
                //    _numberDoc.Visible = false;

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
                EntityInitialization((Document) cachedDoc);
                Cache.Remove("CopyDoc" + guid);
            }
            else
            {
                // документ должен всегда быть инициализирован
                EntityInitialization();

                var copyId = Request["CopyId"];

                // если кеш не получен получаем по ID
                if (!string.IsNullOrEmpty(copyId))
                {
                    Doc.DocumentData.Id = Doc.Id = copyId;

                    Doc.Load();
                    Doc.DocumentData.Load();
                }
            }

            PrepareDocToCopy(Doc);
            DocumentToControls();
            SetControlProperties();
        }

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
            if (Doc == null) return "";

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
                    _signsManager.RenderControlDocSings(w);
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
            if (_docControlFilters == null) _docControlFilters = DocTypeLink.GetControlFilter(Doc.TypeId);

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
        ///     Установить фокус на контрол по его ID
        /// </summary>
        /// <param name="controlId">HtmlID контрола</param>
        public override void V4SetFocus(string controlId)
        {
            if (controlId == "DocNumber")
            {
                if (Doc.IsNew)
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
                else
                    base.V4SetFocus("docNumberInp");

                return;
            }

            base.V4SetFocus(controlId);
        }

        /// <summary>
        ///     Получить заголовок страницы документа
        /// </summary>
        private string GetPageTitle()
        {
            _sb.Clear();
            var docType = GetDocType();
            if (!IsInDocView)
            {
                if (!string.IsNullOrEmpty(Doc.DocumentName))
                    _sb.Append(Doc.DocumentName);
                else if (!string.IsNullOrEmpty(docType))
                    _sb.Append(docType);
                else
                    _sb.Append(Resx.GetString("msgDoc")); // Документ
            }

            if (Doc.IsNew)
            {
                _sb.Append(string.Format(" ({0})", Resx.GetString("newDoc"))); // Новый документ
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(Doc.Number))
                    _sb.Append(string.Format(" №{0}", Doc.Number));

                if (Doc.Date > DateTime.MinValue)
                    _sb.Append(string.Format(" {0} {1}", Resx.GetString("DateFrom"), Doc.Date.ToString("dd.MM.yyyy")));
            }

            if (!Doc.IsNew && !string.IsNullOrEmpty(Doc.DocumentName) && !string.IsNullOrEmpty(docType))
                _sb.Append(string.Format(" ({0})", docType));

            return _sb.ToString();
        }

        private string GetDocType()
        {
            var _sb = new StringBuilder(100);
            if (IsRusLocal && Doc.TypeId > 0 && !string.IsNullOrEmpty(Doc.DocType.TypeDocRu))
                _sb.Append(Doc.DocType.TypeDocRu);
            else if ((IsEstLocal || IsEngLocal) && Doc.TypeId > 0)
                _sb.Append(Doc.DocType.TypeDocEn);
            else if (!string.IsNullOrEmpty(Doc.DocType.TypeDocEn))
                _sb.Append(Doc.DocType.TypeDocEn);

            return _sb.ToString();
            ;
        }

        protected override void ProcessCommand(string cmd, NameValueCollection param)
        {
            var preDocSigned = Doc.Signed;
            bool afterDocSigned;

            switch (cmd)
            {
                // обновить подписи 
                case "RefreshSigns":
                    _signsManager.RefreshControlDocSings();
                    break;
                // Добавить подпись
                case "AddDocumentSign":
                    AddDocumentSign();
                    break;
                // удаление подписи
                case "RemoveSign":
                    var deleted = _signsManager.RemoveSign(int.Parse(param["IdSign"]));

                    if (deleted)
                    {
                        if (Doc.Signed)
                            OnSignChanged();
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
                case "SignalSyncSigns":
                    SignalSyncSigns(param["sign"], param["wuId"]);
                    break;
                // Сохранение документа
                case "SaveButton":
                    ShowSaveData = param["ShowSaveData"] == "1";
                    SaveDocument();
                    RestoreCursor();
                    break;
                // Сохранение документа
                case "ApplyButton":
                    ShowSaveData = param["ShowSaveData"] == "1";
                    SaveDocument(false);
                    RestoreCursor();
                    break;
                // Сохранение документа
                case "SaveDocument":
                    List<DBCommand> cmds = null;

                    if (!CheckBeforeSave())
                        break;

                    //проверка на похожие документы
                    var saveAs = V4Request["saveAs"] ?? "";
                    if (saveAs.Equals(""))
                        if (!CheckForSimilarity(out saveAs))
                            break;

                    Doc.Save(false, cmds);

                    var doc = Doc as IDocumentWithPositions;
                    if (doc != null) doc.SaveDocumentPositions(false, cmds);

                    JS.Write("v4_closeSaveConfirmForm();");
                    if (param["AfterSaveProcess"] == "1")
                    {
                        RefreshDocument();
                    }
                    else
                    {
                        var sendMessage = DocViewParams.SignMessageWorkDone;
                        var employeeInstead = CurrentUser.EmployeeId;
                        AfterSave("", sendMessage, employeeInstead, false, false);
                    }

                    RestoreCursor();
                    break;
                case "BeforeDocCopy":
                    if (Doc.CompareToChanges((Document) OriginalEntity))
                        ShowConfirm("Перед копированием текущий документ будет сохранен. Продолжить?",
                            Resx.GetString("errDoisserWarrning"),
                            Resx.GetString("CONFIRM_StdCaptionYes"),
                            Resx.GetString("CONFIRM_StdCaptionNo"),
                            "cmd('cmd', 'SaveAndCopy');", null, null);
                    else
                        CopyDoc();
                    break;
                case "DeleteEForm":
                    Doc.DeleteEForm();
                    V4DropWindow();
                    break;
                // Копировать на основании текущего документа
                case "SaveAndCopy":
                    if (CheckBeforeSave())
                    {
                        List<DBCommand> cmdsc = null;
                        Doc.Save(false, cmdsc);

                        CopyDoc();
                    }

                    break;
                case "DocCopy":
                    CopyDoc();
                    break;

                // Обновление документа
                case "RefreshDoc":

                    // Для корректной проверки DocEditable
                    // получаем актуальные данные
                    Doc.GetSignsFromDb();
                    RefreshDocument();

                    break;
                case "GenerateNumber":
                    SetGenerateNumber(param["gType"] == "1");
                    break;
                case "SaveDocumentDesription":
                    SaveDocumentDesription(param["Description"]);
                    break;
                default:
                    base.ProcessCommand(cmd, param);
                    break;
            }

            afterDocSigned = Doc.Signed;

            TranslateDocPageEvent(V4Request.Params, preDocSigned, afterDocSigned);
        }

        private void SaveDocumentDesription(string _description)
        {
            Doc.Description = _description;
            Doc.SaveDescription();
            if (!IsInDocView)
                RefreshDocument();
        }

        private void HideDocSignDialog()
        {
            JS.Write("$('#v4signalSignDivOuter').remove();");
        }

        private void SignalSyncSigns(string sign, string wuId)
        {
            if (IsInDocView) return;

            var oldDocEditable = DocEditable;

            //если был не редактируемым
            if (!oldDocEditable)
            {
                //если остался нередактируемым - обновляем подписи
                if (!DocEditable)
                    _signsManager.RefreshControlDocSings();
                else
                    RefreshDocument();

                return;
            }

            if (sign.Equals("-1")) HideDocSignDialog();

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


            sb.Append("$('#v4signalSignDivOuter').remove();");
            sb.Append("$(\"body\").prepend(\"<div class='v4div-outer-container' id='v4signalSignDivOuter'></div>\");");
            sb.Append(
                "$(\"#v4signalSignDivOuter\").append(\"<div class='v4div-inner-container' id='v4signalSignDivInner'></div>\");");
            sb.Append(
                "$(\"#v4signalSignDivInner\").append(\"<div class='v4div-centered-content' style='width:500px; z-index:9000' id='v4signalSignDivContent'>");

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

            sb.Append("$(\"#v4signalSignDivContent\").draggable({handle: '#v4SignTrTitle', containment: 'document'});");
            JS.Write(sb.ToString());
        }

        /// <summary>
        ///     Метод события копирования докумета
        /// </summary>
        protected void CopyDoc()
        {
            // отвязать bind
            var field = DocumentToControlsUnBind();
            var docNum = Doc.Number;
            var docDesc = Doc.Description;

            Doc.NumberBind = null;
            Doc.DescriptionBinder = null;

            var clone = Doc.Clone();

            // восстановить bind
            Doc.NumberBind = new BinderValue();
            Doc.DescriptionBinder = new BinderValue();

            Doc.Number = docNum;
            Doc.Description = docDesc;

            DocumentToControlsRestoreBind(field);
            Doc.DocumentData.Id = Doc.Id = EntityId;
            
            clone.NumberBind = new BinderValue();
            clone.DescriptionBinder = new BinderValue();
            PrepareDocToCopy(clone);

            Cache["CopyDoc" + IDPage] = clone;
            var urlForCopy = V4Request.RawUrl.Substring(0, V4Request.RawUrl.IndexOf('?')) + "?CopyDoc=" + IDPage +
                             "&CopyId=" + Doc.Id;
            JS.Write("Kesco.windowOpen('{0}', null, null, 'docCopy');", urlForCopy);
        }

        private string GetCurrentDocEditUrl(string url)
        {
            if (!string.IsNullOrEmpty(Config.direction_OldVersion))
            {
                url = Request.Url.AbsoluteUri.Split('?')[0];
                ;
            }

            var qs = Request.QueryString.ToString()
                .Split(new[] {"&"}, StringSplitOptions.RemoveEmptyEntries);

            var param = "";
            foreach (var s in qs)
            {
                var pair = s.Split(new[] {"="}, StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length == 2)
                    if (pair[0].ToLower() != "type" && pair[0].ToLower() != "isie8" &&
                        pair[0].ToLower() != "docview" && pair[0].ToLower() != "nosign")
                        param += "&" + pair[0] + "=" + pair[1];
                if (!string.IsNullOrEmpty(param))
                {
                    param = param.Remove(0, 1);
                    param = "?" + param;
                }
            }

            return url + param;
        }

        /// <summary>
        ///     Сформировать кнопки меню
        /// </summary>
        protected virtual void SetDocMenuButtons()
        {
            var btnEdit = MenuButtons.Find(btn => btn.ID == "btnEdit");
            if (!Doc.Unavailable && Doc.DocType != null && !Doc.DocType.Unavailable &&
                !string.IsNullOrEmpty(Doc.DocType.URL)
                && ShowEditButton && IsInDocView && !Doc.Signed)
                btnEdit.OnClick = string.Format("Kesco.windowOpen('{0}', null, null, this);", GetCurrentDocEditUrl(Doc.DocType.URL));
            else
                RemoveMenuButton(btnEdit);

            var btnCancel = MenuButtons.Find(btn => btn.ID == "btnCancel");
            RemoveMenuButton(btnCancel);


            var btnSave = MenuButtons.Find(btn => btn.ID == "btnSave");
            var btnApply = MenuButtons.Find(btn => btn.ID == "btnApply");
            if (ShowSaveButton && !IsInDocView)
            {
                btnSave.OnClick = !string.IsNullOrEmpty(CopyId)
                    ? string.Format("v4_createDialogSaveContent('{0}','{1}');save_dialogShow('{2}');",
                        Resx.GetString("TTN_lblOpenInDocumentsArchive"), Resx.GetString("TTN_lblContinueEditing"),
                        Resx.GetString("TTN_lblSaveDocument"))
                    : "cmdasync('cmd', 'SaveButton', 'ShowSaveData', v4_showSaveData(event));";

                btnApply.OnClick = !string.IsNullOrEmpty(CopyId)
                    ? string.Format("v4_createDialogSaveContent('{0}','{1}');save_dialogShow('{2}');",
                        Resx.GetString("TTN_lblOpenInDocumentsArchive"), Resx.GetString("TTN_lblContinueEditing"),
                        Resx.GetString("TTN_lblSaveDocument"))
                    : "cmdasync('cmd', 'ApplyButton', 'ShowSaveData', v4_showSaveData(event));";
            }
            else
            {
                RemoveMenuButton(btnSave);
                RemoveMenuButton(btnApply);
            }

            var btnRefresh = MenuButtons.Find(btn => btn.ID == "btnRefresh");
            var btnRecheck = MenuButtons.Find(btn => btn.ID == "btnReCheck");

            if (!ShowRefreshButton || Doc.IsNew)
                RemoveMenuButton(btnRefresh);

            if (btnRecheck != null)
            {
                if (!Doc.IsNew)
                    RemoveMenuButton(btnRecheck);
                else
                    btnRecheck.OnClick = "cmdasync('cmd', 'RefreshNotification');";

            }

            if (ShowCopyButton && !Doc.IsNew && !Doc.Unavailable && !Doc.DataUnavailable)
            {
                var btnCopy = new Button
                {
                    ID = "btnCopy",
                    V4Page = this,
                    Text = Resx.GetString("cmdCopy"),
                    Title = Resx.GetString("cmdCopyTooltip"),
                    IconJQueryUI = ButtonIconsEnum.Copy,
                    Width = 105,
                    OnClick = "cmdasync('cmd', 'BeforeDocCopy');"
                };

                AddMenuButton(btnCopy);
            }

            if (!Doc.IsNew && IsEditable)
            {

                var btnCopy = new Button
                {
                    ID = "btnDelete",
                    V4Page = this,
                    Text = Resx.GetString("cmdDelete"),
                    Title = Resx.GetString("cmdDeleteEFormTooltip"),
                    IconJQueryUI = ButtonIconsEnum.Delete,
                    Width = 105,
                    OnClick = $"if (confirm('{Resx.GetString("cmdDeleteEFormMsg")}')) cmdasync('cmd', 'DeleteEForm');"
                };

                AddMenuButton(btnCopy);
            }
        }

        /// <summary>
        ///     Сформировать заглавие для страницы документа
        /// </summary>
        private void RenderDocTitle(TextWriter w, string titleText, bool createDiv = true)
        {
            if (!IsRenderDocTitle && Doc != null && !string.IsNullOrEmpty(Doc.DocumentName))
                return;

            if (createDiv)
                w.Write(@"<div id='divDocTitle' class='v4pageTitle'>");

            
            if (!IsInDocView && !IsPrintVersion && !Doc.IsNew)
            {
                var showImige = Doc.ImageCode > 0 ? "1" : "0";
                w.Write(
                    "<a onclick=\"cmdasync('cmd', 'ShowInDocView', 'DocId', {0},'openImage', {1});\" title=\"{2}\">",
                    Doc.Id, showImige, Resx.GetString("cmdOpenDoc") + " #" + Doc.Id);

                w.Write("<div class=\"v4DivTable\">");
                w.Write("<div class=\"v4DivTableRow\">");

                if (Doc.ImageCode > 0)
                {
                    w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
                    w.Write("<img src=\"/Styles/DocMain.gif\" border=\"0\"/>");
                    w.Write("</div>");
                }

                w.Write("<div class=\"v4DivTableCell v4pageTitle\" style=\"text-align:left;padding-left:0px !important;\">");
                w.Write(titleText);
                w.Write("</div>");

                w.Write("</div>");
                w.Write("</div>");
                w.Write("</a>");
            }
            else
            {
                w.Write( titleText );
            }

            if (createDiv) w.Write("</div>");
        }

        /// <summary>
        ///     Обновить заглавие для страницы документа
        /// </summary>
        public void RefreshDocTitle()
        {
            using (var w = new StringWriter())
            {
                var titleText = GetPageTitle();

                RenderDocTitle(w, titleText, false);
                w.Write("<script>document.title = '{0}'</script>", titleText);
                JS.Write("$('#divDocTitle').html('{0}');", HttpUtility.JavaScriptStringEncode(w.ToString(), false));
            }
        }

        protected void AddDocumentSign()
        {
            var isFirstSign = false;
            var type = V4Request["type"] ?? "";
            var saveAs = V4Request["saveAs"] ?? "";
            var employeeInsteadOfId = string.IsNullOrEmpty(V4Request["EmplInsteadOfId"])
                ? 0
                : Convert.ToInt32(V4Request["EmplInsteadOfId"]);

            sParam.Add("callMethod", "AddDocumentSign");
            sParam.Add("type", type);
            sParam.Add("id", V4Request["id"]);
            sParam.Add("saveAs", saveAs);
            sParam.Add("checkSave", V4Request["checkSave"]);
            sParam.Add("checkNumber", V4Request["checkNumber"]);
            sParam.Add("checkSign", V4Request["checkSign"]);
            sParam.Add("checkSimilar", V4Request["checkSimilar"]);
            sParam.Add("sendMessage", V4Request["SendMessage"]);

            var sendMessage = sParam["sendMessage"] == "1";

            if (!Doc.Signed && sParam["checkSave"] == null)
                if (!CheckBeforeSave())
                    return;

            if (!DocNumberIsCorrect && sParam["checkNumber"] == null)
                if (!CheckDocNumber(sParam, type))
                    return;

            if (type.Length > 0)
                if (sParam["checkSign"] == null && !CheckBeforeSign())
                    return;

            if (Doc.CompareToChanges((Document) OriginalEntity))
            {
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

                if (!EntityId.IsNullEmptyOrZero()) OnDocumentSaved();
            }

            if (!EntityId.IsNullEmptyOrZero())
            {
                _signsManager.AddSign(employeeInsteadOfId, int.Parse(type), out isFirstSign);

                if (!isFirstSign)
                    OnSignChanged();
            }

            //далее определяем что-же делать дальше
            if (type.Equals("1") && sendMessage != DocViewParams.SignMessageWorkDone)
            {
                DocViewParams.SignMessageWorkDone = sendMessage;
                DocViewParams.SaveDVParameters();
            }

            AfterSave(type, sendMessage, employeeInsteadOfId, true, isFirstSign);
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
        /// <param name="refreshParams">Перегрузить форму с параметрами</param>
        protected bool SaveDocument(bool loadDocView, string refreshParams = "")
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

            if (Doc.CompareToChanges((Document) OriginalEntity))
            {
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

                if (!EntityId.IsNullEmptyOrZero()) OnDocumentSaved();
            }

            if (!ShowSaveData)
            {
                if (loadDocView)
                    AfterSave("", sendMessage, employeeInstead, false, false);
                else
                    RefreshDocument(refreshParams);
            }

            ShowSaveData = false;
            return true;
        }

        private void AfterSave(string SignType, bool SendMessage, int EmployeeInsteadOf, bool signChanged,
            bool isfirstSign)
        {
            if (IsKescoRun)
                AfterSave_KescoRun(SignType, SendMessage, EmployeeInsteadOf, signChanged, isfirstSign);
            else
                AfterSave_Srv4js(SignType, SendMessage, EmployeeInsteadOf, signChanged, isfirstSign);
        }

        /// <summary>
        ///     Действия после сохранения документа
        /// </summary>
        /// <param name="SignType">Тип подписи(финальная или обычная)</param>
        /// <param name="SendMessage">Отправить сообщение</param>
        /// <param name="EmployeeInsteadOf">Замещающий сотрудник</param>
        /// <param name="signChanged">true - изменена только подпись(для оптимизации)</param>
        private void AfterSave_Srv4js(string SignType, bool SendMessage, int EmployeeInsteadOf, bool signChanged,
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

            var dv = !IsInDocView;
            var ms = signed && SendMessage;

            var _signer = EmployeeInsteadOf.ToString();
            var msg = SignType.Equals("1") ? _signsManager.GetFinalSignMessage() : _signsManager.GetSignMessage();

            JS.Write(
                "v4_tryOpenDocumentInDocView({0}, {1}, {2}, {3}, {4}, {5}, '{6}');",
                Doc.Id,
                signChanged ? "true" : "false",
                isfirstSign ? "true" : "false",
                dv ? "true" : "false",
                ms ? "true" : "false", _signer,
                msg);

            sParam.Clear();
        }

        /// <summary>
        ///     Действия после сохранения документа
        /// </summary>
        /// <param name="SignType">Тип подписи(финальная или обычная)</param>
        /// <param name="SendMessage">Отправить сообщение</param>
        /// <param name="EmployeeInsteadOf">Замещающий сотрудник</param>
        /// <param name="signChanged">true - изменена только подпись(для оптимизации)</param>
        private void AfterSave_KescoRun(string SignType, bool SendMessage, int EmployeeInsteadOf, bool signChanged,
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
            var dv = !IsInDocView;
            var ms = signed && SendMessage;

            var _signer = EmployeeInsteadOf.ToString();
            var msg = SignType.Equals("1") ? _signsManager.GetFinalSignMessage() : _signsManager.GetSignMessage();

            JS.Write(
                "v4_tryOpenDocumentInDocView({0}, {1}, {2}, {3}, {4}, {5}, '{6}');",
                Doc.Id,
                signChanged ? "true" : "false",
                isfirstSign ? "true" : "false",
                dv ? "true" : "false",
                ms ? "true" : "false", _signer,
                msg);

            sParam.Clear();
        }

        /// <summary>
        ///     Обновить страницу документа(полная перезагрузка страницы)
        /// </summary>
        /// <remarks>
        ///     Если документ не сохранен и не имеет Id, то введеные данные сотрутся
        /// </remarks>
        public void RefreshDocument(string addParam = "")
        {
            if (addParam.Length > 0)
                addParam = Doc.Id.Length > 0 || IsInDocView || NoSign ? "&" + addParam : "?" + addParam;
            V4Navigate(Url4Reload + addParam);
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
            {
                doc.SaveDocumentPositions(true, cmds);                
            }
            if (ShowSaveData)
            {
                ShowMessage(GeterateTextFromSqlCmd(cmds), "SQL TRACE", MessageStatus.Information, "", 605, 605);
                cmds = null;
            }

            return Doc.Id;
        }

        private string GeterateTextFromSqlCmd(List<DBCommand> cmds)
        {
            var sb = new StringBuilder();

            if (cmds.Count == 0)
            {
                sb.Append("Отсутствуют sql-команды для выполнения");
                return sb.ToString();
            }

            var inx = 0;
            var cnt = 0;
            sb.Append("<div style=\"height:500px; overflow:auto;\"");
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
                    foreach (var p in cmd.ParamsIn)
                    {
                        inx++;
                        sb.AppendLine(string.Format("<span style='margin-left:15px'>{0} = '{1}'{2}</span> ", p.Key,
                            p.Value, inx < cnt ? "," : ""));
                    }
                }

                inx = 0;
                cnt = cmd.ParamsOut != null ? cmd.ParamsOut.Count : 0;
                if (cnt > 0)
                {
                    sb.AppendLine("--Исходящие параметры:");
                    foreach (var p in cmd.ParamsOut)
                    {
                        inx++;
                        sb.AppendLine(string.Format("<span style='margin-left:15px'>{0} = '{1}'{2}</span> ", p.Key,
                            p.Value, inx < cnt ? "," : ""));
                    }
                }

                sb.AppendLine("=======================================");
            });
            sb.Append("</div>");
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

            _signsManager.RefreshControlDocSings();
            return true;
        }

        /// <summary>
        ///     Проверка на изменение документа
        /// </summary>
        private bool CheckForChanged()
        {
            var dt = Doc.Changed;
            int usr;
            bool hasFinishSign;
            bool hasSign;

            Document.CheckForChanged(Doc.DocId, ref dt, out hasFinishSign, out hasSign, out usr);

            if (hasFinishSign || hasSign ||
                dt != DateTime.MinValue && usr > 0 && !dt.Equals(Doc.Changed) && usr != CurrentUser.EmployeeId)
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

        private bool CheckForSimilarity(out string SaveAs)
        {
            if (IsKescoRun)
                return CheckForSimilarity_KescoRun(out SaveAs);


            return CheckForSimilarity_Srv4js(out SaveAs);
        }

        /// <summary>
        ///     Проверка на похожесть документа
        /// </summary>
        /// <returns>true - OK</returns>
        private bool CheckForSimilarity_Srv4js(out string SaveAs)
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
            var cnt = Document.SimilarCount(Doc.DocId, Doc.TypeId, Doc.Number, Doc.Date, similarPersons, eqc);

            if (cnt == 0)
                return true;

            var qs = V4Request.QueryString.AllKeys;
            var query = qs.Where(s => s != "idp")
                .Aggregate("cmd(", (current, s) => current + "'" + s + "', '" + V4Request.QueryString[s] + "', ");
            query += "'saveAs', '0');";
            //Вызываем взаимодействие с DocView - CHECKSIMILAR
            JS.Write(@"srv4js(""CHECKSIMILAR"",""type={0}&date={1}", Doc.TypeId,
                Doc.Date == DateTime.MinValue ? "" : Doc.Date.ToString("dd.MM.yyyy"));
            JS.Write("&number={0}&id={1}", Doc.Number, Doc.IsNew ? "0" : Doc.Id);

            JS.Write("&personids=" + similarPersons);

            JS.Write(@""",");

            //очередной параметр srv4js
            //функция которая вызовется после окончания диалога с DocView
            JS.Write("function(rez,obj){"); //begin of callback function ()

            //проверка наличия ошибки после взаимодействия с DocView
            JS.Write(
                "var caption = '';if(rez.error != '1'){caption = 'Документ не был сохранен, т.к. в найдены похожие документы. Откройте Архив документов и пересохраните документ.';}");
            JS.Write("if(rez.error){{v4_showMessage(rez.errorMsg + caption, '{0}', {1});return;}}",
                Resx.GetString("alertError"), (int) MessageStatus.Error);
            //возможны варианты ответов от DocView
            JS.Write("switch(rez.value){"); //begin of switch

            JS.Write("case '0': break;"); //отмена

            JS.Write("case '-1':v4_showMessage('Ошибка взаимодействия с архивом документов!, '{0}', {1}');",
                Resx.GetString("alertError"), (int) MessageStatus.Error);

            JS.Write("case '-2':"); //сохранение несмотря ни на что saveAs = '0' установлено выше
            JS.Write(query);
            JS.Write("break;");

            JS.Write("default:"); //добавление эл формы к сущ. документу saveAs = rez.value
            JS.Write("break;");

            JS.Write("}"); //end of switch

            JS.Write("}"); //end of callback function

            //последний параметр srv4js	- stateObject для CallBack function
            JS.Write(",null);");

            return false;
        }

        /// <summary>
        ///     Проверка на похожесть документа
        /// </summary>
        /// <returns>true - OK</returns>
        private bool CheckForSimilarity_KescoRun(out string SaveAs)
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
            var cnt = Document.SimilarCount(Doc.DocId, Doc.TypeId, Doc.Number, Doc.Date, similarPersons, eqc);

            if (cnt == 0)
                return true;

            JS.Write(DocViewInterop.CheckSimilarDocument(HttpContext.Current,
                Doc.IsNew ? "0" : Doc.Id,
                Doc.TypeId.ToString(),
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
                if (!old.Contains(Doc.DocumentData.PersonId1 ?? 0))
                    return true;
                else
                    dps1.Add(Doc.DocumentData.PersonId1);
            if (Doc.DocumentData.PersonId2 > 0)
                if (!old.Contains(Doc.DocumentData.PersonId2 ?? 0))
                    return true;
                else
                    dps1.Add(Doc.DocumentData.PersonId2);
            if (Doc.DocumentData.PersonId3 > 0)
                if (!old.Contains(Doc.DocumentData.PersonId3 ?? 0))
                    return true;
                else
                    dps1.Add(Doc.DocumentData.PersonId3);
            if (Doc.DocumentData.PersonId4 > 0)
                if (!old.Contains(Doc.DocumentData.PersonId4 ?? 0))
                    return true;
                else
                    dps1.Add(Doc.DocumentData.PersonId4);
            if (Doc.DocumentData.PersonId5 > 0)
                if (!old.Contains(Doc.DocumentData.PersonId5 ?? 0))
                    return true;
                else
                    dps1.Add(Doc.DocumentData.PersonId5);
            if (Doc.DocumentData.PersonId6 > 0)
                if (!old.Contains(Doc.DocumentData.PersonId6 ?? 0))
                    return true;
                else
                    dps1.Add(Doc.DocumentData.PersonId6);

            //если хотябы одно лицо убрали из лиц в документах данных, оно будет удалено из всех лиц  -> надо проверять
            if (!dps1.Any()) return false;
            return oldData.Any(p => !dps1.Contains(Convert.ToInt32(p)));
        }

        /// <summary>
        /// </summary>
        private string GetPersonsForSimilarCheck(List<int> old, List<int> oldData)
        {
            foreach (var p in oldData)
                if (old.Contains(p))
                    old.Remove(p);
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
                if (!string.IsNullOrEmpty(Doc.DocType.URL))
                    url = Doc.DocType.URL + (Doc.DocType.URL.Contains("?") ? "&" : "?") + "id=" + Doc.Id;
                var dNum = Doc.Number == null ? "" : Doc.Number.Trim();

                //TODO: УДАЛЕН ConfirmNumber - переделать
                //JS.Write("alert('УДАЛЕН ConfirmNumber');");

                JS.Write("ConfirmNumber.render('\"" + HttpUtility.HtmlEncode(dNum) + "\" " +
                         Resx.GetString("msgConfirm1") + "<br/>" +
                         Resx.GetString("msgConfirm2") + " \"" + HttpUtility.HtmlEncode(dNum) + "\"?','" + signType +
                         "','" + (!NumberReadOnly && IsEditable) + "','" +
                         (!Doc.Signed && !IsReadOnly) + "','" + url + "','" + Resx.GetString("msgOldTitle") + "', '" +
                         Resx.GetString("msgConfirm") + "', '" + Resx.GetString("msgBtn1") + "', '" +
                         string.Format(Resx.GetString("msgBtn2") + " \"{0}\"", HttpUtility.HtmlEncode(dNum)) + "', '" +
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

            if (ValidateDocument(out li, sParam["callMethod"]))
                return true;

            RenderErrors(li);
            sParam.Clear();

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

                ShowMessage(w + text, Resx.GetString("errIncorrectlyFilledField"), MessageStatus.Error, "", 500, 200);
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
            w.Write("<a onclick=\"cmd('cmd', 'ShowInDocView', 'DocId', {0});\" href=\"#\">", id);
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
                OriginalValue = OriginalEntity == null ? "" : ((Document) OriginalEntity).Description,
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
            StartRenderVariablePart(w, 200, 334, 600);
        }

        /// <summary>
        ///     Начало рендера изменяемой части документа
        /// </summary>
        protected void StartRenderVariablePart(TextWriter w, int labelWidth, int fieldWidth, int fieldsetWidth,
            int fieldRows = 2, bool labelNoWrap = false)
        {
            if (Doc == null) return;

            w.WriteLine("<div>{0}:</div>", Resx.GetString("lblDescription"));
            w.WriteLine("<div>");
            w.Write("<div style='vertical-align: top;'>");

            if (!DocEditable)
            {
                w.Write("<div style='display: inline-block; vertical-align: top;'>");

                w.Write(
                    "<div><span id = \"v4_btnTxaDocEdit\" class=\"ui-icon ui-icon-pencil\" border=0 style=\"display:inline-block;cursor:pointer\" title=\"{0}\" onclick=\"{1}\"></span></div>",
                    Resx.GetString("lblChangeDocumentDescription"), "v4_EditDocumentDescription();");

                w.Write(
                    "<div><span id=\"v4_btnTxaDocDesc\" class=\"ui-icon ui-icon-disk\" border=0 style=\"display:inline-block;cursor:pointer\" title=\"{0}\" onclick=\"{1}\"></span></div>",
                    Resx.GetString("lblSaveDocumentDescription"), "v4_SaveDocumentDescription();");
                w.Write("<script>$('#v4_btnTxaDocDesc').hide();</script>");

                w.Write(
                    "<div><span id=\"v4_btnTxaDocCancel\" class=\"ui-icon ui-icon-closethick\" border=0 style=\"display:inline-block;cursor:pointer\" title=\"{0}\" onclick=\"{1}\"></span></div>",
                    Resx.GetString("lblCancelDocumentDescription"), "v4_CancelDocumentDescription();");
                w.Write("<script>$('#v4_btnTxaDocCancel').hide();</script>");

                w.Write("</div>");
            }

            var txaDesc = new TextArea
            {
                V4Page = this,
                Value = Doc.Description,
                OriginalValue = OriginalEntity == null ? "" : ((Document) OriginalEntity).Description,
                MaxLength = 500,
                Width = fieldWidth,
                ID = "v4_txaDocDesc",
                Rows = fieldRows,
                BindStringValue = Doc.DescriptionBinder,
                NextControl = NextControlAfterDocDesc
            };
            txaDesc.Changed += txaDocDesc_OnChanged;
            V4Controls.Add(txaDesc);
            txaDesc.RenderControl(w);

            if (!DocEditable)
            {
                var txaDescRead = new TextArea
                {
                    V4Page = this,
                    Value = Doc.Description,
                    OriginalValue = OriginalEntity == null ? "" : ((Document) OriginalEntity).Description,
                    MaxLength = 500,
                    Width = fieldWidth,
                    ID = "v4_txaDocDescRead",
                    Rows = fieldRows,
                    BindStringValue = Doc.DescriptionBinder,
                    NextControl = NextControlAfterDocDesc,
                    IsDisabled = true
                };
                txaDescRead.RenderControl(w);
                w.Write("<script>$('#v4_txaDocDesc').hide();</script>");
            }

            w.Write("</div></div>");
            w.Write("<div style=\"clear: both; line-height: 0; height: 0;\">&nbsp;</div>");

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
            //w.Write("</div></fieldset>");
        }


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
                else w.Write(d.GetFullDocumentName(CurrentUser));
                RenderLinkEnd(w);

                w.Write("</td>");

                w.Write("</tr>");

                if (Doc.Date > DateTime.MinValue && d.Date > DateTime.MinValue && Doc.Date < d.Date)
                {
                    w.Write("<tr><td colspan='2'>");
                    RenderNtf(w,
                        new List<Notification>
                        {
                            new Notification
                            {
                                Message = Resx.GetString("NTF_DateBaseDoc"),
                                Status = NtfStatus.Error,
                                DashSpace = false,
                                SizeIsNtf = true
                            }
                        });

                    w.Write("</td></tr>");
                }
            }

            w.Write("</table>");
        }

        private void TranslateDocPageEvent(NameValueCollection Params, bool preDocSigned, bool afterDocSigned)
        {
            if (ItemId == 0) return;

            var cmd = Params["cmd"];
            switch (cmd)
            {
                case "RefreshDoc":
                    JS.Write("$('#btnRefresh').attr('disabled', 'disabled');");
                    JS.Write(
                        "setTimeout(function(){{$('#btnRefresh').removeAttr('disabled');}}, 0);");
                    break;
                case "RefreshSigns":
                    break;
                case "AddDocumentSign":
                    cmd = preDocSigned != afterDocSigned ? "RefreshDoc" : "RefreshSigns";
                    break;

                case "RemoveSign":
                    cmd = preDocSigned != afterDocSigned ? "RefreshDoc" : "RefreshSigns";
                    break;

                case "RemoveSignsAll":
                    cmd = "RefreshForce";
                    break;

                default:
                    return;
            }

            var nvc = new NameValueCollection {{"cmd", cmd}};
            TranslatePageEvent(nvc);
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
                if (!(p is DocPage)) return;
                var dp = (DocPage) p;

                //todo: Добавить проверку на наличие изменений в объекте
                if (dp.ItemId == 0 || dp.ItemId != ItemId || dp.ItemName != ItemName || dp.IDPage == IDPage) return;

                existsMessage = true;
                switch (cmd)
                {
                    case "RefreshDoc":
                    case "RefreshForce":
                        if (!clearCache)
                        {
                            ClearCacheObjects();
                            clearCache = true;
                        }

                        dp.V4Navigate(CurrentUrl);
                        break;
                    case "RefreshSigns":
                        dp._signsManager.RefreshControlDocSings();
                        break;
                }
            });

            if (!existsMessage) return;
            SendCmdAsyncMessage();
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

        /// <summary>
        ///     Отрисовка сообщения, если документ не подписан куратором
        /// </summary>
        /// <returns></returns>
        public virtual string RenderKuratorSign(string contractId)
        {
            using (var w = new StringWriter())
            {
                if (Doc.IsNew || Doc.Unavailable || Doc.DataUnavailable || contractId.IsNullEmptyOrZero()) return "";
                var d = GetObjectById(typeof(Dogovor), contractId) as Dogovor;
                if (d == null || d.Unavailable) return "";
                if (!d.IsDogovor) return "";

                var fl = Doc.DocSigns.Any(sign => sign.SignId > 0 && sign.EmployeeId.ToString() == d.Kurator.Id);
                if (fl) return "";

                w.Write(IsRusLocal ? Doc.TypeDocRu : Doc.TypeDocEn);
                w.Write(" " + Resx.GetString("TTN_msgNotSignedCurator"));

                return w.ToString();
            }
        }

        #region Методы необходимые или доступные для переопределения программисту

        /// <summary>
        ///     Копирование данных документа на контролы.
        /// </summary>
        protected virtual void DocumentToControls()
        {
            if (FieldsToControlsMapping == null) return;

            foreach (var item in FieldsToControlsMapping.Where(item => null != item.Value))
                if (item.Value.IsMultipleSelect)
                {
                    List<Entities.Item> selectedItems;
                    if (!string.IsNullOrEmpty(CopyId))
                        selectedItems = new Document(CopyId).GetDocLinksItems(Convert.ToInt32(item.Value.Id));
                    else
                        selectedItems = Doc.GetDocLinksItems(Convert.ToInt32(item.Value.Id));

                    ((Select) item.Key).SelectedItems.Clear();
                    ((Select) item.Key).SelectedItems.AddRange(selectedItems);
                    //((Select) item.Key).RefreshRequired = true;
                    if (!item.Key.IsReadOnlyAlways)
                        item.Key.IsReadOnly = !DocEditable;
                }
                else
                {
                    if (null == item.Value.Value)
                    {
                        item.Key.Value = string.Empty;
                    }
                    else
                    {
                        if ("DateTime" == item.Value.Value.GetType().Name)
                        {
                            var dp = item.Key as DatePicker;
                            if (dp != null) dp.ValueDate = item.Value.Value as DateTime?;
                        }
                        else
                        {
                            item.Key.Value = item.Value.Value.ToString();
                        }
                    }

                    item.Key.BindDocField = item.Value;
                    //item.Key.OriginalValue = item.Value.ValueString;

                    item.Key.OriginalValue = "";
                    if (OriginalEntity != null)
                        item.Key.OriginalValue = ((Document) OriginalEntity).Fields
                            .First(fld => fld.Value.DocFieldId == item.Value.DocFieldId).Value.ValueString;

                    item.Key.IsRequired = item.Value.IsRequired;
                    if (!item.Key.IsReadOnlyAlways)
                        item.Key.IsReadOnly = !DocEditable;
                }
        }

        /// <summary>
        ///     Добавление Id документа основания соответствующему свойству объекта doc.
        /// </summary>
        protected void SetBaseDocTypeFilter()
        {
            if (!V4IsPostBack)
            {
                var fieldId = Request.QueryString["fieldId"];
                if (!string.IsNullOrEmpty(fieldId) && FieldsToControlsMapping.Any(item => item.Value.Id == fieldId) &&
                    DocId != 0)
                {
                    var item = FieldsToControlsMapping.First(s => s.Value.Id == fieldId);
                    var doc = new Document(DocId.ToString());
                    if (item.Value.IsMultipleSelect)
                    {
                        ((Select) item.Key).SelectedItems.Clear();
                        ((Select) item.Key).SelectedItems.AddRange(new List<Entities.Item>
                            {new Entities.Item {Id = doc.Id, Value = doc}});
                        ((Select) item.Key).RefreshRequired = true;
                    }
                    else
                    {
                        item.Key.BindDocField.Value = DocId;
                    }
                }
            }
        }

        /// <summary>
        ///     Копирование данных документа на контролы - UnBind
        /// </summary>
        protected Dictionary<string, DocField> DocumentToControlsUnBind()
        {
            var ret = new Dictionary<string, DocField>();
            if (FieldsToControlsMapping == null) return null;

            foreach (var item in FieldsToControlsMapping.Where(item =>
                null != item.Value && !item.Value.IsMultipleSelect))
            {
                ret.Add(item.Key.ID, item.Key.BindDocField);
                item.Key.BindDocField = null;
            }

            return ret;
        }

        protected void DocumentToControlsRestoreBind(Dictionary<string, DocField> fields)
        {
            if (FieldsToControlsMapping == null) return;
            foreach (var item in FieldsToControlsMapping.Where(item =>
                null != item.Value && !item.Value.IsMultipleSelect)) item.Key.BindDocField = fields[item.Key.ID];
        }

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

            if (Doc.DocType.NumberGenType != NumGenTypes.MustBeGenerated && !GenerateNumber &&
                Doc.Number.IsNullEmptyOrZero())
                errors.Add(Resx.GetString("DocNumberNotExist"));

            if (Doc.Date == DateTime.MinValue)
            {
                if (Doc.DocType.NumberGenType == NumGenTypes.MustBeGenerated)
                    Doc.Date = DateTime.Now;
                else
                    errors.Add(Resx.GetString("DocDateNotExist"));
            }

            if (Doc.Fields != null)
            {
                var msg = Resx.GetString("msgNoDataField");

                foreach (var f in Doc.Fields.Values)
                    // если связь с документыДанные(DataColomnName) отсутствует, то не проверяем
                    if (f.IsRequired && !f.DataColomnName.In("", "КодДокумента") && !f.Id.In(exeptions))
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

            return errors.Count <= 0;
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


            _signsManager.RefreshControlDocSings();

            ShowSaveButton = DocEditable;
            ShowRefreshButton = true;
            SetDocMenuButtons();

            // RefreshMenuButtons();
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

        //Обновляет поля специфичные для данного документа(без полной перезагрузки страницы)
        protected override void RefreshNotification()
        {
            ClearCacheObjects();
            // в блоке RefreshDoc уже получили актуальные подписи
            _signsManager.RefreshControlDocSings();

            RefreshDocTitle();
            RefreshNumber();
            RefreshNtf();
            RefreshManualNotifications();
        }


        /// <summary>
        ///     Очистка загруженных внешних свойств объекта
        /// </summary>
        protected override void ClearLoadedExternalProperties()
        {
            base.ClearLoadedExternalProperties();
            Doc?.LoadedExternalProperties?.Clear();
        }

        // Виртуальная функция, чтобы программист мог обновить те части формы, какие считает нужным при нажатии кнопки Обновить
        protected virtual void RefreshManualNotifications()
        {
        }

        /// <summary>
        ///     Обновляет табличные поля, специфичные для данного документа(без полной перезагрузки страницы)
        /// </summary>
        public virtual void RefreshTableCurrentDoc()
        {
        }

        /// <summary>
        ///     Подготовка документа к копированию. Удаление всех лишних полей.
        /// </summary>
        protected virtual void PrepareDocToCopy(Document doc)
        {
            doc.Id = "0";
            doc.Number = string.Empty;
            doc.DocumentData.Id = string.Empty;
            doc.ChangePersonID = 0;
            doc.DocumentData.ChangePersonID = 0;
            doc.Changed = DateTime.MinValue;
            doc.DocumentData.ChangeDate = DateTime.MinValue;
            //doc.Date = DateTime.MinValue;
            doc.DocSignsClear();

            //TODO: Почему документы основания не должны копироваться  
            if (doc.Type == DocTypeEnum.ТоварноТранспортнаяНакладная)
            {
                doc.RemoveAllBaseDocs(doc.GetDocField("707").DocFieldId);
                doc.RemoveAllBaseDocs(doc.GetDocField("709").DocFieldId);
                doc.RemoveAllBaseDocs(doc.GetDocField("708").DocFieldId);
                doc.RemoveAllBaseDocs(doc.GetDocField("1598").DocFieldId);
                doc.RemoveAllBaseDocs(doc.GetDocField("1707").DocFieldId);
                doc.RemoveAllBaseDocs(doc.GetDocField("1632").DocFieldId);
                doc.RemoveAllBaseDocs(doc.GetDocField("1579").DocFieldId);
                doc.RemoveAllBaseDocs(doc.GetDocField("1631").DocFieldId);
            }
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
            if (Doc != null && Doc.DocType != null)
                return Doc.DocType.HelpURL;

            return null;
        }

        /// <summary>
        ///     Загружает данные связаные с текущим документом
        /// </summary>
        protected override void EntityLoadData(string idEntity)
        {
            if (OriginalEntity != null || Doc.IsNew) return;

            Doc.Load();
            Doc.DocumentData.Load();

            var positions = Doc as IDocumentWithPositions;
            if (positions != null)
                ((IDocumentWithPositions) Doc).LoadDocumentPositions();

            OriginalEntity = Doc.Clone();
        }

        #endregion

        #region номер документа

        /// <summary>
        ///     Обновление номера, даты и наименования документа
        /// </summary>
        protected void RefreshrDocNumDateNameRows()
        {
            using (var w = new StringWriter())
            {
                RenderDocNumDateNameRows(w);
                JS.Write(
                    "if(document.getElementById('trDocNumDateName'))document.getElementById('trDocNumDateName').innerHTML={0};",
                    HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///     Вывод номера, даты и наименования документа
        /// </summary>
        /// <param name="w"></param>
        protected void RenderDocNumDateNameRows(TextWriter w)
        {
            if (IsInDocView || !DocEditable) return;

            if (!V4IsPostBack) w.Write(@"<div id=""trDocNumDateName"">");

            w.Write("<div class=\"v4DivTable\">");


            w.Write("<div class=\"v4DivTableRow\">");

            w.Write(
                $"<div class=\"v4DivTableCell v4PaddingCell\" style=\"{(DocEditable ? "width:130px; " : "")}text-align:left;\">");
            w.Write($"{Resx.GetString("lblDocNumber")}:");
            w.Write("</div>");
            w.Write(
                $"<div class=\"v4DivTableCell v4PaddingCell\" style=\"text-align:left;{(Doc.IsNew && Doc.DocType.NumberGenType == NumGenTypes.CanBeGenerated ? "padding-top:15px;" : "")}\">");
            RenderNumber(w);
            w.Write("</div>");

            if (ShowDocDate)
            {
                w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
                w.Write($"{Resx.GetString("lblDate")}:");
                w.Write("</div>");

                w.Write("<div class=\"v4DivTableCell\">");
                if (DocEditable && !IsPrintVersion)
                {
                    _dateDoc = new DatePicker
                    {
                        // TabIndex = 2,
                        ID = "DocDate",
                        V4Page = this,
                        IsRequired = true,
                        IsReadOnly = DocDateReadOnly,
                        Value = Doc.Date == DateTime.MinValue ? "" : Doc.Date.ToString("dd.MM.yyyy"),
                        OriginalValue = OriginalEntity == null
                            ? ""
                            : ((Document) OriginalEntity).Date.ToString("dd.MM.yyyy") + "",
                        IsShowEditingStatus = true,
                        NextControl = NextControlAfterDate
                    };

                    _dateDoc.Changed += OnDocDateChanged;
                    V4Controls.Add(_dateDoc);
                    _dateDoc.RenderControl(w);
                }
                else
                {
                    w.Write(Doc.Date == DateTime.MinValue ? "" : Doc.Date.ToString("dd.MM.yyyy"));
                }

                w.Write("</div>");
            }

            w.Write("</div>");
            w.Write("</div>");

            if ((DocEditable || !string.IsNullOrEmpty(Doc.DocumentName)) && Doc.DocType.NameExist)
            {
                w.Write("<div class='spacer'></div>");

                w.Write("<div class=\"v4DivTable\">");
                w.Write("<div class=\"v4DivTableRow\">");


                w.Write(
                    $"<div class=\"v4DivTableCell v4PaddingCell\" style=\"{(DocEditable ? "width: 130px; " : "")}text-align:left;\">");
                w.Write($"{Resx.GetString("lblDocName")}:");
                w.Write("</div>");


                w.Write("<div class=\"v4DivTableCell\">");
                if (DocEditable && !IsPrintVersion)
                {
                    var docName = new TextBox
                    {
                        //Width = Unit.Percentage(100),
                        Width = Unit.Pixel(400),
                        //  TabIndex = 3,
                        ID = "docName",
                        V4Page = this,
                        Value = Doc.DocumentName ?? "",
                        OriginalValue = OriginalEntity == null ? "" : ((Document) OriginalEntity).DocumentName
                    };
                    docName.Changed += DocNameChanged;
                    V4Controls.Add(docName);
                    docName.RenderControl(w);
                }
                else
                {
                    w.Write(Doc.DocumentName ?? "");
                }

                w.Write("</div>");

                w.Write("</div>");
                w.Write("</div>");
            }

            if (!V4IsPostBack)
            {
                w.Write("<div class='spacer'></div>");
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
        public virtual bool DocEditable => !Doc.Signed && !IsInDocView;

        /// <summary>
        ///     Документ подписан, хотябы один раз
        /// </summary>
        public bool DocSigned => Doc.Signed;

        /// <summary>
        ///     Вывод ссылки на генерацию номера документа
        /// </summary>
        /// <param name="w"></param>
        protected void RenderNumberContent(TextWriter w)
        {
            if (!DocEditable || IsPrintVersion || NumberReadOnly)
            {
                if (!NumberNotExists) w.Write(HttpUtility.HtmlEncode(Doc.Number));
                RenderNumberNotExistsInput(w);
                return;
            }

            if (Doc.DocType.NumberGenType == NumGenTypes.MustBeGenerated)
            {
                if (Doc.IsNew)
                    w.Write("<nobr>{0}</nobr><br>", Resx.GetString("generatingNumberOfDocument"));
                else
                    w.Write("<nobr>{0}</nobr>", HttpUtility.HtmlEncode(Doc.Number));
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
                    if (Doc.IsNew)
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

            w.Write("<table cellspacing=\"0\" cellpadding=\"0\" border=\"0\" style=\"padding-top: 1px;\"><tr><td>");
            if (!DocEditable || IsPrintVersion) cb.IsDisabled = true;
          
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
                JS.Write("if(document.getElementById('docNumber'))document.getElementById('docNumber').innerHTML={0};",
                    HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///     Обязательность номера документа
        /// </summary>
        public bool NumberRequired;

        /// <summary>
        ///     Обработчик изменения флага "у документа нет номера"
        /// </summary>
        /// <param name="notExist">true - флаг включен</param>
        protected void SetNumberNotExists(bool notExist)
        {
            NumberNotExists = notExist;
            NumberReadOnly = NumberNotExists && !NumberRequired;
            RefreshNumber();
            _dateDoc.Focus();
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
            _dateDoc.Focus();
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
            Doc.DocumentName = e.NewValue;
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
    }
}