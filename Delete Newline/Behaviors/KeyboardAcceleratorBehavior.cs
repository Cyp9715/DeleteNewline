using Microsoft.Xaml.Interactivity;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;

using System.Windows.Input;
using Windows.System;

using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Behaviors
{
    public class KeyboardAcceleratorBehavior : Behavior<UIElement>
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
                typeof(KeyboardAcceleratorBehavior),
                new PropertyMetadata(null));

        private VirtualKeyModifiers _currentModifiers = VirtualKeyModifiers.None;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.KeyDown += OnKeyDown;
            AssociatedObject.KeyUp += OnKeyUp;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.KeyDown -= OnKeyDown;
            AssociatedObject.KeyUp -= OnKeyUp;
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            UpdateModifiers(e.Key, true);

            // ignore modifierKey
            if (IsModifierKey(e.Key))
                return;

            // Run the command only when the modifier key is pressed
            if (_currentModifiers != VirtualKeyModifiers.None)
            {
                ExecuteCommand(e);
                e.Handled = true;
            }
        }

        // KeyUp event only for updating modifier key status
        private void OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            UpdateModifiers(e.Key, false);
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

        private void UpdateModifiers(VirtualKey key, bool isKeyDown)
        {
            switch (key)
            {
                case VirtualKey.Control:
                case VirtualKey.LeftControl:
                case VirtualKey.RightControl:
                    if (isKeyDown)
                        _currentModifiers |= VirtualKeyModifiers.Control;
                    else
                        _currentModifiers &= ~VirtualKeyModifiers.Control;
                    break;
                case VirtualKey.Shift:
                case VirtualKey.LeftShift:
                case VirtualKey.RightShift:
                    if (isKeyDown)
                        _currentModifiers |= VirtualKeyModifiers.Shift;
                    else
                        _currentModifiers &= ~VirtualKeyModifiers.Shift;
                    break;
                case VirtualKey.Menu:
                case VirtualKey.LeftMenu:
                case VirtualKey.RightMenu:
                    if (isKeyDown)
                        _currentModifiers |= VirtualKeyModifiers.Menu;
                    else
                        _currentModifiers &= ~VirtualKeyModifiers.Menu;
                    break;
                case VirtualKey.LeftWindows:
                case VirtualKey.RightWindows:
                    if (isKeyDown)
                        _currentModifiers |= VirtualKeyModifiers.Windows;
                    else
                        _currentModifiers &= ~VirtualKeyModifiers.Windows;
                    break;
                default:
                    break;
            }
        }

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
            var args = new KeyboardAcceleratorEventArgs
            {
                Modifiers = _currentModifiers,
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
}
