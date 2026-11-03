using Register.App.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Register.App
{
    public interface ISpecification<T>
    {
        bool IsSatisfiedBy(T entity);
        public ISpecification<T> And(ISpecification<T> other);
        public ISpecification<T> Or(ISpecification<T> other);
    }

    public class Specification<T>(Func<T, bool> predicate) : ISpecification<T>
    {
        private readonly Func<T, bool> _predicate = predicate;

        public bool IsSatisfiedBy(T entity)
        {
            return _predicate(entity);
        }
        public ISpecification<T> And(ISpecification<T> other)
        {
            return new Specification<T>(entity => this.IsSatisfiedBy(entity) && other.IsSatisfiedBy(entity));
        }
        public ISpecification<T> Or(ISpecification<T> other)
        {
            return new Specification<T>(entity => this.IsSatisfiedBy(entity) || other.IsSatisfiedBy(entity));
        }
    }
    public static class DiagnosisSpecification 
    { 
        public static ISpecification<Diagnosis> HasSourceSystem() => new Specification<Diagnosis>(x => !string.IsNullOrWhiteSpace(x.SourceSystem)); 
        public static ISpecification<Diagnosis> HasSourceRecordId() => new Specification<Diagnosis>(x => !string.IsNullOrWhiteSpace(x.SourceRecordId)); 
        public static ISpecification<Diagnosis> HasPatient() => new Specification<Diagnosis>(x => x.PatientId != Guid.Empty); 
        public static ISpecification<Diagnosis> HasIcdCode() => new Specification<Diagnosis>(x => x.IcdCode is not null); 
        public static ISpecification<Diagnosis> HasSymptomStart() => new Specification<Diagnosis>(x => x.SymptomStart is not null);
        public static ISpecification<Diagnosis> DiagnosisDateNotInTheFuture() => new Specification<Diagnosis>(x => x.DiagnosisDate < DateOnly.FromDateTime(DateTime.Now));

    }
}
