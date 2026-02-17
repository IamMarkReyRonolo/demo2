using MySqlConnector;
using System.Data;
using WpfApp3.Models;

namespace WpfApp3.Services
{
    public class AllotmentBeneficiariesRepository
    {
        // Assigned + endorsed only
        public List<BeneficiaryRecord> GetAssignedEndorsed(int allotmentId)
        {
            using var conn = MySqlDb.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
SELECT
    b.id,
    b.first_name,
    b.last_name,
    b.gender,
    b.barangay,
    ab.share_amount,
    ab.share_qty,
    ab.share_unit
FROM allotment_beneficiaries ab
JOIN beneficiaries b ON b.id = ab.beneficiary_id
WHERE ab.allotment_id = @aid
  AND b.status = 'Endorsed'
ORDER BY b.last_name, b.first_name;";

            cmd.Parameters.AddWithValue("@aid", allotmentId);

            using var rd = cmd.ExecuteReader();
            var list = new List<BeneficiaryRecord>();

            while (rd.Read())
            {
                list.Add(new BeneficiaryRecord
                {
                    Id = rd.GetInt32("id"),
                    FirstName = rd.GetString("first_name"),
                    LastName = rd.GetString("last_name"),
                    Gender = rd.GetString("gender"),
                    Barangay = rd.GetString("barangay"),
                    ShareAmount = rd.IsDBNull("share_amount") ? null : rd.GetDecimal("share_amount"),
                    ShareQty = rd.IsDBNull("share_qty") ? null : rd.GetInt32("share_qty"),
                    ShareUnit = rd.IsDBNull("share_unit") ? null : rd.GetString("share_unit"),
                });
            }

            return list;
        }

        // For Add modal: endorsed NOT assigned to this project (+ optional search)
        public List<BeneficiaryRecord> GetAvailableEndorsedNotAssigned(int allotmentId, string searchLower)
        {
            using var conn = MySqlDb.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
SELECT
    b.id,
    b.first_name,
    b.last_name,
    b.gender,
    b.barangay
FROM beneficiaries b
LEFT JOIN allotment_beneficiaries ab
    ON ab.beneficiary_id = b.id
   AND ab.allotment_id = @aid
WHERE b.status = 'Endorsed'
  AND ab.id IS NULL
  AND (
        @q = '' OR
        CAST(b.id AS CHAR) LIKE CONCAT('%', @q, '%') OR
        LOWER(b.first_name) LIKE CONCAT('%', @q, '%') OR
        LOWER(b.last_name) LIKE CONCAT('%', @q, '%') OR
        LOWER(b.gender) LIKE CONCAT('%', @q, '%') OR
        LOWER(b.barangay) LIKE CONCAT('%', @q, '%')
      )
ORDER BY b.last_name, b.first_name;";

            cmd.Parameters.AddWithValue("@aid", allotmentId);
            cmd.Parameters.AddWithValue("@q", (searchLower ?? "").Trim().ToLowerInvariant());

            using var rd = cmd.ExecuteReader();
            var list = new List<BeneficiaryRecord>();

            while (rd.Read())
            {
                list.Add(new BeneficiaryRecord
                {
                    Id = rd.GetInt32("id"),
                    FirstName = rd.GetString("first_name"),
                    LastName = rd.GetString("last_name"),
                    Gender = rd.GetString("gender"),
                    Barangay = rd.GetString("barangay"),
                });
            }

            return list;
        }

        public void AddAssignments(int allotmentId, List<int> beneficiaryIds)
        {
            using var conn = MySqlDb.OpenConnection();
            using var tx = conn.BeginTransaction();

            foreach (var bid in beneficiaryIds)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;

                cmd.CommandText = @"
INSERT INTO allotment_beneficiaries (allotment_id, beneficiary_id)
VALUES (@aid, @bid)
ON DUPLICATE KEY UPDATE updated_at = CURRENT_TIMESTAMP;";

                cmd.Parameters.AddWithValue("@aid", allotmentId);
                cmd.Parameters.AddWithValue("@bid", bid);

                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }

        public void UpdateShareMoney(int allotmentId, int beneficiaryId, decimal amount)
        {
            using var conn = MySqlDb.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
UPDATE allotment_beneficiaries
SET share_amount = @amt,
    share_qty = NULL,
    share_unit = NULL
WHERE allotment_id = @aid AND beneficiary_id = @bid;";

            cmd.Parameters.AddWithValue("@amt", amount);
            cmd.Parameters.AddWithValue("@aid", allotmentId);
            cmd.Parameters.AddWithValue("@bid", beneficiaryId);

            cmd.ExecuteNonQuery();
        }

        public void UpdateShareInKind(int allotmentId, int beneficiaryId, int qty, string unit)
        {
            using var conn = MySqlDb.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
UPDATE allotment_beneficiaries
SET share_amount = NULL,
    share_qty = @qty,
    share_unit = @unit
WHERE allotment_id = @aid AND beneficiary_id = @bid;";

            cmd.Parameters.AddWithValue("@qty", qty);
            cmd.Parameters.AddWithValue("@unit", unit);
            cmd.Parameters.AddWithValue("@aid", allotmentId);
            cmd.Parameters.AddWithValue("@bid", beneficiaryId);

            cmd.ExecuteNonQuery();
        }

        public void RemoveAssignment(int allotmentId, int beneficiaryId)
        {
            using var conn = MySqlDb.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
DELETE FROM allotment_beneficiaries
WHERE allotment_id = @aid AND beneficiary_id = @bid;";

            cmd.Parameters.AddWithValue("@aid", allotmentId);
            cmd.Parameters.AddWithValue("@bid", beneficiaryId);

            cmd.ExecuteNonQuery();
        }
    }
}
