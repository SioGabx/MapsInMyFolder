using System.Xml.Linq;

namespace MapsInMyFolder.Commun.Capabilities
{
    public partial class WMTSParser
    {
        public class BoundingBox
        {
            public BoundingBox(double SouthwestX, double SouthwestY, double NortheastX, double NortheastY)
            {
                this.SouthwestX = SouthwestX;
                this.SouthwestY = SouthwestY;
                this.NortheastX = NortheastX;
                this.NortheastY = NortheastY;
            }

            public double SouthwestX { get; set; }
            public double SouthwestY { get; set; }
            public double NortheastX { get; set; }
            public double NortheastY { get; set; }

            internal static BoundingBox ParseBBox(XElement LowerCorner, XElement UpperCorner)
            {
                string lowerCorner = LowerCorner?.Value;
                string upperCorner = UpperCorner?.Value;

                if (!string.IsNullOrEmpty(lowerCorner) && !string.IsNullOrEmpty(upperCorner))
                {
                    string[] lowerCoords = lowerCorner.Split(' ');
                    string[] upperCoords = upperCorner.Split(' ');

                    if (lowerCoords.Length == 2 && upperCoords.Length == 2)
                    {
                        double southwestX = double.Parse(lowerCoords[0]);
                        double southwestY = double.Parse(lowerCoords[1]);
                        double northeastX = double.Parse(upperCoords[0]);
                        double northeastY = double.Parse(upperCoords[1]);

                        return new BoundingBox(southwestX, southwestY, northeastX, northeastY);
                    }
                }

                return new BoundingBox(0, 0, 0, 0);
            }
        }
    }
}