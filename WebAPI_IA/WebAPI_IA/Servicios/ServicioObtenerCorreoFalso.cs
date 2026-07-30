using System.ComponentModel;

namespace WebAPI_IA.Servicios
{
    internal class ServicioObtenerCorreoFalso
    {
        [Description("Obtiene el correo de una persona")]
        public string ObtenerCorreo([Description("Nombre de la persona")] string nombre) => $"{nombre}@ejemplo.com";
    }
}
