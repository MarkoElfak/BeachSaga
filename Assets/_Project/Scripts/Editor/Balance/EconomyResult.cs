namespace BeachTycoon.EditorTools
{
    /// <summary>What one beach earns and pays per hour in the balance model.</summary>
    public struct EconomyResult
    {
        public double Visitors;
        public double Sales;
        public double Weddings;
        public double Rent;
        public double Wages;
        public double Unserved;

        public double Net => Sales + Weddings + Rent - Wages;
    }
}
