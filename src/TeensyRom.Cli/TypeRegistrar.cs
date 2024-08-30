using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public object Resolve(Type type)
    {
        AnsiConsole.WriteLine($"DEBUG: TypeResolver.Resolve({type.FullName})");
        if (type == null)
        {
            AnsiConsole.WriteLine($"DEBUG: TypeResolver.Resolve() Type was null");
            return null;
        }
        var service = _provider.GetService(type);

        if(service is null)
        {
            AnsiConsole.WriteLine($"DEBUG: TypeResolver.Resolve() Service was null");
            throw new InvalidOperationException($"TypeResolver.cs: Could not resolve type '{type.FullName}'.");
        }

        return service;
    }

    public void Dispose()
    {
        AnsiConsole.WriteLine($"DEBUG: TypeResolver.Dispose()");
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _builder;

    public TypeRegistrar(IServiceCollection builder)
    {
        _builder = builder;
    }

    public ITypeResolver Build()
    {
        AnsiConsole.WriteLine($"DEBUG: TypeRegistrar.Build()");
        return new TypeResolver(_builder.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        AnsiConsole.WriteLine($"DEBUG: TypeRegistrar.Register() {service.FullName}");
        _builder.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _builder.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> func)
    {
        AnsiConsole.WriteLine($"DEBUG: TypeRegistrar.RegisterLazy() {service.FullName}");
        if (func is null)
        {
            AnsiConsole.WriteLine($"DEBUG: TypeRegistrar.RegisterLazy() func was null");
            throw new ArgumentNullException(nameof(func));
        }

        _builder.AddSingleton(service, (provider) => func());
    }
}
