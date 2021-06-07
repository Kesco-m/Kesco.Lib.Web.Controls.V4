using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.DALC;
using Kesco.Lib.Web.Controls.V4.Common;
using Kesco.Lib.Web.Settings;
using Kesco.Lib.Web.SignalR;
using Page = Kesco.Lib.Web.Controls.V4.Common.Page;
using SQLQueries = Kesco.Lib.Entities.SQLQueries;

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

        public delegate void ChangeTreeViewItemParentDelegate(string id, string newParent);

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
        private LikeDislike _currentBtnLike;
        private Button _currentBtnSave;
        private Button _currentBtnSearch;
        private Button _currentBtnExtSearch;
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
        public ChangeTreeViewItemParentDelegate ChangeTreeViewItemParent;
        public DataTable _dtLocal;
        public List<TreeViewAddUserFilter> AddFilterUser;

        /// <summary>
        ///     Коллекция кнопок меню
        /// </summary>
        public List<Button> MenuButtons;

        private string SearchText;
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
            LikeButtonVisible = false;
            IsEditableInDialog = true;
            AddFormTitle = Resx.GetString("lblAddition");
            EditFormTitle = Resx.GetString("lblEdit");
            ChangeOrderMessage = Resx.GetString("Inv_msgChangeOrderTreeView");
            MoveItemMessage1 = Resx.GetString("Inv_msgMoveItemTreeView1");
            MoveItemMessage2 = Resx.GetString("Inv_msgMoveItemTreeView2");
            MenuButtons = new List<Button>();
            IsAllowReturn = true;
            IsAllowReturnValue = true;
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
        ///     Дополнительный html-контент
        /// </summary>
        public string AdditionalContentText { get; set; }

        /// <summary>
        ///     Параметр в таблице Настройки
        /// </summary>
        public string ParamName { get; set; }

        /// <summary>
        ///     Показывать результаты поиска в отдельном окне
        /// </summary>
        public bool IsSearchResultInOtherWindow { get; set; }

        /// <summary>
        ///     Параметр в таблице Настройки
        /// </summary>
        public string NodeName { get; set; }

        /// <summary>
        ///     Сохранять состояние дерева
        /// </summary>
        public bool IsSaveState { get; set; }

        /// <summary>
        ///     Возмоджность возврата значений
        /// </summary>
        public bool IsAllowReturn { get; set; }

        /// <summary>
        ///     Включить DND
        /// </summary>
        public bool IsDraggable { get; set; }

        /// <summary>
        ///     Загружать данные при выборе узла дерева
        /// </summary>
        public bool IsLoadData { get; set; }

        /// <summary>
        ///     Строка запроса для фильтра
        /// </summary>
        public string DataSourceFilter { get; set; }

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
        ///     Пользовательское контекстное меню
        /// </summary>
        public bool ContextMenuCustom { get; set; }

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
        ///     Связь полей с условиями "выключенного" узла
        /// </summary>
        public TreeViewOffCondition OffTreeNodeFieldMap { get; set; }
        
        /// <summary>
        ///     Показывать галочку у корневого узла
        /// </summary>
        public bool RootCheckVisible { get; set; }

        /// <summary>
        ///     Может возвращать значения (по умолчанию = true)
        /// </summary>
        public bool IsAllowReturnValue { get; set; }

        /// <summary>
        ///     Удалять фильтр по умолчанию при очистке фильтра
        /// </summary>
        public bool IsDeleteDefaultFilter { get; set; }

        /// <summary>
        ///     Возможность поиска
        /// </summary>
        public bool IsSearchMenu { get; set; }

        /// <summary>
        ///     Возможность расширенного поиска
        /// </summary>
        public bool ShowFilterOptions { get; set; }

        /// <summary>
        ///     Количество найденных записей
        /// </summary>
        public int SearchResultCount { get; set; }

        /// <summary>
        ///     Тип возврата
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        ///     Условие возврата
        /// </summary>
        public string ReturnCondition { get; set; }

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
        ///     Текст сообщения при изменении порядка следования узлов
        /// </summary>
        public string ChangeOrderMessage { get; set; }

        /// <summary>
        ///     Первая часть текста сообщения при изменении подчинения узлов
        /// </summary>
        public string MoveItemMessage1 { get; set; }

        /// <summary>
        ///     Вторая часть текста сообщения при изменении подчинения узлов
        /// </summary>
        public string MoveItemMessage2 { get; set; }

        /// <summary>
        ///     Отображать в результатах поиска все узлы, родителем которых является корень, независимо от наличия в них совпадений
        /// </summary>
        public bool ShowTopNodesInSearchResult { get; set; }


        /// <summary>
        ///     Показывать кнопку вызова справки
        /// </summary>
        public bool HelpButtonVisible { get; set; }

        /// <summary>
        ///     Показывать кнопку оценки интерфейса
        /// </summary>
        public bool LikeButtonVisible { get; set; }

        /// <summary>
        ///     Показывать дополнительный контент
        /// </summary>
        public bool AddContentVisible { get; set; }

        /// <summary>
        ///     Список колонок, исключающихся для расширенного поиска
        /// </summary>
        public List<string> ColumnAdvSearchList { get; set; }

        /// <summary>
        ///     Список колонок, исключающих пустые значения
        /// </summary>
        public List<string> ColumnNotNullSearchList { get; set; }

        /// <summary>
        ///     Список колонок, исключающих поиск по нескольким словам
        /// </summary>
        public List<string> ColumnNotSplitWhenFilter { get; set; }

        /// <summary>
        ///     Настройки
        /// </summary>
        public TreeViewSettings Settings { get; private set; }

        /// <summary>
        ///     Наименование условий для поиска
        /// </summary>
        public string ConditionName { get; set; }

        public Dictionary<string, string> ColumnsHeaderAlias { get; set; }

        public Dictionary<string, object> ColumnsDefaultValues { get; set; }

        public Dictionary<string, TypeCode> ColumnsType { get; set; }
        
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

                    if (newParent != oldParent && ChangeTreeViewItemParent != null)
                    {
                        ChangeTreeViewItemParent(id, newParent);
                        break;
                    }

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

                    try
                    {
                        DBManager.ExecuteNonQuery(sql, CommandType.Text, DbSourceSettings.ConnectionString, sqlParams);
                    }
                        catch (Exception ex)
                    {
                        V4Page.ShowMessage(ex.Message, Resx.GetString("alertError"), MessageStatus.Error);
                    }
                    JS.Write("v4_reloadParentNode('{0}', '{1}');", ID, id);
                }
                    break;
                case "ReLoadTreeView":
                {
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
                }
                    break;
                case "DeselectAllTreeView":
                    JS.Write("v4_treeViewDeselectAllNodes('{0}');", ID);
                    V4Page.V4DropWindow();
                    break;
                case "Search":
                    SearchText = param["SearchText"];
                    var clmn = Settings.TableColumns.FirstOrDefault(d => d.FieldName == "text");
                    if (clmn != null)
                    {
                        var filterId = SearchParam == "1" ? ((int)TreeViewColumnUserFilterEnum.НачинаетсяС).ToString(): (
                            (int) TreeViewColumnUserFilterEnum.Содержит).ToString();

                        clmn.FilterUser = new TreeViewColumnUserFilter
                        {
                            FilterType = (TreeViewColumnUserFilterEnum)int.Parse(filterId),
                            FilterValue1 = param["SearchText"],
                            FilterValue2 = SearchParam
                        };

                        RenderAdvancedSearchSettings(true);
                        Settings.SaveOriginalFilter();
                        SetFilterByColumnValues(SearchText, SearchParam);
                    }

                    JS.Write("$('#divSearchCount_{0}').attr('style', 'margin-left: 4px;');", ID);

                    /*
                    var searchText = param["SearchText"];
                    JS.Write("v4_reloadSearchNode('{0}','{1}','{2}');", ID, searchText, SearchParam);
                    */
                    break;
                case "ChangeSearchParamTreeView":
                    SearchParam = param["SearchParam"];
                    break;
                case "ChangeAddFilterUser":
                    var _filterId = param["FilterId"];
                    if (!Settings.FilterClause.IsNullEmptyOrZero())
                    {
                        var filterClauseVal = Settings.FilterClause.Split(',');
                        if (filterClauseVal.Contains(_filterId))
                        {
                            filterClauseVal = filterClauseVal.Where(v => v != _filterId).ToArray();
                            Settings.FilterClause = string.Join(", ", filterClauseVal.ToArray());
                        }
                        else
                        {
                            Settings.FilterClause = Settings.FilterClause + "," + _filterId;
                        }
                    }
                    else
                    {
                        Settings.FilterClause = _filterId;
                    }

                    RenderAdvancedSearchSettings();
                    break;
                case "SetSearchCount":
                    if (Settings.FilterClause == "" && !Settings.TableColumns.Exists(x => x.FilterUser != null) && (Settings.AddTableColumns != null && !Settings.AddTableColumns.Exists(x => x.FilterUser != null)))
                    {
                        var searchCountText = SearchResultCount > 100
                            ? string.Format(Resx.GetString("lblOver100"), 100)
                            : string.Format(Resx.GetString("lTotalFound2"), SearchResultCount);
                        JS.Write("v4_SetSearchResult('{0}', '{1}', '{2}', 0);", ID, searchCountText,
                            Resx.GetString("lblEmptySearchString"));
                    }
                    else
                    {
                        var searchCountText = SearchResultCount > 100
                            ? string.Format(Resx.GetString("lblOver100"), 100)
                            : string.Format(Resx.GetString("lTotalFound2"), SearchResultCount);
                        JS.Write("v4_SetSearchResult('{0}', '{1}', '{2}', 1);", ID, searchCountText,
                            Resx.GetString("lblEmptySearchString"));
                    }

                    JS.Write("$('.found').first().focus();");
                    if (IsLoadData) LoadData("0");
                    break;
                case "RenderAdvancedSearchSettings":
                {
                    Settings.RestoreOriginalFilter();
                    RenderAdvancedSearchSettings();
                }
                    break;
                case "OpenUserFilterForm":
                    var p0 = Settings.TableColumns.FirstOrDefault(x => x.Id == param["ColumnId"]);

                    if (p0 == null && Settings.AddTableColumns != null)
                        p0 = Settings.AddTableColumns.FirstOrDefault(x => x.Id == param["ColumnId"]);

                    if (p0 != null)
                    {
                        if (DbSourceSettings.FieldValuesList != null && DbSourceSettings.FieldValuesList.ContainsKey(p0.FieldName))
                        {
                            p0.RenderColumnUserFilterForm(V4Page, param["FilterId"], param["SetValue"], DbSourceSettings.FieldValuesList[p0.FieldName]);
                        }
                        else
                        {
                            p0.RenderColumnUserFilterForm(V4Page, param["FilterId"], param["SetValue"]);
                        }
                    }
                    else
                        V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"),
                            MessageStatus.Error);
                    break;
                case "SetFilterColumnByUser":
                    if (!param["FilterId"].IsNullEmptyOrZero())
                        SetFilterColumnByUser(param["ColumnId"], param["FilterId"]);
                    RenderAdvancedSearchSettings();
                    break;
                case "ClearFilterColumnValues":
                    ClearFilterColumnValues(param["ColumnId"]);
                    RenderAdvancedSearchSettings();
                    break;
                case "ClearAllFilterColumnValuesAndRefresh":
                    JS.Write("isDefaultFilter = 'true';");
                    ClearAllFilterColumnValuesAndRefresh();
                    JS.Write("v4_FullReloadTreeView('{0}');", ID);
                    break;
                case "ClearAllFilterColumnValues":
                    ClearAllFilterColumnValues();
                    RenderAdvancedSearchSettings();
                    break;
                //Фильтр по выбранным значениям
                case "SetFilterByColumnValues":
                    if (IsSearchResultInOtherWindow)
                    {
                        RefreshSearchResultRows();
                    }

                    if (IsOnlyDefaultFilter())
                    {
                        JS.Write("isDefaultFilter = 'true';");

                        JS.Write("$('#divExtFilter_{0}').attr('style', 'display: none; vertical-align: middle; white-space:nowrap;');", ID);
                        JS.Write("$('#divSearchCount_{0}').attr('style', 'display: none; margin-left: 4px;');", ID);
                        ClearAllFilterColumnValuesAndRefresh();
                        JS.Write("v4_FullReloadTreeView('{0}');", ID);
                    }
                    else
                    {
                        JS.Write("isDefaultFilter = 'false';");
                        Settings.FilterClause = param["Data"];

                        if (Settings.FilterClause == "" && !Settings.TableColumns.Exists(x => x.FilterUser != null) && (Settings.AddTableColumns != null && !Settings.AddTableColumns.Exists(x => x.FilterUser != null)))
                        {
                            ClearAllFilterColumnValues(false);
                            JS.Write("$('#divExtFilter_{0}').attr('style', 'display: none; vertical-align: middle; white-space:nowrap;');", ID);
                            JS.Write("$('#divSearchCount_{0}').attr('style', 'display: none; margin-left: 4px;');", ID);
                            Settings.SaveOriginalFilter();
                            SetFilterByColumnValues("", "clearfilter");
                        }
                        else
                        {
                            Settings.SaveOriginalFilter();
                            SetFilterByColumnValues("", "filter");
                        }
                    }

                    break;

            }
        }

        /// <summary>
        /// Возвращает признак, что установлены только фильтры по умолчанию
        /// </summary>
        /// <returns></returns>
        private bool IsOnlyDefaultFilter()
        {
            if (Settings.TableColumns.Exists(x => x.DefaultValue == null && x.FilterUser != null || x.DefaultValue != null && x.FilterUser == null))
                return false;

            if (Settings.AddTableColumns != null && 
                Settings.AddTableColumns.Exists(x => x.DefaultValue == null && x.FilterUser != null || x.DefaultValue != null && x.FilterUser == null))
                return false;

            if (ColumnsDefaultValues != null)
            {
                foreach (var defaultValue in ColumnsDefaultValues)
                {
                    var clmn = Settings.TableColumns.FirstOrDefault(x => x.FieldName == defaultValue.Key);
                    if (clmn == null && Settings.AddTableColumns != null)
                        clmn = Settings.AddTableColumns.FirstOrDefault(x => x.FieldName == defaultValue.Key);
                    if (clmn != null)
                    {
                        if (clmn.ColumnType == TreeViewColumnTypeEnum.Boolean)
                        {
                            if (clmn.FilterUser.FilterType == TreeViewColumnUserFilterEnum.Нет &&
                                (int) defaultValue.Value == 1 ||
                                clmn.FilterUser.FilterType == TreeViewColumnUserFilterEnum.Да &&
                                (int) defaultValue.Value == 0)
                                return false;
                        }
                        else
                        {
                            if (clmn.FilterUser.FilterValue1 != defaultValue.Value)
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        protected void RefreshSearchResultRows()
        {
            using (var w = new StringWriter())
            {
                RenderSearchResultRows(w);
                JS.Write("document.getElementById('divSearchResult_{0}').innerHTML={1};", ID, HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        /// Отрисовка списка найденных значений
        /// </summary>
        /// <param name="w"></param>
        public void RenderSearchResultRows(TextWriter w)
        {
            var tvHandler = new TreeViewDataHandler();
            var addQuery = tvHandler.GetAddQuery(this);

            var sql = DataSourceFilter.Replace("@ADDWHERE", addQuery[0]).Replace("@ADDFIELD", addQuery[2]).Replace("@ADDTABLE", addQuery[1]);

            var dt = DBManager.GetData(sql, Config.DS_user);
            if (dt.Rows.Count != 0)
            {
                w.Write("<br/>");
                w.Write("<table id='searchResult' width='100%' class='grid' style='border-collapse:collapse; background-color: white; padding: 2px 2px 2px 2px;'>");
                w.Write("<tr class='gridHeader'>");
                foreach (DataColumn c in dt.Columns)
                {
                    if (c.ColumnName != "Id")
                        w.Write("<td>{0}</td>", HttpUtility.HtmlEncode(c.ColumnName));
                }
                w.Write("</tr>");

                foreach (DataRow r in dt.Rows)
                {
                    w.Write("<tr>");

                    foreach (DataColumn c in dt.Columns)
                    {
                        if (c.ColumnName != "Id")
                            w.Write("<td><a href='{1}'>{0}</a></td>", HttpUtility.HtmlEncode(r[c.ColumnName].ToString()), V4Page.V4Request.Url.AbsolutePath + "?id="+r["Id"]);
                    }
                    w.Write("</tr>");
                }

                w.Write("</table>");
            }
            else
            {
                w.Write("Данные по установленным параметрам не найдены");
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
                ,CSSClass = "button_disabled"
            };
            //V4Page.V4Controls.Add(_currentBtnSearch);

            _currentBtnExtSearch = new Button
            {
                V4Page = V4Page,
                ID = "btnExtSearch_" + ID,
                Text = Resx.GetString("lblFilter"),
                IconJQueryUI = ButtonIconsEnum.Search,
                OnClick = string.Format("v4_ShowExtSearchTreeView('{0}');", TreeViewCmdListnerIndex)
                ,CSSClass = "button_disabled"
            };

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

            if (LikeButtonVisible)
            {
                _currentBtnLike = new LikeDislike
                {
                    ID = "btnLike_" + ID,
                    V4Page = V4Page,
                    LikeId = V4Page.LikeId,
                    Style = "float: right; margin-right: 11px; margin-top: 3px; cursor: pointer;"
                };
                V4Page.V4Controls.Add(_currentBtnLike);
            }

            var returnData = V4Page.Request.QueryString["return"];
            var returnId = 0;
            if (IsAllowReturn && int.TryParse(returnData, out returnId))
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
            /*
            _currentSearchTextBoxCtrl.Value = searchRequest;
            var isSearchRequest = !string.IsNullOrEmpty(searchRequest);
            if (isSearchRequest)
                JS.Write("v4_ShowSearchTreeView('{0}');" +
                         "var input = $('#tbSearchText_{0}_0');$('#divMenuTreeView_{0}').hide(); var len = input.val().length; input[0].setSelectionRange(len, len);" +
                         _currentBtnFind.OnClick, ID);
            */
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
        ///     Установка клиентских функций для добавления,редактирования,удаления записи
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
        /// 
        /// </summary>
        /// <param name="customCmdFunc">Название клиентской функции, вызываемой при нажатии на пункт меню</param>
        /// <param name="onClient">Команда обрабатывается на клиенте</param>
        public void SetServiceCustom(List<List<string>> customCmdFunc, bool onClient = false)
        {
            _customCmdFunc = customCmdFunc;
            _customCmdFuncOnClient = onClient;
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

            var rootIds = string.Empty;

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
            sourceContent = sourceContent.Replace(_constReturnCondition, ReturnCondition);
            sourceContent = sourceContent.Replace(_constListenerIndex, TreeViewCmdListnerIndex.ToString());

            sourceContent = sourceContent.Replace("[MSG1]", ChangeOrderMessage);
            sourceContent = sourceContent.Replace("[MSG2]", MoveItemMessage1);
            sourceContent = sourceContent.Replace("[MSG3]", MoveItemMessage2);

            sourceContent = sourceContent.Replace(_constIsSaveState, IsSaveState.ToString().ToLower());
            sourceContent = sourceContent.Replace(_constIsDND, IsDraggable.ToString().ToLower());
            sourceContent = sourceContent.Replace(_constIsLoadById, LoadById == 0 ? "false" : "true");
            sourceContent = sourceContent.Replace(_constLoadId, LoadById.ToString());

            sourceContent = sourceContent.Replace(_constTreeViewState, TreeViewState);

            sourceContent = sourceContent.Replace(_constUrlTreeViewSaveState,
                JsonData + "?type=save_state&Clid=" + ClId + "&ParamName=" + ParamName);

            var cm = ContextMenuAdd || ContextMenuRename || ContextMenuDelete || ContextMenuCustom ? "{ 'items': v4_customMenu }" : "false";
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

            if (ContextMenuCustom && HasUpdate)
            {
                int itemNum = 4;

                foreach (List<string> customCmd in _customCmdFunc)
                {
                    itemNum++;
                    if (existItemMenu) cm += ",";
                    {
                        var actionFuncBody = _customCmdFuncOnClient
                            ? customCmd[1]
                            : "cmdasync('cmd', '" + customCmd[1] + "', 'Id', node.id);";

                        cm += "'item"+ itemNum + "': {'icon': '/styles/" + customCmd[2] + "','label': '" + customCmd[0] + "','action': function() {" + actionFuncBody + "}}";
                        existItemMenu = true;
                    }
                }
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
                sourceContent = sourceContent.Replace(_constCtrlOrderText, "<b>&nbsp;" + Resx.GetString("Cfi_lblSortBy") + ":</b>");
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
                <div onclick="" {0}; v4_HideSearchTreeView('{1}', '{2}'); "" style=""display: inline-block; float: left; margin-right: 11px; width: 25px; height: 20px; text-align: center;"" class=""ui-button ui-widget ui-state-default ui-corner-all ui-button-text-icon-primary"">
                <span class=""ui-button-icon-primary ui-icon ui-icon-close""></span>
                <span class=""ui-button-text""></span>
                </div>
            ", menu, ID, TreeViewCmdListnerIndex);

            sourceContent = sourceContent.Replace(_constCtrlSearchCloseButton, closebutton);

            sourceContent = sourceContent.Replace(_constFiltered, Resx.GetString("lblFiltered"));
            sourceContent = sourceContent.Replace(_constFilterHeaderName, NodeName);

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

            if (ShowFilterOptions)
                using (TextWriter currentExtSearchButtonTextWriter = new StringWriter())
                {
                    var currentWriter = new HtmlTextWriter(currentExtSearchButtonTextWriter);
                    _currentBtnExtSearch.RenderControl(currentWriter);
                    sourceContent = sourceContent.Replace(_constCtrlExtSearchButton, currentExtSearchButtonTextWriter.ToString());
                }
            else
            {
                sourceContent = sourceContent.Replace(_constCtrlExtSearchButton, "");
            }

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

            //
            if (LikeButtonVisible)
                using (TextWriter currentLikeButtonTextWriter = new StringWriter())
                {
                    var currentWriter = new HtmlTextWriter(currentLikeButtonTextWriter);
                    _currentBtnLike.RenderControl(currentWriter);
                    sourceContent = sourceContent.Replace(_constCtrlLikeButton, currentLikeButtonTextWriter.ToString());
                }
            else
                sourceContent = sourceContent.Replace(_constCtrlLikeButton, "");


            if (AddContentVisible && (HasUpdate || HasInsert || HasDelete))
                sourceContent = sourceContent.Replace(_constAddContent, AdditionalContentText);
            else
                sourceContent = sourceContent.Replace(_constAddContent, "");

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
        private const string _constReturnCondition = "[RETURN_CONDITION]";
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
        private const string _constCtrlExtSearchButton = "[C_EXTSEARCHBUTTON]";
        private const string _constCtrlSearchCloseButton = "[C_SEARCHCLOSEBUTTON]";
        private const string _constCtrlSearchShowTop = "[C_SEARCHSHOWTOP]";
        private const string _constFiltered = "[FILTERED]";
        private const string _constFilterHeaderName = "[C_FILTERNAME]";
        private const string _constCtrlFindButton = "[C_FINDBUTTON]";
        private const string _constCtrlHelpButton = "[C_HELPBUTTON]";
        private const string _constCtrlLikeButton = "[C_LIKEBUTTON]";
        private const string _constAddContent = "[C_ADDCONTENT]";
        
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
        private List<List<string>> _customCmdFunc = new List<List<string>>();
        private bool _customCmdFuncOnClient = false;
        #endregion

        #region AdvSearch
        /// <summary>
        ///     Установка источника данных на основании переданных параметров
        /// </summary>
        public void SetAdvSearchDataSource(DataTable dt)
        {
            Settings = new TreeViewSettings(dt, ID, TreeViewCmdListnerIndex, V4Page)
            {
                IsFilterEnable = ShowFilterOptions
            };
            SetColumnHeaderAlias();
        }

        /// <summary>
        ///     Установка алиасов названий колонок
        /// </summary>
        public void SetColumnHeaderAlias()
        {
            if (ColumnsHeaderAlias == null) return;
            foreach (var field in ColumnsHeaderAlias) Settings.SetColumnHeaderAlias(field.Key, field.Value);
        }

        public void SetColumnAdvSearchName(List<string> fields)
        {
            ColumnAdvSearchList = fields;
        }

        public void SetColumnNotNullSearch(List<string> fields)
        {
            ColumnNotNullSearchList = fields;
        }

        /// <summary>
        /// Список колонок, по которым не будет осуществляться поиск по нескольким словам 
        /// </summary>
        /// <param name="fields"></param>
        public void SetColumnNotSplitWhenFilter(List<string> fields)
        {
            ColumnNotSplitWhenFilter = fields;
        }

        public void RenderAdvancedSearchSettings(bool isHideFilterDialog = false)
        {
            var w = new StringWriter();
            var m = new StringWriter();
            var f = new StringWriter();
            var t = new StringWriter();
            var s = new StringWriter();

            if (Settings.IsFilterEnable)
            {
                V4Page.JS.Write(@"tv_clientLocalization = {{
                ok_button:""{0}"",
                cancel_button:""{1}"" ,
                empty_filter_value:""{2}"" 
            }};",
                    Resx.GetString("cmdApply"),
                    Resx.GetString("cmdCancel"),
                    Resx.GetString("msgEmptyFilterValue")
                );

                V4Page.JS.Write(
                    "$('#divColumnSettingsForm_ClearUserFilter_{0}').html('');$('#divColumnSettingsForm_ClearUserFilter_{0}').hide();",
                    Settings.TreeViewId);
            }

            if (Settings.TableColumns.Exists(x => x.FilterUser != null) || !Settings.FilterClause.IsNullEmptyOrZero() || (Settings.AddTableColumns != null && Settings.AddTableColumns.Exists(x => x.FilterUser != null)))
            {
                w = new StringWriter();
                RenderClearFilterBlock(w);
                V4Page.JS.Write(
                    "$('#divColumnSettingsForm_ClearUserFilter_{0}').html('{1}');$('#divColumnSettingsForm_ClearUserFilter_{0}').show();",
                    Settings.TreeViewId,
                    HttpUtility.JavaScriptStringEncode(w.ToString()));
            }
            else
            {
                V4Page.JS.Write(
                    "$('#divColumnSettingsForm_ClearUserFilter_{0}').html('');$('#divColumnSettingsForm_ClearUserFilter_{0}').hide();",
                    Settings.TreeViewId);
            }

            var iFilterCount = 0;
            w = new StringWriter();
            for (var c = 0; c < Settings.TableColumns.Count; c++)
            {
                var col = Settings.TableColumns[c];
                if (ColumnAdvSearchList == null || !ColumnAdvSearchList.Contains(col.FieldName))
                {
                    if (c == 0) col.RenderStartUserFilterBlock(w);
                    if (DbSourceSettings.FieldValuesList != null && DbSourceSettings.FieldValuesList.ContainsKey(col.FieldName))
                    {
                        col.RenderUserFilterBlock(w, DbSourceSettings.FieldValuesList[col.FieldName]);
                        col.RenderTextUserFilterBlock(f, iFilterCount, DbSourceSettings.FieldValuesList[col.FieldName]);
                        col.RenderTextUserFilterBlock(t, iFilterCount, DbSourceSettings.FieldValuesList[col.FieldName], false);
                        if (col.FilterUser != null) iFilterCount++;
                    }
                    else
                    {
                        col.RenderUserFilterBlock(w);
                        col.RenderTextUserFilterBlock(f, iFilterCount);
                        col.RenderTextUserFilterBlock(t, iFilterCount, null, false);
                        if (col.FilterUser != null) iFilterCount++;
                    }

                    if (c == Settings.TableColumns.Count - 1 && Settings.AddTableColumns == null) col.RenderEndUserFilterBlock(w);
                    {
                        m.Write(
                            "$('#v4_userFilterMenu_{0}_{2}').menu({{select: function(event, ui) {{v4_openUserAdvSearchFilterForm(ui.item, {1});}}}});",
                            Settings.TreeViewId, Settings.TreeViewCmdListnerIndex, col.Id);
                    }
                }
            }

            if (Settings.AddTableColumns != null)
            for (var c = 0; c < Settings.AddTableColumns.Count; c++)
            {
                var col = Settings.AddTableColumns[c];
                if (ColumnAdvSearchList == null || !ColumnAdvSearchList.Contains(col.FieldName))
                {
                    if (DbSourceSettings.FieldValuesList != null && DbSourceSettings.FieldValuesList.ContainsKey(col.FieldName))
                    {
                        col.RenderUserFilterBlock(w, DbSourceSettings.FieldValuesList[col.FieldName]);
                        col.RenderTextUserFilterBlock(f, iFilterCount, DbSourceSettings.FieldValuesList[col.FieldName]);
                        col.RenderTextUserFilterBlock(t, iFilterCount, DbSourceSettings.FieldValuesList[col.FieldName], false);
                        if (col.FilterUser != null) iFilterCount++;
                    }
                    else
                    {
                        col.RenderUserFilterBlock(w);
                        col.RenderTextUserFilterBlock(f, iFilterCount);
                        col.RenderTextUserFilterBlock(t, iFilterCount, null, false);
                        if (col.FilterUser != null) iFilterCount++;
                    }

                    if (c == Settings.AddTableColumns.Count - 1) col.RenderEndUserFilterBlock(w);
                    {
                        m.Write(
                            "$('#v4_userFilterMenu_{0}_{2}').menu({{select: function(event, ui) {{v4_openUserAdvSearchFilterForm(ui.item, {1});}}}});",
                            Settings.TreeViewId, Settings.TreeViewCmdListnerIndex, col.Id);
                    }
                }
            }

            V4Page.JS.Write("$('#divColumnSettingsForm_UserFilter_{0}').html('{1}'); $('#divColumnSettingsForm_UserFilter_{0}').show();",
                Settings.TreeViewId,
                HttpUtility.JavaScriptStringEncode(w.ToString()));

            V4Page.JS.Write(m.ToString());

            if (isHideFilterDialog == false)
            {
                V4Page.JS.Write("v4_advancedSearchForm(\"{0}\",\"{1}\", 'btnExtSearch_{0}', \"{2}\");",
                    ID,
                    TreeViewCmdListnerIndex,
                    HttpUtility.JavaScriptStringEncode(string.Format("{0}",
                        V4Page.Resx.GetString("lblSettingFilter"))));

                FillUniqueClause();

                RenderValuesFilterBlock(f, iFilterCount);

                V4Page.JS.Write("$('#divAdvancedSearchForm_Filter_{0}').html('{1}');", ID,
                    (f.ToString().IsNullEmptyOrZero())
                        ? ""
                        : Resx.GetString("lblSetFilter") + ": " + HttpUtility.JavaScriptStringEncode(f.ToString()));

            }

            if (AddFilterUser != null && Settings.IsFilterEnable)
            {
                w = new StringWriter();
                RenderValuesBlock(w);
                RenderValuesBlock(s, false);
                if (isHideFilterDialog == false)
                    V4Page.JS.Write("$('#divColumnSettingsForm_Values_{0}').html('{1}');", Settings.TreeViewId, HttpUtility.JavaScriptStringEncode(w.ToString()));
            }

            if (isHideFilterDialog)
            {
                var title = t.ToString().IsNullEmptyOrZero() ? "" : Resx.GetString("lblSetFilter") + ": " + t;
                if (!s.ToString().IsNullEmptyOrZero()) title += s;

                if (!title.IsNullEmptyOrZero())
                {
                    V4Page.JS.Write("$('#iconExtFilter_{0}').attr('title', '{1}');", ID, title);
                }

                V4Page.JS.Write("$('#iconExtFilterOff_{0}').attr('title', '{1}');", ID, Resx.GetString("lblRemoveAllInstalledFilters"));
                V4Page.JS.Write("$('#iconExtFilterLook_{0}').attr('title', '{1}');", ID, Resx.GetString("lblOpenSearchResultsSeparateWindow"));
            }

            V4Page.JS.Write("$('#btnUAdvSearch_Apply_{0}').focus();", ID);
        }

        /// <summary>
        ///     Получение списка уникальных условий
        /// </summary>
        public void FillUniqueClause()
        {
            var sqlParams = new Dictionary<string, object> { { "@ТипУсловия", ConditionName } };
            var dtLocal = DBManager.GetData(SQLQueries.SELECT_ДополнительныеФильтрыПоиска, Config.DS_user, CommandType.Text, sqlParams);

            if (dtLocal.Rows.Count > 0)
            {
                AddFilterUser = new List<TreeViewAddUserFilter>();

                AddFilterUser = (from DataRow row in dtLocal.Rows
                    select new TreeViewAddUserFilter
                    {
                        FilterId = row["КодУсловия"].ToString(),
                        FilterName = row["Условие"].ToString(),
                        FilterSQL = row["Запрос"].ToString()
                    }).ToList();
            }
        }

        /// <summary>
        ///     Формирование контролов выбора уникальных значений колонки
        /// </summary>
        /// <param name="w"></param>
        private void RenderValuesBlock(TextWriter w, bool isHtml = true)
        {
            if (AddFilterUser.Count == 0) return;

            if (isHtml)
                w.Write("<div class=\"v4DivTable\" >");
                      
            var valueInFilter = false;
            var checkState = "";
            var iFilter = 1;
            foreach (var item in AddFilterUser)
            {
                if (Settings.FilterClause != null)
                    valueInFilter = Settings.FilterClause.Split(',').Contains(item.FilterId);

                if (isHtml)
                {
                    checkState = valueInFilter ? "checked" : "";
                    w.Write("<div class=\"v4DivTableRow\">");
                    w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
                    w.Write(
                        "<input type='checkbox' class=\"classValueCheckBox\" {0} data-id=\"{1}\" onclick='cmdasync(\"cmd\", \"Listener\", \"ctrlId\", {2} ,\"cmdName\", \"ChangeAddFilterUser\", \"FilterId\", {1} )' >",
                        checkState,
                        item.FilterId,
                        TreeViewCmdListnerIndex
                        );
                    w.Write("</div>");
                    w.Write("<div class=\"v4DivTableCell v4PaddingCell\" style=\"text-align:left; white-space: nowrap;\">");

                    w.Write(item.FilterName);

                    w.Write("</div>");
                    w.Write("</div>");
                }
                else
                {
                    if (valueInFilter)
                    {
                        if (iFilter > 1) w.Write(" и ");
                        w.Write("["+item.FilterName+"]");
                    }
                }

                iFilter++;
            }

            if (isHtml) w.Write("</div>");
        }

        /// <summary>
        ///     Формирование контролов выбора уникальных значений колонки
        /// </summary>
        /// <param name="w"></param>
        private void RenderValuesFilterBlock(TextWriter w, int filterNum)
        {
            if (AddFilterUser == null || AddFilterUser.Count == 0) return;

            var valueInFilter = false;
            var iFilter = 0;
            foreach (var item in AddFilterUser)
            {
                if (Settings.FilterClause != null)
                    valueInFilter = Settings.FilterClause.Split(',').Contains(item.FilterId);

                if (valueInFilter)
                {
                    if (iFilter == 0) w.Write("<div class=\"v4DivTable\" >");

                    w.Write("<div class=\"v4DivTableRow\">");
                    w.Write("<div class=\"v4DivTableCell v4PaddingCell\">");
                    w.Write(filterNum > 0 ? " и" : "");
                    w.Write("</div>");
                    w.Write(
                        "<div class=\"v4DivTableCell v4PaddingCell\" style=\"text-align:left; white-space: nowrap;\">");
                    w.Write("[" + item.FilterName + "]");
                    w.Write("</div>");
                    w.Write("</div>");
                    iFilter++;
                    filterNum++;
                }
            }
            if (iFilter > 0) w.Write("</div>");
        }

        /// <summary>
        ///     Создание пользовательского фильтра
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="filterId"></param>
        private void SetFilterColumnByUser(string columnId, string filterId)
        {
            var clmn = Settings.TableColumns.FirstOrDefault(x => x.Id == columnId);
            if (clmn == null && Settings.AddTableColumns != null)
                clmn = Settings.AddTableColumns.FirstOrDefault(x => x.Id == columnId);

            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumnFound"), Resx.GetString("alertError"),
                    MessageStatus.Error);
                return;
            }

            if (DbSourceSettings.FieldValuesList != null && DbSourceSettings.FieldValuesList.ContainsKey(clmn.FieldName))
            {
                clmn.FilterUser = new TreeViewColumnUserFilter
                {
                    FilterType = TreeViewColumnUserFilterEnum.Равно,
                    FilterValue1 = filterId,
                    FilterValue2 = null
                };
            }
            else
            {
                var filter = (TreeViewColumnUserFilterEnum)int.Parse(filterId);
                object objField1 = null;
                object objField2 = null;

                if (filter == TreeViewColumnUserFilterEnum.НеУказано || filter == TreeViewColumnUserFilterEnum.Указано)
                {
                    clmn.FilterUser = new TreeViewColumnUserFilter { FilterType = filter };
                }
                else
                {
                    objField1 = GetFilterUserControlValue(clmn, clmn.FilterUserCtrlBaseName + "_" + ID + "_1");
                    if (filter == TreeViewColumnUserFilterEnum.Между)
                        objField2 = GetFilterUserControlValue(clmn, clmn.FilterUserCtrlBaseName + "_" + ID + "_2");
                }

                clmn.FilterUser = new TreeViewColumnUserFilter
                {
                    FilterType = filter,
                    FilterValue1 = objField1,
                    FilterValue2 = objField2
                };
            }

        }

        /// <summary>
        ///     Получение значений установленных фильтров
        /// </summary>
        /// <param name="clmn"></param>
        /// <param name="ctrlName"></param>
        /// <returns></returns>
        private object GetFilterUserControlValue(TreeViewColumn clmn, string ctrlName)
        {
            object value = null;
            if (!V4Page.V4Controls.ContainsKey(ctrlName)) return value;

            var ctrl = V4Page.V4Controls[ctrlName];
            if (ctrl is DatePicker)
            {
                value = ((DatePicker)ctrl).ValueDate;
            }
            else if (ctrl is Number)
            {
                if (clmn.ColumnType == TreeViewColumnTypeEnum.Int)
                {
                    value = ((Number)ctrl).ValueInt;
                }
                else
                {
                    if (clmn.ColumnType == TreeViewColumnTypeEnum.Double)
                    {
                        var valueDecimal = ((Number)ctrl).ValueDecimal;
                        if (valueDecimal != null) value = (double)valueDecimal;
                    }
                    else
                    {
                        value = ((Number)ctrl).ValueDecimal;
                    }
                }
            }
            else
            {
                value = ctrl.Value;
            }

            return value;
        }

        /// <summary>
        ///     Удаление установленных значений фильтров
        /// </summary>
        /// <param name="columnId"></param>
        private void ClearFilterColumnValues(string columnId)
        {
            var clmn = Settings.TableColumns.FirstOrDefault(x => x.Id == columnId);
            if (clmn == null && Settings.AddTableColumns != null)
                clmn = Settings.AddTableColumns.FirstOrDefault(x => x.Id == columnId);

            if (clmn == null)
            {
                V4Page.ShowMessage(Resx.GetString("msgErrorIdColumn"), Resx.GetString("alertError"),
                    MessageStatus.Error);
                return;
            }

            ClearFilterColumnValue(clmn, true);
        }

        /// <summary>
        ///     Удаление установленного значения фильтра в соответствии со значением по умолчанию
        /// </summary>
        private void ClearFilterColumnValue(TreeViewColumn clmn, bool OnlyOneFilterDelete = false)
        {
            if (clmn.DefaultValue == null || IsDeleteDefaultFilter || OnlyOneFilterDelete)
            {
                clmn.FilterUser = null;
            }
            else
            {
                var filterType = TreeViewColumnUserFilterEnum.Равно;
                var filterValue = clmn.DefaultValue;

                if (clmn.ColumnType == TreeViewColumnTypeEnum.Boolean)
                {
                    filterType = (int)filterValue == 0
                        ? TreeViewColumnUserFilterEnum.Нет
                        : TreeViewColumnUserFilterEnum.Да;
                    filterValue = null;
                }

                clmn.FilterUser = new TreeViewColumnUserFilter
                {
                    FilterType = filterType,
                    FilterValue1 = filterValue,
                    FilterValue2 = null
                };
            }
        }

        /// <summary>
        ///     Удаление установленных значений фильтров
        /// </summary>
        private void ClearAllFilterColumnValues(bool onlyDefaultFilter = true)
        {
            foreach (var clmn in Settings.TableColumns)
            {
                ClearFilterColumnValue(clmn, !onlyDefaultFilter);
            }

            if (Settings.AddTableColumns != null)
            foreach (var clmn in Settings.AddTableColumns)
            {
                ClearFilterColumnValue(clmn, !onlyDefaultFilter);
            }

            Settings.FilterClause = "";
        }

        /// <summary>
        ///     Удаление установленных значений фильтров и оригинальных значений для очистки фильтра
        /// </summary>
        private void ClearAllFilterColumnValuesAndRefresh()
        {
            ClearAllFilterColumnValues();

            foreach (var clmn in Settings.TableColumns)
            {
                clmn.FilterUserOriginal = clmn.FilterUser;
            }

            if (Settings.AddTableColumns != null)
                foreach (var clmn in Settings.AddTableColumns)
                {
                    clmn.FilterUserOriginal = clmn.FilterUser;
                }

            Settings.FilterClauseOriginal = "";

        }

        /// <summary>Адрес грузополучателя
        ///     Установка фильтра по выбранному значению
        /// </summary>
        private void SetFilterByColumnValues(string searchText, string searchParam)
        {
            if (OffTreeNodeFieldMap != null)
            {
                OffTreeNodeFieldMap.Condition = null;
                var clmn = Settings.TableColumns.FirstOrDefault(d => d.FilterUser != null && d.FieldName == OffTreeNodeFieldMap.FieldName1);
                if (clmn != null)
                {
                    OffTreeNodeFieldMap.Condition = clmn.FilterUser.FilterType == TreeViewColumnUserFilterEnum.Да;
                }
            }

            JS.Write("v4_reloadSearchNode('{0}','{1}','{2}');", ID, searchText, searchParam);
            if (searchParam != "clearfilter")
            {
                JS.Write("$('#divExtFilter_{0}').attr('style', 'display: inline-block; vertical-align: middle; white-space:nowrap;');", ID);

                if (!IsSearchResultInOtherWindow)
                    JS.Write("$('#iconExtFilterLook_{0}').attr('style', 'display: none; cursor: pointer;');", ID);

                JS.Write("$('#iconExtFilter_{0}').attr('onclick', 'v4_ShowExtSearchTreeView({1})');", ID, Settings.TreeViewCmdListnerIndex);
                JS.Write("$(\"#iconExtFilterOff_{0}\").attr(\"onclick\", \"v4_clearAllAdvSearchColumnValuesAndRefresh({1}, '{0}')\");", ID, Settings.TreeViewCmdListnerIndex);
                JS.Write("$(\"#iconExtFilterLook_{0}\").attr(\"onclick\", \"tv_dialogShow_{0}()\");", ID);
            }

            JS.Write("v4_treeViewHandleResize('{0}');", ID);
            RenderAdvancedSearchSettings(true);

        }

        /// <summary>
        ///     Отрисовка кнопки отмены фильтрации
        /// </summary>
        /// <param name="w"></param>
        private void RenderClearFilterBlock(TextWriter w)
        {
            w.Write("<div class=\"v4DivTable\">");

            w.Write("<div class=\"v4DivTableRow\">");

            w.Write(
                "<div class=\"v4DivTableCell v4PaddingCell\">");

            w.Write(
                "<span class=\"ui-icon ui-icon-delete\" tabindex=\"0\" onkeydown=\"v4_element_keydown(event, this);\" style=\"display: inline-block;cursor:pointer\" onclick=\"v4_clearAllAdvSearchColumnValues({0}, '{1}');\"></span>",
                Settings.TreeViewCmdListnerIndex, ID );

            w.Write(
                "</div><div class=\"v4DivTableCell\" tabindex=\"0\" onkeydown=\"v4_element_keydown(event, this);\" style=\"text-align:left;\"><a onclick=\"v4_clearAllAdvSearchColumnValues({0}, '{1}');\"><nobr>{2}</nobr></a></div>",
                Settings.TreeViewCmdListnerIndex, ID, Resx.GetString("lblDeleteAllFilter"));
            w.Write("</div>");
            w.Write("</div>");
            w.Write("</div>");
        }

        public void CloseDialogForm(string tvDialogId, string tvDialogIdp)
        {
            KescoHub.SendMessage(new SignalMessage
            {
                PageId = V4Page.IDPage,
                ItemId = V4Page.ItemId.ToString(),
                ItemName = V4Page.ItemName,
                IsV4Script = true,
                Message = $"<js>v4_tv_Records_Close('{tvDialogId}', '{tvDialogIdp}');</js>"
            }, SignaRReceiveClientsMessageEnum.Self);
        }

        #endregion
    }
}