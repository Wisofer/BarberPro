using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BarberNic.Utils;

namespace BarberNic.Controllers.Web;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        var rol = SecurityHelper.GetUserRole(User);
        var nombre = SecurityHelper.GetUserFullName(User);
        
        ViewBag.Rol = rol;
        ViewBag.Nombre = nombre;
        
        return View();
    }
}

