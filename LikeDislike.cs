using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Kesco.Lib.Web.Settings;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>\
    ///     Контрол оценки
    /// </summary>
    class LikeDislike : V4Control
    {
        /// <summary>
        ///     0 - статус не установлен, 1 - нравится, 2 - не нравится
        /// </summary>
        public int Like { get; set; }

        public int LikeId { get; set; }
        public string InterfaceVersion { get; set; }

        /// <summary>
        ///     Тут можно указать атрибут Style
        /// </summary>
        public string Style { get; set; }

        /// <summary>
        ///     Дата последней установки сатуса
        /// </summary>
        public DateTime ChangeDate { get; set; }
        public string CurrentUser { get; set; }

        /// <summary>
        /// Инициализация
        /// </summary>
        protected void GetStatus()
        {
            CurrentUser = V4Page.CurrentUser.Id;

            Dictionary<string, object> sqlParams = new Dictionary<string, object>
            {
                { "@КодИдентификатораОценки", LikeId },
                { "@ВерсияПО", InterfaceVersion },
                { "@КодСотрудника", CurrentUser },
                { "@Изменено", DateTime.Today }
            };

            var dt = DBManager.GetData(SQLQueries.SELECT_ОценкиИнтерфейса, Config.DS_errors, CommandType.Text, sqlParams);
            Like = 0;
            if (null != dt && dt.Rows.Count > 0)
            {
                Like = Convert.ToInt32(dt.Rows[0]["Оценка"].ToString());
            }
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        public override void RenderControl(TextWriter w)
        {
            GetStatus();
            w.Write("<div style=\"{0}\">", Style);
            w.Write("<img id=\"{2}_L\" src=\"/Styles/{0}.png\" title=\"{1}\" onclick=\"cmd('ctrl','{2}', 'LikeId', '{3}' , 'v', '1');\" />", Like > 0 ? "like" : "like_off", Resx.GetString("lb_Like") + (Like > 0 ? " (" + Like + ")" : ""), HtmlID, LikeId);
            w.Write("<div id=\"spL_{0}\" style=\"font-size: 5pt; width:15px; text-align:center; display:inline-block;\">{1}</div>", HtmlID, (Like > 0 ? Like.ToString() : ""));
            w.Write("&nbsp;");
            w.Write("<img id=\"{2}_D\" src=\"/Styles/{0}.png\" title=\"{1}\" onclick=\"cmd('ctrl','{2}', 'LikeId', '{3}', 'v', '-1');\" />", Like < 0 ? "dislike" : "dislike_off", Resx.GetString("lb_NotLike") + (Like < 0 ? " (" + Like * -1 + ")" : ""), HtmlID, LikeId);
            w.Write("<div id=\"spR_{0}\" style=\"font-size: 5pt; width:15px; text-align:center; display:inline-block;\">{1}</div>", HtmlID, Like < 0 ? (-Like).ToString() : "");
            w.Write("</div>");
        }


        public override void ProcessCommand(NameValueCollection collection)
        {
            if (collection["v"] != null)
            {
                Like = Convert.ToInt32(collection["v"]);
                var sqlParams = new Dictionary<string, object>
                {
                    { "@КодИдентификатораОценки", LikeId },
                    { "@ВерсияПО",  InterfaceVersion},
                    { "@Оценка",  Like}
                };

                DBManager.ExecuteNonQuery(SQLQueries.INSERT_ОценкиИнтерфейса, CommandType.Text, Config.DS_errors, sqlParams);
                GetStatus();

                JS.Write("$('#{0}').attr('src','/Styles/{1}.png');", HtmlID + "_L", Like > 0 ? "like" : "like_off");
                JS.Write("gi('{0}').innerHTML = '{1}';", "spL_" + HtmlID, Like > 0 ? Like.ToString() : "");
                JS.Write("$('#{0}').attr('src','/Styles/{1}.png');", HtmlID + "_D", Like < 0 ? "dislike" : "dislike_off");
                JS.Write("gi('{0}').innerHTML = '{1}';", "spR_" + HtmlID, Like < 0 ? (-Like).ToString() : "");
            }

            else
            {
                base.ProcessCommand(collection);
            }
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {

        }

    }
}
