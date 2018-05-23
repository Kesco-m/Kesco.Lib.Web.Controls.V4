using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    public class BarCodeHandler : IHttpHandler
    {
        public static Hashtable ht;


        public void ProcessRequest(HttpContext context)
        {
            string text = "*." + context.Request.QueryString["id"].ToUpper() + "*";

            float y = 0;
            float x = 0;
            float h = 30;
            float w;

            SolidBrush bb = new SolidBrush(Color.Black);

            Bitmap bmp = new Bitmap(1000, (int)h + 12);
            bmp.SetResolution(300, 300);

            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);

            int[] arr;

            char ch;

            for (int i = 0; i < text.Length; i++)
            {
                ch = text[i];
                if (i > 0)
                {
                    w = 1;

                    if (ch != '*' && ch != '.') g.DrawString(ch.ToString(), new Font("Arial", 9, GraphicsUnit.Pixel), bb, x + 3, y + h);

                    x += w;
                }

                if (ht.ContainsKey(ch))
                {
                    arr = (int[])ht[ch];
                    w = arr[0] * 2 + 1;
                    g.FillRectangle(bb, x, y, w, h);
                    x += w;
                    w = arr[5] * 2 + 1;
                    x += w;
                    w = arr[1] * 2 + 1;
                    g.FillRectangle(bb, x, y, w, h);
                    x += w;
                    w = arr[6] * 2 + 1;
                    x += w;
                    w = arr[2] * 2 + 1;
                    g.FillRectangle(bb, x, y, w, h);
                    x += w;
                    w = arr[7] * 2 + 1;
                    x += w;
                    w = arr[3] * 2 + 1;
                    g.FillRectangle(bb, x, y, w, h);
                    x += w;
                    w = arr[8] * 2 + 1;
                    x += w;
                    w = arr[4] * 2 + 1;
                    g.FillRectangle(bb, x, y, w, h);
                    x += w;
                }
            }
            g.Dispose();

            context.Response.ContentType = "image/gif";

            Rectangle rect = new Rectangle(0, 0, (int)x, bmp.Height);

            Bitmap bmp2 = bmp.Clone(rect, PixelFormat.Format32bppPArgb);
            bmp.Dispose();

            bmp2.SetResolution(300, 300);

            bmp2.Save(context.Response.OutputStream, ImageFormat.Gif);
            bmp2.Dispose();
        }


        static BarCodeHandler()
        {
            ht = new Hashtable(39);
            //char				bars		spaces
            ht['1'] = new int[] { 1, 0, 0, 0, 1, 0, 1, 0, 0 };
            ht['2'] = new int[] { 0, 1, 0, 0, 1, 0, 1, 0, 0 };
            ht['3'] = new int[] { 1, 1, 0, 0, 0, 0, 1, 0, 0 };
            ht['4'] = new int[] { 0, 0, 1, 0, 1, 0, 1, 0, 0 };
            ht['5'] = new int[] { 1, 0, 1, 0, 0, 0, 1, 0, 0 };
            ht['6'] = new int[] { 0, 1, 1, 0, 0, 0, 1, 0, 0 };
            ht['7'] = new int[] { 0, 0, 0, 1, 1, 0, 1, 0, 0 };
            ht['8'] = new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0 };
            ht['9'] = new int[] { 0, 1, 0, 1, 0, 0, 1, 0, 0 };
            ht['0'] = new int[] { 0, 0, 1, 1, 0, 0, 1, 0, 0 };

            ht['A'] = new int[] { 1, 0, 0, 0, 1, 0, 0, 1, 0 };
            ht['B'] = new int[] { 0, 1, 0, 0, 1, 0, 0, 1, 0 };
            ht['C'] = new int[] { 1, 1, 0, 0, 0, 0, 0, 1, 0 };
            ht['D'] = new int[] { 0, 0, 1, 0, 1, 0, 0, 1, 0 };
            ht['E'] = new int[] { 1, 0, 1, 0, 0, 0, 0, 1, 0 };
            ht['F'] = new int[] { 0, 1, 1, 0, 0, 0, 0, 1, 0 };
            ht['G'] = new int[] { 0, 0, 0, 1, 1, 0, 0, 1, 0 };
            ht['H'] = new int[] { 1, 0, 0, 1, 0, 0, 0, 1, 0 };
            ht['I'] = new int[] { 0, 1, 0, 1, 0, 0, 0, 1, 0 };
            ht['J'] = new int[] { 0, 0, 1, 1, 0, 0, 0, 1, 0 };
            ht['K'] = new int[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
            ht['L'] = new int[] { 0, 1, 0, 0, 1, 0, 0, 0, 1 };
            ht['M'] = new int[] { 1, 1, 0, 0, 0, 0, 0, 0, 1 };
            ht['N'] = new int[] { 0, 0, 1, 0, 1, 0, 0, 0, 1 };
            ht['O'] = new int[] { 1, 0, 1, 0, 0, 0, 0, 0, 1 };
            ht['P'] = new int[] { 0, 1, 1, 0, 0, 0, 0, 0, 1 };
            ht['Q'] = new int[] { 0, 0, 0, 1, 1, 0, 0, 0, 1 };
            ht['R'] = new int[] { 1, 0, 0, 1, 0, 0, 0, 0, 1 };
            ht['S'] = new int[] { 0, 1, 0, 1, 0, 0, 0, 0, 1 };
            ht['T'] = new int[] { 0, 0, 1, 1, 0, 0, 0, 0, 1 };
            ht['U'] = new int[] { 1, 0, 0, 0, 1, 1, 0, 0, 0 };
            ht['V'] = new int[] { 0, 1, 0, 0, 1, 1, 0, 0, 0 };
            ht['W'] = new int[] { 1, 1, 0, 0, 0, 1, 0, 0, 0 };
            ht['X'] = new int[] { 0, 0, 1, 0, 1, 1, 0, 0, 0 };
            ht['Y'] = new int[] { 1, 0, 1, 0, 0, 1, 0, 0, 0 };
            ht['Z'] = new int[] { 0, 1, 1, 0, 0, 1, 0, 0, 0 };

            ht['-'] = new int[] { 0, 0, 0, 1, 1, 1, 0, 0, 0 };
            ht['.'] = new int[] { 1, 0, 0, 1, 0, 1, 0, 0, 0 };
            ht[' '] = new int[] { 0, 1, 0, 1, 0, 1, 0, 0, 0 };
            ht['*'] = new int[] { 0, 0, 1, 1, 0, 1, 0, 0, 0 };
            ht['$'] = new int[] { 0, 0, 0, 0, 0, 1, 1, 1, 0 };
            ht['/'] = new int[] { 0, 0, 0, 0, 0, 1, 1, 0, 1 };
            ht['+'] = new int[] { 0, 0, 0, 0, 0, 1, 0, 1, 1 };
            ht['%'] = new int[] { 0, 0, 0, 0, 0, 0, 1, 1, 1 };
        }


        /*
        public void ProcessRequest(System.Web.HttpContext context)
        {
                string text = "(." + context.Request.QueryString["id"] + ")";

                string _sz=context.Request.QueryString["sz"];
                int sz= (_sz==null||!Regex.IsMatch(_sz,"\\d{1,2}"))?16:int.Parse(_sz) ;
                if(sz<10||sz>90) sz=16;

                Bitmap pic = new Bitmap(10,10);
                pic.SetResolution(300,300);
                Graphics g = Graphics.FromImage(pic);

                //string fontName = "IDAutomationHC39M.ttf";
                //PrivateFontCollection privateFontCollection = new PrivateFontCollection();
                //privateFontCollection.AddFontFile("c:\\"+fontName);
                //FontFamily fontFamily = privateFontCollection.Families[0]; 

                Font font = new Font("IDAutomationHC39M", sz, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel); 

                SizeF tsz=g.MeasureString(text,font,1000);

                g.Dispose();
                pic.Dispose();

                Bitmap ready = new Bitmap((int)tsz.Width,(int)tsz.Height);
                ready.SetResolution(300,300);
                g = Graphics.FromImage(ready);
                g.Clear(Color.White);
                g.DrawString(text, font, new SolidBrush(Color.Black),0,0); 

                context.Response.ContentType = "image/gif";

                ready.Save(context.Response.OutputStream, ImageFormat.Gif); 

                ready.Dispose();
        }
*/

        public bool IsReusable
        {
            get { return true; }
        }
    }
}
