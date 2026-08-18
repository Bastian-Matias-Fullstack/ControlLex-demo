using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Aplicacion.Repositorio;


namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Soporte,Abogado")]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteRepository _clienteRepository;

        public ClientesController(IClienteRepository clienteRepository)
        {
            _clienteRepository = clienteRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetClientes()
        {
            var clientes = (await _clienteRepository.ObtenerTodosAsync())
                .Select(c => new
                {
                    c.Id,
                    c.Nombre
                })
                .ToList();

            return Ok(clientes);
        }
    }
}
