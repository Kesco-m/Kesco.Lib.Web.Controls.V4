using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Settings;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     \
    ///     Контрол оценки
    /// </summary>
    internal class LikeDislike : V4Control
    {
        /// <summary>
        ///     0 - статус не установлен, 1 - нравится, 2 - не нравится
        /// </summary>
        public int Like { get; set; }

        public string LikeId { get; set; }


        /// <summary>
        ///     Дата последней установки сатуса
        /// </summary>
        public DateTime ChangeDate { get; set; }

        public string CurrentUser { get; set; }

        /// <summary>
        ///     Инициализация
        /// </summary>
        protected bool GetStatus()
        {
            CurrentUser = V4Page.CurrentUser.Id;

            var enabledEst = DBManager.ExecuteScalar(SQLQueries.SELECT_ИдентификаторОценкиИнтерфейса, CommandType.Text,
                Config.DS_errors, new Dictionary<string, object> {{"@КодИдентификатораОценки", LikeId}});
            if (enabledEst.ToString() != "0")
            {
                Like = 0;
                var sqlParams = new Dictionary<string, object>
                {
                    {"@КодИдентификатораОценки", LikeId}
                };

                var dt = DBManager.GetData(SQLQueries.SELECT_ОценкиИнтерфейса, Config.DS_errors, CommandType.Text,
                    sqlParams);
                if (null != dt && dt.Rows.Count > 0) Like = Convert.ToInt32(dt.Rows[0]["Оценка"].ToString());

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        public override void RenderControl(TextWriter w)
        {
            if (GetStatus())
            {
                w.Write("<div style=\"{0}\">", Style);
                w.Write(
                    "<div id=\"spL_{0}\" style=\"font-size: 5pt; text-align:center; display:inline-block;\">{1}</div>",
                    HtmlID, Like > 0 ? Like.ToString() : "");
                w.Write("&nbsp;");
                w.Write(
                    "<img id=\"{2}_L\" src=\"/Styles/{0}.png\" title=\"{1}\" onclick=\"cmd('ctrl','{2}', 'LikeId', '{3}' , 'v', '1');\" />",
                    Like > 0 ? "like" : "like_off", Resx.GetString("lb_Like"),HtmlID, LikeId);
                w.Write("&nbsp;");
                w.Write(
                    "<img id=\"{2}_D\" src=\"/Styles/{0}.png\" title=\"{1}\" onclick=\"cmd('ctrl','{2}', 'LikeId', '{3}', 'v', '-1');\" />",
                    Like < 0 ? "dislike" : "dislike_off", Resx.GetString("lb_NotLike"), HtmlID, LikeId);
                w.Write("&nbsp;");
                w.Write(
                    "<div id=\"spR_{0}\" style=\"font-size: 5pt; text-align:center; display:inline-block;\">{1}</div>",
                    HtmlID, Like < 0 ? (-Like).ToString() : "");
                w.Write("</div>");
            }
        }


        public override void ProcessCommand(NameValueCollection collection)
        {
            if (collection["v"] != null)
            {
                Like = Convert.ToInt32(collection["v"]);
                var sqlParams = new Dictionary<string, object>
                {
                    {"@КодИдентификатораОценки", LikeId},
                    {"@Оценка", Like}
                };

                try
                {
                    DBManager.ExecuteNonQuery(SQLQueries.INSERT_ОценкиИнтерфейса, CommandType.Text, Config.DS_errors, sqlParams);
                }
                catch (Exception e)
                {
                    V4Page.ShowMessage(e.Message, Resx.GetString("errDoisserWarrning"), MessageStatus.Error);
                    Logger.WriteEx( new DetailedException("Ошибка при установке оценки!", e));
                }

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