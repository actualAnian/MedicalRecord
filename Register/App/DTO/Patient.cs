using System;
using System.Collections.Generic;
using System.Text;

namespace Register.App.DTO
{
    public class Patient(Guid id, string pesel)
    {
        public Guid Id { get; } = id;
        public string PESEL { get; set; } = pesel;
    }
}
