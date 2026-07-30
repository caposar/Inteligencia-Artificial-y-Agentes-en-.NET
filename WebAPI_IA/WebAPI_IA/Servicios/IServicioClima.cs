namespace WebAPI_IA.Servicios
{
    public interface IServicioClima
    {
        Task<string> ObtenerClima(string ciudad);
    }
}
