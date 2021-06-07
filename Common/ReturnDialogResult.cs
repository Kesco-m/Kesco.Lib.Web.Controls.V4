using System.Web;

namespace Kesco.Lib.Web.Controls.V4.Common
{
    public class ReturnDialogResult
    {
        /// <summary>
        ///     Метод вызова расширенного поиска сущности
        /// </summary>
        /// <param name="page">Страница, откуда вызывается расширенный поиск</param>
        /// <param name="callBackFunction">Функция, которая должна обработать возвращаемые результаты</param>
        /// <param name="ctrlId">Идентификатор контрола(для V4.Select)</param>
        /// <param name="url">Web-Адрес страницы расширенного поиска</param>
        /// <param name="parameters">Дополнительные параметры, которые будут дописаны к строке запроса</param>
        /// <param name="isMultiReturn">
        ///     Нужен ли множественный возврат со страницы расширенного поиска(работает, только, если
        ///     страница поддерживает этот функционал)
        /// </param>
        /// <param name="clid">Код настройки клиента</param>
        /// <param name="dialogWidth">Ширина окна формы поиска</param>
        /// <param name="dialogHeight">Высота окна форомы поиска</param>
        public static void ShowAdvancedDialogSearch(Page page, string callBackFunction, string ctrlId, string url,
            string parameters, bool isMultiReturn, int clid, int dialogWidth, int dialogHeight)
        {
            if (HttpContext.Current.Request.ApplicationPath != null)
            {
                var callbackUrl = HttpContext.Current.Request.Url.Scheme + "://" +
                                  HttpContext.Current.Request.Url.Authority +
                                  HttpContext.Current.Request.ApplicationPath.TrimEnd('/') + "/dialogResult.ashx";
                var urlAdv = parameters;

                if (url.IndexOf('?') == -1) url += "?";
                else url += "&";

                //todo: Заменить на mvc=4 после публикации ТТН и скриптов V4
                url += string.Format("return={0}&mvc=4&clid={1}&control={2}&callbackKey={3}&callbackUrl={4}",
                    isMultiReturn ? 2 : 1,
                    clid,
                    HttpUtility.UrlEncode(ctrlId),
                    HttpUtility.UrlEncode(page.IDPage),
                    HttpUtility.UrlEncode(callbackUrl)
                );
                url += (urlAdv.Length > 0 ? "&" : "") + urlAdv;
            }

            page.JS.Write("v4_isStopBlur = false; v4_stopAsyncEvent = false;");
            page.JS.Write("$.v4_windowManager.selectEntity('{0}', '{1}', '{2}', {3}, '{4}');",
                HttpUtility.JavaScriptStringEncode(url),
                HttpUtility.JavaScriptStringEncode(ctrlId),
                HttpUtility.JavaScriptStringEncode(page.IDPage),               
                callBackFunction,
                isMultiReturn);
        }
    }
}