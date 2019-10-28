using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;
using Kesco.Lib.Web.Settings;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол - Изменил, изменено
    /// </summary>
    public class Changed : V4Control
    {
        /// <summary>
        ///     Время изменения
        /// </summary>
        private DateTime? _change;

        /// <summary>
        ///     Предыдущее значение код изменившего
        /// </summary>
        private int? _prevChangedByID;

        /// <summary>
        ///     Локализованная надпись Изменил:
        /// </summary>
        private string ChangeWord { get; set; }

        /// <summary>
        ///     Локализованная ФИО изменившего
        /// </summary>
        private string ChangedName { get; set; }

        /// <summary>
        ///     Код изменившего
        /// </summary>
        public int? ChangedByID { get; set; }

        /// <summary>
        ///     Локализованное время изменения
        /// </summary>
        public string Change
        {
            get
            {
                if (_change != null && _change != DateTime.MinValue)
                    return _change.Value.ToString("yyyy-MM-dd HH:mm:ss");
                return null;
            }
            set { _change = Convert.ToDateTime(value); }
        }

        /// <summary>
        ///     Установить время изменения
        /// </summary>
        public DateTime? SetChangeDateTime
        {
            set { _change = value; }
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        public override void RenderControl(TextWriter w)
        {
            if (ChangedByID != null && _change != null && _change != DateTime.MinValue)
            {
                SetParametrs();

                w.Write("<div id='{0}' style='display:{1}; float: right; padding-right: 5px;'>", HtmlID,
                    !Visible ? "none" : "");
                w.Write("<label id='{0}'>{1}</label>", HtmlID + "_word", ChangeWord);
                w.Write(
                    "<a id='{0}' class='v4_callerControl' data-id='{1}' caller-type='2' onclick=\"v4_windowOpen('{3}');\" style='margin-left: 5px; margin-right: 5px;' >{2}</a>",
                    HtmlID + "_link", ChangedByID, ChangedName, Config.user_form + "?id=" + ChangedByID);
                w.Write("<label id='{0}'></label>", HtmlID + "_date");
                w.Write("</div>");
                w.Write("<script>$('#{0}').text(v4_toLocalTime('{1}','dd.mm.yyyy hh:mi:ss'));</script>",
                    HtmlID + "_date", Change);
            }
            else
            {
                w.Write("<div id='{0}' style='display:{1}; float: right; padding-right: 5px;'>", HtmlID,
                    !Visible ? "none" : "");
                w.Write("<label id='{0}'></label>", HtmlID + "_word");
                w.Write("<a id='{0}' style='margin-left: 5px; margin-right: 5px;'></a>", HtmlID + "_link");
                w.Write("<label id='{0}'></label>", HtmlID + "_date");
                w.Write("</div>");
            }
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            if (ChangedByID == null || _change == null || _change == DateTime.MinValue)
            {
                JS.Write("gi('{0}').style.display='none';", HtmlID);
            }
            else
            {
                SetParametrs();
                JS.Write(@"   
                           $('#{0}')[0].setAttribute('data-id', '{1}');                           
                           $('#{0}')[0].setAttribute('caller-type', '2');
                           $('#{0}')[0].setAttribute('class', 'v4_callerControl');
                           $('#{0}')[0].setAttribute('onclick', ""v4_windowOpen('{8}');"");     
                           v4_setToolTip();                      
                           $('#{2}').text('{3}');
                           $('#{4}').text(v4_toLocalTime('{5}','dd.mm.yyyy hh:mi:ss'));
                           $('#{0}').text('{6}');
                           if(gi('{7}')) gi('{7}').style.display='';
                           ", HtmlID + "_link", ChangedByID, HtmlID + "_word", ChangeWord, HtmlID + "_date", Change,
                    ChangedName, HtmlID, Config.user_form + "?id=" + ChangedByID);
            }

            if (PropertyChanged.Contains("Visible"))
                JS.Write("if(gi('{0}')) gi('{0}').style.display='{1}';", HtmlID,
                    Visible && ChangedByID != null ? "" : "none");
        }

        /// <summary>
        ///     Метод устанавливает параметры по ID изменившего
        /// </summary>
        private void SetParametrs()
        {
            if (_prevChangedByID == ChangedByID)
                return;

            // если изменивший равен текущему пользователю, то не лезем в базу, т.к. он уже есть
            if (V4Page.CurrentUser.EmployeeId == ChangedByID)
            {
                ChangedName = V4Page.IsRusLocal ? V4Page.CurrentUser.FullName : V4Page.CurrentUser.FullNameEn;
            }
            else
            {
                if (ChangedByID != null || ChangedByID > 0)
                {
                    var sqlParams = new Dictionary<string, object>
                    {
                        {"@Id", new object[] {ChangedByID, DBManager.ParameterTypes.Int32}}
                    };
                    var dt = DBManager.GetData(SQLQueries.SELECT_ID_Сотрудник, Config.DS_user, CommandType.Text,
                        sqlParams);

                    if (V4Page.IsRusLocal && dt.Rows.Count == 1)
                        ChangedName = dt.Rows[0].Field<string>("Сотрудник");
                    else if (dt.Rows.Count == 1) ChangedName = dt.Rows[0].Field<string>("Employee");
                }
            }

            if (string.IsNullOrEmpty(ChangedName))
            {
                ChangedName = "";
                ChangeWord = "";
                ChangedByID = null;
            }
            else
            {
                ChangeWord = Resx.GetString("lblChangedBy") + ":";
            }

            _prevChangedByID = ChangedByID;
        }
    }
}