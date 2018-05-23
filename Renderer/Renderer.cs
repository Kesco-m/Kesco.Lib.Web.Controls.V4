using System.IO;
using System.Web;
using Kesco.Lib.Web.Controls.V4.Common;

namespace Kesco.Lib.Web.Controls.V4.Renderer
{
    /// <summary>
    ///     Базовый класс для NumberRenderer
    /// </summary>
    public class Renderer
    {
        protected Page Page;

        /// <summary>
        ///     Получение V4Page
        /// </summary>
        public Page V4Page
        {
            get { return Page ?? (Page = (Page) HttpContext.Current.Handler); }
        }

        /// <summary>
        ///     Отрисовка
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="value">Значение</param>
        public virtual void Render(TextWriter w, string value)
        {
            w.Write(value);
        }

        /// <summary>
        ///     Отрисовка
        /// </summary>
        /// <param name="w">Поток</param>
        public virtual void Render(TextWriter w)
        {
        }
    }
}