using Register.DTO;

namespace Register
{
    public class DiagnosisQueries
    {
        public IEnumerable<Diagnosis> GetDiagnosisRecords([Service] IDiagnosisRepository diagnosisRepository)
        {
            return new List<Diagnosis>
            {

            };
        }

    }
}