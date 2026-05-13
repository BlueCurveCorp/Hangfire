using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("NexusForge.SqlServer")]
[assembly: AssemblyDescription("SQL Server job storage for NexusForge")]
[assembly: Guid("3d96bf2f-8854-4872-aee3-faf81d121a4d")]
[assembly: CLSCompliant(true)]

[assembly: InternalsVisibleTo("NexusForge.SqlServer.Tests")]
// Allow the generation of mocks for internal types
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
