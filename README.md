# Medical Register

Medical Register is a small ASP.NET Core service that demonstrates the same medical-diagnosis workflow through both a REST minimal API and a HotChocolate GraphQL endpoint.
It registers diagnoses for patients identified by PESEL or an external source identity, validates ICD-10 codes, stores diagnoses in SQL Server,
forwards new diagnoses to a mock external registry with retry handling, changes a diagnosis status through a small decider-based workflow, and reports how many diagnoses use a given ICD-10 code.

## Stack

- ASP.NET Core minimal API
- HotChocolate GraphQL
- SQL Server
- Dapper
- Docker Compose

## Prerequisites

- .NET 10 SDK
- Docker

## Configuration

Create a `.env` file in the repository root for the database container:

```env
MSSQL_SA_PASSWORD=YourStrong!Password
DB_USER=registerUser
DB_PASSWORD=UserPassword!
```

Copy the example application settings:

```bash
cp Register/appsettings.example.json Register/appsettings.json
```

Then fill in the connection string, or provide it through an environment variable:

```bash
ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=MedicalRegister;User Id=registerUser;Password=UserPassword!;TrustServerCertificate=True;"
```

## Start the database

From the repository root:

```bash
docker compose up -d
```

The `database-init` container creates the `MedicalRegister` database, application user, and tables.

After the database is running, you can load the seed data with:

```bash
docker exec -it medical-register-db sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<rootPassword>" -C -d MedicalRegister -i /scripts/seedData.sql -b'
```

## Run the application

From the repository root:

```bash
dotnet run --project Register/Register.csproj
```

The examples below assume the application is listening on `http://localhost:5000`.

```bash
export BASE_URL=http://localhost:5000
```

Examples use `bash` and `curl`. Equivalent PowerShell examples are available in `minimalApiExample.ps1` and `graphQLExample.ps1`.

## Running tests

Unit tests do not require a database:

```bash
dotnet test Register.UnitTests/Register.UnitTests.csproj
```

Integration tests require the Docker database to be running:

```bash
docker compose up -d
dotnet test Register.IntegrationTests/Register.IntegrationTests.csproj
```

The integration tests reset the `Diagnoses`, `PatientExternalIdentifiers`, and `Patients` tables before each test and then insert their own test data. They use `ConnectionStrings__DefaultConnection` or `MEDICAL_REGISTER_TEST_CONNECTION` when set, otherwise they fall back to the local Docker connection string.

## REST API

| Method | Path | Description |
| --- | --- | --- |
| `POST` | `/diagnoses` | Register a diagnosis. |
| `GET` | `/patients` | Find a patient by PESEL or external identity. |
| `GET` | `/patients/{patientId}/diagnoses` | List a patient's diagnoses by clinical status. |
| `POST` | `/diagnoses/status` | Change a diagnosis status. |
| `GET` | `/diagnoses/count` | Count diagnoses by ICD system and code. |

### Register a diagnosis

```bash
curl -X POST "$BASE_URL/diagnoses" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceSystem": "HIS",
    "sourceRecordId": "D-101",
    "patientPesel": "44051401359",
    "medicalRecordNumber": null,
    "icdCode": "A00",
    "codingSystem": "http://hl7.org/fhir/sid/icd-10",
    "diagnosisDate": "2026-11-04",
    "clinicalStatus": "Active",
    "confirmationStatus": "Confirmed",
    "symptomStartDate": "2026-10-31",
    "symptomStartAge": null,
    "visitId": "VISIT-1"
  }'
```
### Find a patient

By PESEL:

```bash
curl -G "$BASE_URL/patients" \
  --data-urlencode "pesel=44051401359"
```

By external identity:

```bash
curl -G "$BASE_URL/patients" \
  --data-urlencode "sourceSystem=HIS" \
  --data-urlencode "sourceRecordId=PATIENT-1"
```

### List a patient's diagnoses

```bash
curl -G "$BASE_URL/patients/11111111-1111-1111-1111-111111111111/diagnoses" \
  --data-urlencode "clinicalStatuses=Active"
```

### Change a diagnosis status

```bash
curl -X POST "$BASE_URL/diagnoses/status" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceSystem": "HIS",
    "sourceDiagnosisRecordId": "D-101",
    "clinicalStatus": "Healed"
  }'
```
Only `Active` -> `Healed` transition is accepted by the status decider.

### Count diagnoses by ICD code

```bash
curl -G "$BASE_URL/diagnoses/count" \
  --data-urlencode "icdSystem=http://hl7.org/fhir/sid/icd-10" \
  --data-urlencode "icdCode=A00"
```

Example response after the seed data and both example registrations:

```json
4
```

## GraphQL API

GraphQL requests are sent to:

```text
POST /graphql
```

| Type | Field | Description |
| --- | --- | --- |
| Mutation | `registerDiagnosis` | Register a diagnosis. |
| Mutation | `changeDiagnosisStatus` | Change a diagnosis status. |
| Query | `amountOfDiagnosisWithICDCode` | Count diagnoses by ICD system and code. |

### Register a diagnosis

```bash
curl -X POST "$BASE_URL/graphql" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "mutation Register($input: RegisterDiagnosisRequestInput!) { registerDiagnosis(input: $input) { success errors { code message } } }",
    "variables": {
      "input": {
        "sourceSystem": "HIS",
        "sourceRecordId": "D-102",
        "patientPesel": "44051401359",
        "medicalRecordNumber": null,
        "icdCode": "A00",
        "codingSystem": "http://hl7.org/fhir/sid/icd-10",
        "diagnosisDate": "2026-11-04",
        "clinicalStatus": "Active",
        "confirmationStatus": "Confirmed",
        "symptomStartDate": "2026-10-31",
        "symptomStartAge": null,
        "visitId": "VISIT-1"
      }
    }
  }'
```

### Change a diagnosis status

```bash
curl -X POST "$BASE_URL/graphql" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "mutation ChangeStatus($input: ChangeDiagnosisStatusRequestInput!) { changeDiagnosisStatus(input: $input) { success errors { code message } } }",
    "variables": {
      "input": {
        "sourceSystem": "HIS",
        "sourceDiagnosisRecordId": "D-102",
        "clinicalStatus": "Healed"
      }
    }
  }'
```

### Count diagnoses by ICD code

```bash
curl -X POST "$BASE_URL/graphql" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "query Count($input: GetDiagnosisCountRequestInput!) { amountOfDiagnosisWithICDCode(input: $input) { success amount errors { code message } } }",
    "variables": {
      "input": {
        "icdSystem": "http://hl7.org/fhir/sid/icd-10",
        "icdCode": "A00"
      }
    }
  }'
```

## External registry mock

New diagnoses are forwarded to the mock external registry at `POST /externalRequest`. The mock intentionally returns `503 Service Unavailable` for the first two attempts and accepts the third attempt. The client retries automatically.

## Database access

Connect to the SQL Server container:

```bash
docker exec -it medical-register-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C
```

Inspect the schema:

```sql
USE MedicalRegister;
GO

SELECT column_name
FROM information_schema.columns
WHERE table_name = 'Patients';
GO
```
