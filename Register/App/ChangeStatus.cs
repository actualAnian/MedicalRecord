using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Register.App
{
    internal class ChangeStatusDecider
    {
        public static Outcome<DiagnosisEvent> Decide(
            DiagnosisCommand command, DiagnosisState state, DateTime today)
        {
            switch(command, state)
            {
                case (DiagnosisCommand.SetAsHealed c, DiagnosisState.Active):
                    return Outcome<DiagnosisEvent>.Ok(new DiagnosisEvent.Healed(c.patientId));
                case (DiagnosisCommand.SetAsHealed, DiagnosisState.Healed):
                    return Outcome<DiagnosisEvent>.No(new Refusal("Diagnosis is already healed."));
                default:
                    return Outcome<DiagnosisEvent>.No(new Refusal("Invalid command for current diagnosis state."));
            }
        }
        public static DiagnosisState Evolve(DiagnosisEvent @event, DiagnosisState state)
        {
            switch (@event, state)
            {
                case (DiagnosisEvent.Healed, DiagnosisState.Active):
                    return new DiagnosisState.Healed();
                default:
                    throw new InvalidOperationException("Invalid event for current diagnosis state.");
            }
        }
    }
    public readonly record struct Outcome<T>(T? Fact, Refusal? Refusal)
    {
        public static Outcome<DiagnosisEvent> Ok(DiagnosisEvent fact) => new(fact, null);
        public static Outcome<DiagnosisEvent> No(Refusal refusal) => new(null, refusal);

    }
    public abstract record DiagnosisEvent 
    {
        public sealed record Healed(Guid PatientId) : DiagnosisEvent;
    }
    public abstract record DiagnosisCommand 
    {
        public sealed record SetAsHealed(Guid patientId) : DiagnosisCommand;
    }
    public abstract record DiagnosisState
    {
        public sealed record Active : DiagnosisState;
        public sealed record Healed : DiagnosisState;
    }
    public record class Refusal(string message) { public string Message { get; set; } = message; }
}