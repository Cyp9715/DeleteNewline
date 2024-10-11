using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Services;

public class KeybindCollectManagerService
{
    public List<RegexChain> RegexChains { get; set; } = new();

    public KeybindCollectManagerService()
    {
        InitializeAsync();
    }

    public Task InitializeAsync()
    {
        // 초기화 로직이 위치해야 함.
        RegexChains.Add(new RegexChain("Chine 1"));
        RegexChains.Add(new RegexChain("Chine 2"));
        RegexChains.Add(new RegexChain("Chine 3"));
        RegexChains.Add(new RegexChain("Chine 4"));

        return Task.CompletedTask;
    }

    public List<RegexChain> GetRegexChains()
    {
        return RegexChains;
    }
}