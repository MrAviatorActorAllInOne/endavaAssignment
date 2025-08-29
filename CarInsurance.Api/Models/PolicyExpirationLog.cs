namespace CarInsurance.Api.Models
{
    public class PolicyExpirationLog
    {
        public long Id { get; set; }

        public long PolicyId { get; set; }
        public InsurancePolicy Policy { get; set; } = default!;

        public DateOnly EndDate { get; set; }
        public DateTimeOffset LoggedAt { get; set; }
    }
}
