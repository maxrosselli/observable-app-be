namespace MonitoringPOC.Models
{
    public class OrderDto
    {
        public string OrderId { get; set; }
        public string Destination { get; set; }
        public string ItemType { get; set; }
        public double WeightKg { get; set; }
        public string Priority { get; set; }
    }
}
