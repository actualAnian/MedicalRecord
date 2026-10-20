$baseUrl = 'http://localhost:5000'

$query = @'
mutation Register($input: RegisterDiagnosisRequestInput!) {
  registerDiagnosis(input: $input) {
    success
    errors { code message }
  }
}
'@

$payload = @{
    query     = $query
    variables = @{
        input = @{
            sourceSystem       = 'HIS'
            sourceRecordId     = [guid]::NewGuid().ToString('N')
            patientPesel       = '44051401359'
            medicalRecordNumber = 'MR-123'
            icdCode            = 'A00'
            codingSystem       = 'http://hl7.org/fhir/sid/icd-10'
            diagnosisDate      = (Get-Date).AddDays(-1).ToString('yyyy-MM-dd')
            clinicalStatus     = 'Active'
            confirmationStatus = 'Confirmed'
            symptomStartDate   = (Get-Date).AddDays(-5).ToString('yyyy-MM-dd')
            symptomStartAge    = $null
            visitId            = 'VISIT-1'
        }
    }
} | ConvertTo-Json -Depth 10


try {
    $response = Invoke-WebRequest `
        -Uri "$baseUrl/graphql" `
        -Method Post `
        -ContentType 'application/json' `
        -Headers @{ Accept = 'application/json' } `
        -Body $payload `
        -UseBasicParsing

    "HTTP $([int]$response.StatusCode)"
    $response.Content
}
catch [System.Net.WebException] {
    $resp = $_.Exception.Response

    if ($null -ne $resp) {
        $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
        $content = $reader.ReadToEnd()
        $reader.Dispose()

        "HTTP $([int]$resp.StatusCode)"
        $content
    }
    else {
        # e.g. connection refused - no HTTP response
        $_
    }
}