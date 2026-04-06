param(
    [Parameter()]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory)]
    [string]$FrontendBaseUrl
)

if ($ApiBaseUrl) {
    Describe 'Backend API' {
        BeforeAll {
            # Phase 1: Wait for /health endpoint (basic ASP.NET health check).
            # ACA cold-start from zero replicas + image pull can take 2+ minutes.
            $healthRetries = 15
            $retryDelay = 10
            $healthy = $false

            for ($i = 1; $i -le $healthRetries; $i++) {
                $timestamp = Get-Date -Format 'HH:mm:ss'
                try {
                    $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -UseBasicParsing -TimeoutSec 10
                    if ($response.StatusCode -eq 200) {
                        $healthy = $true
                        Write-Host "[$timestamp] Phase 1: /health is ready (attempt $i/$healthRetries)"
                        break
                    }
                    else {
                        Write-Host "[$timestamp] Phase 1 attempt $i/${healthRetries}: /health returned status $($response.StatusCode), retrying in ${retryDelay}s..."
                        Start-Sleep -Seconds $retryDelay
                    }
                }
                catch {
                    $errorDetail = $_.Exception.Message
                    if ($_.Exception.Response) {
                        $errorDetail = "HTTP $([int]$_.Exception.Response.StatusCode) - $($_.Exception.Message)"
                    }
                    Write-Host "[$timestamp] Phase 1 attempt $i/${healthRetries}: /health not ready ($errorDetail), retrying in ${retryDelay}s..."
                    Start-Sleep -Seconds $retryDelay
                }
            }

            if (-not $healthy) {
                throw "Backend /health did not become healthy after $healthRetries attempts. URL: $ApiBaseUrl/health"
            }

            # Phase 2: Wait for /api/status to report overall: Healthy (deep dependency check).
            $statusRetries = 20
            $statusDelay = 10
            $statusHealthy = $false

            for ($i = 1; $i -le $statusRetries; $i++) {
                $timestamp = Get-Date -Format 'HH:mm:ss'
                try {
                    # Use -SkipHttpErrorCheck so we can read 503 response body for diagnostics
                    $response = Invoke-WebRequest -Uri "$ApiBaseUrl/api/status" -UseBasicParsing -TimeoutSec 30 -SkipHttpErrorCheck
                    $status = $response.Content | ConvertFrom-Json

                    $miStatus = $status.managedIdentity.status
                    $cosmosStatus = $status.cosmosDb.status
                    $searchStatus = $status.aiSearch.status
                    $embeddingsStatus = $status.embeddings.status
                    $overall = $status.overall

                    Write-Host "[$timestamp] Phase 2 attempt $i/${statusRetries} - HTTP $($response.StatusCode): overall=$overall, managedIdentity=$miStatus, cosmosDb=$cosmosStatus, aiSearch=$searchStatus, embeddings=$embeddingsStatus"

                    if ($overall -eq 'Healthy') {
                        $statusHealthy = $true
                        break
                    }

                    # Log per-dependency errors for CI diagnostics
                    if ($status.managedIdentity.error) { Write-Host "  managedIdentity error: $($status.managedIdentity.error)" }
                    if ($status.cosmosDb.error) { Write-Host "  cosmosDb error: $($status.cosmosDb.error)" }
                    if ($status.aiSearch.error) { Write-Host "  aiSearch error: $($status.aiSearch.error)" }

                    Start-Sleep -Seconds $statusDelay
                }
                catch {
                    Write-Host "[$timestamp] Phase 2 attempt $i/${statusRetries}: /api/status request failed ($($_.Exception.Message)), retrying in ${statusDelay}s..."
                    Start-Sleep -Seconds $statusDelay
                }
            }

            if (-not $statusHealthy) {
                Write-Host "WARNING: /api/status did not report Healthy after $statusRetries attempts. Individual tests may fail with more specific errors."
            }
        }

        It 'Health endpoint returns Healthy' {
            $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -UseBasicParsing -TimeoutSec 10
            $response.StatusCode | Should -Be 200
            $response.Content | Should -BeLike '*Healthy*'
        }

        It 'Liveness endpoint returns 200' {
            $response = Invoke-WebRequest -Uri "$ApiBaseUrl/alive" -UseBasicParsing -TimeoutSec 10
            $response.StatusCode | Should -Be 200
        }

        It 'Status endpoint reports all dependencies healthy' {
            $response = Invoke-WebRequest -Uri "$ApiBaseUrl/api/status" -UseBasicParsing -TimeoutSec 30
            $response.StatusCode | Should -Be 200

            $status = $response.Content | ConvertFrom-Json
            $status.overall | Should -Be 'Healthy'
            $status.managedIdentity.status | Should -Be 'Healthy'
            $status.cosmosDb.status | Should -Be 'Healthy'
        }
    }
}

Describe 'Frontend SPA' {
    It 'Returns 200 and contains root div' {
        $response = Invoke-WebRequest -Uri $FrontendBaseUrl -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200
        $response.Content | Should -Match '<div id="root"'
    }

    It 'Serves JavaScript assets' {
        $html = (Invoke-WebRequest -Uri $FrontendBaseUrl -UseBasicParsing -TimeoutSec 10).Content
        $assetMatch = [regex]::Match($html, 'src="(/assets/[^"]+\.js)"')

        if (-not $assetMatch.Success) {
            Set-ItResult -Inconclusive -Because 'No JS asset reference found in HTML'
            return
        }

        $assetUrl = "$FrontendBaseUrl$($assetMatch.Groups[1].Value)"
        $response = Invoke-WebRequest -Uri $assetUrl -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200
    }
}
