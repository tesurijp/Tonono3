namespace Tonono3.SKKEngine;

public sealed record SkkUiSnapshot(
    long Version,
    bool IsVisible,
    string StatusText,
    bool IsInRegistrationMode,
    string RegistrationReading,
    string RegistrationWord,
    string Composition,
    string CandidateList);
