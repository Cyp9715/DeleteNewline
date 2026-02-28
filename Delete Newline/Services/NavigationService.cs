using System.Diagnostics.CodeAnalysis;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.ViewModels;
using Delete_Newline.Helpers;
using Delete_Newline.ViewModels;

namespace Delete_Newline.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IPageService _pageService;
    private object? _lastParameterUsed;
    private Frame? _frame;
    private NavigationView? _navigationView;

    public event NavigatedEventHandler? Navigated;

    public Frame? Frame
    {
        get
        {
            if (_frame == null)
            {
                _frame = App.MainWindow.Content as Frame;
                RegisterFrameEvents();
            }

            return _frame;
        }

        set
        {
            UnregisterFrameEvents();
            _frame = value;
            RegisterFrameEvents();
        }
    }

    [MemberNotNullWhen(true, nameof(Frame), nameof(_frame))]
    public bool CanGoBack => Frame != null && Frame.CanGoBack;

    public NavigationService(IPageService pageService)
    {
        _pageService = pageService;
    }
    
    [MemberNotNull(nameof(_navigationView))]
    public void InitializeNavigationView(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.BackRequested += OnBackRequested;
        _navigationView.ItemInvoked += OnItemInvoked;
    }
    
    public NavigationViewItem? GetSelectedItem(Type pageType)
    {
        if (_navigationView != null)
        {
            return GetSelectedItemFromMenuItems(_navigationView.MenuItems, pageType) ?? 
                   GetSelectedItemFromMenuItems(_navigationView.FooterMenuItems, pageType);
        }
        return null;
    }
    
    private NavigationViewItem? GetSelectedItemFromMenuItems(IEnumerable<object> menuItems, Type pageType)
    {
        foreach (var item in menuItems.OfType<NavigationViewItem>())
        {
            if (IsMenuItemForPageType(item, pageType))
            {
                return item;
            }

            var selectedChild = GetSelectedItemFromMenuItems(item.MenuItems, pageType);
            if (selectedChild != null)
            {
                return selectedChild;
            }
        }
        return null;
    }

    private NavigationViewItem? FindMenuItemByContent(IEnumerable<object> menuItems, object? content)
    {
        foreach (var item in menuItems.OfType<NavigationViewItem>())
        {
            if (Equals(item.Content, content))
            {
                return item;
            }

            var child = FindMenuItemByContent(item.MenuItems, content);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private NavigationViewItem? ResolveInvokedItem(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem invokedContainer)
        {
            return invokedContainer;
        }

        if (sender.SelectedItem is NavigationViewItem selectedItem)
        {
            return selectedItem;
        }

        // Fallback for cases where InvokedItemContainer is null after runtime shell/localization refresh.
        return FindMenuItemByContent(sender.MenuItems, args.InvokedItem)
               ?? FindMenuItemByContent(sender.FooterMenuItems, args.InvokedItem);
    }
    
    private bool IsMenuItemForPageType(NavigationViewItem menuItem, Type sourcePageType)
    {
        if (menuItem.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
        {
            return _pageService.GetPageType(pageKey) == sourcePageType;
        }
        return false;
    }
    
    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) => GoBack();
    
    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            NavigateTo(typeof(SettingsViewModel).FullName!);
            return;
        }

        var selectedItem = ResolveInvokedItem(sender, args);
        if (selectedItem?.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
        {
            NavigateTo(pageKey);
        }
    }

    private void RegisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated += OnNavigated;
        }
    }

    private void UnregisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated -= OnNavigated;
        }
    }

    public bool GoBack()
    {
        if (CanGoBack)
        {
            var vmBeforeNavigation = _frame.GetPageViewModel();
            _frame.GoBack();
            if (vmBeforeNavigation is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedFrom();
            }

            return true;
        }

        return false;
    }

    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
    {
        var pageType = _pageService.GetPageType(pageKey);

        if (_frame != null && (_frame.Content?.GetType() != pageType || (parameter != null && !parameter.Equals(_lastParameterUsed))))
        {
            _frame.Tag = clearNavigation;
            var vmBeforeNavigation = _frame.GetPageViewModel();
            var navigated = _frame.Navigate(pageType, parameter);
            if (navigated)
            {
                _lastParameterUsed = parameter;
                if (vmBeforeNavigation is INavigationAware navigationAware)
                {
                    navigationAware.OnNavigatedFrom();
                }
            }

            return navigated;
        }

        return false;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            var clearNavigation = frame.Tag is bool shouldClear && shouldClear;
            if (clearNavigation)
            {
                frame.BackStack.Clear();
            }

            if (frame.GetPageViewModel() is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(e.Parameter);
            }

            Navigated?.Invoke(sender, e);
        }
    }
}
