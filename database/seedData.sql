USE MedicalRegister;
GO

DECLARE @Patient1Id UNIQUEIDENTIFIER;
DECLARE @Patient2Id UNIQUEIDENTIFIER;

SELECT @Patient1Id = Id
FROM dbo.Patients
WHERE PESEL = '44051401359';

IF @Patient1Id IS NULL
BEGIN
    SET @Patient1Id = '11111111-1111-1111-1111-111111111111';

    INSERT INTO dbo.Patients (Id, PESEL)
    VALUES (@Patient1Id, '44051401359');
END

SELECT @Patient2Id = Id
FROM dbo.Patients
WHERE PESEL = '02070803628';

IF @Patient2Id IS NULL
BEGIN
    SET @Patient2Id = '22222222-2222-2222-2222-222222222222';

    INSERT INTO dbo.Patients (Id, PESEL)
    VALUES (@Patient2Id, '02070803628');
END

IF NOT EXISTS (
    SELECT 1
    FROM dbo.PatientExternalIdentifiers
    WHERE SourceSystem = 'HIS'
      AND SourceRecordId = 'PATIENT-1'
)
BEGIN
    INSERT INTO dbo.PatientExternalIdentifiers (SourceSystem, SourceRecordId, PatientId)
    VALUES ('HIS', 'PATIENT-1', @Patient1Id);
END

IF NOT EXISTS (
    SELECT 1
    FROM dbo.PatientExternalIdentifiers
    WHERE SourceSystem = 'HIS'
      AND SourceRecordId = 'PATIENT-2'
)
BEGIN
    INSERT INTO dbo.PatientExternalIdentifiers (SourceSystem, SourceRecordId, PatientId)
    VALUES ('HIS', 'PATIENT-2', @Patient2Id);
END

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Diagnoses
    WHERE SourceSystem = 'HIS'
      AND SourceRecordId = 'D-001'
)
BEGIN
    INSERT INTO dbo.Diagnoses
    (
        Id,
        SourceSystem,
        SourceRecordId,
        PatientId,
        IcdSystem,
        IcdCode,
        DiagnosisDate,
        ClinicalStatus,
        ConfirmationStatus,
        SymptomStartType,
        SymptomStartDate,
        SymptomStartAge
    )
    VALUES
    (
        '33333333-3333-3333-3333-333333333333',
        'HIS',
        'D-001',
        @Patient1Id,
        'http://hl7.org/fhir/sid/icd-10',
        'A00',
        '2026-11-04',
        0,
        1,
        'FromDate',
        '2026-10-31',
        NULL
    );
END

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Diagnoses
    WHERE SourceSystem = 'HIS'
      AND SourceRecordId = 'D-002'
)
BEGIN
    INSERT INTO dbo.Diagnoses
    (
        Id,
        SourceSystem,
        SourceRecordId,
        PatientId,
        IcdSystem,
        IcdCode,
        DiagnosisDate,
        ClinicalStatus,
        ConfirmationStatus,
        SymptomStartType,
        SymptomStartDate,
        SymptomStartAge
    )
    VALUES
    (
        '44444444-4444-4444-4444-444444444444',
        'HIS',
        'D-002',
        @Patient2Id,
        'http://hl7.org/fhir/sid/icd-10',
        'A00',
        '2026-11-03',
        0,
        1,
        'PatientAge',
        NULL,
        30
    );
END

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Diagnoses
    WHERE SourceSystem = 'HIS'
      AND SourceRecordId = 'D-003'
)
BEGIN
    INSERT INTO dbo.Diagnoses
    (
        Id,
        SourceSystem,
        SourceRecordId,
        PatientId,
        IcdSystem,
        IcdCode,
        DiagnosisDate,
        ClinicalStatus,
        ConfirmationStatus,
        SymptomStartType,
        SymptomStartDate,
        SymptomStartAge
    )
    VALUES
    (
        '55555555-5555-5555-5555-555555555555',
        'HIS',
        'D-003',
        @Patient1Id,
        'http://hl7.org/fhir/sid/icd-10',
        'A00.1',
        '2026-10-25',
        1,
        0,
        'FromDate',
        '2026-10-15',
        NULL
    );
END
GO
