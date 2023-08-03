using FirstMinimalApi.WebApi.Data;
using FirstMinimalApi.WebApi.Models;
using Microsoft.EntityFrameworkCore;
using MiniValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MinimalContextDb>
    (opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/fornecedores", async (MinimalContextDb context)
    => await context.Fornecedores.ToListAsync())
    .WithName("GetFornecedores")
    .WithTags("Fornecedor");

app.MapGet("/fornecedores/{id}", async (Guid id, MinimalContextDb context)
    => await context.Fornecedores.FindAsync(id) is Fornecedor fornecedor
    ? Results.Ok(fornecedor)
    : Results.NotFound())
    .Produces<Fornecedor>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetFornecedoresPorId")
    .WithTags("Fornecedor");

app.MapPost("/fornecedores", async (MinimalContextDb context, Fornecedor fornecedor) =>
    {
        if (!MiniValidator.TryValidate(fornecedor, out var errors))
            return Results.ValidationProblem(errors);

        await context.Fornecedores.AddAsync(fornecedor);
        var result = await context.SaveChangesAsync();

        return result > 0
        ? Results.CreatedAtRoute("GetFornecedoresPorId", new { id = fornecedor.Id }, fornecedor)
        : Results.BadRequest("Houve um erro ao salvar o fornecedor.");
    })
    .Produces<Fornecedor>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("PostFornecedor")
    .WithTags("Fornecedor");

app.Run();