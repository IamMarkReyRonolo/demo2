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
    IFNULL(b.classification,'None') AS classification,
    ab.share_amount,
    ab.share_qty,
    ab.share_unit,
    ab.is_released
FROM allotment_beneficiaries ab
JOIN beneficiaries b ON b.id = ab.beneficiary_id
WHERE ab.allotment_id = @aid
  AND b.status = 'Endorsed'
ORDER BY b.last_name, b.first_name;";

            cmd.Parameters.AddWithValue("@aid", allotmentId);

            using var rd = cmd.ExecuteReader();
            var list = new List<BeneficiaryRecord>();

            var oId = rd.GetOrdinal("id");
            var oFirst = rd.GetOrdinal("first_name");
            var oLast = rd.GetOrdinal("last_name");
            var oGender = rd.GetOrdinal("gender");
            var oBarangay = rd.GetOrdinal("barangay");
            var oClass = rd.GetOrdinal("classification");
            var oShareAmt = rd.GetOrdinal("share_amount");
            var oShareQty = rd.GetOrdinal("share_qty");
            var oShareUnit = rd.GetOrdinal("share_unit");
            var oReleased = rd.GetOrdinal("is_released");

            while (rd.Read())
            {
                list.Add(new BeneficiaryRecord
                {
                    Id = rd.IsDBNull(oId) ? 0 : rd.GetInt32(oId),
                    FirstName = rd.IsDBNull(oFirst) ? "" : rd.GetString(oFirst),
                    LastName = rd.IsDBNull(oLast) ? "" : rd.GetString(oLast),
                    Gender = rd.IsDBNull(oGender) ? "" : rd.GetString(oGender),
                    Barangay = rd.IsDBNull(oBarangay) ? "" : rd.GetString(oBarangay),
                    Classification = rd.IsDBNull(oClass) ? "None" : rd.GetString(oClass),

                    ShareAmount = rd.IsDBNull(oShareAmt) ? (decimal?)null : rd.GetDecimal(oShareAmt),
                    ShareQty = rd.IsDBNull(oShareQty) ? (int?)null : rd.GetInt32(oShareQty),
                    ShareUnit = rd.IsDBNull(oShareUnit) ? null : rd.GetString(oShareUnit),

                    // ✅ new
                    IsReleased = !rd.IsDBNull(oReleased) && Convert.ToInt32(rd.GetValue(oReleased)) == 1
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
    b.barangay,
    IFNULL(b.classification,'None') AS classification
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
        LOWER(b.barangay) LIKE CONCAT('%', @q, '%') OR
        LOWER(IFNULL(b.classification,'')) LIKE CONCAT('%', @q, '%')
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
                    Classification = rd.GetString("classification"),
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

            // ✅ IMPORTANT: recompute shares after insert
            using (var recompute = conn.CreateCommand())
            {
                recompute.Transaction = tx;
                recompute.CommandText = "CALL sp_recompute_allotment_shares(@aid);";
                recompute.Parameters.AddWithValue("@aid", allotmentId);
                recompute.ExecuteNonQuery();
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

        public void MarkReleased(int allotmentId, int beneficiaryId)
        {
            using var conn = MySqlDb.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
UPDATE allotment_beneficiaries
SET is_released = 1
WHERE allotment_id = @aid AND beneficiary_id = @bid;";

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
            using var tx = conn.BeginTransaction();

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
DELETE FROM allotment_beneficiaries
WHERE allotment_id = @aid AND beneficiary_id = @bid;";
                cmd.Parameters.AddWithValue("@aid", allotmentId);
                cmd.Parameters.AddWithValue("@bid", beneficiaryId);
                cmd.ExecuteNonQuery();
            }

            // ✅ recompute shares after delete
            using (var recompute = conn.CreateCommand())
            {
                recompute.Transaction = tx;
                recompute.CommandText = "CALL sp_recompute_allotment_shares(@aid);";
                recompute.Parameters.AddWithValue("@aid", allotmentId);
                recompute.ExecuteNonQuery();
            }

            tx.Commit();
        }


        public static void AssignAndRecompute(int allotmentId, IEnumerable<int> beneficiaryIds)
        {
            using var conn = MySqlDb.OpenConnection();
            using var tx = conn.BeginTransaction();

            const string insertSql = @"
INSERT IGNORE INTO allotment_beneficiaries (allotment_id, beneficiary_id)
VALUES (@a, @b);";

            using (var cmd = new MySqlCommand(insertSql, conn, tx))
            {
                cmd.Parameters.Add("@a", MySqlDbType.Int32).Value = allotmentId;
                var pB = cmd.Parameters.Add("@b", MySqlDbType.Int32);

                foreach (var bid in beneficiaryIds)
                {
                    pB.Value = bid;
                    cmd.ExecuteNonQuery();
                }
            }

            using (var recompute = new MySqlCommand("CALL sp_recompute_allotment_shares(@a);", conn, tx))
            {
                recompute.Parameters.AddWithValue("@a", allotmentId);
                recompute.ExecuteNonQuery();
            }

            tx.Commit();
        }

        public static void RemoveAndRecompute(int allotmentId, int beneficiaryId)
        {
            using var conn = MySqlDb.OpenConnection();
            using var tx = conn.BeginTransaction();

            using (var del = new MySqlCommand(
                "DELETE FROM allotment_beneficiaries WHERE allotment_id=@a AND beneficiary_id=@b;",
                conn, tx))
            {
                del.Parameters.AddWithValue("@a", allotmentId);
                del.Parameters.AddWithValue("@b", beneficiaryId);
                del.ExecuteNonQuery();
            }

            using (var recompute = new MySqlCommand("CALL sp_recompute_allotment_shares(@a);", conn, tx))
            {
                recompute.Parameters.AddWithValue("@a", allotmentId);
                recompute.ExecuteNonQuery();
            }

            tx.Commit();
        }
    }
}
