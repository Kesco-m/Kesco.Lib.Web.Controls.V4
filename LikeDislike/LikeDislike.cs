using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Web;
using System.Web.Util;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Settings;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол оценки
    /// </summary>
    internal class LikeDislike : V4Control
    {
        /// <summary>
        ///     0 - статус не установлен, 1 - нравится, 2 - не нравится
        /// </summary>
        public int Like
        {
            get { return string.IsNullOrEmpty(Value) ? 0 : Int32.Parse(Value); }
            set
            {
                Value = value.ToString();
            }
        }

        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }

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
                if (null != dt && dt.Rows.Count > 0)
                {
                    Like = Convert.ToInt32(dt.Rows[0]["Оценка"].ToString());
                }

                dt = DBManager.GetData(SQLQueries.SELECT_ОценкаИнтерфейсаИтого, Config.DS_errors, CommandType.Text,
                    sqlParams);
                if (null != dt && dt.Rows.Count > 0)
                {
                    LikeCount = Convert.ToInt32(dt.Rows[0]["Нравится"].ToString());
                    DislikeCount = Convert.ToInt32(dt.Rows[0]["НеНравится"].ToString());
                }

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
            IsNoModifying = true;
            if (GetStatus())
            {
                var startLikeDate = "";
                var startLikeArr = LikeId.Split('_');
                if (startLikeArr.Length > 1)
                {
                    try
                    {
                        startLikeDate = " начиная с " + ConvertExtention.Convert.Str2DateTime(startLikeArr[startLikeArr.Length - 1]).ToString("dd.MM.yyyy") + ":<br/>";
                    }
                    catch {
                    }
                }

                w.Write("<div style=\"{0}\">", Style);
                w.Write(
                    "<div id=\"spL_{0}\" style=\"font-size: 5pt; text-align:center; display:inline-block;\">{1}</div>",
                    HtmlID, Like > 0 ? Like.ToString() : "");
                w.Write("&nbsp;");
                w.Write(
                    "<img class=\"likedislike\" tabindex =\"0\" onkeydown=\"v4_element_keydown(event, this);\" id=\"{2}_L\" src=\"/Styles/{0}.png\" title=\"{1}\" onclick=\"cmd('ctrl','{2}', 'LikeId', '{3}' , 'v', '1');\" />",
                    Like > 0 ? "like" : "like_off", HttpUtility.JavaScriptStringEncode(Resx.GetString("lb_Like")) + "<br/><b>Итого," + startLikeDate + "нравится: " + LikeCount + " не нравится: " + DislikeCount + "</b>", HtmlID, LikeId);
                w.Write("&nbsp;");
                w.Write(
                    "<img class=\"likedislike\" tabindex =\"0\" onkeydown=\"v4_element_keydown(event, this);\" id=\"{2}_D\" src=\"/Styles/{0}.png\" title=\"{1}\" onclick=\"cmd('ctrl','{2}', 'LikeId', '{3}', 'v', '-1');\" />",
                    Like < 0 ? "dislike" : "dislike_off", HttpUtility.JavaScriptStringEncode(Resx.GetString("lb_NotLike")) + "<br/><b>Итого," + startLikeDate + "нравится: " + LikeCount + " не нравится: " + DislikeCount + "</b>", HtmlID, LikeId);
                w.Write("&nbsp;");
                w.Write(
                    "<div id=\"spR_{0}\" style=\"font-size: 5pt; text-align:center; display:inline-block;\">{1}</div>",
                    HtmlID, Like < 0 ? (-Like).ToString() : "");
                w.Write("</div>");
                w.Write(@"<script>$('.likedislike').qtip({
                        position: {
                            my: 'top right',
                            at: 'bottom left'
                        },
                        style: 'qtip-color'
                        });</script>");
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

                Flush();

            }

            else
            {
                base.ProcessCommand(collection);
            }
        }

        public override void OnChanged(ProperyChangedEventArgs e)
        {
            GetStatus();
            base.OnChanged(e);
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            base.Flush();

            var startLikeDate = "";
            var startLikeArr = LikeId.Split('_');
            if (startLikeArr.Length > 1)
            {
                try
                {
                    startLikeDate = " начиная с " + ConvertExtention.Convert.Str2DateTime(startLikeArr[startLikeArr.Length - 1]).ToString("dd.MM.yyyy") + ":<br/>";
                }
                catch{
                }
            }

            JS.Write("$('#{0}').attr('src','/Styles/{1}.png');", HtmlID + "_L", Like > 0 ? "like" : "like_off");
            JS.Write("if(gi('{0}')) gi('{0}').innerHTML = '{1}';", "spL_" + HtmlID, Like > 0 ? Like.ToString() : "");
            JS.Write("$('#{0}').attr('src','/Styles/{1}.png');", HtmlID + "_D", Like < 0 ? "dislike" : "dislike_off");
            JS.Write("if(gi('{0}')) gi('{0}').innerHTML = '{1}';", "spR_" + HtmlID, Like < 0 ? (-Like).ToString() : "");

            JS.Write(" $('#{0}_L').attr('title','{1}');", HtmlID, HttpUtility.JavaScriptStringEncode(Resx.GetString("lb_Like")) + "<br/><b>Итого," + startLikeDate + "нравится: " + LikeCount + " не нравится: " + DislikeCount + "</b>");
            JS.Write(" $('#{0}_D').attr('title','{1}');", HtmlID, HttpUtility.JavaScriptStringEncode(Resx.GetString("lb_NotLike")) + "<br/><b>Итого," + startLikeDate + "нравится: " + LikeCount + " не нравится: " + DislikeCount + "</b>");
        }
    }
}