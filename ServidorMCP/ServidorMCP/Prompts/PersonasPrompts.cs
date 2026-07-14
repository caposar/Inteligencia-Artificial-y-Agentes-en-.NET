using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ServidorMCP.Prompts
{
    [McpServerPromptType]
    public class PersonasPrompts
    {
        [McpServerPrompt, Description("Prompt para consultar todas las personas.")]
        public static ChatMessage ConsultarTodas() => new ChatMessage(ChatRole.User,
            """
            Obtén el listado completo de personas usando la tool disponible.
            Luego presenta la información en español de forma clara y resumida.
            """);

        [McpServerPrompt, Description("Prompt para consultar una persona por id.")]
        public static ChatMessage ConsultarPorId(
            [Description("Id de la persona a consultar.")] int id)
            => new(
                ChatRole.User,
                $"""
                Busca la persona con id {id} usando la tool disponible.

                Si existe:
                - muestra sus datos en español,
                - indica si está activa o no.

                Si no existe:
                - indícalo claramente.
                """
            );

        [McpServerPrompt, Description("Prompt para activar una persona.")]
        public static ChatMessage ActivarPersona(
            [Description("Id de la persona.")] int id)
            => new(
                ChatRole.User,
                $"""
                Activa la persona con id {id} usando la tool disponible.
                Debes enviar activo = true.

                Luego explica en español si la operación fue exitosa o no.
                """
            );

        [McpServerPrompt, Description("Prompt para desactivar una persona.")]
        public static ChatMessage DesactivarPersona(
            [Description("Id de la persona.")] int id)
            => new(
                ChatRole.User,
                $"""
                Desactiva la persona con id {id} usando la tool disponible.
                Debes enviar activo = false.

                Luego explica en español si la operación fue exitosa o no.
                """
            );
    }
}
