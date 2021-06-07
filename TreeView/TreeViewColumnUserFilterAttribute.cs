using System;

namespace Kesco.Lib.Web.Controls.V4.TreeView
{
    public class TreeViewColumnUserFilterAttribute : Attribute
    {
        internal TreeViewColumnUserFilterAttribute(string aliasString, string aliasNumber, string aliasDate, string aliasBoolean, bool isAllowNull)
        {
            AliasString = aliasString;
            AliasNumber = aliasNumber;
            AliasDate = aliasDate;
            AliasBoolean = aliasBoolean;
            IsAllowNull = isAllowNull;
        }

        public string AliasString { get; }
        public string AliasNumber { get; }
        public string AliasDate { get; }
        public string AliasBoolean { get; }
        public bool IsAllowNull { get; }
    }
}