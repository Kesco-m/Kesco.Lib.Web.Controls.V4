using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Kesco.Lib.Web.Controls.V4.Renderer
{
    /// <summary>
    ///     Вспомогательный класс для модального окна(перенесено из V3)
    /// </summary>
    public class DialogWindowFeatures
    {
        public bool center = true;
        //:{ yes | no | 1 | 0 | on | off } Specifies whether to center the dialog window within the desktop. The default is yes. 

        public int dialogHeight = 400;
        //:sHeight Sets the height of the dialog window (see Remarks for default unit of measure). 

        public bool dialogHide = false;
        //:{ yes | no | 1 | 0 | on | off } Specifies whether the dialog window is hidden when printing or using print preview. This feature is only available when a dialog box is opened from a trusted application. The default is no. ;

        public int dialogLeft = 100;
        //:sXPos Sets the left position of the dialog window relative to the upper-left corner of the desktop. 

        public int dialogTop = 100;
        //:sYPos Sets the top position of the dialog window relative to the upper-left corner of the desktop. 

        public int dialogWidth = 400;
        public bool edgeSunken = false;
        // | raised } Specifies the edge style of the dialog window. The default is raised. ;

        public bool help = false;
        //:{ yes | no | 1 | 0 | on | off } Specifies whether the dialog window displays the context-sensitive Help icon. The default is yes. 

        public bool resizable;
        //:{ yes | no | 1 | 0 | on | off } Specifies whether the dialog window has fixed dimensions. The default is no. 

        public bool scroll = true;
        //:{ yes | no | 1 | 0 | on | off } Specifies whether the dialog window displays scrollbars. The default is yes. 

        public bool status = false;
        //:{ yes | no | 1 | 0 | on | off } Specifies whether the dialog window displays a status bar. The default is yes for untrusted dialog windows and no for trusted dialog windows. 

        public bool unadorned = false;

        public DialogWindowFeatures(int dialogWidth, int dialogHeight, bool resizable, bool scroll)
        {
            this.dialogWidth = dialogWidth;
            this.dialogHeight = dialogHeight;
            this.resizable = resizable;
            this.scroll = scroll;
            center = true;
        }

        public DialogWindowFeatures(int dialogWidth, int dialogHeight, int dialogLeft, int dialogTop, bool resizable,
            bool scroll)
        {
            this.dialogWidth = dialogWidth;
            this.dialogHeight = dialogHeight;
            this.resizable = resizable;
            this.scroll = scroll;
            center = false;
            this.dialogLeft = dialogLeft;
            this.dialogTop = dialogTop;
        }

        public override string ToString()
        {
            var s = "";
            s += "dialogWidth:" + dialogWidth + "px;";
            s += "dialogHeight:" + dialogHeight + "px;";
            if (center)
            {
                s += "center:1;";
            }
            else
            {
                s += "dialogLeft:" + dialogLeft + "px;";
                s += "dialogTop:" + dialogTop + "px;";
                s += "center:0;";
            }

            s += "dialogHide:" + (dialogHide ? "1" : "0");
            s += ";edgeSunken:" + (edgeSunken ? "sunken" : "raised");
            s += ";help:" + (help ? "1" : "0");
            s += ";resizable:" + (resizable ? "1" : "0");
            s += ";scroll:" + (scroll ? "1" : "0");
            s += ";status:" + (status ? "1" : "0");
            s += ";unadorned:" + (unadorned ? "1" : "0");

            return s;
        }

        public void Load(int clid)
        {
            var parameters = new NameValueCollection();
            parameters.Add("Width", dialogWidth.ToString());
            parameters.Add("Height", dialogHeight.ToString());
            if (parameters["Width"] != null && Regex.IsMatch(parameters["Width"], "^\\d+$"))
                dialogWidth = int.Parse(parameters["Width"]);
            if (parameters["Height"] != null && Regex.IsMatch(parameters["Height"], "^\\d+$"))
                dialogHeight = int.Parse(parameters["Height"]);
        }
    }
}