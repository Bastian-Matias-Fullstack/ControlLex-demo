using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : Controller
    {
        // 🔐 Solo usuarios con rol "Admin" pueden acceder
        [Authorize(Roles = "Admin")]
        [HttpGet("solo-admins")]
        public IActionResult SoloAdmins()
        {
            var username = User.Identity?.Name;
            return Ok($"🎉 Bienvenido {username}, accediste como Admin.");
        }

    }
}
    
