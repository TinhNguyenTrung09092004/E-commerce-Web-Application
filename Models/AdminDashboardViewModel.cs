namespace WebShop.Models
{
    public class AdminDashboardViewModel
    {
        public int NumberOfAdmins { get; set; }
        public int NumberOfCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int OrdersThisMonth { get; set; }
        public Dictionary<string, int>? CategoryDistribution { get; set; }
        public Dictionary<string, decimal>? MonthlyRevenue { get; set; }
        public Dictionary<string, int>? NewCustomersByMonth { get; set; }
    }
}