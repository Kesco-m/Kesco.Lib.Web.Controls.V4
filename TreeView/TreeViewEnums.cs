namespace Kesco.Lib.Web.Controls.V4.TreeView
{
    public enum TreeViewColumnTypeEnum
    {
        Int = 1,
        String = 2,
        Date = 4,
        Boolean = 8,
        Decimal = 16,
        Double = 32,
        Float = 64,
        Person = 128,
        Document = 256
    }

    public enum TreeViewColumnFilterEqualEnum
    {
        NotIn = 0,
        In = 1
    }

    public enum TreeViewColumnOrderByDirectionEnum
    {
        Asc = 0,
        Desc = 1
    }

    public enum TreeViewOffConditionTypeEnum
    {
        Int = 1,
        ToDate = 2,
        FromToDate = 3
    }

    public enum TreeViewColumnUserFilterEnum
    {
        [TreeViewColumnUserFilter("lblNoEmptyString", "lblAnyValueSpecified", "lblAnyValueSpecified", "", true)]
        Указано = 0,

        [TreeViewColumnUserFilter("lblEmptyString", "lblNoValue", "lblNoValue", "", false)]
        НеУказано = 1,

        [TreeViewColumnUserFilter("lblEqually", "lblEqually", "lblEqually", "", true)]
        Равно = 10,

        [TreeViewColumnUserFilter("lblNotEqual", "lblNotEqual", "lblNotEqual", "", true)]
        НеРавно = 11,

        [TreeViewColumnUserFilter("", "lblBetween", "lblInInterval", "", true)]
        Между = 150,

        [TreeViewColumnUserFilter("lblBeginWith", "", "", "", true)]
        НачинаетсяС = 20,

        [TreeViewColumnUserFilter("lblEndsWith", "", "", "", true)]
        ЗаканчиваетсяНа = 21,

        [TreeViewColumnUserFilter("lblContains", "", "", "", true)]
        Содержит = 30,

        [TreeViewColumnUserFilter("lblNoContains", "", "", "", true)]
        НеСодержит = 31,

        [TreeViewColumnUserFilter("", "lblMore", "lblMore", "", true)]
        Больше = 100,

        [TreeViewColumnUserFilter("", "lblMoreOrEqual", "lblMoreOrEqual", "", true)]
        БольшеИлиРавно = 101,

        [TreeViewColumnUserFilter("", "lblLess", "lblLess", "", true)]
        Меньше = 110,

        [TreeViewColumnUserFilter("", "lblLessOrEqual", "lblLessOrEqual", "", true)]
        МеньшеИлиРавно = 111,

        [TreeViewColumnUserFilter("", "", "", "QSBtnYes", true)]
        Да = 200,
        [TreeViewColumnUserFilter("", "", "", "QSBtnNo", true)]
        Нет = 201
    }
}