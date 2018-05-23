using System.Web.UI;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Рендеринг контента на страницу
    /// </summary>
    public class RenderContent : Control
    {
        /// <summary>
        ///     Наименование метода рендеринга
        /// </summary>
        public string RenderMethod { get; set; }

        /// <summary>
        ///     Рендеринг контента
        /// </summary>
        /// <param name="writer">Поток</param>
        public override void RenderControl(HtmlTextWriter writer)
        {
            if (string.IsNullOrEmpty(RenderMethod)) return;
            var t = Page.GetType();
            var m = t.GetMethod(RenderMethod);
            if (m != null)
                m.Invoke(Page, new object[] {writer});
        }
    }
}