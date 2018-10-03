using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    public enum GridColumnTypeEnum
    {
        Int = 1,
        String = 2,
        Date = 4,
        Boolean = 8,
        Decimal = 16,
        Person = 32,
        Document = 64
    }

    public enum GridColumnFilterEqualEnum
    {
        NotIn = 0,
        In = 1
    }

    public enum GridColumnOrderByDirectionEnum
    {
        Asc = 0,
        Desc = 1
    }

    public enum GridColumnUserFilterEnum
    {
        [GridColumnUserFilter("lblNoEmptyString", "lblAnyValueSpecified", "lblAnyValueSpecified")]
        Указано = 0,
        [GridColumnUserFilter("lblEmptyString", "lblNoValue", "lblNoValue")]
        НеУказано = 1,

        [GridColumnUserFilter("lblEqually", "lblEqually", "lblEqually")]
        Равно = 10,
        [GridColumnUserFilter("lblNotEqual", "lblNotEqual", "lblNotEqual")]
        НеРавно = 11,

        [GridColumnUserFilter("", "lblBetween", "lblInInterval")]
        Между = 150,

        [GridColumnUserFilter("lblBeginWith", "", "")]
        НачинаетсяС = 20,
        [GridColumnUserFilter("lblEndsWith", "", "")]
        ЗаканчиваетсяНа = 21,
        [GridColumnUserFilter("lblContains", "", "")]
        Содержит = 30,
        [GridColumnUserFilter("lblNoContains", "", "")]
        НеСодержит = 31,

        [GridColumnUserFilter("", "lblMore", "lblMore")]
        Больше = 100,
        [GridColumnUserFilter("", "lblMoreOrEqual", "lblMoreOrEqual")]
        БольшеИлиРавно = 101,
        [GridColumnUserFilter("", "lblLess", "lblLess")]
        Меньше = 110,
        [GridColumnUserFilter("", "lblLessOrEqual", "lblLessOrEqual")]
        МеньшеИлиРавно = 111,


        [GridColumnUserFilter("", "", "")]
        Истина = 200,
        [GridColumnUserFilter("", "", "")]
        Ложь = 201
    }

}
