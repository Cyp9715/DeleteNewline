namespace Delete_Newline.Contracts.Services;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs);
}
