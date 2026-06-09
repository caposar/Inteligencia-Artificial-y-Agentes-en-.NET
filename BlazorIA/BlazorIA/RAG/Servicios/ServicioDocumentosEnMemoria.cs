using BlazorIA.RAG.Modelos;

namespace BlazorIA.RAG.Servicios
{
    public class ServicioDocumentosEnMemoria
    {
        public List<Documento> ObtenerDocumentos() =>
        [
            new Documento
            {
                Titulo = "Política de vacaciones",
                Contenido = """
                Todo colaborador obtiene 14 días laborables de vacaciones al cumplir un año en la empresa.
                La solicitud debe realizarse con al menos 15 días de anticipación.
                El líder directo debe aprobar la solicitud antes de su ejecución.
                Las vacaciones no pueden fraccionarse en bloques menores a 2 días, salvo aprobación especial de Recursos Humanos.
                """
            },
            new Documento
            {
                Titulo = "Trabajo remoto",
                Contenido = """
                Los colaboradores pueden trabajar remoto hasta 3 días por semana.
                Deben asistir presencialmente a reuniones estratégicas cuando sean convocados.
                El empleado debe garantizar una conexión estable a internet y un ambiente adecuado para videollamadas.
                Los equipos suministrados por la empresa son de uso exclusivo para funciones laborales.
                """
            },
            new Documento
            {
                Titulo = "Solicitud de equipos",
                Contenido = """
                Para solicitar una laptop o accesorio nuevo, el colaborador debe abrir un ticket en la mesa de ayuda.
                El ticket debe incluir justificación de negocio y aprobación del supervisor.
                El tiempo estimado de entrega de equipos en inventario es de 3 días laborables.
                Si el equipo no está disponible, Compras notificará la fecha estimada de reposición.
                """
            },
            new Documento
            {
                Titulo = "Soporte técnico",
                Contenido = """
                El horario de soporte técnico es de lunes a viernes de 8:00 a.m. a 6:00 p.m.
                Incidentes críticos tienen prioridad alta y deben reportarse por el canal de emergencias.
                Las contraseñas no deben compartirse bajo ninguna circunstancia.
                El restablecimiento de contraseña puede solicitarse desde el portal de autoservicio.
                """
            }
        ];

    }
}
