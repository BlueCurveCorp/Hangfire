Include "packages\NexusForge.Build.0.5.0\tools\psake-common.ps1"

Task Default -Depends Pack

Task Merge -Depends Compile -Description "Run ILRepack /internalize to merge required assemblies." {
    Repack-Assembly @("NexusForge.Core", "net451") @("Cronos", "CronExpressionDescriptor", "Microsoft.Owin")
    Repack-Assembly @("NexusForge.Core", "net46") @("Cronos", "CronExpressionDescriptor", "Microsoft.Owin")
    Repack-Assembly @("NexusForge.SqlServer", "net451") @("Dapper")

    Repack-Assembly @("NexusForge.Core", "netstandard1.3") @("Cronos")
    Repack-Assembly @("NexusForge.Core", "netstandard2.0") @("Cronos")
    Repack-Assembly @("NexusForge.SqlServer", "netstandard1.3") @("Dapper")
    Repack-Assembly @("NexusForge.SqlServer", "netstandard2.0") @("Dapper")
}

Task Test -Depends Merge -Description "Run unit and integration tests against merged assemblies." {
    # Dependencies shouldn't be re-built, because we need to run tests against merged assemblies to test
    # the same assemblies that are distributed to users. Since the `dotnet test` command doesn't support
    # the `--no-dependencies` command directly, we need to re-build tests themselves first.
    Exec { ls "tests\**\*.csproj" | % { dotnet build -c Release --no-restore --no-dependencies $_.FullName } }

    # We are running unit test project one by one, because pipelined version like the line above does not
    # support halting the whole execution pipeline when "dotnet test" command fails due to a failed test,
    # silently allowing build process to continue its execution even with failed tests.
    Exec { dotnet test -c Release --no-build "tests\NexusForge.Core.Tests" }
    Exec { dotnet test -c Release --no-build "tests\NexusForge.SqlServer.Tests" }
    Exec { dotnet test -c Release --no-build -p:TestTfmsInParallel=false "tests\NexusForge.SqlServer.Msmq.Tests" }
}

Task Collect -Depends Test -Description "Copy all artifacts to the build folder." {
    Collect-Assembly "NexusForge.Core" "net451"
    Collect-Assembly "NexusForge.SqlServer" "net451"
    Collect-Assembly "NexusForge.SqlServer.Msmq" "net451"
    Collect-Assembly "NexusForge.NetCore" "net451"
    Collect-Assembly "NexusForge.AspNetCore" "net451"

    Collect-Assembly "NexusForge.Core" "net46"

    Collect-Assembly "NexusForge.Core" "netstandard1.3"
    Collect-Assembly "NexusForge.SqlServer" "netstandard1.3"
    Collect-Assembly "NexusForge.NetCore" "netstandard1.3"
    Collect-Assembly "NexusForge.AspNetCore" "netstandard1.3"
    
    Collect-Assembly "NexusForge.Core" "netstandard2.0"
    Collect-Assembly "NexusForge.SqlServer" "netstandard2.0"
    Collect-Assembly "NexusForge.AspNetCore" "netstandard2.0"
    Collect-Assembly "NexusForge.NetCore" "netstandard2.0"

    Collect-Assembly "NexusForge.NetCore" "net461"
    Collect-Assembly "NexusForge.AspNetCore" "net461"

    Collect-Assembly "NexusForge.AspNetCore" "netcoreapp3.0"
    Collect-Assembly "NexusForge.NetCore" "netstandard2.1"
    
    Collect-Tool "src\NexusForge.SqlServer\DefaultInstall.sql"

    Collect-Localizations "NexusForge.Core" "net451"
    Collect-Localizations "NexusForge.Core" "net46"
    Collect-Localizations "NexusForge.Core" "netstandard1.3"
    Collect-Localizations "NexusForge.Core" "netstandard2.0"

    Collect-File "README.md"
    Collect-File "LICENSE.md"
    Collect-File "NOTICES"
    Collect-File "COPYING.LESSER"
    Collect-File "COPYING"
    Collect-File "LICENSE_STANDARD"
    Collect-File "LICENSE_ROYALTYFREE"
}

Task Pack -Depends Collect -Description "Create NuGet packages and archive files." {
    $version = Get-PackageVersion

    Create-Package "NexusForge" $version
    Create-Package "NexusForge.Core" $version
    Create-Package "NexusForge.SqlServer" $version
    Create-Package "NexusForge.SqlServer.Msmq" $version
    Create-Package "NexusForge.AspNetCore" $version
    Create-Package "NexusForge.NetCore" $version

    Create-Archive "NexusForge-$version"
}

Task Sign -Depends Pack -Description "Sign artifacts." {
    $version = Get-PackageVersion
    Sign-ArchiveContents "NexusForge-$version" "nexusforge"
}
