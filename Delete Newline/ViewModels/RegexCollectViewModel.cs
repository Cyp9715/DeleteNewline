using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;

namespace Delete_Newline.ViewModels;

public partial class RegexCollectViewModel : ObservableRecipient
{
    private readonly RegexCollectSaveService _regexCollectSaveService;
    public INavigationService NavigationService { get; }

    [ObservableProperty]
    private ObservableCollection<RegexPageStructure> _regexConfigs;

    [ObservableProperty]
    private ObservableCollection<RegexPageStructure> _filteredRegexConfigs = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearchVisible;

    public bool CanReorderRegexConfigs => !IsSearchVisible || string.IsNullOrWhiteSpace(SearchText);

    public RegexCollectViewModel(RegexCollectSaveService regexCollectSaveService,
        INavigationService navigationService)
    {
        _regexCollectSaveService = regexCollectSaveService;
        NavigationService = navigationService;
        RegexConfigs = _regexCollectSaveService.RegexConfigs;
        FilteredRegexConfigs = RegexConfigs;
        RegexConfigs.CollectionChanged += RegexConfigs_CollectionChanged;
        foreach (RegexPageStructure regexConfig in RegexConfigs)
        {
            regexConfig.PropertyChanged += RegexConfig_PropertyChanged;
        }
    }

    private void RegexConfigs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (RegexPageStructure regexConfig in e.OldItems)
            {
                regexConfig.PropertyChanged -= RegexConfig_PropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (RegexPageStructure regexConfig in e.NewItems)
            {
                regexConfig.PropertyChanged += RegexConfig_PropertyChanged;
            }
        }

        RefreshFilteredRegexConfigs();
    }

    private void RegexConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RegexPageStructure.HotkeyName))
        {
            RefreshFilteredRegexConfigs();
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshFilteredRegexConfigs();
        OnPropertyChanged(nameof(CanReorderRegexConfigs));
    }

    partial void OnIsSearchVisibleChanged(bool value)
    {
        RefreshFilteredRegexConfigs();
        OnPropertyChanged(nameof(CanReorderRegexConfigs));
    }

    public void ShowSearch()
    {
        IsSearchVisible = true;
    }

    public void HideSearch(bool clearSearchText = true)
    {
        IsSearchVisible = false;
        if (clearSearchText)
        {
            SearchText = string.Empty;
        }
    }

    private void RefreshFilteredRegexConfigs()
    {
        FilteredRegexConfigs = !IsSearchVisible || string.IsNullOrWhiteSpace(SearchText)
            ? RegexConfigs
            : new ObservableCollection<RegexPageStructure>(RegexProfileFilter.FilterByName(RegexConfigs, SearchText));
    }

    public string GetHotkeyDisplayText(RegexPageStructure config)
    {
        return HotkeyFormatter.GetDisplayText(config.Hotkey.Modifiers, config.Hotkey.Key);
    }

    [RelayCommand]
    private void AddRegex()
    {
        _regexCollectSaveService.AddRegexConfig();
    }

    [RelayCommand]
    private Task RemoveRegexAsync(object? parameter)
    {
        return RemoveRegexConfigsAsync(parameter);
    }

    public async Task RemoveRegexConfigsAsync(object? parameter)
    {
        IReadOnlyList<RegexPageStructure> targets = GetRemovalTargets(parameter);
        if (targets.Count > 0)
        {
            await _regexCollectSaveService.RemoveRegexConfigsAsync(targets);
        }
    }

    private static IReadOnlyList<RegexPageStructure> GetRemovalTargets(object? parameter)
    {
        if (parameter is RegexPageStructure regexConfig)
        {
            return [regexConfig];
        }

        if (parameter is System.Collections.IEnumerable selectedItems)
        {
            return selectedItems
                .OfType<RegexPageStructure>()
                .Distinct()
                .ToArray();
        }

        return [];
    }

    [RelayCommand]
    private void NavigateToRegexPage(RegexPageStructure regexConfig)
    {
        App.GetService<RegexViewModel>().CurrentRegexConfig = regexConfig;
        NavigationService.NavigateTo(typeof(RegexViewModel).FullName!);
    }
}
