#Requires -Modules @{ ModuleName='Pester'; ModuleVersion='5.0.0' }

<#
.SYNOPSIS
    Integration tests for World Management and Entity Management APIs.

.DESCRIPTION
    End-to-end integration tests that verify the World and Entity Management APIs
    work correctly against a running Aspire AppHost instance.
    
    These tests use HTTP requests to validate API endpoints. The Aspire AppHost
    automatically manages all dependencies (Cosmos DB Emulator, API service, etc.).

.NOTES
    Prerequisites:
    1. Start the Aspire AppHost:
       dotnet run --project src/Orchestration/AppHost/LibrisMaleficarum.AppHost.csproj
    
    2. Wait for services to be ready (30-60 seconds for Cosmos DB SSL stabilization)
       - Verify in Aspire Dashboard that all services show "Running" status
    
    3. Run tests:
       Invoke-Pester tests/Api.Tests/Integration/WorldAndEntityApiIntegration.Tests.ps1 -Output Detailed
    
    See tests/Api.Tests/Integration/README.md for detailed instructions.
#>

BeforeAll {
    $script:BaseUrl = 'https://localhost:7201/api/v1'
    $script:WorldId = $null
    $script:ContinentId = $null
    $script:CountryId = $null
    
    # Wait for Cosmos DB Emulator SSL to stabilize
    Write-Host "Waiting 10 seconds for Cosmos DB Emulator SSL to stabilize..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
}

Describe 'World Management API Integration Tests' -Tag 'Integration', 'API', 'World' {
    
    Context 'When retrieving worlds for a new user' {
        It 'Should return an empty list' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds" -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response | Should -Not -BeNullOrEmpty
            $response.data | Should -Not -BeNullOrEmpty
            $response.data.Count | Should -Be 0
        }
    }
    
    Context 'When creating a new world' {
        It 'Should create the world successfully' {
            # Arrange
            $createRequest = @{
                name = 'Middle Earth'
                description = 'A fantasy world created by J.R.R. Tolkien'
            } | ConvertTo-Json
            
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds" -Method Post `
                -Body $createRequest -ContentType 'application/json' -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response | Should -Not -BeNullOrEmpty
            $response.data | Should -Not -BeNullOrEmpty
            $response.data.name | Should -Be 'Middle Earth'
            $response.data.description | Should -Be 'A fantasy world created by J.R.R. Tolkien'
            $response.data.id | Should -Not -BeNullOrEmpty
            $response.data.ownerId | Should -Not -BeNullOrEmpty
            
            # Store for later tests
            $script:WorldId = $response.data.id
        }
    }
    
    Context 'When retrieving worlds after creation' {
        It 'Should return the created world' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds" -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -Be 1
            $response.data[0].name | Should -Be 'Middle Earth'
            $response.data[0].id | Should -Be $script:WorldId
        }
    }
    
    Context 'When retrieving a specific world by ID' {
        It 'Should return the correct world' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:WorldId" -Method Get `
                -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.id | Should -Be $script:WorldId
            $response.data.name | Should -Be 'Middle Earth'
        }
    }
}

Describe 'Entity Management API Integration Tests' -Tag 'Integration', 'API', 'Entity' {
    
    Context 'When creating a top-level entity (continent)' {
        It 'Should create the entity without a parent' {
            # Arrange
            $createRequest = @{
                name = 'Eriador'
                description = 'A region in Middle Earth'
                entityType = 'Location'
                tags = @('continent', 'northwest')
            } | ConvertTo-Json
            
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:WorldId/entities" -Method Post `
                -Body $createRequest -ContentType 'application/json' -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.name | Should -Be 'Eriador'
            $response.data.entityType | Should -Be 'Location'
            $response.data.tags | Should -Contain 'continent'
            $response.data.tags | Should -Contain 'northwest'
            $response.data.parentId | Should -BeNullOrEmpty
            
            # Store for later tests
            $script:ContinentId = $response.data.id
        }
    }
    
    Context 'When creating a child entity (country)' {
        It 'Should create the entity with a parent reference' {
            # Arrange
            $createRequest = @{
                name = 'The Shire'
                description = 'Homeland of the Hobbits'
                entityType = 'Location'
                parentId = $script:ContinentId
                tags = @('country', 'hobbits')
            } | ConvertTo-Json
            
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:WorldId/entities" -Method Post `
                -Body $createRequest -ContentType 'application/json' -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.name | Should -Be 'The Shire'
            $response.data.parentId | Should -Be $script:ContinentId
            $response.data.tags | Should -Contain 'hobbits'
            
            # Store for later tests
            $script:CountryId = $response.data.id
        }
    }
    
    Context 'When retrieving all entities in a world' {
        It 'Should return both the continent and country' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:WorldId/entities" -Method Get `
                -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -Be 2
            $response.data.name | Should -Contain 'Eriador'
            $response.data.name | Should -Contain 'The Shire'
        }
    }
    
    Context 'When retrieving children of an entity' {
        It 'Should return only direct children' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:WorldId/entities/$script:ContinentId/children" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -Be 1
            $response.data[0].name | Should -Be 'The Shire'
            $response.data[0].parentId | Should -Be $script:ContinentId
        }
    }
    
    Context 'When filtering entities by tag' {
        It 'Should return only entities with the specified tag' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:WorldId/entities?tags=hobbits" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -Be 1
            $response.data[0].name | Should -Be 'The Shire'
        }
    }
    
    Context 'When partially updating an entity (PATCH)' {
        It 'Should update only the specified fields' {
            # Arrange
            $patchRequest = @{
                description = 'A peaceful land where Hobbits live in comfort'
            } | ConvertTo-Json
            
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:WorldId/entities/$script:CountryId" `
                -Method Patch -Body $patchRequest -ContentType 'application/json' -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.description | Should -Be 'A peaceful land where Hobbits live in comfort'
            $response.data.name | Should -Be 'The Shire'  # Name should remain unchanged
        }
    }
    
    Context 'When attempting to delete an entity with children' {
        It 'Should reject the deletion without cascade flag' {
            # Act & Assert
            {
                Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:WorldId/entities/$script:ContinentId" `
                    -Method Delete -SkipCertificateCheck -TimeoutSec 30 -ErrorAction Stop
            } | Should -Throw
        }
    }
    
    Context 'When deleting an entity with cascade flag' {
        It 'Should recursively delete the entity and all descendants' {
            # Act
            Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:WorldId/entities/$script:ContinentId`?cascade=true" `
                -Method Delete -SkipCertificateCheck -TimeoutSec 30
            
            # Assert - verify entities are gone
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:WorldId/entities" -Method Get `
                -SkipCertificateCheck -TimeoutSec 30
            $response.data.Count | Should -Be 0
        }
    }
}

Describe 'Entity Management Advanced Features' -Tag 'Integration', 'API', 'Entity', 'Advanced' {
    
    BeforeAll {
        # Create test entities for advanced feature tests
        $createWorld = @{ name = 'Test World'; description = 'For advanced tests' } | ConvertTo-Json
        $worldResponse = Invoke-RestMethod -Uri "$script:BaseUrl/worlds" -Method Post `
            -Body $createWorld -ContentType 'application/json' -SkipCertificateCheck -TimeoutSec 30
        $script:TestWorldId = $worldResponse.data.id
    }
    
    Context 'When filtering by entity type' {
        BeforeAll {
            # Create entities of different types
            $location = @{ name = 'Mountain'; entityType = 'Location'; description = 'A tall peak' } | ConvertTo-Json
            $character = @{ name = 'Gandalf'; entityType = 'Character'; description = 'A wizard' } | ConvertTo-Json
            
            Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:TestWorldId/entities" -Method Post `
                -Body $location -ContentType 'application/json' -SkipCertificateCheck -TimeoutSec 30 | Out-Null
            Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:TestWorldId/entities" -Method Post `
                -Body $character -ContentType 'application/json' -SkipCertificateCheck -TimeoutSec 30 | Out-Null
        }
        
        It 'Should return only entities of the specified type' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:TestWorldId/entities?type=Character" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -Be 1
            $response.data[0].name | Should -Be 'Gandalf'
            $response.data[0].entityType | Should -Be 'Character'
        }
    }
    
    Context 'When using pagination' {
        BeforeAll {
            # Create multiple entities
            1..5 | ForEach-Object {
                $entity = @{ name = "Entity $_"; entityType = 'Item'; description = "Test entity $_" } | ConvertTo-Json
                Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:TestWorldId/entities" -Method Post `
                    -Body $entity -ContentType 'application/json' -SkipCertificateCheck -TimeoutSec 30 | Out-Null
            }
        }
        
        It 'Should respect the limit parameter' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:TestWorldId/entities?limit=3" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -Be 3
            $response.meta.nextCursor | Should -Not -BeNullOrEmpty
        }
        
        It 'Should support cursor-based pagination' {
            # Act - Get first page
            $firstPage = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:TestWorldId/entities?limit=3" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Act - Get second page with cursor
            $cursor = [System.Web.HttpUtility]::UrlEncode($firstPage.meta.nextCursor)
            $secondPage = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:TestWorldId/entities?limit=3&cursor=$cursor" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $firstPage.data.Count | Should -Be 3
            $secondPage.data.Count | Should -BeGreaterThan 0
            # Ensure no overlap
            $firstPage.data.id | Should -Not -Contain $secondPage.data[0].id
        }
    }
}

Describe 'Search and Filter API Integration Tests' -Tag 'Integration', 'API', 'Search' {
    
    BeforeAll {
        # Create a test world for search tests
        $createRequest = @{
            name = 'Search Test World'
            description = 'World for testing search functionality'
        } | ConvertTo-Json
        
        $worldResponse = Invoke-RestMethod -Uri "$script:BaseUrl/worlds" -Method Post `
            -Body $createRequest -ContentType 'application/json' -SkipCertificateCheck -TimeoutSec 30
        
        $script:SearchWorldId = $worldResponse.data.id
        
        # Create entities with different names for search testing
        $entities = @(
            @{ name = 'Dragon Quest'; entityType = 'Quest'; description = 'A quest to find the ancient dragon'; tags = @('fantasy', 'adventure') }
            @{ name = 'Knight Tournament'; entityType = 'Event'; description = 'Annual tournament of knights'; tags = @('medieval', 'combat') }
            @{ name = 'Dragon Slayer Sword'; entityType = 'Item'; description = 'A legendary sword'; tags = @('weapon', 'legendary') }
            @{ name = 'The Shire'; entityType = 'Location'; description = 'Home of the hobbits'; tags = @('peaceful', 'village') }
            @{ name = 'Hobbit Inn'; entityType = 'Building'; description = 'The Green Dragon inn'; tags = @('tavern', 'peaceful') }
        )
        
        foreach ($entity in $entities) {
            $json = $entity | ConvertTo-Json
            Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:SearchWorldId/entities" -Method Post `
                -Body $json -ContentType 'application/json' -SkipCertificateCheck -TimeoutSec 30 | Out-Null
        }
        
        # Wait for indexing
        Start-Sleep -Seconds 2
    }
    
    Context 'When searching by name' {
        It 'Should return entities with partial name match' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:SearchWorldId/search?q=dragon" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -BeGreaterThanOrEqual 2
            $response.data.name | Should -Contain 'Dragon Quest'
            $response.data.name | Should -Contain 'Dragon Slayer Sword'
        }
        
        It 'Should be case-insensitive' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:SearchWorldId/search?q=DRAGON" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -BeGreaterThanOrEqual 2
        }
    }
    
    Context 'When searching by description' {
        It 'Should return entities with description match' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:SearchWorldId/search?q=hobbits" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -BeGreaterThanOrEqual 1
            $response.data.description | Should -Contain 'Home of the hobbits'
        }
    }
    
    Context 'When searching by tags' {
        It 'Should return entities with matching tags' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:SearchWorldId/search?q=peaceful" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -BeGreaterThanOrEqual 2
            $response.data.tags | Should -Contain @('peaceful', 'village')
            $response.data.tags | Should -Contain @('tavern', 'peaceful')
        }
    }
    
    Context 'When sorting search results' {
        It 'Should sort by name ascending' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:SearchWorldId/search?q=dragon&sortBy=name&sortOrder=asc" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -BeGreaterThan 0
            # First result should come alphabetically before second
            if ($response.data.Count -gt 1) {
                $response.data[0].name | Should -BeLessOrEqual $response.data[1].name
            }
        }
        
        It 'Should sort by createdDate descending' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:SearchWorldId/search?q=dragon&sortBy=createdDate&sortOrder=desc" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -BeGreaterThan 0
            # Results should be ordered newest first
            if ($response.data.Count -gt 1) {
                [DateTime]$response.data[0].createdDate | Should -BeGreaterOrEqual ([DateTime]$response.data[1].createdDate)
            }
        }
    }
    
    Context 'When paginating search results' {
        It 'Should respect limit parameter' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:SearchWorldId/search?q=e&limit=2" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            $response.data.Count | Should -BeLessOrEqual 2
        }
        
        It 'Should provide cursor for next page' {
            # Act
            $response = Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:SearchWorldId/search?q=e&limit=2" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30
            
            # Assert
            if ($response.data.Count -eq 2) {
                $response.meta.nextCursor | Should -Not -BeNullOrEmpty
            }
        }
    }
    
    Context 'When search query is invalid' {
        It 'Should return 400 for empty query' {
            # Act & Assert
            { Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:SearchWorldId/search?q=" `
                -Method Get -SkipCertificateCheck -TimeoutSec 30 } | Should -Throw
        }
    }
    
    AfterAll {
        # Clean up search test world
        Invoke-RestMethod -Uri "$script:BaseUrl/worlds/$script:SearchWorldId" -Method Delete `
            -SkipCertificateCheck -TimeoutSec 30 -ErrorAction SilentlyContinue | Out-Null
    }
}

AfterAll {
    Write-Host "`nIntegration tests completed. Test data remains in Cosmos DB Emulator for inspection." -ForegroundColor Cyan
    Write-Host "To clean up test data, restart the Aspire AppHost (Ctrl+C and restart)." -ForegroundColor Gray
    Write-Host "View test data in Aspire Dashboard → cosmosdb resource → Data Explorer" -ForegroundColor Gray
}
