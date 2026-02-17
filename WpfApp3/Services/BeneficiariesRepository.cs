using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;
using WpfApp3.Models;

namespace WpfApp3.Services
{
    public class BeneficiariesRepository
    {
        public void EnsureTable()
        {
            using var conn = MySqlDb.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS beneficiaries (
  id INT AUTO_INCREMENT PRIMARY KEY,
  source_person_id INT NULL,

  beneficiary_id VARCHAR(50) NOT NULL,
  civil_registry_id VARCHAR(50) NOT NULL,

  first_name VARCHAR(100) NOT NULL,
  middle_name VARCHAR(100) NULL,
  last_name VARCHAR(100) NOT NULL,

  gender VARCHAR(20) NULL,
  date_of_birth VARCHAR(50) NULL,
  classification VARCHAR(50) NULL,

  barangay VARCHAR(100) NULL,
  present_address VARCHAR(255) NULL,

  status ENUM('Not Validated','Endorsed','Pending','Rejected') NOT NULL DEFAULT 'Not Validated',

  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  UNIQUE KEY uq_beneficiary_id (beneficiary_id),
  KEY idx_status (status),
  KEY idx_source_person_id (source_person_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            cmd.ExecuteNonQuery();
        }

        public List<ValidatorRecord> GetByStatus(string status)
        {
            using var conn = MySqlDb.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
SELECT
  id,
  source_person_id,
  beneficiary_id,
  civil_registry_id,
  first_name,
  middle_name,
  last_name,
  gender,
  date_of_birth,
  classification,
  barangay,
  present_address,
  status
FROM beneficiaries
WHERE status = @status
ORDER BY updated_at DESC, id DESC;";
            cmd.Parameters.AddWithValue("@status", status);

            var list = new List<ValidatorRecord>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(Map(r));
            }
            return list;
        }

        // Used to "overlay" external list with saved DB info (by BeneficiaryId)
        public Dictionary<string, ValidatorRecord> GetByBeneficiaryIds(IEnumerable<string> beneficiaryIds)
        {
            var ids = beneficiaryIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (ids.Count == 0) return new Dictionary<string, ValidatorRecord>(StringComparer.OrdinalIgnoreCase);

            using var conn = MySqlDb.OpenConnection();
            using var cmd = conn.CreateCommand();

            // IN (@p0,@p1,...) safely
            var paramNames = new List<string>();
            for (int i = 0; i < ids.Count; i++)
            {
                var p = $"@p{i}";
                paramNames.Add(p);
                cmd.Parameters.AddWithValue(p, ids[i]);
            }

            cmd.CommandText = $@"
SELECT
  id,
  source_person_id,
  beneficiary_id,
  civil_registry_id,
  first_name,
  middle_name,
  last_name,
  gender,
  date_of_birth,
  classification,
  barangay,
  present_address,
  status
FROM beneficiaries
WHERE beneficiary_id IN ({string.Join(",", paramNames)});";

            var dict = new Dictionary<string, ValidatorRecord>(StringComparer.OrdinalIgnoreCase);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var rec = Map(r);
                if (!string.IsNullOrWhiteSpace(rec.BeneficiaryId))
                    dict[rec.BeneficiaryId] = rec;
            }

            return dict;
        }

        public void Upsert(ValidatorRecord person, string status)
        {
            if (person is null) throw new ArgumentNullException(nameof(person));
            if (string.IsNullOrWhiteSpace(person.BeneficiaryId)) throw new ArgumentException("BeneficiaryId is required.");
            if (string.IsNullOrWhiteSpace(person.CivilRegistryId)) throw new ArgumentException("CivilRegistryId is required.");
            if (string.IsNullOrWhiteSpace(person.FirstName)) throw new ArgumentException("FirstName is required.");
            if (string.IsNullOrWhiteSpace(person.LastName)) throw new ArgumentException("LastName is required.");

            using var conn = MySqlDb.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
INSERT INTO beneficiaries (
  source_person_id,
  beneficiary_id,
  civil_registry_id,
  first_name,
  middle_name,
  last_name,
  gender,
  date_of_birth,
  classification,
  barangay,
  present_address,
  status
)
VALUES (
  @source_person_id,
  @beneficiary_id,
  @civil_registry_id,
  @first_name,
  @middle_name,
  @last_name,
  @gender,
  @date_of_birth,
  @classification,
  @barangay,
  @present_address,
  @status
)
ON DUPLICATE KEY UPDATE
  source_person_id = VALUES(source_person_id),
  civil_registry_id = VALUES(civil_registry_id),
  first_name = VALUES(first_name),
  middle_name = VALUES(middle_name),
  last_name = VALUES(last_name),
  gender = VALUES(gender),
  date_of_birth = VALUES(date_of_birth),
  classification = VALUES(classification),
  barangay = VALUES(barangay),
  present_address = VALUES(present_address),
  status = VALUES(status);";

            cmd.Parameters.AddWithValue("@source_person_id", person.Id); // external id (from left list)
            cmd.Parameters.AddWithValue("@beneficiary_id", person.BeneficiaryId.Trim());
            cmd.Parameters.AddWithValue("@civil_registry_id", person.CivilRegistryId.Trim());
            cmd.Parameters.AddWithValue("@first_name", person.FirstName.Trim());
            cmd.Parameters.AddWithValue("@middle_name", (person.MiddleName ?? "").Trim());
            cmd.Parameters.AddWithValue("@last_name", person.LastName.Trim());
            cmd.Parameters.AddWithValue("@gender", (person.Gender ?? "").Trim());
            cmd.Parameters.AddWithValue("@date_of_birth", (person.DateOfBirth ?? "").Trim());
            cmd.Parameters.AddWithValue("@classification", (person.Classification ?? "").Trim());
            cmd.Parameters.AddWithValue("@barangay", (person.Barangay ?? "").Trim());
            cmd.Parameters.AddWithValue("@present_address", (person.PresentAddress ?? "").Trim());
            cmd.Parameters.AddWithValue("@status", status);

            cmd.ExecuteNonQuery();
        }

        private static ValidatorRecord Map(MySqlDataReader r)
        {
            // UI 'Id' column: prefer source_person_id; fallback to internal id
            var internalId = Convert.ToInt32(r["id"]);
            var sourceIdObj = r["source_person_id"];
            var sourceId = sourceIdObj == DBNull.Value ? (int?)null : Convert.ToInt32(sourceIdObj);

            return new ValidatorRecord
            {
                Id = sourceId ?? internalId,
                BeneficiaryId = Convert.ToString(r["beneficiary_id"]) ?? "",
                CivilRegistryId = Convert.ToString(r["civil_registry_id"]) ?? "",
                FirstName = Convert.ToString(r["first_name"]) ?? "",
                MiddleName = Convert.ToString(r["middle_name"]) ?? "",
                LastName = Convert.ToString(r["last_name"]) ?? "",
                Gender = Convert.ToString(r["gender"]) ?? "",
                DateOfBirth = Convert.ToString(r["date_of_birth"]) ?? "",
                Classification = Convert.ToString(r["classification"]) ?? "",
                Barangay = Convert.ToString(r["barangay"]) ?? "",
                PresentAddress = Convert.ToString(r["present_address"]) ?? "",
                Status = Convert.ToString(r["status"]) ?? ""
            };
        }
    }
}
