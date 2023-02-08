using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MedPlot
{
    class Complexo
    {
        public double re;
        public double im;

        public Complexo(double Real, double Imaginario)
        {
            re = Real;
            im = Imaginario;
        }

        public static Complexo operator ^(Complexo arg1, int arg2)
        {
            int i = 0;
            Complexo x = new Complexo(0.0, 0.0);

            if (arg2 == 0)
            {
                return x;
            }
            else
            {
                x = arg1;
                for (i = 1; i < arg2; i++)
                {
                    x = x * arg1;
                }
                return x;
            }
        }

        public static Complexo operator +(Complexo arg1, Complexo arg2)
        {
            return (new Complexo(arg1.re + arg2.re, arg1.im + arg2.im));
        }

        public static Complexo operator -(Complexo arg1)
        {
            return (new Complexo(-arg1.re, -arg1.im));
        }

        public static Complexo operator -(Complexo arg1, Complexo arg2)
        {
            return (new Complexo(arg1.re - arg2.re, arg1.im - arg2.im));
        }

        public static Complexo operator *(Complexo arg1, Complexo arg2)
        {
            return (new Complexo(arg1.re * arg2.re - arg1.im * arg2.im, arg1.re * arg2.im + arg2.re * arg1.im));
        }

        public static Complexo operator /(Complexo arg1, Complexo arg2)
        {
            double c1, c2, d;
            d = arg2.re * arg2.re + arg2.im * arg2.im;
            if (d == 0)
            {
                return (new Complexo(0, 0));
            }
            c1 = arg1.re * arg2.re + arg1.im * arg2.im;
            c2 = arg1.im * arg2.re - arg1.re * arg2.im;
            return (new Complexo(c1 / d, c2 / d));
        }

        public double Abs()
        {
            return (Math.Sqrt(re * re + im * im));
        }

        //Arg of complex number in degrees
        public double Arg()
        {
            double ret = 0;
            if (re != 0)
                ret = (180 / Math.PI) * Math.Atan2(im, re);
            return (ret);

        }

        public override string ToString()
        {
            return (String.Format("Complex: ({0}, {1})", re, im));
        }

        public string ToComplexString(int iRounding)
        {
            string ComplexNumber = Convert.ToString(Math.Round(re, iRounding)) + "+" + Convert.ToString(Math.Round(im, iRounding)) + "i";
            return ComplexNumber;
        }
    }
}
