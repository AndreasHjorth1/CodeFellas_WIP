namespace Prosjekt.Utils
{
    public static class Distance
    {
        /// <summary>
        /// Kalkulerer avstanden mellom to koordinater i meter. Bruker Haversine-formelen.
        /// </summary>
        public static double InMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000;
            double f1 = lat1 * Math.PI / 180;
            double f2 = lat2 * Math.PI / 180;
            double dF = (lat2 - lat1) * Math.PI / 180;
            double dL = (lng2 - lng1) * Math.PI / 180;

            double a = Math.Sin(dF / 2) * Math.Sin(dF / 2)
                     + Math.Cos(f1) * Math.Cos(f2) * Math.Sin(dL / 2) * Math.Sin(dL / 2);

            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
