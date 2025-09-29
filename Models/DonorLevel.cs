namespace Donation_Website.Models
{
    public class DonorLevel
    {
        public string Name { get; }
        public decimal Threshold { get; }
        public string Color { get; }
        public string Icon { get; }

        public DonorLevel(string name, decimal threshold, string color, string icon)
        {
            Name = name;
            Threshold = threshold;
            Color = color;
            Icon = icon;
        }
    }
}
