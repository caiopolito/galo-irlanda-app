namespace GaloIrlanda.Api.Common.Auth;

public static class Roles
{
    public const string Admin = "admin";
    public const string Editor = "editor";
    public const string Socio = "socio";
}

public static class Policies
{
    /// <summary>Só administradores (excluir conteúdo, gerenciar usuários).</summary>
    public const string Admin = "admin";

    /// <summary>Editores e administradores (criar e editar conteúdo).</summary>
    public const string Editor = "editor";

    /// <summary>Sócios (área do sócio).</summary>
    public const string Socio = "socio";
}
