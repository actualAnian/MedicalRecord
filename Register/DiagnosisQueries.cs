using Register.DTO;
using Register.Repositories;

namespace Register
{
    public class DiagnosisQueries
    {
        public IEnumerable<DTO.Diagnosis> GetDiagnosisRecords([Service] IDiagnosisRepository diagnosisRepository)
        {
            return new List<Diagnosis>
            {

            };
        }

    }
}