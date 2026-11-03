namespace Register.DataBase.DTO
{
    public class PatientDb
    {
        public Guid Id { get; set; }
        public string PESEL { get; set; } = string.Empty;
    }
}
