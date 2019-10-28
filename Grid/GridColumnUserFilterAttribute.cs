using System;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    public class GridColumnUserFilterAttribute : Attribute
    {
        internal GridColumnUserFilterAttribute(string aliasString, string aliasNumber, string aliasDate)
        {
            AliasString = aliasString;
            AliasNumber = aliasNumber;
            AliasDate = aliasDate;
        }

        public string AliasString { get; }
        public string AliasNumber { get; }
        public string AliasDate { get; }
    }
}