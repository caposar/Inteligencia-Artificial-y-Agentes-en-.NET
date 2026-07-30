using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace WebAPI_IA.Servicios.Chatbots
{
    //public class ChatClientFactory(IConfiguration configuration, IServiceProvider sp) : IChatClientFactory
    //{
    //    public IChatClient Crear()
    //    {
    //        var llaveOpenAI = configuration.GetValue<string>("OPENAI_LLAVE");
    //        var modelo = configuration.GetValue<string>("OPENAI_MODELO");

    //        var cliente = new OpenAI.Chat.ChatClient(modelo ?? "gpt-5.4-nano", llaveOpenAI).AsIChatClient();

    //        return cliente.AsBuilder()
    //        .UseFunctionInvocation(null, c =>
    //        {
    //            c.IncludeDetailedErrors = true;
    //        })
    //        .Build(sp);
    //    }
    //}

    public class ChatClientFactory(IConfiguration configuration, IServiceProvider sp) : IChatClientFactory
    {
        public IChatClient Crear()
        {
            var llaveGroq = configuration.GetValue<string>("GROQ_LLAVE");
            var modelo = configuration.GetValue<string>("GROQ_MODELO");

            // Configurar el cliente para que apunte al endpoint compatible de Groq
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.groq.com/openai/v1")
            };

            var clienteOpenAI = new OpenAI.Chat.ChatClient(modelo, new ApiKeyCredential(llaveGroq!), options);
            var cliente = clienteOpenAI.AsIChatClient();

            return cliente.AsBuilder()
            .UseFunctionInvocation(null, c =>
            {
                c.IncludeDetailedErrors = true;
            })
            .Build(sp);
        }
    }
}
