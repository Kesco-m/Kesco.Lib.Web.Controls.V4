using System.Collections.Specialized;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums.Docs;
using Kesco.Lib.Web.Settings.Parameters;

namespace Kesco.Lib.Web.Controls.V4.Common
{
    /// <summary>
    ///     Класс параметров окна формы
    /// </summary>
    public class WindowParameters
    {
        /// <summary>
        ///     Конструктор
        /// </summary>
        /// <param name="left">Отступ слева</param>
        /// <param name="top">Отступ сверху</param>
        /// <param name="width">Ширина</param>
        /// <param name="height">Высота</param>
        public WindowParameters(string left, string top, string width, string height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        /// <summary>
        ///     Отступ слева
        /// </summary>
        public string Left { get; private set; }

        /// <summary>
        ///     Отступ сверху
        /// </summary>
        public string Top { get; private set; }

        /// <summary>
        ///     Ширина
        /// </summary>
        public string Width { get; private set; }

        /// <summary>
        ///     Высота
        /// </summary>
        public string Height { get; private set; }
    }

    /// <summary>
    ///     Класс вспомогательного объекта для сохранения и восстановления размеров и положения окна
    /// </summary>
    public class WndSizePosKeeper
    {
        private readonly Page _p;
        private readonly string _paramHeight;
        private readonly string _paramWidth;
        private readonly string _paramX;
        private readonly string _paramY;

        private WndSizePosKeeper()
        {
        }

        /// <summary>
        ///     Инициализирует новый экземпляр класса WndSizePosKeeper
        /// </summary>
        /// <param name="p">Экземпляр страницы V4</param>
        public WndSizePosKeeper(Page p)
        {
            _p = p;
            _paramX = p.WindowParameters.Left;
            _paramY = p.WindowParameters.Top;
            _paramWidth = p.WindowParameters.Width;
            _paramHeight = p.WindowParameters.Height;
        }

        /// <summary>
        ///     Метод, который необходимо вызвать при загрузке страницы
        /// </summary>
        public void OnLoad()
        {
            //Восстановление размеров окна
            var WindowParameterNamesCollection = new StringCollection {_paramX, _paramY, _paramWidth, _paramHeight};

            //Объект доступа к параметрам из БД сохраненнных настроек
            var parametersManager = new AppParamsManager(_p.ClId, WindowParameterNamesCollection);

            var isRequired = false;
            var strX = parametersManager.GetParameterValue(_p.Request.QueryString, _paramX, out isRequired, "-1");
            var strY = parametersManager.GetParameterValue(_p.Request.QueryString, _paramY, out isRequired, "-1");
            var strWidth =
                parametersManager.GetParameterValue(_p.Request.QueryString, _paramWidth, out isRequired, "1024");
            var strHeight =
                parametersManager.GetParameterValue(_p.Request.QueryString, _paramHeight, out isRequired, "768");

            if (strWidth.ToInt() > 0 && strHeight.ToInt() > 0)
            {
                _p.JS.Write("$(document).ready(function () {{v4_setWindowSizePos({0}, {1}, {2}, {3});}});", strX, strY, strWidth, strHeight);
            }
            //размеры восстановлены
        }

        /// <summary>
        ///     Процедура обработки клиентских запросов, вызывается с клиента либо синхронно, либо асинхронно
        /// </summary>
        /// <param name="cmd">Название команды</param>
        /// <param name="param">Коллекция параметров</param>
        public void ProcessCommand(string cmd, NameValueCollection param)
        {
            switch (cmd)
            {
                case "SaveWindowSizePos":
                    StoreWindowSize(param["x"], param["y"], param["width"], param["height"]);
                    break;
            }
        }

        /// <summary>
        ///     Метод для сохранения размеров окна в БН настроек пользователей
        /// </summary>
        /// <param name="strWidth">Ширина окна</param>
        /// <param name="strHeight">Высота окна</param>
        private void StoreWindowSize(string strX, string strY, string strWidth, string strHeight)
        {
            //Объект доступа к параметрам из БД сохраненнных настроек
            var parametersManager = new AppParamsManager(_p.ClId, new StringCollection());

            parametersManager.Params.Add(new AppParameter(_paramX, strX, AppParamType.SavedWithClid));
            parametersManager.Params.Add(new AppParameter(_paramY, strY, AppParamType.SavedWithClid));
            parametersManager.Params.Add(new AppParameter(_paramWidth, strWidth, AppParamType.SavedWithClid));
            parametersManager.Params.Add(new AppParameter(_paramHeight, strHeight, AppParamType.SavedWithClid));

            parametersManager.SaveParams();
        }
    }
}