using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorIA.Servicios
{
    internal interface IServicioClima
    {
        Task<string> ObtenerClima(string ciudad);
    }
}
