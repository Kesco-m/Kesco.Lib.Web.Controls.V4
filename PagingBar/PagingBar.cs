using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Web.UI;
using System.Web.UI.WebControls;
using Kesco.Lib.Localization;
using Kesco.Lib.Web.Controls.V4.Common;
using Page = Kesco.Lib.Web.Controls.V4.Common.Page;

namespace Kesco.Lib.Web.Controls.V4.PagingBar
{
    /// <summary>
    ///     Листинг страниц в гриде
    /// </summary>
    public class PagingBar : V4Control, IClientCommandProcessor
    {
        #region Constants

        private const string ListenerTag = "[LISTENER]";
        private const string LCurrentPageTag = "[L_CURRENT_PAGE]";
        private const string LTotalRows = "[L_TOTAL]";
        private const string LRowsPerPage = "[L_ROWS_PER_PAGE]";
        private const string LFirstPage = "[L_FIRST_PAGE]";
        private const string LPrevPage = "[L_PREV_PAGE]";
        private const string LNextPage = "[L_NEXT_PAGE]";
        private const string LLastPage = "[L_LAST_PAGE]";
        private const string IDTag = "[CID]";
        private const string LTabIndex = "[TabIndex]";
        private const string CtrlCurNumbTag = "[C_CURRENT_NUMB]";
        private const string CtrlRowPerPageTag = "[C_ROWS_PER_PAGE_NUMB]";

        #endregion

        #region Private Members

        protected int ListenerPos;
        protected Page page;
        private Number _currentPageCtrl;
        private Number _rowsPerPageCtrl;
        private int _maxPages = 1;

        /// <summary>
        ///     Менеджер ресурсов
        /// </summary>
        private readonly ResourceManager _resx = Resources.Resx;

        #endregion

        #region Private Methods

        private void SetButtonsDisabledState()
        {
            var isFirst = (CurrentPageNumber == 1) || Disabled;
            var isLast = (CurrentPageNumber == MaxPageNumber) || Disabled;
            
            //Сосотояние кнопки _btnNext не зависит от _btnFirst, здесь выход преждевременен
            //if (V4Page.JS.ToString().Contains(String.Format("gi('{0}_btnFirst').src = '{1}';", ID, isFirst ? "/STYLES/PageFirst.gif" : "/STYLES/PageFirstActive.gif")))
            //    return;
            V4Page.JS.Write("gi('{0}_btnFirst').disabled = {1}; gi('{0}_btnFirst').className={2};", ID,
                isFirst.ToString(CultureInfo.InvariantCulture).ToLower(),
                isFirst ? "'DisabledButton'" : "'NormalButtonStyle'");

            V4Page.JS.Write("gi('{0}_btnPrev').disabled = {1}; gi('{0}_btnPrev').className={2};", ID,
                isFirst.ToString(CultureInfo.InvariantCulture).ToLower(),
                isFirst ? "'DisabledButton'" : "'NormalButtonStyle'");

            V4Page.JS.Write("gi('{0}_btnLast').disabled = {1}; gi('{0}_btnLast').className={2};", ID,
                isLast.ToString(CultureInfo.InvariantCulture).ToLower(),
                isLast ? "'DisabledButton'" : "'NormalButtonStyle'");

            V4Page.JS.Write("gi('{0}_btnNext').disabled = {1}; gi('{0}_btnNext').className={2};", ID,
                isLast.ToString(CultureInfo.InvariantCulture).ToLower(),
                isLast ? "'DisabledButton'" : "'NormalButtonStyle'");

            V4Page.JS.Write("gi('{0}_btnFirst').src = '{1}';", ID,
                isFirst ? "/STYLES/PageFirst.gif" : "/STYLES/PageFirstActive.gif");
            V4Page.JS.Write("gi('{0}_btnPrev').src = '{1}';", ID,
                isFirst ? "/STYLES/PagePrev.gif" : "/STYLES/PagePrevActive.gif");
            V4Page.JS.Write("gi('{0}_btnNext').src = '{1}';", ID,
                isLast ? "/STYLES/PageNext.gif" : "/STYLES/PageNextActive.gif");
            V4Page.JS.Write("gi('{0}_btnLast').src = '{1}';", ID,
                isLast ? "/STYLES/PageLast.gif" : "/STYLES/PageLastActive.gif");
        }

        private void UpdateTotalPagesLabel()
        {
            V4Page.JS.Write("var objX = gi('{0}_TotalPages'); if (objX) objX.innerHTML = '{1}';", ID, MaxPageNumber);
        }

        private void GoPrevPage()
        {
            CurrentPageNumber--;
            OnCurrentPageTextChanged(_currentPageCtrl, null);
        }

        private void GoNextPage()
        {
            CurrentPageNumber++;
            OnCurrentPageTextChanged(_currentPageCtrl, null);
        }

        private void GoFirstPage()
        {
            CurrentPageNumber = 1;
            OnCurrentPageTextChanged(_currentPageCtrl, null);
        }

        private void GoLastPage()
        {
            CurrentPageNumber = MaxPageNumber;
            OnCurrentPageTextChanged(_currentPageCtrl, null);
        }

        #endregion

        #region Public

        /// <summary>
        ///     Акцессор V4Page
        /// </summary>
        public new Page V4Page
        {
            get { return Page as Page; }
            set { Page = value; }
        }

        /// <summary>
        ///     Количество страниц
        /// </summary>
        public int MaxPageNumber
        {
            get
            {
                if (_maxPages == 0)
                {
                    MaxPageNumber = 1;
                }
                return _maxPages;
            }
            set
            {
                _maxPages = value;
                UpdateTotalPagesLabel();
            }
        }

        /// <summary>
        ///     Текущая страница
        /// </summary>
        public int CurrentPageNumber
        {
            get
            {
                if (_currentPageCtrl.Value.Length == 0 || _currentPageCtrl.ValueInt == 0)
                {
                    CurrentPageNumber = 1;
                }
                return _currentPageCtrl.ValueInt == null ? 0 : (int) _currentPageCtrl.ValueInt;
            }
            set { _currentPageCtrl.ValueInt = value < MaxPageNumber ? value : MaxPageNumber; }
        }

        /// <summary>
        ///     Количество записей на странице
        /// </summary>
        public int RowsPerPage
        {
            get
            {
                if (_rowsPerPageCtrl.Value.Length == 0 || _rowsPerPageCtrl.ValueInt == 0)
                {
                    RowsPerPage = 35;
                }
                return _rowsPerPageCtrl.ValueInt == null ? 0 : (int) _rowsPerPageCtrl.ValueInt;
            }
            set { _rowsPerPageCtrl.ValueInt = value; }
        }

        /// <summary>
        ///     Признак Отключен
        /// </summary>
        public bool Disabled { get; private set; }

        /// <summary>
        ///     TabIndex
        /// </summary>
        public new int TabIndex { get; set; }

        /// <summary>
        ///     ID следующего контрола при переходе по Enter
        /// </summary>
        public new string NextControl { get; set; }

        /// <summary>
        ///     Атрибут для построения справки
        /// </summary>
        public new string Help { get; set; }

        /// <summary>
        ///     Событие смены текущей страницы
        /// </summary>
        public event EventHandler CurrentPageChanged;

        /// <summary>
        ///     Событие смены количество записей на странице
        /// </summary>
        public event EventHandler RowsPerPageChanged;

        /// <summary>
        ///     Отключение контрола
        /// </summary>
        /// <param name="disabled">Признак отключения</param>
        public void SetDisabled(bool disabled, bool clearRowsPerPage = true)
        {
            Disabled = disabled;
            SetButtonsDisabledState();

            if (disabled)
            {
                _currentPageCtrl.Value = String.Empty;
                if (clearRowsPerPage)
                    _rowsPerPageCtrl.Value = String.Empty;
                if (!V4Page.JS.ToString().Contains(String.Format("gi('{0}_TotalPages').innerHTML = '';", ID)))
                    V4Page.JS.Write("if(gi('{0}_TotalPages')) gi('{0}_TotalPages').innerHTML = '';", ID);
            }
        }

        #endregion

        #region Overrided Methods

        public void RenderContolBody(HtmlTextWriter output)
        {
            var currentAsm = Assembly.GetExecutingAssembly();
            var pagingBarContent =
                currentAsm.GetManifestResourceStream("Kesco.Lib.Web.Controls.V4.PagingBar.PagingBarContent.htm");
            if (pagingBarContent == null) return;
            var reader = new StreamReader(pagingBarContent);
            var sourceContent = reader.ReadToEnd();

            sourceContent = sourceContent.Replace(IDTag, ID);
            sourceContent = sourceContent.Replace(LCurrentPageTag, _resx.GetString("PageBar_lCurrentPage"));
            sourceContent = sourceContent.Replace(ListenerTag, ListenerPos.ToString(CultureInfo.InvariantCulture));
            sourceContent = sourceContent.Replace(LTotalRows, _resx.GetString("PageBar_lOf"));
            sourceContent = sourceContent.Replace(LRowsPerPage, _resx.GetString("PageBar_lRowsPerPage"));
            sourceContent = sourceContent.Replace(LFirstPage, _resx.GetString("PageBar_lFirstPage"));
            sourceContent = sourceContent.Replace(LPrevPage, _resx.GetString("PageBar_lPrevPage"));
            sourceContent = sourceContent.Replace(LNextPage, _resx.GetString("PageBar_lNextPage"));
            sourceContent = sourceContent.Replace(LLastPage, _resx.GetString("PageBar_lLastPage"));
            sourceContent = sourceContent.Replace(LTabIndex, TabIndex.ToString(CultureInfo.InvariantCulture));

            using (TextWriter currentPageTextWriter = new StringWriter())
            {
                var currentPageWriter = new HtmlTextWriter(currentPageTextWriter);
                _currentPageCtrl.RenderControl(currentPageWriter);
                sourceContent = sourceContent.Replace(CtrlCurNumbTag, currentPageTextWriter.ToString());
            }

            using (TextWriter rowsCountTextWriter = new StringWriter())
            {
                var rowsCountWriter = new HtmlTextWriter(rowsCountTextWriter);
                _rowsPerPageCtrl.RenderControl(rowsCountWriter);
                sourceContent = sourceContent.Replace(CtrlRowPerPageTag, rowsCountTextWriter.ToString());
            }

            sourceContent = sourceContent.Replace("\n", "").Replace("\r", "").Replace("\t", "");
            output.Write(sourceContent);
        }

        public override void RenderControl(HtmlTextWriter output)
        {
            RenderContolBody(output);
        }
        
        protected override void OnInit(EventArgs e)
        {
            PreOnInit();
            base.OnInit(e);
            V4LocalInit();
        }

        public void PreOnInit()
        {
            if (!V4Page.Listeners.Contains(this)) V4Page.Listeners.Add(this);
            ListenerPos = V4Page.Listeners.IndexOf(this);

        }

        public void V4LocalInit()
        {
            if (V4Page.V4IsPostBack) return;

            _currentPageCtrl = new Number();
            _currentPageCtrl.V4Page = V4Page;
            _currentPageCtrl.Width = new Unit("30");
            _currentPageCtrl.ID = "currentPage" + ID;
            _currentPageCtrl.HtmlID = "currentPage" + ID;
            _currentPageCtrl.TabIndex = TabIndex;
            _currentPageCtrl.NextControl = "rowsPerPage" + ID;
            V4Page.V4Controls.Add(_currentPageCtrl);
            _currentPageCtrl.V4OnInit();

            _rowsPerPageCtrl = new Number();
            _rowsPerPageCtrl.V4Page = V4Page;
            _rowsPerPageCtrl.Width = new Unit("30");
            _rowsPerPageCtrl.ID = "rowsPerPage" + ID;
            _rowsPerPageCtrl.HtmlID = "rowsPerPage" + ID;
            _rowsPerPageCtrl.TabIndex = TabIndex;
            _rowsPerPageCtrl.NextControl = String.IsNullOrEmpty(NextControl) ? "currentPage" + ID : NextControl;
            V4Page.V4Controls.Add(_rowsPerPageCtrl);
            _rowsPerPageCtrl.V4OnInit();

            _currentPageCtrl.Changed += OnCurrentPageTextChanged;
            _rowsPerPageCtrl.Changed += OnRowsPerPageTextChanged;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetButtonsDisabledState();
        }

        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="param">Коллекция параметров</param>
        public void ProcessClientCommand(NameValueCollection param)
        {
            var name = param["cmdName"];
            var nextCommand = ID + "_goNext";
            var prevCommand = ID + "_goPrev";
            var firstCommand = ID + "_goFirst";
            var lastCommand = ID + "_goLast";

            if (name.Equals(nextCommand))
            {
                GoNextPage();
            }
            else if (name.Equals(prevCommand))
            {
                GoPrevPage();
            }
            else if (name.Equals(firstCommand))
            {
                GoFirstPage();
            }
            else if (name.Equals(lastCommand))
            {
                GoLastPage();
            }
        }

        #endregion

        #region Event Handlers

        private void OnCurrentPageTextChanged(object sender, ProperyChangedEventArgs e)
        {
            if (CurrentPageNumber > MaxPageNumber)
            {
                CurrentPageNumber = MaxPageNumber;
            }

            if (CurrentPageNumber < 1)
            {
                CurrentPageNumber = 1;
            }

            if (CurrentPageChanged != null)
            {
                CurrentPageChanged(sender, e);
            }
            SetButtonsDisabledState();
        }

        private void OnRowsPerPageTextChanged(object sender, ProperyChangedEventArgs e)
        {
            if (RowsPerPage < 1)
            {
                RowsPerPage = 1;
            }

            //if (RowsPerPage > 400)
            //{
            //    RowsPerPage = 400;
            //}

            CurrentPageNumber = 1;
            SetButtonsDisabledState();

            if (RowsPerPageChanged != null)
            {
                RowsPerPageChanged(sender, e);
            }
        }

        #endregion
    }
}