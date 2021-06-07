using System;
using System.IO;
using System.Linq;
using System.Web;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Entities.Documents;
using Kesco.Lib.Entities.Transactions;
using Kesco.Lib.Log;

namespace Kesco.Lib.Web.Controls.V4.Common.DocumentPage
{
    /// <summary>
    ///     Класс управление контролов подписей
    /// </summary>
    public class SignsManager
    {
        private const string ContainerHtmlId = "v4_divDocSigns";

        private const string ContainerSignFormId = "v4_divSignForm";
        private const string ContainerSignFormWarning = "v4_divSignFormWarning";
        private const string ContainerSignFormButtons = "v4_divSignFormButtons";
        private const string ContainerSignFormMsg = "v4_divSignFormMsg";

        /// <summary>
        ///     Указатель на DocPage
        /// </summary>
        private readonly DocPage _docPage;

        /// <summary>
        ///     Конструктор класс менеджер подписей
        /// </summary>
        /// <param name="docPage">Страница документа</param>
        public SignsManager(DocPage docPage)
        {
            _docPage = docPage;
        }

        /// <summary>
        ///     Обновление контрола подписей
        /// </summary>
        public void RefreshControlDocSings()
        {
            try
            {
                using (var w = new StringWriter())
                {
                    RenderControlDocSings(w);
                    var docSigns = w.ToString();
                    _docPage.JS.Write(
                        $"var objDivSign = document.getElementById(\"{ContainerHtmlId}\"); if(objDivSign) objDivSign.innerHTML=\"{HttpUtility.JavaScriptStringEncode(docSigns)}\";");
                }
            }
            catch (Exception e)
            {
                _docPage.ShowMessage("Не удалось обновить подписи документов: " + e.Message);
            }
        }

        #region Render

        /// <summary>
        ///     Формирование разметки контрола подписей
        /// </summary>
        /// <param name="w">Поток вывода</param>
        public void RenderControlDocSings(TextWriter w)
        {
            if (_docPage.IsInDocView)
            {
                w.Write("");
                return;
            }

            if (!_docPage.V4IsPostBack)
            {
                RenderControlBeginContainer(w, ContainerSignFormId, false, "style=\"display:none;\"");

                RenderControlBeginContainer(w, ContainerSignFormWarning, "v4DivTable");
                RenderControlBeginContainer(w, $"{ContainerSignFormWarning}_0", "v4DivTableRow");

                RenderControlBeginContainer(w, $"{ContainerSignFormWarning}_0_0", "v4DivTableCell");
                RenderControlEndContainer(w);

                RenderControlBeginContainer(w, $"{ContainerSignFormWarning}_0_1", "v4DivTableCell",
                    "style=\"text-align:left\"");
                RenderControlEndContainer(w);

                RenderControlEndContainer(w);
                RenderControlEndContainer(w);


                RenderControlBeginContainer(w, ContainerSignFormButtons, true);
                RenderControlBeginContainer(w, ContainerSignFormMsg, true);


                RenderControlEndContainer(w);


                RenderControlBeginContainer(w, ContainerHtmlId, "v4pageDocHeader");
            }

            RenderControlBeginContainer(w, $"{ContainerHtmlId}_Signs", "v4DivTable",
                "style=\"float:right; margin-right: 5px\"");

            _docPage.Doc.GetSignsFromDb();
            var signs = _docPage.Doc.DocSigns;
            signs?.ForEach(delegate(DocSign sign)
            {
                var signId = sign.Id.Replace('-', '_');
                RenderControlBeginContainer(w, $"{ContainerHtmlId}_{signId}", "v4DivTableRow");

                RenderControlBeginContainer(w, $"{ContainerHtmlId}_{signId}_0", "v4DivTableCell",
                    "style=\"text-align:left\"");
                w.Write(sign.SignText);
                RenderControlEndContainer(w);

                RenderControlBeginContainer(w, $"{ContainerHtmlId}_{signId}_1", "v4DivTableCell v4NoWrap",
                    "style=\"padding-left:5px;text-align:left;\"");
                _docPage.RenderLinkEmployee(w, $"{ContainerHtmlId}_{signId}_{sign.EmployeeInsteadOfId}_1",
                    sign.EmployeeInsteadOfId.ToString(), sign.EmployeeInsteadOfFio, NtfStatus.Empty, false);
                if (sign.EmployeeId != sign.EmployeeInsteadOfId)
                {
                    w.Write(" / ");
                    _docPage.RenderLinkEmployee(w, $"{ContainerHtmlId}_{signId}_{sign.EmployeeId}_1_2",
                        sign.EmployeeId.ToString(), sign.EmployeeFio, NtfStatus.Empty, false);
                }

                RenderControlEndContainer(w);


                RenderControlBeginContainer(w, $"{ContainerHtmlId}_{signId}_2", "v4DivTableCell v4NoWrap localDT",
                    "style=\"padding-left:5px\"", _docPage.V4IsPostBack ? "localTime=false" : "");
                if (!_docPage.V4IsPostBack)
                    w.Write("<script>document.write(v4_toLocalTime(\"{0:yyyy-MM-dd HH:mm:ss}\",\"{1}\"));</script>",
                        sign.Date, "dd.mm.yyyy hh:mi:ss");
                else
                    w.Write(sign.Date.ToString("yyyy-MM-dd HH:mm:ss"));

                RenderControlEndContainer(w);

                if (!_docPage.IsPrintVersion && sign.CanDelete == 1)
                {
                    RenderControlBeginContainer(w, $"{ContainerHtmlId}_{signId}_3", "v4DivTableCell",
                        "style=\"padding-left:5px\"");
                    w.Write(
                        $"<img src=\"/STYLES/Delete.gif\" border=\"0\" alt=\"{_docPage.Resx.GetString("lCancelSign")}\" style=\"cursor: pointer\"");
                    w.Write("onclick =\"");
                    RenderDeleteSignEventOnClick(w, sign);
                    w.Write("\">");
                    RenderControlEndContainer(w);
                }

                RenderControlEndContainer(w);
            });

            RenderControlEndContainer(w);

            if (!_docPage.IsPrintVersion && !_docPage.Doc.Finished)
            {
                RenderControlBeginContainer(w, $"{ContainerHtmlId}_Link", "v4DivTable",
                    "style=\"clear:right; float:right; margin-top: 4px; margin-bottom: 4px; margin-right: 5px\"");

                RenderControlBeginContainer(w, $"{ContainerHtmlId}_Link_0", "v4DivTableRow");


                RenderControlBeginContainer(w, $"{ContainerHtmlId}_link_0_1", "v4DivTableCell v4NoWrap",
                    "style=\"padding-left:5px;\"");
                w.Write(
                    "<a id=\"sign-final\" style=\"color: #6495ED\" onclick=\"v4_prepareSignDocument({0}, 1, {1}, '{2}','{3}','{4}','{5}','{6}','{7}');\">{8}</a>",
                    _docPage.CurrentUser.EmployeeId,
                    DocViewParams.SignMessageWorkDone ? 1 : 0,
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("msgFinishSign")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("msgSendFinishMsg")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("DOCUMENT_Sign_Title")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("msgSign")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("msgSignInsteadOf")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("cmdCancel")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("cmdSignFinal"))
                );


                RenderControlEndContainer(w);
                RenderControlBeginContainer(w, $"{ContainerHtmlId}_link_0_0", "v4DivTableCell v4NoWrap",
                    "style=\"padding-left:15px;\"");
                w.Write(
                    "<a id=\"sign-common\" style=\"color: blue\" onclick=\"v4_prepareSignDocument({0}, 0, 1,'','{1}','{2}','{3}','{4}','{5}');\">{6}</a>",
                    _docPage.CurrentUser.EmployeeId,
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("msgSendMsg")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("DOCUMENT_Sign_Title")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("msgSign")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("msgSignInsteadOf")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("cmdCancel")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("cmdSign")));

                RenderControlEndContainer(w);

                RenderControlEndContainer(w);

                RenderControlEndContainer(w);
            }

            RenderControlBeginContainer(w, "", false, "style=\"clear: both; line-height: 0; height: 0;\"");
            w.Write("&nbsp;");
            RenderControlEndContainer(w);

            if (!_docPage.V4IsPostBack)
            {
                RenderControlEndContainer(w);
            }
        }

        /// <summary>
        ///     Формирование разметки начала контейнера в контроле подписей
        /// </summary>
        /// <param name="w">Поток вывода</param>
        /// <param name="htmlId">Идентификатор контрейна</param>
        /// <param name="closeTag">Отрисовывать срасу же и закрывающий тег</param>
        /// <param name="style">Дополнительный стиль для контейнера</param>
        private void RenderControlBeginContainer(TextWriter w, string htmlId, bool closeTag, string style = "")
        {
            RenderControlBeginContainer(w, htmlId, "", style);
            if (closeTag) RenderControlEndContainer(w);
        }

        /// <summary>
        ///     Формирование разметки открывающего тега контейнера в контроле подписей
        /// </summary>
        /// <param name="w">Поток вывода</param>
        /// <param name="htmlId">Идентификатор контрейна</param>
        /// <param name="className">CSS-класс контейнера</param>
        /// <param name="style">Дополнительный стиль для контейнера</param>
        /// <param name="data">Дополнительные аттрибуты контейнера</param>
        private void RenderControlBeginContainer(TextWriter w, string htmlId, string className = "", string style = "",
            string data = "")
        {
            w.Write($"<div id = \"{htmlId}\" class=\"{className}\" {style} {data}>");
        }

        /// <summary>
        ///     Формирование разметки закрывающего тега контейнера в контроле подписей
        /// </summary>
        /// <param name="w">Поток вывода</param>
        private void RenderControlEndContainer(TextWriter w)
        {
            w.Write("</div>");
        }

        /// <summary>
        ///     Формирование разметки клиентского скрипта для кнопки удаления подписи
        /// </summary>
        /// <param name="w">Поток вывода</param>
        /// <param name="sign">Объект подпись</param>
        private void RenderDeleteSignEventOnClick(TextWriter w, DocSign sign)
        {
            if (sign.EmployeeId == _docPage.CurrentUser.EmployeeId ||
                sign.EmployeeInsteadOfId == _docPage.CurrentUser.EmployeeId)
                w.Write($"cmdasync('cmd', 'RemoveSign','IdSign', '{sign.SignId}');");
            else
                w.Write("v4_showConfirm('{0}','{1}','{2}','{3}',{4},{5},'{6}','{7}','{8}',{9},{10});",
                    HttpUtility.JavaScriptStringEncode(string.Format(
                        _docPage.Resx.GetString("msgDelSign").Replace(Environment.NewLine, "<br>"),
                        sign.EmployeeInsteadOfFio)),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("alertWarning")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("CONFIRM_StdCaptionYes")),
                    HttpUtility.JavaScriptStringEncode(_docPage.Resx.GetString("CONFIRM_StdCaptionNo")),
                    100,
                    100,
                    HttpUtility.JavaScriptStringEncode($"cmdasync('cmd', 'RemoveSign','IdSign', '{sign.SignId}');"),
                    "",
                    HttpUtility.JavaScriptStringEncode("sign-common"),
                    500,
                    150);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Удалить все подписи документа
        /// </summary>
        /// <returns></returns>
        public bool RemoveSignsAll()
        {
            _docPage.Doc.GetSignsFromDb(); // кешируются в свойствах объекта
            var singsSorted = _docPage.Doc.DocSigns.AsEnumerable().OrderByDescending(o => o.Date).ToList();

            foreach (var s in singsSorted)
                DocSign.RemoveSign(s.Id);
            _docPage.Doc.GetSignsFromDb();
            if (!_docPage.Doc.Signed) return true;

            _docPage.ShowMessage(_docPage.Resx.GetString("msgDelSignAll"), _docPage.Resx.GetString("CONFIRM_StdTitle"));

            return false;
        }

        /// <summary>
        ///     Удаление подпись документа
        /// </summary>
        /// <param name="signId">Идентификатор подписи</param>
        /// <returns>Результат операции</returns>
        public bool RemoveSign(int signId)
        {
            var sign = _docPage.Doc.DocSigns.First(i => i.Id == signId.ToString());

            if (sign == null || sign.Unavailable)
            {
                // Подпись недоступна, возможно она уже удалена.
                _docPage.ShowMessage(_docPage.Resx.GetString("msgDelSignUnavaible"));
                return false;
            }

            try
            {
                if (sign.SignType == 1)
                {
                    var docTrans = Transaction.GetTransactionsByDocId(_docPage.Doc.DocId);

                    if (docTrans.Count > 0)
                        Transaction.RemoveTrans(_docPage.Doc.Id);
                }

                DocSign.RemoveSign(sign.Id);
                _docPage.Doc.GetSignsFromDb();
            }
            catch (Exception e)
            {
                Logger.WriteEx(e);
                _docPage.ShowMessage(e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Добавление подписи под документом
        /// </summary>
        /// <param name="employeeInsteadOfId">Идентификатор сотрудника ЗА</param>
        /// <param name="signType">Тип подписи(1-завершающая; 0-обычная)</param>
        /// <param name="isFirstSign">Признак первой подписи под документом</param>
        public void AddSign(int employeeInsteadOfId, int signType, out bool isFirstSign)
        {
            isFirstSign = !_docPage.Doc.Signed;

            var sign = new DocSign
            {
                DocId = _docPage.Doc.DocId,
                EmployeeId = _docPage.CurrentUser.EmployeeId,
                EmployeeInsteadOfId = employeeInsteadOfId,
                SignType = Convert.ToByte(signType),
                Date = DateTime.Now
            };

            sign.Create();
            _docPage.Doc.GetSignsFromDb();
        }

        /// <summary>
        ///     Подлучение текста сообщения для отправки после подписания обычной подписью
        /// </summary>
        /// <returns>Текст сообщения</returns>
        public string GetSignMessage()
        {
            var signText = "";

            if (!_docPage.Doc.Unavailable && _docPage.Doc.TypeId > 0)
                signText = DocSign.GetSignText(_docPage.Doc.TypeId, 0);

            if (signText.Length == 0)
                signText = _docPage.Resx.GetString("ntf_DocIsSigned");

            return signText;
        }

        /// <summary>
        ///     Подлучение текста сообщения для отправки после подписания завершающей подписью
        /// </summary>
        /// <returns>Текст сообщения</returns>
        public string GetFinalSignMessage()
        {
            var signText = "";

            if (!_docPage.Doc.Unavailable && _docPage.Doc.TypeId > 0)
                signText = DocSign.GetSignText(_docPage.Doc.TypeId, 1);

            if (signText.Length == 0)
                signText = _docPage.Resx.GetString("ntf_DocIsCompleted");

            return signText;
        }

        #endregion
    }
}