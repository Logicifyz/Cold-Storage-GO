namespace Cold_Storage_GO.Models
{
    public class SubscriptionDto
    {
        public Guid SubscriptionId { get; set; }
        public Guid UserId { get; set; }
        public string SubscriptionType { get; set; }
        public string SubscriptionChoice { get; set; }
        public string Status { get; set; }
        public bool? AutoRenewal { get; set; }
        public UserDto User { get; set; }
    }

    public class UserDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public UserProfileDto UserProfile { get; set; }
    }

    public class UserProfileDto
    {
        public string StreetAddress { get; set; }
        public string PostalCode { get; set; }
    }
}
