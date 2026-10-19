using System;
using System.Collections.Generic;
using System.Text;

namespace Register.DTO
{
    public class Patient(Guid id)
    {
        public Guid Id { get; } = id;
    }
}
