using Delete_Newline.Helpers.Hotkeys;
using Windows.System;
using Hotkey = (Windows.System.VirtualKeyModifiers Modifiers, Windows.System.VirtualKey Key);

namespace Delete_Newline.Tests;

public class HotkeyRegistrationTests
{
    private static readonly Hotkey AltF1 = (VirtualKeyModifiers.Menu, VirtualKey.F1);
    private static readonly Hotkey CtrlF2 = (VirtualKeyModifiers.Control, VirtualKey.F2);

    [Fact]
    public void SwitchingToAnotherAppResumesHotkeysWithoutTextBoxLosingFocus()
    {
        var os = new FakeRegistration();
        var manager = os.Manager;
        object recorder = new();
        Assert.True(manager.Register(AltF1, HotkeyType.Regex));
        manager.SetWindowActive(true);
        manager.SetRecording(recorder, true);
        Assert.Empty(os.Active);

        manager.SetWindowActive(false);
        Assert.Contains(AltF1, os.Active);
        manager.SetWindowActive(true);
        Assert.Empty(os.Active);
        manager.SetRecording(recorder, false);
        Assert.Contains(AltF1, os.Active);
    }

    [Fact]
    public void RepeatedActivationDoesNotRegisterTheSameNativeHotkeyTwice()
    {
        var os = new FakeRegistration();
        os.Manager.Register(AltF1, HotkeyType.Regex);
        for (int i = 0; i < 10; i++) os.Manager.SetWindowActive(false);
        Assert.Equal(1, os.RegisterCalls);
    }

    [Fact]
    public void EditingAHotkeyWhileRecordingKeepsItSuspendedUntilAnotherAppIsActive()
    {
        var os = new FakeRegistration();
        os.Manager.SetWindowActive(true);
        os.Manager.SetRecording(new object(), true);
        Assert.True(os.Manager.Register(AltF1, HotkeyType.Regex));
        Assert.Empty(os.Active);
        os.Manager.Unregister(AltF1, HotkeyType.Regex);
        Assert.True(os.Manager.Register(CtrlF2, HotkeyType.Regex));
        Assert.Empty(os.Active);
        os.Manager.SetWindowActive(false);
        Assert.Equal([CtrlF2], os.Active);
    }

    [Fact]
    public void UnloadingOldRecorderDoesNotEndNewRecorderSession()
    {
        var os = new FakeRegistration();
        object oldRecorder = new(), newRecorder = new();
        os.Manager.Register(AltF1, HotkeyType.Regex);
        os.Manager.SetWindowActive(true);
        os.Manager.SetRecording(oldRecorder, true);
        os.Manager.SetRecording(newRecorder, true);
        os.Manager.SetRecording(oldRecorder, false);
        Assert.Empty(os.Active);
        os.Manager.SetRecording(newRecorder, false);
        Assert.Contains(AltF1, os.Active);
    }

    [Fact]
    public void RegexAndOcrCannotClaimTheSameSuspendedShortcut()
    {
        var os = new FakeRegistration();
        os.Manager.Register(AltF1, HotkeyType.Regex);
        os.Manager.SetWindowActive(true);
        os.Manager.SetRecording(new object(), true);
        Assert.True(os.Manager.Contains(AltF1));
        Assert.False(os.Manager.Register(AltF1, HotkeyType.Ocr));
        os.Manager.Unregister(AltF1, HotkeyType.Ocr);
        Assert.True(os.Manager.Contains(AltF1, HotkeyType.Regex));
    }

    [Fact]
    public void NativeRegistrationFailureIsRetriedWithoutLosingConfiguration()
    {
        var os = new FakeRegistration();
        object recorder = new();
        os.Manager.Register(AltF1, HotkeyType.Regex);
        os.Manager.SetWindowActive(true);
        os.Manager.SetRecording(recorder, true);
        os.AllowRegistration = false;
        os.Manager.SetWindowActive(false);
        Assert.Empty(os.Active);
        Assert.True(os.Manager.Contains(AltF1));
        os.AllowRegistration = true;
        os.Manager.Refresh();
        Assert.Contains(AltF1, os.Active);
    }

    [Fact]
    public void RemovingSuspendedShortcutDoesNotRestoreItLater()
    {
        var os = new FakeRegistration();
        os.Manager.Register(AltF1, HotkeyType.Regex);
        os.Manager.SetWindowActive(true);
        os.Manager.SetRecording(new object(), true);
        os.Manager.Unregister(AltF1, HotkeyType.Regex);
        os.Manager.SetWindowActive(false);
        Assert.Empty(os.Active);
        Assert.False(os.Manager.Contains(AltF1));
    }

    [Fact]
    public void RefreshDuringRecordingKeepsShortcutsSuspended()
    {
        var os = new FakeRegistration();
        os.Manager.Register(AltF1, HotkeyType.Regex);
        os.Manager.SetWindowActive(true);
        os.Manager.SetRecording(new object(), true);
        os.Manager.Refresh();
        Assert.Empty(os.Active);
        os.Manager.SetWindowActive(false);
        Assert.Contains(AltF1, os.Active);
    }

    private sealed class FakeRegistration
    {
        public readonly HashSet<Hotkey> Active = [];
        public bool AllowRegistration = true;
        public int RegisterCalls;
        public HotkeyRegistrationManager Manager { get; }

        public FakeRegistration() => Manager = new(hotkey =>
        {
            RegisterCalls++;
            return AllowRegistration && Active.Add(hotkey);
        }, hotkey => Active.Remove(hotkey));
    }
}
