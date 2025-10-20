using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace App.Helper { }

public static class GetAllClaimsPermissions
{
    #region Get All Controller Action
    public static List<Claim> GetAllControllerActionsUpdated()
    {
        Assembly asm = AppDomain.CurrentDomain.GetAssemblies()[1]; //Assembly.GetExecutingAssembly();

        var controlleractionlist = asm.GetTypes()
            .Where(type => typeof(Controller).IsAssignableFrom(type))
            .SelectMany(type =>
                type.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
            .Where(m => !m.GetCustomAttributes(typeof(CompilerGeneratedAttribute),
                true).Any())
            .Select(x => new
            {
                Controller = x.DeclaringType.Name,
                Action = x.Name,
                ReturnType = x.ReturnType.Name,
                Attributes = string.Join(",",
                    x.GetCustomAttributes().Select(a => a.GetType().Name.Replace("Attribute", "")))
            })
            .OrderBy(x => x.Controller).ThenBy(x => x.Action).ToList();

        var AllClaims = new List<Claim>();

        foreach (var item in controlleractionlist)
        {
            var ClaimName = $"{item.Controller.Replace("Controller", "")} - {item.Action}";
            var claim = new Claim(ClaimName, ClaimName);
            if (!AllClaims.Exists(c => c.Value == claim.Value))
                AllClaims.Add(claim);
        }
        return AllClaims; //.Distinct().ToList<Claim>()
    }
    #endregion
}

