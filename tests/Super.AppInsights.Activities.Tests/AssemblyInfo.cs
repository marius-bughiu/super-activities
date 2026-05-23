using Xunit;

// The extension registry is process-global static state, so tests must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
