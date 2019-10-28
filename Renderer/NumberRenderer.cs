using System;
using System.Globalization;
using System.IO;
using Convert = Kesco.Lib.ConvertExtention.Convert;

namespace Kesco.Lib.Web.Controls.V4.Renderer
{
    /// <summary>
    ///     Класс отрисовки числа
    /// </summary>
    public class NumberRenderer : Renderer
    {
        private readonly bool _alignRight;
        private readonly string _groupSeparator;
        private readonly int _maxScale;
        private readonly int _minScale;
        private decimal _d; //value
        private bool _undefined = true; //undefined

        /// <summary>
        ///     Конструктор с параметрами
        /// </summary>
        /// <param name="minScale">Минимальное значение</param>
        /// <param name="maxScale">Максимальное значение</param>
        /// <param name="groupSeparator">Разделитель</param>
        public NumberRenderer(int minScale, int maxScale, string groupSeparator)
            : this(minScale, maxScale, groupSeparator, false)
        {
        }

        /// <summary>
        ///     Конструктор с параметрами
        /// </summary>
        /// <param name="minScale">Минимальное значение</param>
        /// <param name="maxScale">Максимальное значение</param>
        /// <param name="groupSeparator">Разделитель</param>
        /// <param name="alignRight">Признак выравнивания по правому краю</param>
        public NumberRenderer(int minScale, int maxScale, string groupSeparator, bool alignRight)
        {
            _minScale = minScale;
            _maxScale = maxScale;
            _groupSeparator = groupSeparator;
            _alignRight = alignRight;
            Format = null;
        }

        /// <summary>
        ///     Конструктор без параметров
        /// </summary>
        public NumberRenderer()
        {
            Format = (NumberFormatInfo) NumberFormatInfo.CurrentInfo.Clone();
        }

        /// <summary>
        ///     Конструктор с параметром Формат
        /// </summary>
        /// <param name="format"></param>
        public NumberRenderer(NumberFormatInfo format)
        {
            Format = format;
        }

        /// <summary>
        ///     Формат числа
        /// </summary>
        public NumberFormatInfo Format { get; }

        /// <summary>
        ///     Значение в виде строки
        /// </summary>
        public string ValueString
        {
            get
            {
                if (_undefined) return "";
                return Convert.Decimal2Str(_d, 0);
            }
            set
            {
                if (value.Length > 0)
                {
                    _d = Convert.Str2Decimal(value);
                    _undefined = false;
                }
                else
                {
                    _d = 0;
                    _undefined = true;
                }
            }
        }

        /// <summary>
        ///     Значение
        /// </summary>
        public decimal Value
        {
            set
            {
                _d = value;
                _undefined = false;
            }
            get { return _d; }
        }

        /// <summary>
        ///     Признак неопределенности
        /// </summary>
        public bool IsUndefined
        {
            set
            {
                _undefined = value;
                if (_undefined) _d = 0;
            }
            get { return _undefined; }
        }

        public override void Render(TextWriter w, string value)
        {
            ValueString = value;
            Render(w);
        }

        public override void Render(TextWriter w)
        {
            if (_undefined) return;

            if (_alignRight) w.Write("<div align=\"right\">");
            if (Format != null)
            {
                var scale = Format.NumberDecimalDigits;
                var n = 0;
                for (var r = _d; r - decimal.Truncate(r) != 0; r *= 10) n++;
                var nfi = (NumberFormatInfo) Format.Clone();
                nfi.NumberDecimalDigits = Math.Max(scale, n);
                w.Write(_d.ToString("N", nfi));
            }
            else
            {
                w.Write(Number.FormatNumber(ValueString, _minScale, _maxScale, _groupSeparator));
            }

            if (_alignRight) w.Write("</div>");
        }
    }
}