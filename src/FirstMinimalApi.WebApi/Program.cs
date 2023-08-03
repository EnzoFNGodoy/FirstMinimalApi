using FirstMinimalApi.WebApi.Data;
using FirstMinimalApi.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MiniValidation;
using NetDevPack.Identity.Interfaces;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddIdentityEntityFrameworkContextConfiguration
    (opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("FirstMinimalApi.WebApi")));

builder.Services.AddDbContext<MinimalContextDb>
    (opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtConfiguration(builder.Configuration)
    .AddNetDevPackIdentity<IdentityUser>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ExcluirFornecedor",
        policy => policy.RequireClaim("ExcluirFornecedor"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API Sample",
        Description = "Developed by Enzo Godoy",
        Contact = new OpenApiContact { Name = "Enzo Godoy", Email = "enzofngodoy@hotmail.com" }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu token}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthConfiguration();

app.UseHttpsRedirection();

app.MapPost("/registro", [AllowAnonymous] async (
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IOptions<AppJwtSettings> appJwtSettings,
    IJwtBuilder jwtBuilder,
    RegisterUser registerUser) =>
{
    if (registerUser is null)
        return Results.BadRequest("Usuário não informado.");

    if (!MiniValidator.TryValidate(registerUser, out var errors))
        return Results.ValidationProblem(errors);

    var user = new IdentityUser
    {
        UserName = registerUser.Email,
        Email = registerUser.Email,
        EmailConfirmed = true
    };

    var result = await userManager.CreateAsync(user, registerUser.Password);

    if (!result.Succeeded)
        return Results.BadRequest(result.Errors);

    var jwt = new JwtBuilder()
    .WithUserManager(userManager)
    .WithJwtSettings(appJwtSettings.Value)
    .WithEmail(user.Email)
    .WithJwtClaims()
    .WithUserClaims()
    .WithUserRoles()
    .BuildUserResponse();

    return Results.Ok(jwt);
})
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("RegistroUsuario")
    .WithTags("Usuario");

app.MapPost("/login", [AllowAnonymous] async (
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IOptions<AppJwtSettings> appJwtSettings,
    IJwtBuilder jwtBuilder,
    LoginUser loginUser) =>
{
    if (loginUser is null)
        return Results.BadRequest("Usuário não informado.");

    if (!MiniValidator.TryValidate(loginUser, out var errors))
        return Results.ValidationProblem(errors);

    var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

    if (result.IsLockedOut)
        return Results.BadRequest("Usuário bloqueado.");

    if (!result.Succeeded)
        return Results.BadRequest("Usuário ou senha inválidos.");

    var jwt = new JwtBuilder()
    .WithUserManager(userManager)
    .WithJwtSettings(appJwtSettings.Value)
    .WithEmail(loginUser.Email)
    .WithJwtClaims()
    .WithUserClaims()
    .WithUserRoles()
    .BuildUserResponse();

    return Results.Ok(jwt);
})
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("LoginUsuario")
    .WithTags("Usuario");

app.MapGet("/fornecedores", [AllowAnonymous] async (MinimalContextDb context)
    => await context.Fornecedores.ToListAsync())
    .WithName("GetFornecedores")
    .WithTags("Fornecedor");

app.MapGet("/fornecedores/{id}", [AllowAnonymous] async (Guid id, MinimalContextDb context)
    => await context.Fornecedores.FindAsync(id) is Fornecedor fornecedor
    ? Results.Ok(fornecedor)
    : Results.NotFound())
    .Produces<Fornecedor>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetFornecedoresPorId")
    .WithTags("Fornecedor");

app.MapPost("/fornecedores", [Authorize] async (MinimalContextDb context, Fornecedor fornecedor) =>
    {
        if (!MiniValidator.TryValidate(fornecedor, out var errors))
            return Results.ValidationProblem(errors);

        await context.Fornecedores.AddAsync(fornecedor);
        var result = await context.SaveChangesAsync();

        return result > 0
        ? Results.CreatedAtRoute("GetFornecedoresPorId", new { id = fornecedor.Id }, fornecedor)
        : Results.BadRequest("Houve um erro ao salvar o fornecedor.");
    })
    .ProducesValidationProblem()
    .Produces<Fornecedor>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("PostFornecedor")
    .WithTags("Fornecedor");

app.MapPut("/fornecedores/{id}", [Authorize] async (Guid id, MinimalContextDb context, Fornecedor fornecedor) =>
{
    var fornecedorExistente = await context.Fornecedores
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == id);

    if (fornecedorExistente is null)
        return Results.NotFound();

    if (!MiniValidator.TryValidate(fornecedor, out var errors))
        return Results.ValidationProblem(errors);

    context.Fornecedores.Update(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0
    ? Results.NoContent()
    : Results.BadRequest("Houve um erro ao editar o fornecedor.");
})
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("PutFornecedor")
    .WithTags("Fornecedor");

app.MapDelete("/fornecedores/{id}", [Authorize] async (Guid id, MinimalContextDb context) =>
{
    var fornecedorExistente = await context.Fornecedores.FindAsync(id);

    if (fornecedorExistente is null)
        return Results.NotFound();

    context.Fornecedores.Remove(fornecedorExistente);
    var result = await context.SaveChangesAsync();

    return result > 0
    ? Results.NoContent()
    : Results.BadRequest("Houve um erro ao deletar o fornecedor.");
})
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization("ExcluirFornecedor")
    .WithName("DeleteFornecedor")
    .WithTags("Fornecedor");

app.Run();