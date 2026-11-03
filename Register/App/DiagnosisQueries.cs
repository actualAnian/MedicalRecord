using Register.App.DTO;
using Register.DataBase.Repositories;

namespace Register.App
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