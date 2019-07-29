using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;
using Kesco.Lib.Web.Controls.V4.Common;
using Page = Kesco.Lib.Web.Controls.V4.Common.Page;

namespace Kesco.Lib.Web.Controls.V4.TreeView
{
    public enum TreeViewOrderByDirectionEnum
    {
        Desc = -1,
        None = 0,
        Asc = 1
    }

    public class TreeView : V4Control, IClientCommandProcessor
    {
        public delegate void LoadDataDelegate(string id);

        public enum CheckBoxBehavior
        {
            Disabled = 0,
            MultipleSelect
        }

        public enum DockStyle
        {
            None = 0,
            Fill
        }

        private Button _currentBtnCancel;
        private Button _currentBtnFind;
        private Button _currentBtnHelp;
        private Button _currentBtnSave;
        private Button _currentBtnSearch;
        private Button _currentBtnSortAlphabet;

        private Button _currentBtnSortTreeLocation;
        private Div _currentHeaderCtrl;
        private TextBox _currentNameCtrl;

        private Div _currentPatchCtrl;

        private Radio _currentSearchRadioCtrl;
        private TextBox _currentSearchTextBoxCtrl;
        private TreeViewOrderByDirectionEnum _orderByDirectionAlphabet = TreeViewOrderByDirectionEnum.None;
        private TreeViewOrderByDirectionEnum _orderByDirectionLocation = TreeViewOrderByDirectionEnum.Asc;
        protected int ItemId;
        protected string ItemName;
        public LoadDataDelegate LoadData;

        /// <summary>
        ///     Коллекция кнопок меню
        /// </summary>
        public List<Button> MenuButtons;

        private string SearchParam;

        protected int TreeViewCmdListnerIndex;

        /// <summary>
        ///     Конструктор класса
        /// </summary>
        public TreeView()
        {
            ClId = 0;
            RootIds = new List<int>();
            SelectedIds = new List<int>();
            RootVisible = true;
            RootCheckVisible = false;
            IsDraggable = true;
            ShowTopNodesInSearchResult = false;
            HelpButtonVisible = false;
            IsEditableInDialog = true;
            AddFormTitle = Resx.GetString("lblAddition");
            EditFormTitle = Resx.GetString("lblEdit");
            MenuButtons = new List<Button>();
        }

        /// <summary>
        ///     Акцессор V4Page
        /// </summary>
        public new Page V4Page
        {
            get { return Page as Page; }
            set { Page = value; }
        }

        /// <summary>
        ///     название ashx файла
        /// </summary>
        protected string JsonData { get; set; }

        /// <summary>
        ///     Настройки источника данных
        /// </summary>
        public TreeViewDbSourceSettings DbSourceSettings { get; set; }

        /// <summary>
        ///     ID клиента
        /// </summary>
        public int ClId { get; set; }

        /// <summary>
        ///     Загрузка переданного id
        /// </summary>
        public int LoadById { get; set; }

        /// <summary>
        ///     Параметр в таблице Настройки
        /// </summary>
        public string ParamName { get; set; }

        /// <summary>
        ///     Сохранять состояние дерева
        /// </summary>
        public bool IsSaveState { get; set; }

        /// <summary>
        ///     Включить DND
        /// </summary>
        public bool IsDraggable { get; set; }

        /// <summary>
        ///     Загружать данные при выборе узла дерева
        /// </summary>
        public bool IsLoadData { get; set; }

        /// <summary>
        ///     Состояние TreeView
        /// </summary>
        protected string TreeViewState { get; set; }

        /// <summary>
        ///     Разрешить контекстное меню в режиме диалога
        /// </summary>
        public bool IsEditableInDialog { get; set; }

        /// <summary>
        ///     Возможность добавления из контекстного меню
        /// </summary>
        public bool ContextMenuAdd { get; set; }

        /// <summary>
        ///     Возможность переименования из контекстного меню
        /// </summary>
        public bool ContextMenuRename { get; set; }

        /// <summary>
        ///     Возможность удаления из контекстного меню
        /// </summary>
        public bool ContextMenuDelete { get; set; }

        /// <summary>
        ///     Возможность менять сортировку
        /// </summary>
        public bool IsOrderMenu { get; set; }

        /// <summary>
        ///     Поведение CheckBox на узлах дерева
        /// </summary>
        public CheckBoxBehavior Checkable { get; set; }

        /// <summary>
        ///     Указывает какие границы элемента управления привязаны к контейнеру
        /// </summary>
        private DockStyle Dock { get; set; }

        /// <summary>
        ///     Возможность изменять ширину элемента управления
        /// </summary>
        public bool Resizable { get; set; }

        /// <summary>
        ///     Список идентификаторов корневых узлов, на основе которых будет построена иерархия
        /// </summary>
        protected List<int> RootIds { get; set; }

        /// <summary>
        ///     Список идентификаторов узлов, которым при загрузке необходимо проставить checked (при наличии параметра return = 2)
        /// </summary>
        protected List<int> SelectedIds { get; set; }

        /// <summary>
        ///     Показывать корневой узел
        /// </summary>
        public bool RootVisible { get; set; }

        /// <summary>
        ///     Показывать галочку у корневого узла
        /// </summary>
        public bool RootCheckVisible { get; set; }

        /// <summary>
        ///     Возможность поиска
        /// </summary>
        public bool IsSearchMenu { get; set; }

        /// <summary>
        ///     Количество найденных записей
        /// </summary>
        public int SearchResultCount { get; set; }

        /// <summary>
        ///     Тип возврата
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        ///     Свойство указывающее, что у пользователя есть права на select
        /// </summary>
        private bool HasSelect { get; set; }

        /// <summary>
        ///     Свойство указывающее, что у пользователя есть права на добавление
        /// </summary>
        public bool HasInsert { get; set; }

        /// <summary>
        ///     Свойство указывающее, что у пользователя есть права на обновление
        /// </summary>
        public bool HasUpdate { get; set; }

        /// <summary>
        ///     Свойство указывающее, что у пользователя есть права на удаление
        /// </summary>
        public bool HasDelete { get; set; }

        /// <summary>
        ///     Заголовок формы добавления узла
        /// </summary>
        public string AddFormTitle { get; set; }

        /// <summary>
        ///     Заголовок формы редактирования узла
        /// </summary>
        public string EditFormTitle { get; set; }

        /// <summary>
        ///     Заголовок названия узла (выводится на форме добавления/редактирования узла)
        /// </summary>
        public string NodeNameHeader { get; set; }

        /// <summary>
        ///     Отображать в результатах поиска все узлы, родителем которых является корень, независимо от наличия в них совпадений
        /// </summary>
        public bool ShowTopNodesInSearchResult { get; set; }

        /// <summary>
        ///     Показывать кнопку вызова справки
        /// </summary>
        public bool HelpButtonVisible { get; set; }

        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="param">Параметры</param>
        public void ProcessClientCommand(NameValueCollection param)
        {
            List<string> validList;
            switch (param["cmdName"])
            {
                case "LoadTreeViewData":
                    LoadData(param["Id"]);
                    break;
                case "AddTreeViewItem":
                case "EditTreeViewItem":
                {
                    ItemId = int.Parse(param["Id"]);

                    if (ItemId == 0 && param["cmdName"] == "EditTreeViewItem")
                        return;

                    if (_editCmdFuncName.IsNullEmptyOrZero())
                    {
                        var sqlParams = new Dictionary<string, object> {{"@id", ItemId}};

                        var sql = string.Format(SQLQueries.SELECT_ДеревоПолныйПутьКУзлу, DbSourceSettings.TableName,
                            DbSourceSettings.PkField, DbSourceSettings.NameField,
                            string.IsNullOrEmpty(DbSourceSettings.PathField)
                                ? string.Empty
                                : ", " + DbSourceSettings.PathField);

                        var dt = DBManager.GetData(sql, DbSourceSettings.ConnectionString, CommandType.Text, sqlParams);
                        if (dt.Rows.Count > 0 || ItemId == 0 && param["cmdName"] == "AddTreeViewItem")
                        {
                            var nodePath = string.Empty;

                            if (ItemId > 0)
                            {
                                if (!string.IsNullOrEmpty(DbSourceSettings.PathField))
                                    nodePath = dt.Rows[0][DbSourceSettings.PathField].ToString();
                                else if (!string.IsNullOrEmpty(DbSourceSettings.TreePathFuncName))
                                    nodePath = GetNodePath(ItemId);

                                nodePath = nodePath.Replace("_", " ");

                                if (param["cmdName"] == "EditTreeViewItem")
                                {
                                    var pos = nodePath.LastIndexOf("/" + dt.Rows[0][DbSourceSettings.NameField],
                                        StringComparison.Ordinal);
                                    if (pos > -1) nodePath = nodePath.Remove(pos, nodePath.Length - pos);
                                }
                            }

                            _currentPatchCtrl.Value = nodePath;
                            _currentNameCtrl.Value = param["cmdName"] == "AddTreeViewItem"
                                ? ""
                                : dt.Rows[0][DbSourceSettings.NameField].ToString();

                            JS.Write("v4_treeViewItemEdit_dialogShow('{4}', '{5}', '{0}','{1}','{2}','{3}');",
                                param["cmdName"] == "AddTreeViewItem" ? AddFormTitle : EditFormTitle,
                                Resx.GetString("cmdSave"), Resx.GetString("cmdCancel"), param["cmdName"], ID,
                                TreeViewCmdListnerIndex);
                        }
                    }
                    else
                    {
                        JS.Write("cmd('cmd','{0}','Id','{1}');",
                            param["cmdName"] == "AddTreeViewItem" ? _addCmdFuncName : _editCmdFuncName, ItemId);
                    }
                }
                    break;
                case "DeleteTreeViewItem":
                {
                    ItemId = int.Parse(param["Id"]);
                    ItemName = param["Name"];
                    var itemType = param["Type"];

                    if (ItemId == 0)
                        return;

                    var strCmd = string.Empty;
                    var msgAdditionalText = string.Empty;


                    if (_deleteCmdFuncName.IsNullEmptyOrZero())
                    {
                        strCmd = string.Format(
                            "cmdasync(\"cmd\",\"Listener\",\"ctrlId\",\"{0}\", \"cmdName\",\"{1}\",\"Id\",\"{2}\");",
                            TreeViewCmdListnerIndex, "DeleteTreeViewItemOK", ItemId);

                        msgAdditionalText = itemType == "folder"
                            ? "<br/>" + Resx.GetString("msgChildNodesWillBeMoved")
                            : string.Empty;
                    }
                    else
                    {
                        strCmd = string.Format("cmd(\"cmd\",\"{0}\",\"Id\",\"{1}\");", _deleteCmdFuncName, ItemId);
                    }

                    JS.Write("v4_showConfirm('{0}','{1}','{2}','{3}','{4}', 500);",
                        string.Format("{0} «{1}»?{2}", Resx.GetString("msgDeleteConfirm"),
                            HttpUtility.HtmlEncode(ItemName), msgAdditionalText),
                        Resx.GetString("errDoisserWarrning"),
                        Resx.GetString("CONFIRM_StdCaptionYes"),
                        Resx.GetString("CONFIRM_StdCaptionNo"),
                        strCmd
                    );
                }
                    break;
                case "DeleteTreeViewItemOK":
                {
                    var sqlParams = new Dictionary<string, object> {{"@Id", ItemId}};
                    var sql = string.Format(SQLQueries.DELETE_ДеревоУдалениеУзла, DbSourceSettings.TableName,
                        DbSourceSettings.PkField);

                    DBManager.ExecuteNonQuery(sql, CommandType.Text, DbSourceSettings.ConnectionString, sqlParams);
                    JS.Write("v4_reloadParentNode('{0}', '{1}');", ID,
                        ItemId); //JS.Write("v4_deleteNode('{0}', '{1}');", ID, ItemId); 
                }
                    break;
                case "SetTreeViewItem":
                    if (ValidateItem(out validList))
                        SaveData(param["type"]);
                    else
                        RenderErrors(validList, "<br/> " + Resx.GetString("_Msg_НеСохраняется"));
                    break;
                case "MoveTreeViewItem":
                {
                    var id = param["Id"];
                    var oldParent = param["old_parent"];
                    var newParent = param["new_parent"];
                    var oldPosition = param["old_position"];
                    var newPosition = param["new_position"];

                    var sqlParams = new Dictionary<string, object>
                    {
                        {"@Id", id},
                        {"@Parent", newParent}
                    };

                    var sql = string.Empty;

                    if (oldParent == newParent)
                    {
                        sqlParams.Add("@OldPosition", oldPosition);
                        sqlParams.Add("@NewPosition", newPosition);

                        sql = string.Format(SQLQueries.UPDATE_ДеревоПеремещениеУзлаБезСменыРодителя,
                            DbSourceSettings.TableName, DbSourceSettings.ViewName, DbSourceSettings.PkField);
                    }
                    else
                    {
                        sql = string.Format(SQLQueries.UPDATE_ДеревоПеремещениеУзла, DbSourceSettings.TableName,
                            DbSourceSettings.PkField);
                    }

                    DBManager.ExecuteNonQuery(sql, CommandType.Text, DbSourceSettings.ConnectionString, sqlParams);
                    JS.Write("v4_reloadParentNode('{0}', '{1}');", ID, id);
                }
                    break;
                case "ReLoadTreeView":
                    var orderByField = param["OrderByField"];

                    var orderByDirection = TreeViewOrderByDirectionEnum.None;
                    var imgPathSortLocation = string.Empty;
                    var imgPathSortAlphabet = string.Empty;

                    if (orderByField == "L")
                    {
                        if (_orderByDirectionLocation == TreeViewOrderByDirectionEnum.Asc)
                        {
                            _orderByDirectionLocation = TreeViewOrderByDirectionEnum.Desc;
                            imgPathSortLocation = _constImgPathSortDesc;
                        }
                        else
                        {
                            _orderByDirectionLocation = TreeViewOrderByDirectionEnum.Asc;
                            imgPathSortLocation = _constImgPathSortAsc;
                        }

                        _orderByDirectionAlphabet = TreeViewOrderByDirectionEnum.None;
                        imgPathSortAlphabet = _constImgPathEmpty;
                        orderByDirection = _orderByDirectionLocation;
                    }
                    else
                    {
                        if (_orderByDirectionAlphabet == TreeViewOrderByDirectionEnum.Asc)
                        {
                            _orderByDirectionAlphabet = TreeViewOrderByDirectionEnum.Desc;
                            imgPathSortAlphabet = _constImgPathSortDesc;
                        }
                        else
                        {
                            _orderByDirectionAlphabet = TreeViewOrderByDirectionEnum.Asc;
                            imgPathSortAlphabet = _constImgPathSortAsc;
                        }

                        _orderByDirectionLocation = TreeViewOrderByDirectionEnum.None;
                        imgPathSortLocation = _constImgPathEmpty;
                        orderByDirection = _orderByDirectionAlphabet;
                    }

                    JS.Write("v4_reloadOrderNode('{0}', '{1}', '{2}');", ID, orderByField,
                        orderByDirection == TreeViewOrderByDirectionEnum.Asc ? "ASC" : "DESC");
                    JS.Write("$('#btnSortTreeLocation_{0}').find('img').attr('src', '{1}');", ID, imgPathSortLocation);
                    JS.Write("$('#btnSortAlphabet_{0}').find('img').attr('src', '{1}');", ID, imgPathSortAlphabet);
                    break;
                case "DeselectAllTreeView":
                    JS.Write("v4_treeViewDeselectAllNodes('{0}');", ID);
                    V4Page.V4DropWindow();
                    break;
                case "Search":
                    var searchText = param["SearchText"];
                    JS.Write("v4_reloadSearchNode('{0}','{1}','{2}');", ID, searchText, SearchParam);
                    break;
                case "ChangeSearchParamTreeView":
                    SearchParam = param["SearchParam"];
                    break;
                case "SetSearchCount":
                    var searchCountText = SearchResultCount > 100
                        ? string.Format(Resx.GetString("lblOver100"), 100)
                        : string.Format(Resx.GetString("lTotalFound"), SearchResultCount);
                    JS.Write("v4_SetSearchResult('{0}', '{1}', '{2}');", ID, searchCountText,
                        Resx.GetString("lblEmptySearchString"));
                    JS.Write("$('.found').first().focus();");
                    break;
            }
        }

        /// <summary>
        ///     Инициализация
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInit(EventArgs e)
        {
            if (!V4Page.Listeners.Contains(this)) V4Page.Listeners.Add(this);
            TreeViewCmdListnerIndex = V4Page.Listeners.IndexOf(this);

            base.OnInit(e);

            if (V4Page.V4IsPostBack) return;

            var sqlParams = new Dictionary<string, object> {{"@TableName", DbSourceSettings.TableName}};

            var dt = DBManager.GetData(SQLQueries.SELECT_ПраваНаТаблицу, DbSourceSettings.ConnectionString,
                CommandType.Text, sqlParams);

            if (dt.Rows.Count > 0)
            {
                HasSelect = Convert.ToBoolean(dt.Rows[0]["PermOnSelect"]);
                HasInsert = Convert.ToBoolean(dt.Rows[0]["PermOnInsert"]);
                HasUpdate = Convert.ToBoolean(dt.Rows[0]["PermOnUpdate"]);
                HasDelete = Convert.ToBoolean(dt.Rows[0]["PermOnDelete"]);
            }

            _currentPatchCtrl = new Div {V4Page = V4Page, ID = "dvPatch_" + ID, HtmlID = "dvPatch_" + ID};
            V4Page.V4Controls.Add(_currentPatchCtrl);

            _currentHeaderCtrl = new Div
            {
                V4Page = V4Page, ID = "dvHeader_" + ID, HtmlID = "dvHeader_" + ID,
                Value = (string.IsNullOrEmpty(NodeNameHeader) ? Resx.GetString("TreeView_lblName") : NodeNameHeader) +
                        ":"
            };
            V4Page.V4Controls.Add(_currentHeaderCtrl);

            _currentNameCtrl = new TextBox
            {
                V4Page = V4Page,
                ID = "tbName_" + ID,
                HtmlID = "tbName_" + ID,
                Width = new Unit("400px")
            };
            V4Page.V4Controls.Add(_currentNameCtrl);

            _currentBtnSortTreeLocation = new Button
            {
                V4Page = V4Page,
                ID = "btnSortTreeLocation_" + ID,
                Text = "&nbsp;" + Resx.GetString("Cfi_lblSortTreeLocation"),
                HtmlID = "btnSortTreeLocation_" + ID,
                OnClick =
                    "cmdasync('cmd', 'Listener', 'ctrlId', " + TreeViewCmdListnerIndex +
                    @", 'cmdName', 'ReLoadTreeView', 'OrderByField', 'L');",
                IconKesco = "/styles/sort-asc.png"
                //IconJQueryUI = ButtonIconsEnum.SortAsc
            };
            //V4Page.V4Controls.Add(_currentBtnSortTreeLocation);

            _currentBtnSortAlphabet = new Button
            {
                V4Page = V4Page,
                ID = "btnSortAlphabet_" + ID,
                Text = "&nbsp;" + Resx.GetString("Cfi_lblSortAlphabet"),
                HtmlID = "btnSortAlphabet_" + ID,
                OnClick =
                    "cmdasync('cmd', 'Listener', 'ctrlId', " + TreeViewCmdListnerIndex +
                    @", 'cmdName', 'ReLoadTreeView', 'OrderByField', '" + DbSourceSettings.NameField + "');",
                IconKesco = "/styles/Empty.gif"
            };
            //V4Page.V4Controls.Add(_currentBtnSortAlphabet);

            var menu = string.Format(
                IsOrderMenu
                    ? "$('#btnSearch_{0}').hide();"
                    : "$('#btnSearch_{0}').hide(); $('#divMenuTreeView_{0}').hide();", ID);
            _currentBtnSearch = new Button
            {
                V4Page = V4Page,
                ID = "btnSearch_" + ID,
                Text = Resx.GetString("lblSearch"),
                IconJQueryUI = ButtonIconsEnum.Search,
                OnClick = string.Format("{0}; v4_ShowSearchTreeView('{1}');", menu, ID)
            };
            //V4Page.V4Controls.Add(_currentBtnSearch);

            _currentBtnSave = new Button
            {
                V4Page = V4Page,
                ID = "btnSave_" + ID,
                Text = Resx.GetString("Cfi_lblSave"),
                HtmlID = "btnSave_" + ID,
                OnClick = "var ids = v4_treeViewGetCheckedNodesIds('" + ID + "');" +
                          "v4_returnValueArray(ids);"
            };
            //V4Page.V4Controls.Add(_currentBtnSave);

            _currentBtnCancel = new Button
            {
                V4Page = V4Page,
                ID = "btnCancel_" + ID,
                Text = Resx.GetString("Cfi_lblCancel"),
                HtmlID = "btnCancel_" + ID,
                OnClick = string.Format("cmd('cmd', 'Listener', 'ctrlId', {1}, 'cmdName', 'DeselectAllTreeView')", ID,
                    TreeViewCmdListnerIndex)
            };
            //V4Page.V4Controls.Add(_currentBtnCancel);

            _currentBtnHelp = new Button
            {
                ID = "btnHelp_" + ID,
                V4Page = V4Page,
                Text = "",
                Title = Resx.GetString("lblHelp"),
                Width = 27,
                Height = 22,
                IconJQueryUI = ButtonIconsEnum.Help,
                OnClick = string.Format("v4_openHelp('{0}');", V4Page.ID),
                Style = "float: right; margin-right: 11px;"
            };
            //V4Page.V4Controls.Add(_currentBtnHelp);

            var returnData = V4Page.Request.QueryString["return"];
            var returnId = 0;
            if (int.TryParse(returnData, out returnId))
                Checkable = returnId == 2 ? CheckBoxBehavior.MultipleSelect : CheckBoxBehavior.Disabled;

            _currentSearchRadioCtrl = new Radio
                {V4Page = V4Page, ID = "radio_" + ID, IsRow = false, Name = "SearchRadio"};
            _currentSearchRadioCtrl.HtmlID = "radio_" + ID;
            _currentSearchRadioCtrl.Changed += _currentRadioCtrl_OnChanged;
            _currentSearchRadioCtrl.IsRow = true;
            _currentSearchRadioCtrl.Items.Add(new Item("0", " " + Resx.GetString("lblContains")));
            _currentSearchRadioCtrl.Items.Add(new Item("1", " " + Resx.GetString("lblBeginWith")));
            _currentSearchRadioCtrl.Value = "0";
            V4Page.V4Controls.Add(_currentSearchRadioCtrl);

            _currentSearchTextBoxCtrl = new TextBox
            {
                V4Page = V4Page,
                ID = "tbSearchText_" + ID
            };
            V4Page.V4Controls.Add(_currentSearchTextBoxCtrl);

            _currentBtnFind = new Button
            {
                V4Page = V4Page,
                ID = "btnFind_" + ID,
                Text = Resx.GetString("Cfi_lblSearch"),
                IconJQueryUI = ButtonIconsEnum.Search,
                OnClick = string.Format(
                    @"cmdasync('cmd', 'Listener', 'ctrlId', {0}, 'cmdName', 'Search', 'SearchText', $('#divSearchTreeView_{1}').find('input[type=text]').val());",
                    TreeViewCmdListnerIndex, ID)
            };

            var searchRequest = V4Page.Request["search"] ?? string.Empty;
            _currentSearchTextBoxCtrl.Value = searchRequest;
            var isSearchRequest = !string.IsNullOrEmpty(searchRequest);
            if (isSearchRequest)
                JS.Write("v4_ShowSearchTreeView('{0}');" +
                         "var input = $('#tbSearchText_{0}_0'); var len = input.val().length; input[0].setSelectionRange(len, len);" +
                         _currentBtnFind.OnClick, ID);
        }

        /// <summary>
        ///     Добавление кнопок в меню
        /// </summary>
        /// <remarks>
        ///     В качестве параметра может получать:
        ///     одиночный объект,
        ///     объекты через запятую,
        ///     массив объектов Button
        /// </remarks>
        /// <param name="buttons">объект контрола button</param>
        public void AddMenuButton(params Button[] buttons)
        {
            MenuButtons.AddRange(buttons);
        }

        /// <summary>
        ///     Обработка изменения позиции выбора действия
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _currentRadioCtrl_OnChanged(object sender, ProperyChangedEventArgs e)
        {
            JS.Write("cmdasync('cmd', 'Listener', 'ctrlId', " + TreeViewCmdListnerIndex +
                     @", 'cmdName', 'ChangeSearchParamTreeView', 'SearchParam', '" + e.NewValue + "');");
        }

        /// <summary>
        ///     Установка хендлера для дерева
        /// </summary>
        /// <param name="ashxFile">Название хендлера данных</param>
        public void SetJsonData(string ashxFile)
        {
            JsonData = ashxFile;
        }

        /// <summary>
        ///     Установка клиентских функций для добавления,редатирования,удаления записи
        /// </summary>
        /// <param name="addCmdFuncName">Название клиентской функции, вызываемой при добавлении</param>
        /// <param name="editCmdFuncName">Название клиентской функции, вызываемой при редактировании</param>
        /// <param name="deleteCmdFuncName">Название клиентской функции, вызываемой при удалении</param>
        public void SetService(string addCmdFuncName = "", string editCmdFuncName = "", string deleteCmdFuncName = "")
        {
            _addCmdFuncName = addCmdFuncName;
            _editCmdFuncName = editCmdFuncName;
            _deleteCmdFuncName = deleteCmdFuncName;
        }

        /// <summary>
        ///     Получить полный путь к узлу дерева
        /// </summary>
        /// <param name="id">ID узла</param>
        /// <returns>Путь к узлу</returns>
        public string GetNodePath(int id)
        {
            var sqlParams = new Dictionary<string, object>
            {
                {"@FuncName", DbSourceSettings.TreePathFuncName},
                {"@Code", id},
                {"@PathType", DbSourceSettings.TreePathType}
            };

            var path = DBManager.ExecuteScalar(SQLQueries.SELECT_ДеревоПолныйПутьКУзлуФункция, CommandType.Text,
                DbSourceSettings.ConnectionString, sqlParams);

            return path != null ? path.ToString() : string.Empty;
        }

        /// <summary>
        ///     Сформировать сообщение об ошибках
        /// </summary>
        public void RenderErrors(List<string> li, string text = null)
        {
            using (var w = new StringWriter())
            {
                foreach (var l in li)
                    w.Write("<div style='white-space: nowrap;'>{0}</div>", l);

                V4Page.ShowMessage(w + text, Resx.GetString("errIncorrectlyFilledField"), MessageStatus.Error, "", 500,
                    200);
            }
        }

        /// <summary>
        ///     Проверка корректности вводимых полей расположения
        /// </summary>
        protected bool ValidateItem(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(_currentNameCtrl.Value))
                errors.Add(Resx.GetString("msgNoDataField"));

            return errors.Count <= 0;
        }

        /// <summary>
        ///     Сохранение записи
        /// </summary>
        /// <param name="type"></param>
        private void SaveData(string type)
        {
            var sql = "";
            var param = new Dictionary<string, object>
            {
                {"@name", _currentNameCtrl.Value}
            };

            if (type == "AddTreeViewItem")
            {
                if (DbSourceSettings.AddSqlQuery.IsNullEmptyOrZero())
                    sql = string.Format(SQLQueries.INSERT_ДеревоДобавлениеУзла, DbSourceSettings.TableName,
                        DbSourceSettings.NameField, ItemId == 0 ? "NULL" : ItemId.ToString());
                else
                    sql = string.Format(DbSourceSettings.AddSqlQuery, _currentNameCtrl.Value, ItemId);
            }
            else
            {
                param.Add("@id", ItemId);
                sql = string.Format(SQLQueries.UPDATE_ДеревоРедактированиеУзла, DbSourceSettings.TableName,
                    DbSourceSettings.PkField, DbSourceSettings.NameField);

                //JS.Write("v4_treeViewCloseEditForm();");
                //JS.Write("$('#divTreeView_{2}').jstree('set_text','#{0}','{1}');", ItemId, _currentNameCtrl.Value, ID);
                //JS.Write("$('#{0}').attr('text','{1}');", ItemId, _currentNameCtrl.Value);
                //return ;

                //JS.Write("cmdasync('cmd', 'LoadTreeViewData', 'Id', {0});", ItemId);
            }

            DBManager.ExecuteNonQuery(sql, CommandType.Text, DbSourceSettings.ConnectionString, param);
            JS.Write("v4_treeViewCloseEditForm();");
            JS.Write("v4_reloadNode('{0}', '{1}');", ID, ItemId);
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="output"></param>
        public override void RenderControl(HtmlTextWriter output)
        {
            var currentAsm = Assembly.GetExecutingAssembly();
            var treeViewContent =
                currentAsm.GetManifestResourceStream("Kesco.Lib.Web.Controls.V4.TreeView.TreeViewContent.htm");
            if (treeViewContent == null) return;
            var reader = new StreamReader(treeViewContent);
            var sourceContent = reader.ReadToEnd();

            sourceContent = sourceContent.Replace(_constIdTag, ID);

            string rootIds = string.Empty;

            if (!string.IsNullOrWhiteSpace(V4Page.Request.QueryString["rootids"]))
                rootIds = V4Page.Request.QueryString["rootids"];
            else if (!string.IsNullOrWhiteSpace(V4Page.Request.QueryString["root"]))
                rootIds = V4Page.Request.QueryString["root"];
            else if (!string.IsNullOrWhiteSpace(V4Page.Request.QueryString["parent"]))
                rootIds = V4Page.Request.QueryString["parent"];
            else
                rootIds = string.Join(",", RootIds);
            
            sourceContent = sourceContent.Replace(_constRootIds, rootIds);

            sourceContent = sourceContent.Replace(_constSelectedIds,
                V4Page.Request.QueryString["selectedids"] ?? string.Join(",", SelectedIds));

            using (TextWriter currentDivTextWriter = new StringWriter())
            {
                var currentWriter = new HtmlTextWriter(currentDivTextWriter);
                _currentPatchCtrl.RenderControl(currentWriter);
                sourceContent = sourceContent.Replace(_constCtrlPatch, currentDivTextWriter.ToString());
            }

            using (TextWriter currentDivTextWriter = new StringWriter())
            {
                var currentWriter = new HtmlTextWriter(currentDivTextWriter);
                _currentHeaderCtrl.RenderControl(currentWriter);
                sourceContent = sourceContent.Replace(_constCtrlHeader, currentDivTextWriter.ToString());
            }

            sourceContent = sourceContent.Replace(_constJsonData, JsonData);
            sourceContent = sourceContent.Replace(_constReturnData, V4Page.Request.QueryString["return"]);
            sourceContent = sourceContent.Replace(_constReturnType, ReturnType);
            sourceContent = sourceContent.Replace(_constListenerIndex, TreeViewCmdListnerIndex.ToString());

            sourceContent = sourceContent.Replace("[MSG1]", Resx.GetString("Inv_msgChangeOrderTreeView"));
            sourceContent = sourceContent.Replace("[MSG2]", Resx.GetString("Inv_msgMoveItemTreeView1"));
            sourceContent = sourceContent.Replace("[MSG3]", Resx.GetString("Inv_msgMoveItemTreeView2"));

            sourceContent = sourceContent.Replace(_constIsSaveState, IsSaveState.ToString().ToLower());
            sourceContent = sourceContent.Replace(_constIsDND, IsDraggable.ToString().ToLower());
            sourceContent = sourceContent.Replace(_constIsLoadById, LoadById == 0 ? "false" : "true");
            sourceContent = sourceContent.Replace(_constLoadId, LoadById.ToString());

            sourceContent = sourceContent.Replace(_constTreeViewState, TreeViewState);

            sourceContent = sourceContent.Replace(_constUrlTreeViewSaveState,
                JsonData + "?type=save_state&Clid=" + ClId + "&ParamName=" + ParamName);

            var cm = ContextMenuAdd || ContextMenuRename || ContextMenuDelete ? "{ 'items': v4_customMenu }" : "false";
            sourceContent = sourceContent.Replace(_constContextMenu, cm);

            sourceContent = sourceContent.Replace(_constIsLoadData, IsLoadData.ToString().ToLower());

            var existItemMenu = false;
            cm = @"function v4_customMenu(node) { var items = {";
            if (ContextMenuAdd && HasInsert)
            {
                cm += @"'item1': {'icon': '/styles/New.gif', 'label': '" + Resx.GetString("cmdAddChild") + @"',
                    'action': function () { cmdasync('cmd', 'Listener', 'ctrlId', " + TreeViewCmdListnerIndex +
                      @", 'cmdName', 'AddTreeViewItem', 'Id', node.id); }
                    }";
                existItemMenu = true;
            }

            if (ContextMenuRename && HasUpdate)
            {
                if (existItemMenu) cm += ",";
                cm += @"'item2': {'icon': '/styles/Edit.gif','label': '" + Resx.GetString("cmdEdit") + @"',
                    'action': function () { cmdasync('cmd', 'Listener', 'ctrlId', " + TreeViewCmdListnerIndex +
                      @", 'cmdName', 'EditTreeViewItem', 'Id', node.id); }
                }";
            }

            if (ContextMenuDelete && HasDelete)
            {
                if (existItemMenu) cm += ",";
                cm += @"'item3': {'icon': '/styles/Delete.gif','label': '" + Resx.GetString("cmdDelete") + @"',
                    'action': function () { cmdasync('cmd', 'Listener', 'ctrlId', " + TreeViewCmdListnerIndex +
                      @", 'cmdName', 'DeleteTreeViewItem', 'Id', node.id, 'Name', v4_GetTextNode(node.id), 'Type', node.original.type); }
                }";
            }

            cm += string.Format(
                                @"}}; 
                                if (window.dialogWidth && window.dialogHeight && !{0}) {{
                                    delete items.item1;
                                    delete items.item2;
                                    delete items.item3;
                                }}
                                return items;}}", 
                                IsEditableInDialog.ToString().ToLower());

            sourceContent = sourceContent.Replace(_constfuncContextMenu, cm);

            if (_currentNameCtrl != null)
                using (TextWriter currentTextBoxTextWriter = new StringWriter())
                {
                    var currentWriter = new HtmlTextWriter(currentTextBoxTextWriter);
                    _currentNameCtrl.RenderControl(currentWriter);
                    sourceContent = sourceContent.Replace(_constCtrlName, currentTextBoxTextWriter.ToString());
                }

            sourceContent = IsOrderMenu || IsSearchMenu
                ? sourceContent.Replace(_constCtrlMenuStyle, "")
                : sourceContent.Replace(_constCtrlMenuStyle, "display: none;");

            if (IsOrderMenu)
            {
                sourceContent = sourceContent.Replace(_constCtrlOrderText, Resx.GetString("Cfi_lblSortBy") + ":");
                using (TextWriter currentSortTextWriter = new StringWriter())
                {
                    var currentWriter = new HtmlTextWriter(currentSortTextWriter);
                    _currentBtnSortTreeLocation.RenderControl(currentWriter);
                    _currentBtnSortAlphabet.RenderControl(currentWriter);
                    sourceContent = sourceContent.Replace(_constCtrlOrderButtons, currentSortTextWriter.ToString());
                }

                sourceContent = sourceContent.Replace(_constCtrlMenuStyle, "");
            }
            else
            {
                sourceContent = sourceContent.Replace(_constCtrlOrderText, "");
                sourceContent = sourceContent.Replace(_constCtrlOrderButtons, "");
            }

            var pluginCheckBox = "";
            var behaviorCheckBox = "";

            if (Checkable != CheckBoxBehavior.Disabled)
            {
                using (TextWriter currentTextWriter = new StringWriter())
                {
                    var currentHtmlWriter = new HtmlTextWriter(currentTextWriter);
                    _currentBtnSave.RenderControl(currentHtmlWriter);
                    sourceContent = sourceContent.Replace(_constCtrlSaveButton, currentTextWriter.ToString());
                }

                using (TextWriter currentTextWriter = new StringWriter())
                {
                    var currentHtmlWriter = new HtmlTextWriter(currentTextWriter);
                    _currentBtnCancel.RenderControl(currentHtmlWriter);
                    sourceContent = sourceContent.Replace(_constCtrlCancelButton, currentTextWriter.ToString());
                }

                pluginCheckBox = "checkbox";
                behaviorCheckBox =
                    "{'keep_selected_style':false,'three_state':false,'whole_node':false,'tie_selection':false}";
            }
            else
            {
                sourceContent = sourceContent.Replace(_constCtrlBottomMenuStyle, "display: none;");
                pluginCheckBox = "";
                behaviorCheckBox = "null";
            }

            sourceContent = sourceContent.Replace(_constPLCheckboxProps, pluginCheckBox);
            sourceContent = sourceContent.Replace(_constBHCheckboxProps, behaviorCheckBox);

            var multiple = Checkable == CheckBoxBehavior.MultipleSelect;

            sourceContent = sourceContent.Replace(_constCheckboxMultiple, multiple.ToString().ToLower());

            sourceContent = sourceContent.Replace(_constCtrlRootVisible, RootVisible.ToString().ToLower());

            sourceContent = sourceContent.Replace(_constCtrlRootCheckVisible, RootCheckVisible.ToString().ToLower());


            var dockStyleFunc = "false";
            switch (Dock)
            {
                case DockStyle.Fill:
                    dockStyleFunc = "true";
                    break;
            }

            sourceContent = sourceContent.Replace(_constCtrlDockStyle, dockStyleFunc);

            if (Resizable)
                sourceContent = sourceContent.Replace(_constCtrlResizable,
                    "$('#divTreeViewContainer_" + ID + "').resizable({ handles: 'e' });");
            else
                sourceContent = sourceContent.Replace(_constCtrlResizable,
                    "$('#divTreeView_" + ID + "').css('border-right', 'none');");

            var menu = string.Format(
                IsOrderMenu
                    ? "$('#btnSearch_{0}').show();"
                    : "$('#btnSearch_{0}').show(); $('#divMenuTreeView_{0}').show();", ID);
            sourceContent = sourceContent.Replace(_constCtrlSearchStyle, "display: none;");
            var closebutton = string.Format(@"
                <div onclick="" {0}; v4_HideSearchTreeView('{1}'); "" style=""display: inline-block; float: left; margin-right: 11px; width: 25px; height: 20px; text-align: center;"" class=""ui-button ui-widget ui-state-default ui-corner-all ui-button-text-icon-primary"">
                <span class=""ui-button-icon-primary ui-icon ui-icon-close""></span>
                <span class=""ui-button-text""></span>
                </div>
            ", menu, ID);

            sourceContent = sourceContent.Replace(_constCtrlSearchCloseButton, closebutton);

            sourceContent = sourceContent.Replace(_constFiltered, Resx.GetString("lblFiltered"));
            sourceContent = sourceContent.Replace(_constFilterHeaderName, Resx.GetString("Cfi_lblName"));

            sourceContent = sourceContent.Replace(_constPageId, V4Page.IDPage);

            using (TextWriter currentRadioTextWriter = new StringWriter())
            {
                var currentWriter = new HtmlTextWriter(currentRadioTextWriter);
                _currentSearchRadioCtrl.RenderControl(currentWriter);
                sourceContent = sourceContent.Replace(_constCtrlSearchRadio, currentRadioTextWriter.ToString());
            }

            using (TextWriter currentTextBoxTextWriter = new StringWriter())
            {
                var currentWriter = new HtmlTextWriter(currentTextBoxTextWriter);
                _currentSearchTextBoxCtrl.RenderControl(currentWriter);
                sourceContent = sourceContent.Replace(_constCtrlSearchTextBox, currentTextBoxTextWriter.ToString());
            }

            using (TextWriter currentSearchButtonTextWriter = new StringWriter())
            {
                var currentWriter = new HtmlTextWriter(currentSearchButtonTextWriter);
                _currentBtnFind.RenderControl(currentWriter);
                sourceContent = sourceContent.Replace(_constCtrlFindButton, currentSearchButtonTextWriter.ToString());
            }

            if (IsSearchMenu)
                using (TextWriter currentSearchButtonTextWriter = new StringWriter())
                {
                    var currentWriter = new HtmlTextWriter(currentSearchButtonTextWriter);
                    _currentBtnSearch.RenderControl(currentWriter);
                    sourceContent =
                        sourceContent.Replace(_constCtrlSearchButton, currentSearchButtonTextWriter.ToString());
                }
            else
                sourceContent = sourceContent.Replace(_constCtrlSearchButton, "");

            using (TextWriter menuButtonsTextWriter = new StringWriter())
            {
                var writer = new HtmlTextWriter(menuButtonsTextWriter);

                foreach (var b in MenuButtons)
                    //V4Page.V4Controls.Add(b);
                    b.RenderControl(writer);
                //b.PropertyChanged.Clear(); 

                sourceContent = sourceContent.Replace(_constMenuButtons, menuButtonsTextWriter.ToString());
            }

            if (HelpButtonVisible)
                using (TextWriter currentHelpButtonTextWriter = new StringWriter())
                {
                    var currentWriter = new HtmlTextWriter(currentHelpButtonTextWriter);
                    _currentBtnHelp.RenderControl(currentWriter);
                    sourceContent = sourceContent.Replace(_constCtrlHelpButton, currentHelpButtonTextWriter.ToString());
                }
            else
                sourceContent = sourceContent.Replace(_constCtrlHelpButton, "");

            sourceContent =
                sourceContent.Replace(_constCtrlSearchShowTop, ShowTopNodesInSearchResult.ToString().ToLower());

            sourceContent = sourceContent.Replace("\n", "").Replace("\r", "").Replace("\t", "");
            output.Write(sourceContent);
        }

        #region Constants

        private const string _constIdTag = "[CID]";
        private const string _constRootIds = "[C_ROOTIDS]";
        private const string _constSelectedIds = "[C_SELECTEDIDS]";
        private const string _constCtrlPatch = "[C_PATCH]";
        private const string _constCtrlHeader = "[C_HEADER]";
        private const string _constCtrlName = "[C_NAME]";
        private const string _constJsonData = "[JSON_DATA]";
        private const string _constReturnData = "[RETURN_DATA]";
        private const string _constReturnType = "[RETURN_TYPE]";
        private const string _constListenerIndex = "[TreeViewCmdListnerIndex]";
        private const string _constIsSaveState = "[IsSaveState]";
        private const string _constIsDND = "[PL_DND]";
        private const string _constIsLoadById = "[IsLoadById]";
        private const string _constLoadId = "[LoadId]";
        private const string _constTreeViewState = "[TREEVIEW_STATE]";
        private const string _constUrlTreeViewSaveState = "[URL_SAVESTATE]";
        private const string _constIsLoadData = "[IsLoadData]";
        private const string _constContextMenu = "[CONTEXT_MENU]";
        private const string _constfuncContextMenu = "[FUNC_MENU]";
        private const string _constCtrlOrderText = "[ORDERTEXT]";
        private const string _constCtrlOrderButtons = "[ORDERBUTTON]";
        private const string _constCtrlMenuStyle = "[MENUSTYLE]";
        private const string _constCtrlSaveButton = "[SAVEBUTTON]";
        private const string _constCtrlCancelButton = "[CANCELBUTTON]";
        private const string _constCtrlBottomMenuStyle = "[BOTTOMMENUSTYLE]";

        private const string _constPLCheckboxProps = "[PL_CHECKBOX]";
        private const string _constBHCheckboxProps = "[BH_CHECKBOX]";

        private const string _constCheckboxMultiple = "[CHECKBOXMULTIPLE]";
        private const string _constCtrlRootVisible = "[C_ROOTVISIBLE]";
        private const string _constCtrlRootCheckVisible = "[C_ROOTCHECKVISIBLE]";
        private const string _constCtrlResizable = "[C_RESIZABLE]";
        private const string _constCtrlDockStyle = "[C_DOCKSTYLE]";

        private const string _constCtrlSearchStyle = "[SEARCHSTYLE]";
        private const string _constCtrlSearchRadio = "[C_SEARCHRADIO]";
        private const string _constCtrlSearchTextBox = "[C_SEARCHTEXTBOX]";
        private const string _constCtrlSearchButton = "[C_SEARCHBUTTON]";
        private const string _constCtrlSearchCloseButton = "[C_SEARCHCLOSEBUTTON]";
        private const string _constCtrlSearchShowTop = "[C_SEARCHSHOWTOP]";
        private const string _constFiltered = "[FILTERED]";
        private const string _constFilterHeaderName = "[C_FILTERNAME]";
        private const string _constCtrlFindButton = "[C_FINDBUTTON]";
        private const string _constCtrlHelpButton = "[C_HELPBUTTON]";
        private const string _constMenuButtons = "[C_MENUBUTTONS]";
        private const string _constPageId = "[PAGEID]";

        private const string _constImgPathEmpty = "/styles/Empty.gif";
        private const string _constImgPathSortAsc = "/styles/sort-asc.png";
        private const string _constImgPathSortDesc = "/styles/sort-desc.png";

        #endregion

        #region Service

        private string _addCmdFuncName = "";
        private string _editCmdFuncName = "";
        private string _deleteCmdFuncName = "";

        #endregion
    }
}