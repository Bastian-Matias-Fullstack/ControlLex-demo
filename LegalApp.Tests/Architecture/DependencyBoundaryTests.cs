using System.Reflection;
using System.Xml.Linq;
using Aplicacion.Repositorio;
using Aplicacion.Servicios.Casos;
using Dominio.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegalApp.Tests.Architecture;

public sealed class DependencyBoundaryTests
{
    private static readonly string[] DomainForbiddenReferences =
    [
        "Aplicacion",
        "Infraestructura",
        "API",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore"
    ];

    private static readonly string[] ApplicationForbiddenReferences =
    [
        "Infraestructura",
        "API",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Microsoft.Extensions.Configuration",
        "Swashbuckle"
    ];

    [Fact]
    public void Domain_assembly_does_not_reference_outer_or_technical_layers()
    {
        AssertNoAssemblyReferences(typeof(Caso).Assembly, DomainForbiddenReferences);
    }

    [Fact]
    public void Application_assembly_does_not_reference_outer_or_technical_layers()
    {
        AssertNoAssemblyReferences(
            typeof(CerrarCasoService).Assembly,
            ApplicationForbiddenReferences);
    }

    [Fact]
    public void Domain_project_declares_no_packages_or_project_dependencies()
    {
        var project = LoadProject("Dominio", "Dominio.csproj");

        Assert.Empty(GetIncludes(project, "PackageReference"));
        Assert.Empty(GetIncludes(project, "ProjectReference"));
    }

    [Fact]
    public void Application_project_only_points_inward_and_uses_application_packages()
    {
        var project = LoadProject("Aplicacion", "Aplicacion.csproj");
        var projectReferences = GetIncludes(project, "ProjectReference");
        var packageReferences = GetIncludes(project, "PackageReference");

        Assert.Single(projectReferences);
        Assert.EndsWith("Dominio\\Dominio.csproj", projectReferences[0]);
        Assert.DoesNotContain(
            packageReferences,
            reference => ApplicationForbiddenReferences.Any(forbidden =>
                reference.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Application_repository_contracts_do_not_expose_iqueryable()
    {
        var repositoryContracts = typeof(ICasoRepository).Assembly
            .GetTypes()
            .Where(type => type.IsInterface && type.Namespace == "Aplicacion.Repositorio");

        var offendingMethods = repositoryContracts
            .SelectMany(type => type.GetMethods())
            .Where(method => ContainsQueryable(method.ReturnType) ||
                method.GetParameters().Any(parameter => ContainsQueryable(parameter.ParameterType)))
            .Select(method => $"{method.DeclaringType?.Name}.{method.Name}")
            .ToArray();

        Assert.Empty(offendingMethods);
    }

    [Fact]
    public void Api_controllers_do_not_receive_dbcontext()
    {
        var controllerTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));

        var offendingConstructors = controllerTypes
            .SelectMany(type => type.GetConstructors())
            .Where(constructor => constructor.GetParameters().Any(parameter =>
                typeof(DbContext).IsAssignableFrom(parameter.ParameterType)))
            .Select(constructor => constructor.DeclaringType?.Name)
            .ToArray();

        Assert.Empty(offendingConstructors);
    }

    private static void AssertNoAssemblyReferences(
        Assembly assembly,
        IEnumerable<string> forbiddenReferences)
    {
        var forbidden = forbiddenReferences.ToArray();
        var actual = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(reference => forbidden.Any(prefix =>
                reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.Empty(actual);
    }

    private static bool ContainsQueryable(Type type)
    {
        return typeof(IQueryable).IsAssignableFrom(type) ||
            type.IsGenericType && type.GetGenericArguments().Any(ContainsQueryable);
    }

    private static XDocument LoadProject(string directory, string projectFile)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null &&
               !File.Exists(Path.Combine(current.FullName, "SoftwareJuridicoEscalableRobusto.sln")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        return XDocument.Load(Path.Combine(current!.FullName, directory, projectFile));
    }

    private static string[] GetIncludes(XDocument project, string elementName)
    {
        return project.Descendants(elementName)
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
    }
}
