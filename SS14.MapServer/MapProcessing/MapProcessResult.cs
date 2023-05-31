namespace SS14.MapServer.MapProcessing;

public record MapProcessResult(string GitRef, IList<Guid> MapIds);
