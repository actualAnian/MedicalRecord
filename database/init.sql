
USE master;
GO
-- cleanup
IF DB_ID('MedicalRegister') IS NOT NULL
BEGIN
    ALTER DATABASE MedicalRegister
    SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MedicalRegister;
END
GO

CREATE DATABASE MedicalRegister;
GO

IF EXISTS (
    SELECT 1
    FROM sys.server_principals
    WHERE name = '$(DB_USER)'
)
BEGIN
    ALTER LOGIN [$(DB_USER)]
    WITH PASSWORD = '$(DB_PASSWORD)';
END
ELSE
BEGIN
    CREATE LOGIN [$(DB_USER)]
    WITH PASSWORD = '$(DB_PASSWORD)';
END
GO

-- database
USE MedicalRegister;
GO

IF DATABASE_PRINCIPAL_ID('$(DB_USER)') IS NULL
BEGIN
    CREATE USER [$(DB_USER)]
    FOR LOGIN [$(DB_USER)];

    PRINT 'Database user created.';
END
ELSE
BEGIN
    PRINT 'Database user already exists.';
END
GO

ALTER ROLE db_datareader ADD MEMBER [$(DB_USER)];
ALTER ROLE db_datawriter ADD MEMBER [$(DB_USER)];
GRANT VIEW DEFINITION TO [$(DB_USER)];
GO

CREATE TABLE dbo.Patients (
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_Patients PRIMARY KEY,
    PESEL CHAR(11) NOT NULL UNIQUE
);
GO


CREATE TABLE dbo.PatientExternalIdentifiers
(
    SourceSystem     NVARCHAR(100) NOT NULL,
    SourceRecordId   NVARCHAR(100) NOT NULL,
    PatientId        UNIQUEIDENTIFIER    NOT NULL,

    CONSTRAINT PK_PatientExternalIdentifiers
        PRIMARY KEY (SourceSystem, SourceRecordId),

    CONSTRAINT FK_PatientExternalIdentifiers_Patients
        FOREIGN KEY (PatientId)
        REFERENCES dbo.Patients (Id)
);
GO

CREATE TABLE dbo.Diagnoses
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_Diagnoses PRIMARY KEY,
    SourceSystem        NVARCHAR(100) NOT NULL,
    SourceRecordId      NVARCHAR(100) NOT NULL,
    PatientId           UNIQUEIDENTIFIER NOT NULL,
    IcdSystem NVARCHAR(100) NOT NULL,
    IcdCode NVARCHAR(20) NOT NULL,
    DiagnosisDate       DATE NOT NULL,
    ClinicalStatus      TINYINT NOT NULL,
    ConfirmationStatus  TINYINT NOT NULL,
    SymptomStartType    NVARCHAR(20) NOT NULL,
    SymptomStartDate    DATE NULL,
    SymptomStartAge     INT NULL,

    CONSTRAINT FK_Diagnoses_Patients
        FOREIGN KEY (PatientId) REFERENCES dbo.Patients(Id),

    CONSTRAINT UQ_Diagnoses_Source
        UNIQUE (SourceSystem, SourceRecordId),

    CONSTRAINT CK_Diagnoses_SymptomStart CHECK (
        (SymptomStartType = 'FromDate'
            AND SymptomStartDate IS NOT NULL
            AND SymptomStartAge IS NULL)
        OR
        (SymptomStartType = 'PatientAge'
            AND SymptomStartDate IS NULL
            AND SymptomStartAge IS NOT NULL)
        )
);
GO