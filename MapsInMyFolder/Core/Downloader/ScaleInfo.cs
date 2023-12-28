using System;
using System.Collections.Generic;

namespace MapsInMyFolder
{
    public class ScaleInfo
    {
        public double initialScale;
        public double targetScale;
        public double pixelsPerMeters;

        public bool doDrawScale;
        public double drawScaleEchelle;
        public double drawScalePixelLength;

        public ScaleInfo(double initialScale, double targetScale, double pixelsPerMeters, bool doDrawScale, double drawScaleEchelle, double drawScalePixelLength)
        {
            this.initialScale = initialScale;
            this.targetScale = targetScale;
            this.pixelsPerMeters = pixelsPerMeters;
            this.doDrawScale = doDrawScale;
            this.drawScaleEchelle = drawScaleEchelle;
            this.drawScalePixelLength = drawScalePixelLength;
        }

        public static double GetDistanceInMeterPerPixels(double TargetScale)
        {
            const double DPI = 96;
            const double CentimeterPerPixels = 2.54 / DPI;
            return TargetScale * CentimeterPerPixels / 100;
        }

        public static (double Scale, double PixelLenght) SearchOptimalScale(double distancePerPixel, double distanceMaxPixels)
        {
            if (distanceMaxPixels - 100 < 250)
            {
                return (-1, -1);
            }
            const int MinScaleWidth = 250;
            int MaxScaleWidth = Math.Min(750, (int)Math.Round(distanceMaxPixels - 100));
            List<double> AcceptablesValues = new List<double>() { 1, 2, 2.5, 5, 7.5 };

            distanceMaxPixels = Math.Max(MinScaleWidth, Math.Min(distanceMaxPixels * 0.1, MaxScaleWidth));


            double LastAcceptableScale = -1;
            double LastAcceptablePixelLenght = -1;
            int Power = 1;
            while (Power < Math.Pow(10, 10))
            {
                foreach (double value in AcceptablesValues)
                {
                    double ValueMultiplieByPower = value * Power;
                    if (ValueMultiplieByPower % 1 != 0)
                    {
                        continue;
                    }

                    if (ValueMultiplieByPower < 5)
                    {
                        continue;
                    }

                    double PixelRequireToDraw = ValueMultiplieByPower / distancePerPixel;

                    if (PixelRequireToDraw <= distanceMaxPixels)
                    {
                        LastAcceptableScale = ValueMultiplieByPower;
                        LastAcceptablePixelLenght = PixelRequireToDraw;
                    }
                    else
                    {
                        return (LastAcceptableScale, LastAcceptablePixelLenght);
                    }
                }
                Power *= 10;
            }
            return (-1, -1);
        }

    }
}
