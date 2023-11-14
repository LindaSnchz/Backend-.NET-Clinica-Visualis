using AgendaServicios.Web.Data;
using AgendaServicios.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;

namespace AgendaServicios.Web.Controllers
{
	public class TurnosController : Controller
	{
		private readonly ApplicationDbContext _context;

		public TurnosController(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			return RedirectToAction("BuscarCliente");
		}

		private List<TiposServicio> ObtenerTiposDeServicio()
		{
			var tiposServicios =
				(
				from t in _context.TiposServicios
				orderby t.Descripcion
				select t
				).ToList();

			return tiposServicios;
		}

		private List<Servicio> ObtenerServiciosPorTipoServicio(int tipoServicioId)
		{
			var servicios =
				(
				from s in _context.Servicios
				where s.TipoServicioId == tipoServicioId
				orderby s.Descripcion
				select s
				).ToList();

			return servicios;
		}

		[HttpGet]
		public async Task<JsonResult> ObtenerJsonServiciosPorTipoServicio(int tipoServicioId)
		{
			var servicios =
				(
				from s in ObtenerServiciosPorTipoServicio(tipoServicioId)
				select new { s.ServicioId, s.Descripcion }
				).ToList();

			return Json(servicios);
		}


		private Cliente? ObtenerClientePorId(int clienteId)
		{
			var cliente = _context.Clientes
				.Include(c => c.Localidad)
				.Include(c => c.Provincia)
				.FirstOrDefault(m => m.ClienteId == clienteId);

			return cliente;
		}


		#region ObtenerFechasDisponiblesDeTurnosProximos60Dias
		public List<DateTime> ObtenerFechasDisponiblesDeTurnosProximos60Dias(DateTime fecha)
		{
			var fechaDesde = fecha.Date; // Calcula la fecha mínima (fecha dada)
			var fechaHasta = fecha.AddDays(60).Date; // Calcula la fecha máxima (60 días después de la fecha dada)

			var fechas = (from t in _context.Turnos
						  where t.FechaTurno.Date >= fechaDesde &&
								t.FechaTurno.Date < fechaHasta &&
								t.EstadoTurnoId == (int)EstadoTurnoEnum.Libre
						  group t by t.FechaTurno.Date into grouped
						  orderby grouped.Key
						  select grouped.Key).ToList();
			return fechas;
		}

		public List<DateTime> ObtenerFechasDisponiblesDeTurnosProximos60Dias(int año, int mes, int dia)
		{
			var fecha = new DateTime(año, mes, dia);
			return ObtenerFechasDisponiblesDeTurnosProximos60Dias(fecha);
		}

		public List<DateTime> ObtenerFechasDisponiblesDeTurnosProximos60Dias(string fecha)
		{
			var fechaConvertida = DateTime.ParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture);
			return ObtenerFechasDisponiblesDeTurnosProximos60Dias(fechaConvertida);
		}


		[HttpGet]
		public JsonResult ObtenerJsonDeFechasDisponiblesFormateadasYYYYMMDDProximos60Dias(int año, int mes, int dia)
		{
			var fecha = new DateTime(año, mes, dia);
			var fechas = ObtenerFechasDisponiblesDeTurnosProximos60Dias(fecha);
			var fechasFormateadas = FormatearFechasAStringYYYYMMDD(fechas);

			return Json(fechasFormateadas);
		}

		#endregion

		#region ObtenerHorasDisponiblesDeTurnosPorFecha
		public List<TimeSpan> ObtenerHorasDisponiblesDeTurnosPorFecha(DateTime fecha)
		{
			var horas = (from t in _context.Turnos
						 where t.FechaTurno.Date == fecha.Date && t.EstadoTurnoId == (int)EstadoTurnoEnum.Libre
						 orderby t.HoraTurno
						 select t.HoraTurno).ToList();
			return horas;
		}

		public List<TimeSpan> ObtenerHorasDisponiblesDeTurnosPorFecha(int año, int mes, int dia)
		{
			var fecha = new DateTime(año, mes, dia);
			return ObtenerHorasDisponiblesDeTurnosPorFecha(fecha);
		}

		public List<TimeSpan> ObtenerHorasDisponiblesDeTurnosPorFecha(string fecha)
		{
			var fechaConvertida = DateTime.ParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture);
			return ObtenerHorasDisponiblesDeTurnosPorFecha(fechaConvertida);
		}

		public List<string> ObtenerHorasDisponiblesDeTurnosFormateadasPorFecha(string fecha)
		{
			var fechaConvertida = DateTime.ParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture);
			var horas = ObtenerHorasDisponiblesDeTurnosPorFecha(fechaConvertida);

			var horasFormateadas = FormatearHorasAString(horas);
			return horasFormateadas;
		}

		#endregion


		#region Formatear las fecha y horas a string
		private static List<string> FormatearFechasAStringDDMMYYYY(List<DateTime> listaFechas)
		{
			var fechasFormateadas =
				(
				from f in listaFechas
				select f.ToString("dd/MM/yyyy")
				).ToList();

			return fechasFormateadas;
		}

		private static List<string> FormatearFechasAStringYYYYMMDD(List<DateTime> listaFechas)
		{
			var fechasFormateadas =
				(
				from f in listaFechas
				select f.ToString("yyyy-MM-dd")
				).ToList();

			return fechasFormateadas;
		}

		private static List<string> FormatearHorasAString(List<TimeSpan> listaHoras)
		{
			var horasFormateadas =
				(
				from h in listaHoras
				select h.ToString(@"hh\:mm")
				).ToList();

			return horasFormateadas;
		}
		#endregion


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ReservarTurno(ReservaTurno model)
		{
			// Buscar el turno por fecha, hora y estado
			var turnoExistente = await (from t in _context.Turnos
										where t.FechaTurno == model.FechaTurno
										   && t.HoraTurno == model.HoraTurno
										   && t.EstadoTurnoId == (int)EstadoTurnoEnum.Libre
										select t).FirstOrDefaultAsync();

			if (turnoExistente != null)
			{
				// Actualizar el turno con los datos del modelo
				turnoExistente.ClienteId = model.ClienteId;
				turnoExistente.FechaTurno = model.FechaTurno;
				turnoExistente.HoraTurno = model.HoraTurno;
				turnoExistente.ServicioId = model.ServicioId;
				turnoExistente.Observacion = model.Observacion;
				turnoExistente.EstadoTurnoId = (int)EstadoTurnoEnum.Asignado ;

				// Guardar los cambios en la base de datos
				_context.Update(turnoExistente);
				await _context.SaveChangesAsync();

				// Pasamos el modelo a la vista 'TurnoReservado'.
				ViewBag.HideHeader = true;
				return View("TurnoReservado", model);

			}
			return View(model);
		}

		public IActionResult BuscarCliente()
		{
			return View();
		}

		[HttpPost]
		public IActionResult BuscarCliente(string numeroDocumento, string email)
		{
			var cliente = (from c in _context.Clientes
						   where c.NumeroDocumento.ToString() == numeroDocumento || c.CorreoElectronico == email
						   select c).FirstOrDefault();

			if (cliente == null)
			{
				ViewBag.HideHeader = true;
				ViewBag.ErrorMessage = "Paciente no encontrado";
				return View();
			}

			ViewBag.HideHeader = true;
			return View(cliente); // Pasamos el cliente a la vista para mostrar los detalles
		}


		public IActionResult IniciarReservaTurno(int clienteId)
		{
			Cliente? cliente = ObtenerClientePorId(clienteId);

			// Obtener los Tipos de Servicio
			var tiposServicios = ObtenerTiposDeServicio();
			var tipoServicioId = tiposServicios.FirstOrDefault()?.TipoServicioId ?? 0;

			// Obtener los Servicios que pertenecen al Tipo de Servicio especificado
			var servicios = ObtenerServiciosPorTipoServicio(tipoServicioId);

			// Obtener las fechas de los turnos libres
			var fechaHoy = DateTime.Today;
			var listaFechas = ObtenerFechasDisponiblesDeTurnosProximos60Dias(fechaHoy);
			var fecha = listaFechas.FirstOrDefault();
			var listaFechasFormateadas = FormatearFechasAStringDDMMYYYY(listaFechas);

			// Obtener las horas de los turnos libres
			var listaHoras = ObtenerHorasDisponiblesDeTurnosPorFecha(fecha);
			var hora = listaHoras.FirstOrDefault();
			var listaHorasFormateadas = FormatearHorasAString(listaHoras);

			var reservaTurno = new ReservaTurno
			{
				FechaTurno = fecha,
				HoraTurno = hora,
				Cliente = cliente,
				ClienteId = clienteId,
			};

			ViewData["SelectListTiposServicios"] = new SelectList(tiposServicios, "TipoServicioId", "Descripcion");
			ViewData["SelectListServicios"] = new SelectList(servicios, "ServicioId", "Descripcion");
			ViewData["SelectListFechas"] = new SelectList(listaFechasFormateadas);
			ViewData["SelectListHoras"] = new SelectList(listaHorasFormateadas);

			ViewBag.HideHeader = true;
			return View(reservaTurno);
		}
	}
}
