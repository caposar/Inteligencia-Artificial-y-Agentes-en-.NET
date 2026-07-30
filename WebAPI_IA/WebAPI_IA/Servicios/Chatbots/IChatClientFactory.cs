using Microsoft.Extensions.AI;

namespace WebAPI_IA.Servicios.Chatbots
{
    public interface IChatClientFactory
    {
        IChatClient Crear();
    }
}
