using System.ComponentModel.DataAnnotations;

namespace AgendaServicios.Web.Models
{
	public class TurnoViewModel
	{
		public Cliente Cliente { get; set; }
		public Turno Turno { get; set; }

		[Display(Name = "Tipo de servicio")]
		public int TipoServicioId { get; set; }

		[Display(Name = "Servicio")]
		public int ServicioId { get; set; }
	}
}
