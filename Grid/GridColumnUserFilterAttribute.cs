using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public string AliasString { get; private set; }
        public string AliasNumber { get; private set; }
        public string AliasDate { get; private set; }
    }
}
