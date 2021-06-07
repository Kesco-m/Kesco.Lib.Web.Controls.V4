using System.Configuration;
using System.Xml;
using Kesco.Lib.Web.Controls.V4.Common;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    public class ParamConfigurationSectionHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            var list = section.SelectNodes("param");
            if (list == null || list.Count == 0) return null;

            var pi = new ParamItem[list.Count];

            for (var i = 0; i < pi.Length; i++)
                pi[i] = new ParamItem((XmlElement) list[i]);

            return pi;
        }
    }
}