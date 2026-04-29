namespace GradeAssist.Core;

public sealed record SafetyPolicyConfig(
    bool GameFolderWritesAllowed,
    bool RuntimeInjectionAllowed,
    bool AntiTamperBypassAllowed,
    IReadOnlyList<string>? ProhibitedPaths = null,
    IReadOnlyList<string>? ProhibitedTerms = null)
{
    public void Validate()
    {
        if (GameFolderWritesAllowed)
        {
            throw new InvalidOperationException("gameFolderWritesAllowed must be false: game install directories must not be written.");
        }

        if (RuntimeInjectionAllowed)
        {
            throw new InvalidOperationException("runtimeInjectionAllowed must be false: runtime injection is disabled by safety policy.");
        }

        if (AntiTamperBypassAllowed)
        {
            throw new InvalidOperationException("antiTamperBypassAllowed must be false: anti-tamper bypasses are prohibited.");
        }

        if (ProhibitedPaths is not null)
        {
            foreach (var path in ProhibitedPaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArgumentException("prohibitedPaths must not contain empty entries.");
                }
            }
        }

        if (ProhibitedTerms is not null)
        {
            foreach (var term in ProhibitedTerms)
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    throw new ArgumentException("prohibitedTerms must not contain empty entries.");
                }
            }
        }
    }
}
