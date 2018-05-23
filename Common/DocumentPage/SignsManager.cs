using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Web;
using Kesco.Lib.Entities.Corporate;
using Kesco.Lib.Entities.Documents;
using Kesco.Lib.Entities.Transactions;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Settings;

namespace Kesco.Lib.Web.Controls.V4.Common.DocumentPage
{
    /// <summary>
    ///     Класс работы с подписями.
    ///     Вся реализация работы с подписями находится в этом классе
    /// </summary>
    /// <remarks>dependency injection pattern</remarks>
    public class SignsManager
    {
        /// <summary>
        ///     Запрет на инициализацию класса без параметров
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private SignsManager()
        {
        }

        /// <summary>
        ///     Указатель на DocPage
        /// </summary>
        private readonly DocPage _docPage;

        /// <summary>
        ///     указатель на документ, упрощение доступа к _docPage
        /// </summary>
        private Document _doc
        {
            get { return _docPage.Doc; }
        }

        /// <summary>
        ///     указатель на ресурсы, упрощение доступа
        /// </summary>
        private ResourceManager _resx
        {
            get { return _docPage.Resx; }
        }

        /// <summary>
        ///     указатель на скрипты, упрощение доступа
        /// </summary>
        private TextWriter _js
        {
            get { return _docPage.JS; }
        }

        public SignsManager(DocPage docPage)
        {
            _docPage = docPage;
        }

        /// <summary>
        ///     Результат выполнения операции сохранения документа
        /// </summary>
        public int ResultSave;

        /// <summary>
        ///     Формирование частей блока подписи документа
        /// </summary>
        /// <param name="w">Клиентский поток</param>
        /// <param name="wt">скрипт обновления дат, нужен при обновлении</param>
        public void RenderSigns(TextWriter w, TextWriter wt = null)
        {
            // контейнер подписей
            if (!_docPage.V4IsPostBack)
            {
                w.Write(_docPage.IsInDocView
                    ? "<div id=\"divDocSigns\" class=\"v5divDocSignsIE8\">"
                    : "<div id=\"divDocSigns\" class=\"v5divDocSigns\">");
            }

            // блок подписей
            w.Write(@"<div id=""signsTable"" style = ""display: table;width:99%;"">");

            // ??
            if (!_doc.Unavailable && _doc.DocType != null && !_doc.DocType.Unavailable &&
                !String.IsNullOrEmpty(_doc.DocType.URL))
            {
                if (!_doc.Signed && !_docPage.IsPrintVersion && _docPage.IsInDocView)
                {
                    w.Write("<div class=\"blackA\" width=\"100%\" align=\"left\">");
                    var qs = _docPage.Request.QueryString.ToString()
                        .Split(new[] {"&"}, StringSplitOptions.RemoveEmptyEntries);
                    var param = "";
                    foreach (var s in qs)
                    {
                        var pair = s.Split(new[] {"="}, StringSplitOptions.RemoveEmptyEntries);
                        if (pair.Length == 2)
                        {
                            if (pair[0].ToLower() != "type" && pair[0].ToLower() != "isie8" &&
                                pair[0].ToLower() != "docview" && pair[0].ToLower() != "nosign")
                            {
                                param += "&" + pair[0] + "=" + pair[1];
                            }
                        }
                        if (!String.IsNullOrEmpty(param))
                        {
                            param = param.Remove(0, 1);
                            param = "?" + param;
                        }
                    }
                    var url = _doc.DocType.URL + param;
                    w.Write(
                        "<a class=\"lnkbtn\" href=\"#\" onclick=\"v4_windowOpen('{0}');\">",
                        url);
                    w.Write("<img src=\"../../STYLES/Edit.gif\" border=\"0\"> {0}", _resx.GetString("cmdEdit"));
                    w.Write("</a>");
                    w.Write("</div>");
                }
            }

            if (!_docPage.NoSign && !_docPage.IsInDocView)
            {
                var enableAddSigns = true;

                if (!_doc.IsNew)
                {
                    var signs = _doc.DocSigns;

                    // подписанты
                    RenderSignedRows(signs, w, wt, out enableAddSigns);
                }

                if (!_docPage.IsPrintVersion && !_doc.Finished && enableAddSigns && !_docPage.IsInDocView)
                {
                    // базовые надписи подписи 
                    RenderSignLinkButton(w);
                }
            }
            else if (_doc.IsNew)
            {
                w.Write("&nbsp;");
            }

            #region надпись "Документ не по проектам холдинга"

            if (!_doc.IsNew)
            {
                if (_doc.IsNoBProject())
                {
                    w.Write("<div id=\"msgNoBProject\" style=\"color:red; text-align:right;\">");
                    w.Write(_resx.GetString("msgNoBProject"));
                    w.Write("</div>");
                }
            }

            #endregion

            // конец блок подписей
            w.Write("</div>");

            // конец контейнера подписей
            if (!_docPage.V4IsPostBack)
            {
                w.Write("</div>");
            }
        }

        /// <summary>
        ///     Сформировать подписантов документа
        /// </summary>
        /// <param name="signs">Все подписи документа</param>
        /// <param name="w">клиентский поток</param>
        /// <param name="wt">поток для обновления полей дат</param>
        /// <param name="enableAddSigns">возможность работы с подписями</param>
        private void RenderSignedRows(List<DocSign> signs, TextWriter w, TextWriter wt, out bool enableAddSigns)
        {
            enableAddSigns = true;

            if (signs != null && signs.Count > 0)
            {
                // последний элемент со статусом можно подписать
                enableAddSigns = signs[signs.Count - 1].CanSign == 1;

                var userEditUrl = Config.user_form;

                w.Write("<div id=\"divSignesRows\" align=\"right\">");
                w.Write("<table>");

                foreach (var sign in signs)
                {
                    w.Write("<tr>");
                    w.Write("<td class=\"edited\" noWrap>{0}</td>", sign.SignText);
                    w.Write("<td class=\"edited\">");

                    // Замещающий сотрудник
                    if (sign.EmployeeId != sign.EmployeeInsteadOf)
                    {
                        w.Write(
                            "<a class='v4_callerControl' data-id='{1}' caller-type='2' href=\"#\" onclick=\"v4_windowOpen('{0}?id={1}');\">",
                            userEditUrl, sign.EmployeeInsteadOf);
                        w.Write(sign.SubEmployeeFio);
                        w.Write("</a>  / ");
                    }

                    w.Write(
                        "<a class='v4_callerControl' data-id='{1}' caller-type='2' href=\"#\" onclick=\"v4_windowOpen('{0}?id={1}');\">",
                        userEditUrl, sign.EmployeeInsteadOf);
                    w.Write(sign.EmployeeFio);
                    w.Write("</a></td>");

                    var date = sign.Date.ToString("yyyy-MM-dd HH:mm:ss");

                    // Дата подписи - Вывод даты скриптом, т.к. только клиент знает свой локальный часовой пояс. Другие времена в блоке подписей - аналогично.
                    w.Write("<td class=\"edited\">");
                    w.Write("<div id=\"divDocSign{0}\">", sign.SignId);

                    if (wt!=null)
                        wt.Write(
                            "if (document.all(\"divDocSign{2}\")) document.all(\"divDocSign{2}\").innerText=v4_toLocalTime(\"{0}\",\"{1}\");",
                            date, "dd.mm.yyyy hh:mi:ss", sign.SignId);
                    else
                        w.Write("<script>document.write(v4_toLocalTime(\"{0}\",\"{1}\"));</script>", date,
                            "dd.mm.yyyy hh:mi:ss");

                    w.Write("</div>");
                    w.Write("</td>");

                    // ссылка на удаление подписи
                    if (!_docPage.IsPrintVersion &&
                        (_docPage.V4Request == null || !"yes".Equals(_docPage.V4Request["readonly"])))
                    {
                        if (sign.CanDelete == 1)
                        {
                            w.Write(
                                "<td><img src=\"/STYLES/Delete.gif\" border=\"0\" alt=\"{0}\" style=\"cursor: pointer\" onclick=\"cmd('cmd', 'RemoveSign','IdSign', '{1}', 'ask', '1');\"></td>",
                                _resx.GetString("lCancelSign"), sign.SignId);
                        }
                        else
                        {
                            w.Write("<td>&nbsp;</td>");
                        }
                    }
                    w.Write("</tr>");
                }
                w.Write("</table>");
                w.Write("</div>");
            }
        }

        /// <summary>
        ///     Формирует начальную строки подписи документа
        /// </summary>
        /// <param name="w">клиентский поток</param>
        private void RenderSignLinkButton(TextWriter w)
        {
            w.Write(@"<div id=""divEmptyDocSigns"">");
            w.Write(@"<div style=""text-align: right; word-wrap: normal"">");
            w.Write(
                @"<a id=""sign-final"" style=""color: #6495ED"" href=""javascript:void(0)"" onclick=""cmd('cmd','AddSign', 'type', 1);"">{0}</a> &nbsp; &nbsp; &nbsp;",
                _resx.GetString("cmdSignFinal"));
            w.Write(
                @"<a id=""sign-common"" style=""color: blue"" href=""javascript:void(0)"" onclick=""cmd('cmd','AddSign', 'type', 0);"">{0}</a>",
                _resx.GetString("cmdSign"));
            w.Write(@"</div>");
            w.Write(@"</div>");
        }


        /// <summary>
        ///     Удаление подписи
        /// </summary>
        /// <param name="signId"></param>
        /// <param name="ask"></param>
        public bool RemoveSign(string signId, bool ask)
        {
            var sign = _doc.DocSigns.First(i => i.Id == signId);

            if (sign.Unavailable)
            {
                // Подпись недоступна, возможно она уже удалена.
                _docPage.ShowMessage(_resx.GetString("msgDelSignUnavaible"));
                return false;
            }

            if (ask && (sign.EmployeeId != _docPage.CurrentUser.EmployeeId || sign.SignType == 1))
            {
                var b = new StringBuilder();
                var go = true;

                if (sign.SignType == 1) //подпись финальная
                {
                    if (sign.EmployeeId != _docPage.CurrentUser.EmployeeId &&
                        sign.EmployeeInsteadOf != _docPage.CurrentUser.EmployeeId)
                    {
                        // сотрудник не совпадает
                        // Удаление завершающей подписи другого сотрудника невозможно.
                        b.Append(_resx.GetString("msgDelSignOtherEmplUnavaible"));
                        go = false;
                    }
                    else //сотрудник совпадает
                    {
                        var docTrans = Transaction.GetTransactionsByDocId(_doc.DocId);
                        if (docTrans.Count > 0) //есть транзакции
                        {
                            var t = docTrans[0];

                            if ((t.CodeTypeGroup.Equals(1) && _docPage.User.IsInRole("74")) ||
                                (t.CodeTypeGroup.Equals(2) && _docPage.User.IsInRole("72")) ||
                                (t.CodeTypeGroup.Equals(3) && _docPage.User.IsInRole("76"))) //есть роль
                            {
                                b.Append(_resx.GetString("msgDelFinishSign"));
                                b.AppendFormat(_resx.GetString("msgDelFinishSign1"), docTrans.Count);
                                b.Append(_resx.GetString("msgDelFinishSign2"));
                            }
                            else //нет роли
                            {
                                b.Append(_resx.GetString("msgDelFinishSignWithTran"));
                                go = false;
                            }
                        }
                        else //нет транзакций
                        {
                            b.AppendFormat(_resx.GetString("msgDelFinishSign") + _resx.GetString("msgDelFinishSign2"));
                        }
                    }
                }
                else //подпись обычная
                {
                    if (sign.EmployeeId != _docPage.CurrentUser.EmployeeId)
                        b.AppendFormat(_resx.GetString("msgDelSign"), sign.EmployeeFio);
                    else
                    {
                        b.AppendFormat(_resx.GetString("QMsgDelSign"));
                    }
                }

                if (go)
                {
                    _js.Write("ConfirmDeleteSign.render('{0}','{1}','{2}','{3}','{4}');",
                        _resx.GetString("msgOsnAttention0"),
                        HttpUtility.JavaScriptStringEncode(b.ToString()).Replace("\\r\\n", "<br>"),
                        _resx.GetString("QSBtnYes"), _resx.GetString("QSBtnNo"), sign.SignId);
                    //JS.Write("if(confirm('{0}')) cmd('cmd', 'RemoveSign','IdSign', '{1}','ask', '0');", b, signID);
                }
                else
                {
                    _docPage.ShowMessage(b.ToString());
                }
                return false;
            }

            var result = RemoveSign(sign);

            if (result)
            {
                var index = _doc.DocSigns.FindIndex(i => i.SignId == sign.SignId);
                if (index != -1)
                    _doc.DocSigns.RemoveAt(index);

                var last = _doc.DocSigns.LastOrDefault();
                if (last != null)
                    last.CanDelete = 1;
            }
            else
                return false;


            return true;
        }

        /// <summary>
        ///     Удаление всех подписей по документу
        /// </summary>
        /// <returns>True, если удачно</returns>
        public bool RemoveSignsAll()
        {
            _doc.GetSignsFromDb(); // кешируются в свойствах объекта
            var SingsSorted = _doc.DocSigns.AsEnumerable().OrderByDescending(o => o.Date).ToList();

            foreach (var s in SingsSorted)
                DocSign.RemoveSign(s.Id);
            _doc.GetSignsFromDb();
            if (_doc.Signed)
            {
                _docPage.ShowMessage("Невозможно удалить подписи по документу!", "Сообщение");
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Обновить подписи документа, дополнительно обновляет кешируемые в модели данные
        /// </summary>
        public void RefreshSigns(bool fromDb = true)
        {
            try
            {
                using (var w = new StringWriter())
                {
                    using (var wt = new StringWriter())
                    {
                        if (fromDb) _doc.GetSignsFromDb();

                        RenderSigns(w, wt);

                        _js.Write("if (gi('divDocSigns')) gi('divDocSigns').innerHTML={0};",
                            HttpUtility.JavaScriptStringEncode(w.ToString(), true));
                        _js.Write(wt);
                    }
                }
            }
            catch (Exception e)
            {
                _docPage.ShowMessage("Не удалось обновить подписи документов: " + e.Message);
            }
        }

        /// <summary>
        ///     Удаление подписи документа
        /// </summary>
        /// <returns>true - OK</returns>
        protected bool RemoveSign(DocSign sign)
        {
            try
            {
                if (sign.SignType == 1)
                {
                    var docTrans = Transaction.GetTransactionsByDocId(_doc.DocId);

                    if (docTrans.Count > 0)
                        Transaction.RemoveTrans(_doc.Id);
                }

                DocSign.RemoveSign(sign.Id);
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
        ///     Замещающий сотруник либо финальная подпись
        /// </summary>
        /// <returns>
        ///     true - OK
        /// </returns>
        public bool InquireSigner(string signType, ref bool sendMessage, ref int emploeeInstadOf)
        {
            emploeeInstadOf = _docPage.CurrentUser.EmployeeId;

            var arr = _docPage.CurrentUser.CurrentEmployees();
            if (arr == null)
            {
                _docPage.ShowMessage(_resx.GetString("msgUserNotAuth"), _resx.GetString("alertError"));
                return false;
            }

            if (arr.Count == 1 && signType.Equals("0"))
            {
                sendMessage = true;
                return true;
            }

            RenderDialogMultipleSigner(signType, arr);
            return false;
        }

        /// <summary>
        ///     Формирование подтверждения финальной подписи или
        ///     подтвержение замещающего сотрудника
        /// </summary>
        private void RenderDialogMultipleSigner(string signType, IEnumerable<int> arr)
        {
            var disableSendMsg = "yes".Equals(_docPage.V4Request["noSendMsg"]);
            var width = "350px";
            var dialog = "";
            if (signType.Equals("1"))
            {
                var s = _resx.GetString("msgFinishSign");
                if (s != null)
                {
                    dialog += "<img id=\"img1\" src=\"../../STYLES/attention.gif\" width=\"40\" height=\"40\">" +
                              HttpUtility.JavaScriptStringEncode(
                                  s.Replace("&lt;br&gt;", "<br>")
                                      .Replace("&lt;b&gt;", "<b>")
                                      .Replace("&lt;/b&gt;", "</b>"));
                    width = "600px";
                }
            }
            foreach (var empl in arr)
            {
                var isCurrent = false;
                var signer = "";
                Employee employee;
                if (empl.ToString(CultureInfo.InvariantCulture) == _docPage.CurrentUser.Id)
                {
                    isCurrent = true;
                    employee = _docPage.CurrentUser;
                }
                else
                {
                    employee = new Employee(empl.ToString(CultureInfo.InvariantCulture));
                }
                if (!isCurrent)
                {
                    signer = " " + _resx.GetString("msgAs") + " [" +
                             ((employee.Language == "ru") ? employee.FIO : employee.FIOEn) + "]";
                }
                dialog +=
                    "<button style=\"background-color:buttonface;WIDTH: 100%;\" onclick=\"ConfirmMultipleSigner.sign(" +
                    empl + "," + signType + "," +
                    (_docPage.DocNumberIsCorrect ? "1" : "0") + ")\">" + _resx.GetString("msgSign") + signer +
                    "</button><br />";
            }

            dialog +=
                "<button style=\"background-color:buttonface;WIDTH: 100%;\" onclick=\"ConfirmMultipleSigner.cancel()\">" +
                _resx.GetString("ppBtnCancel") + "</button>";
            var sendMessage = !(!DocViewParams.SignMessageWorkDone || disableSendMsg);
            var foot =
                String.Format(
                    "<label for=\"sendMessage\" style=\"color: black;\">{0}</label><input id=\"sendMessage\" type=\"checkbox\" {1} {2} onchange=\"ConfirmMultipleSigner.sms(this.checked)\" NAME=\"sendMessage\">",
                    _resx.GetString("msgSendFinishMsg") + "&nbsp;", (sendMessage ? "checked" : ""),
                    (disableSendMsg) ? "display: none;" : "");

            _js.Write("ConfirmMultipleSigner.render('" + _resx.GetString("msgSign") + " ..." + "', '" + dialog + "', '" +
                      foot + "', '" + width + "');ConfirmMultipleSigner.sendMessage = '" + sendMessage + "';");
        }

        /// <summary>
        ///     Получить сообщение финальной подписи
        /// </summary>
        /// <returns></returns>
        public string GetFinalSignMessage()
        {
            var signText = "";

            if (!_doc.Unavailable && _doc.TypeID > 0)
                signText = DocSign.GetSignText(_doc.TypeID, 1);

            if (signText.Length == 0)
                signText = "Работа завершена";

            return signText;
        }

        /// <summary>
        ///     Получение сообщения
        /// </summary>
        /// <returns></returns>
        public string GetSignMessage()
        {
            var signText = "";

            if (!_doc.Unavailable && _doc.TypeID > 0)
                signText = DocSign.GetSignText(_doc.TypeID, 0);

            if (signText.Length == 0)
                signText = "Электронная форма документа подписана";

            return signText;
        }

        /// <summary>
        ///     Добавить подпись к документу
        /// </summary>
        /// <param name="isFirstSign">Первая подпись</param>
        public void AddSignRecord(int EmployeeInsteadOf, string SignType, out bool isFirstSign)
        {
            isFirstSign = !_doc.Signed;

            var sign = new DocSign
            {
                DocId = _doc.DocId,
                EmployeeId = _docPage.CurrentUser.EmployeeId,
                SignType = Convert.ToByte(SignType),
                EmployeeInsteadOf = EmployeeInsteadOf > 0 ? EmployeeInsteadOf : _docPage.CurrentUser.EmployeeId,
                Date = DateTime.Now
            };

            sign.Create();
        }
    }
}