using System;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Класс описывающий объект, необходимы для формирования ссылки на создание сущности в контроле Select
    /// </summary>
    public class URICreateEntity
    {
        /// <summary>
        ///     Идентификатор ссылки
        /// </summary>
        private readonly Guid _id;

        /// <summary>
        ///     Ссылка на иконку
        /// </summary>
        private readonly string _imgPath = "";

        /// <summary>
        ///     Локализованное название ссылки
        /// </summary>
        private readonly string _label = "";

        /// <summary>
        ///     Ссылка на форму создания сущности
        /// </summary>
        private readonly string _url = "";

        /// <summary>
        ///     Конструктор
        /// </summary>
        /// <param name="imgPath">Ссылка на иконку</param>
        /// <param name="url">Ссылка на форму создания сущности</param>
        /// <param name="label">Локализованное название ссылки</param>
        public URICreateEntity(string imgPath, string url, string label)
        {
            _id = Guid.NewGuid();
            _imgPath = imgPath;
            _url = url;
            _label = label;
        }

        /// <summary>
        ///     Идентификатор ссылки
        /// </summary>
        public Guid Id
        {
            get { return _id; }
        }

        /// <summary>
        ///     Ссылка на иконку
        /// </summary>
        public string ImgPath
        {
            get { return _imgPath; }
        }

        /// <summary>
        ///     Ссылка на форму создания сущности
        /// </summary>
        public string URL
        {
            get { return _url; }
        }

        /// <summary>
        ///     Локализованное название ссылки
        /// </summary>
        public string Label
        {
            get { return _label; }
        }
    }
}