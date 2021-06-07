using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Settings;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    public class UserFoto : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var eTag = string.Empty;
            MemoryStream ms;
            string sqlQuery;

            var Id = context.Request.QueryString["id"];
            var mini = context.Request.QueryString["mini"];
            var phId = context.Request.QueryString["phId"];
            var width = context.Request.QueryString["w"];

            context.Response.Clear();
            context.Response.ClearHeaders();
            var sqlParams = new Dictionary<string, object>();

            try
            {
                if (Id != null)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(Id, "^[0-9]{1,}$"))
                        throw new Exception("Код параметра id!=[0-9]!!!!");

                    if (mini != null && mini == "1")
                    {
                        eTag = "mini";
                        sqlQuery = SQLQueries.SELECT_MiniФотографияСотрудникаПоКодуСотрудника;
                    }
                    else
                    {
                        eTag = "normal";
                        sqlQuery = SQLQueries.SELECT_ФотографияСотрудникаПоКодуСотрудника;
                    }

                    eTag += $"-u{Id}";

                    sqlParams.Add("@КодСотрудника", Id);
                }
                else if (phId != null)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(phId, "^[0-9]{1,}$"))
                        throw new Exception("Код параметра id!=[0-9]!!!!");

                    if (mini != null && mini == "1")
                    {
                        eTag = "mini";
                        sqlQuery = SQLQueries.SELECT_MiniФотографияСотрудникаПоКодуФото;
                    }
                    else
                    {
                        eTag = "normal";
                        sqlQuery = SQLQueries.SELECT_ФотографияСотрудникаПоКодуФото;
                    }

                    eTag += $"-ph{phId}";

                    sqlParams.Add("@КодФотографииСотрудника", phId);
                }
                else
                {
                    context.Response.Redirect(Config.styles + "AlfNoPhoto.jpg", false);
                    return;
                }

                var dt = DBManager.GetData(sqlQuery, Config.DS_user, CommandType.Text, sqlParams);

                if (dt != null && dt.Rows.Count > 0)
                {
                    var timestamp = Convert.ToDateTime(dt.Rows[0]["Изменено"]);
                    eTag += $"-t{timestamp.Ticks}";
                    if (width != null)
                        eTag += $"-w{width}";

                    if (context.Request.Headers["If-None-Match"] != null && context.Request.Headers["If-None-Match"] == eTag)
                    {
                        context.Response.StatusCode = 304;
                        context.Response.StatusDescription = "Not Modified";
                        return;
                    }

                    ms = new MemoryStream((byte[])dt.Rows[0]["Фотография"]);

                    if (ms.Length != 0 && dt.Rows[0][0] != null)
                    {
                        Image img;

                        try
                        {
                            img = Image.FromStream(ms);
                        }
                        catch (Exception ex)
                        {
                            throw new DetailedException(ex.Message, ex);
                        }

                        int w = img.Width;

                        if (width != null)
                            w = int.Parse(width);

                        img = new Bitmap(img, w, (w * img.Height) / img.Width);
                        context.Response.ContentType = "image/jpeg";
                        img.Save(context.Response.OutputStream, ImageFormat.Jpeg);
                        context.Response.Cache.SetCacheability(HttpCacheability.ServerAndPrivate);
                        context.Response.Cache.SetLastModified(timestamp);
                        context.Response.Cache.SetETag(eTag);
                    }
                    else
                        context.Response.Redirect(Config.styles + "AlfNoPhoto.jpg", false);

                    ms.Close();
                }
                else
                {
                    if (mini != null && mini == "1")
                    {
                        context.Response.Redirect(Config.styles + "Empty.gif", false);
                    }
                    else
                    {
                        var result = (int)DBManager.ExecuteScalar(SQLQueries.SELECT_СотрудникФото_EXISTS, CommandType.Text, Config.DS_user, new Dictionary<string, object> { { "@КодСотрудника", Id } });

                        try
                        {
                            if (result == 1)
                            {
                                if (Id.IsNullEmptyOrZero())
                                {
                                    context.Response.Redirect(Config.styles + "CommonEmployeeNoPhoto.jpg", false);
                                }
                                else
                                {
                                    context.Response.Redirect(Config.styles + "AlfNoPhoto.jpg", false);
                                }
                            }
                            else
                            {
                                context.Response.Redirect(Config.styles + "AlfNoPhoto.jpg", false);
                            }

                        }
                        catch (Exception ex)
                        {
                            throw new DetailedException(ex.Message, ex);
                        }
                    }
                    context.Response.CacheControl = "no-cache";
                }

            }
            catch (Exception ex)
            {
                context.Response.Clear();
                context.Response.Redirect(Config.styles + "AlfNoPhoto.jpg", false);

                var sContext = "Context:\n";

                foreach (var key in context.Request.Headers.AllKeys)
                    sContext += $"[{key}]->[{context.Request.Headers[key]}]\n";

                Logger.WriteEx(new DetailedException(ex.Message, ex, sContext));
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
