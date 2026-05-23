using Xunit;

// The framework uses process-global static state (the extension registry and a TypeDescriptor
// provider), so tests must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
