using System;
using System.Collections.Generic;
using System.Text;

namespace PrimerChatbot.Servicios
{
    internal interface IServicioClima
    {
        Task<string> ObtenerClima(string ciudad);
    }
}
