using Microsoft.Xaml.Interactivity;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;

using System.Windows.Input;
using Windows.System;

using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers.Hotkeys;
using Microsoft.UI.Input;
using Windows.UI.Core;

namespace Delete_Newline.Behaviors;

public class KeyboardInputBehavior : Behavior<UIElement>
{
    public string CommandName
    {
        get => (string)GetValue(CommandNameProperty);
        set => SetValue(CommandNameProperty, value);
    }

    public static readonly DependencyProperty CommandNameProperty =
        DependencyProperty.Register(
            nameof(CommandName),
            typeof(string),
            typeof(KeyboardInputBehavior),
            new PropertyMetadata(null));

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.KeyDown += OnKeyDown;
        AssociatedObject.GotFocus += OnGotFocus;
        AssociatedObject.LostFocus += OnLostFocus;
        if (AssociatedObject is FrameworkElement element) element.Unloaded += OnUnloaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.KeyDown -= OnKeyDown;
        AssociatedObject.GotFocus -= OnGotFocus;
        AssociatedObject.LostFocus -= OnLostFocus;
        if (AssociatedObject is FrameworkElement element) element.Unloaded -= OnUnloaded;
        HotkeyRegister.SetRecording(this, false);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Ignore Windows key completely
        if (e.Key == VirtualKey.LeftWindows || e.Key == VirtualKey.RightWindows)
        {
            e.Handled = true;
            return;
        }

        // ignore modifierKey
        if (IsModifierKey(e.Key))
            return;

        // Execute command if any modifier is pressed
        if (CurrentModifiers != VirtualKeyModifiers.None)
        {
            ExecuteCommand(e);
            e.Handled = true;
        }
    }

    private bool IsModifierKey(VirtualKey key)
    {
        return key == VirtualKey.Control ||
               key == VirtualKey.LeftControl ||
               key == VirtualKey.RightControl ||
               key == VirtualKey.Shift ||
               key == VirtualKey.LeftShift ||
               key == VirtualKey.RightShift ||
               key == VirtualKey.Menu ||
               key == VirtualKey.LeftMenu ||
               key == VirtualKey.RightMenu ||
               key == VirtualKey.LeftWindows ||
               key == VirtualKey.RightWindows;
    }

    private void OnGotFocus(object sender, RoutedEventArgs e) => HotkeyRegister.SetRecording(this, true);
    private void OnLostFocus(object sender, RoutedEventArgs e) => HotkeyRegister.SetRecording(this, false);
    private void OnUnloaded(object sender, RoutedEventArgs e) => HotkeyRegister.SetRecording(this, false);

    private static VirtualKeyModifiers CurrentModifiers
    {
        get
        {
            VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;
            if (IsDown(VirtualKey.Control)) modifiers |= VirtualKeyModifiers.Control;
            if (IsDown(VirtualKey.Shift)) modifiers |= VirtualKeyModifiers.Shift;
            if (IsDown(VirtualKey.Menu)) modifiers |= VirtualKeyModifiers.Menu;
            return modifiers;
        }
    }

    private static bool IsDown(VirtualKey key) =>
        (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;

    private void ExecuteCommand(KeyRoutedEventArgs e)
    {
        // Retrieve the DataContext from the associated UI element, typically the ViewModel
        var viewModel = (AssociatedObject as FrameworkElement)?.DataContext;

        // If there is no ViewModel or CommandName is not set, exit the method
        if (viewModel == null || string.IsNullOrEmpty(CommandName))
            return;

        // Use reflection to get the property matching the CommandName from the ViewModel
        var commandProperty = viewModel.GetType().GetProperty(CommandName);
        if (commandProperty == null)
            return;

        // Get the ICommand object from the command property
        var command = commandProperty.GetValue(viewModel) as ICommand;
        if (command == null)
            return;

        // Create event arguments containing the current modifiers and the key pressed
        var args = new KeyboardInputEventArgs
        {
            Modifiers = CurrentModifiers,
            Key = e.Key
        };

        // Check if the command can be executed with the given arguments
        if (command.CanExecute(args))
        {
            // Execute the command with the provided arguments
            command.Execute(args);
        }
    }

}
