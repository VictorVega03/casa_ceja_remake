using CasaCejaRemake.Models;

namespace CasaCejaRemake.Helpers
{
    public static class CreditUIExtensions
    {
        public static string GetStatusColor(this Credit credit)
        {
            return credit.Status switch
            {
                1 => "#FF9800", // Pending - Orange
                2 => "#4CAF50", // Paid - Green
                3 => "#F44336", // Overdue - Red
                4 => "#9E9E9E", // Cancelled - Gray
                _ => "#757575"
            };
        }

        public static bool CanAddPayment(this Credit credit)
        {
            return credit.Status == 1 || credit.Status == 3; // Pending or Overdue
        }
    }

    public static class LayawayUIExtensions
    {
        public static string GetStatusColor(this Layaway layaway)
        {
            return layaway.Status switch
            {
                1 => "#FF9800", // Pending - Orange
                2 => "#4CAF50", // Delivered - Green
                3 => "#F44336", // Expired - Red
                4 => "#9E9E9E", // Cancelled - Gray
                _ => "#757575"
            };
        }

        public static bool CanAddPayment(this Layaway layaway)
        {
            return layaway.Status == 1 || layaway.Status == 3; // Pending or Expired
        }

        public static string GetReadyForDeliveryDisplay(this Layaway layaway)
        {
            return layaway.CanDeliver ? "SI" : "-";
        }

        public static string GetReadyForDeliveryColor(this Layaway layaway)
        {
            return layaway.CanDeliver ? "#4CAF50" : "#757575";
        }
    }
}
