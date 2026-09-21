using Xunit.Sdk;
using Xunit.v3;

// CategoryDatabase is a static singleton that tests load and reload, so test classes must not run
// concurrently or they would observe each other's database state.
[assembly: Parallelization(Mode = ParallelMode.None)]
