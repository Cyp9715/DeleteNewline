using System.Threading.Tasks;
namespace Delete_Newline.Activation;

public interface IActivationHandler
{
    bool CanHandle(object args);

    Task HandleAsync(object args);
}
