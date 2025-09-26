namespace Donation_Website.Models
{
    public class LeaderboardService
    {
        private readonly DBConnection _db = new DBConnection();

        public List<LeaderboardItem> GetTopDonors()
        {
            var topDonors = new List<LeaderboardItem>();
            string query = @"
                SELECT TOP 10 D.DonorId, D.Name AS DonorName, SUM(DN.Amount) AS TotalDonation
                FROM Donation DN
                JOIN Donor D ON DN.DonorId = D.DonorId
                WHERE DN.Status = 'Completed'
                GROUP BY D.DonorId, D.Name
                ORDER BY SUM(DN.Amount) DESC;
            ";

            var cmd = _db.GetQuery(query);

            using (cmd.Connection)
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        topDonors.Add(new LeaderboardItem
                        {
                            DonorName = reader.GetString(1),
                            TotalDonation = reader.GetDecimal(2)
                        });
                    }
                }
            }

            return topDonors;
        }
    }
}
