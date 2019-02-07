/*
MIT License

Copyright (c) 2017 Saied Zarrinmehr

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using SpatialAnalysis.Geometry;
using System;
using System.Collections.Generic;

namespace SpatialAnalysis.Interoperability
{
    public enum Length_Unit_Types
    {
        METERS = 0,
        DECIMETERS = 1,
        CENTIMETERS = 2,
        MILLIMETERS = 3,
        FEET = 4,
        INCHES = 5

    }
    public class UnitConversion
    {
        public Length_Unit_Types FromUnit { get; private set; }
        public Length_Unit_Types ToUnit { get; private set; }
        public UnitConversion(Length_Unit_Types origin, Length_Unit_Types expected)
        {
            this.FromUnit = origin; this.ToUnit = expected;
        }
        public double Convert(double length)
        {
            return UnitConversion.Convert(length, this.FromUnit, this.ToUnit);
        }
        public double Convert(double length, int decimalRoundingFactor)
        {
            return UnitConversion.Convert(length, this.FromUnit, this.ToUnit, decimalRoundingFactor);
        }
        public void Transform(IList<double> values)
        {
            UnitConversion.Transform(values, this.FromUnit, this.ToUnit);
        }
        public void Transform(IEnumerable<UV> values)
        {
            UnitConversion.Transform(values, this.FromUnit, this.ToUnit);
        }
        public void Transform(UV val)
        {
            UnitConversion.Transform(val, this.FromUnit, this.ToUnit);
        }


        private static double ToFeet(double length, Length_Unit_Types originalType)
        {
            double scale = ToFeetScale(originalType);
            return length * scale;
        }
        private static double ToFeetScale(Length_Unit_Types originalType)
        {
            switch (originalType)
            {
                case Length_Unit_Types.METERS:
                    return 3.280839895;
                case Length_Unit_Types.DECIMETERS:
                    return 0.3280839895;
                case Length_Unit_Types.CENTIMETERS:
                    return 0.03280839895;
                case Length_Unit_Types.MILLIMETERS:
                    return 0.003280839895;
                case Length_Unit_Types.FEET:
                    break;
                case Length_Unit_Types.INCHES:
                    return 1.0 / 12.0;
            }
            return 1.0;//feet mapped to feet
        }
        private static double FromFeet(double length, Length_Unit_Types expectedType)
        {
            double scale = FromFeetScale(expectedType);
            return length * scale;
        }
        private static double FromFeetScale(Length_Unit_Types expectedType)
        {
            switch (expectedType)
            {
                case Length_Unit_Types.METERS:
                    return 1.0 / 3.280839895;
                case Length_Unit_Types.DECIMETERS:
                    return 1.0 / 0.3280839895;
                case Length_Unit_Types.CENTIMETERS:
                    return 1.0 / 0.03280839895;
                case Length_Unit_Types.MILLIMETERS:
                    return 1.0 / 0.003280839895;
                case Length_Unit_Types.FEET:
                    break;
                case Length_Unit_Types.INCHES:
                    return 12.0;
            }
            return 1.0;
        }
        private static double ToMeter(double length, Length_Unit_Types originalType)
        {
            double scale = ToMeterScale(originalType);
            return length * scale;
        }
        private static double ToMeterScale(Length_Unit_Types originalType)
        {
            switch (originalType)
            {
                case Length_Unit_Types.METERS:
                    break;
                case Length_Unit_Types.DECIMETERS:
                    return 1.0 / 10.0;
                case Length_Unit_Types.CENTIMETERS:
                    return 1.0 / 100.0;
                case Length_Unit_Types.MILLIMETERS:
                    return 1.0 / 1000.0;
                case Length_Unit_Types.FEET:
                    return 1.0 / 3.280839895;
                case Length_Unit_Types.INCHES:
                    return 1.0 / (12.0 * 3.280839895);
            }
            return 1.0;
        }
        private static double FromMeter(double length, Length_Unit_Types expectedType)
        {
            double scale = FromMeterScale(expectedType);
            return length * scale;
        }
        private static double FromMeterScale(Length_Unit_Types expectedType)
        {
            switch (expectedType)
            {
                case Length_Unit_Types.METERS:
                    break;
                case Length_Unit_Types.DECIMETERS:
                    return 10.0;
                case Length_Unit_Types.CENTIMETERS:
                    return 100.0;
                case Length_Unit_Types.MILLIMETERS:
                    return 1000.0;
                case Length_Unit_Types.FEET:
                    return 3.280839895;
                case Length_Unit_Types.INCHES:
                    return 3.280839895 * 12.0;
            }
            return 1.0;
        }
        private static bool IsImperial(Length_Unit_Types unit_type)
        {
            if (unit_type == Length_Unit_Types.INCHES ||
                unit_type == Length_Unit_Types.FEET)
                return true;
            return false;
        }
        private static bool IsMetric(Length_Unit_Types unit_type)
        {
            if (unit_type == Length_Unit_Types.METERS ||
                unit_type == Length_Unit_Types.DECIMETERS ||
                unit_type == Length_Unit_Types.CENTIMETERS ||
                unit_type == Length_Unit_Types.MILLIMETERS)
                return true;
            return false;
        }
        public static double ConvertScale(Length_Unit_Types originalType, Length_Unit_Types expectedType)
        {
            if (originalType == expectedType) return 1.0;
            //check system equality
            bool originalSystem = IsImperial(originalType);
            bool expectedSystem = IsImperial(expectedType);
            if (originalSystem == expectedSystem)//same unit systems
            {
                if (originalSystem) //both imperial units
                {
                    double scale = ToFeetScale(originalType) * FromFeetScale(expectedType);
                    return scale;
                }
                else //both metric units
                {
                    double scale = ToMeterScale(originalType) * FromMeterScale(expectedType);
                    return scale;
                }
            }
            //different unit systems
            double scale_ = ToMeterScale(originalType) * FromMeterScale(expectedType);
            return scale_;
        }
        public static double Convert(double length, Length_Unit_Types originalType, Length_Unit_Types expectedType)
        {
            if (originalType == expectedType) return length;
            double scale = ConvertScale(originalType, expectedType);
            return length * scale;
        }
        public static double Convert(double length, Length_Unit_Types originalType, Length_Unit_Types expectedType, int decimalRoundingFactor)
        {
            if (originalType == expectedType) return length;
            double scale = ConvertScale(originalType, expectedType);
            return Math.Round(length * scale, decimalRoundingFactor);
        }
        public static void Transform(IList<double> values, Length_Unit_Types originalType, Length_Unit_Types expectedType)
        {
            if (originalType == expectedType) return;
            double scale = ConvertScale(originalType, expectedType);
            for (int i = 0; i < values.Count; i++)
            {
                values[i] *= scale;
            }
        }
        public static void Transform(IEnumerable<UV> values, Length_Unit_Types originalType, Length_Unit_Types expectedType)
        {
            if (originalType == expectedType) return;
            double scale = ConvertScale(originalType, expectedType);
            foreach (UV item in values)
            {
                item.U *= scale;
                item.V *= scale;
            }
        }
        public static void Transform(UV value, Length_Unit_Types originalType, Length_Unit_Types expectedType)
        {
            if (originalType == expectedType) return;
            double scale = ConvertScale(originalType, expectedType);
            value.U *= scale;
            value.V *= scale;
        }

    }

}
