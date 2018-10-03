using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kesco.Lib.Web.Controls.V4
{
    public class SelectedRunItem
    {
        public int Value { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public bool IsDisabled { get; set; }
        public int Order { get; set; }
        public int? GroupValue { get; set; }

    }
}
