using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrilhaNetAzureDesafio.Context;
using TrilhaNetAzureDesafio.Models;

namespace TrilhaNetAzureDesafio.Controllers;

[ApiController]
[Route("[controller]")]
public class FuncionarioController : ControllerBase
{
    private readonly RHContext _context;
    private readonly string _connectionString;
    private readonly string _tableName;

    public FuncionarioController(RHContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetValue<string>("ConnectionStrings:SAConnectionString");
        _tableName = configuration.GetValue<string>("ConnectionStrings:AzureTableName");
    }

    private TableClient GetTableClient()
    {
        var serviceClient = new TableServiceClient(_connectionString);
        var tableClient = serviceClient.GetTableClient(_tableName);

        tableClient.CreateIfNotExists();
        return tableClient;
    }

    [HttpGet("{id}")]
    public IActionResult ObterPorId(int id)
    {
        var funcionario = _context.Funcionarios.Find(id);

        if (funcionario == null)
            return NotFound();

        return Ok(funcionario);
    }

   [HttpPost]
public IActionResult Criar(Funcionario funcionario)
{
    _context.Funcionarios.Add(funcionario);
    
    try
    {
        _context.SaveChanges(); // Salva o funcionário no Banco SQL
    }
    catch (DbUpdateException)
    {
        // Lida com erros de salvamento, se necessário
        return BadRequest("Ocorreu um erro ao salvar o funcionário no Banco SQL.");
    }

    var tableClient = GetTableClient();
    var funcionarioLog = new FuncionarioLog(funcionario, TipoAcao.Inclusao, funcionario.Departamento, Guid.NewGuid().ToString());

    // Salva no Azure Table
    UpsertEntity(tableClient, funcionarioLog);

    return CreatedAtAction(nameof(ObterPorId), new { id = funcionario.Id }, funcionario);
}

    private void UpsertEntity(TableClient tableClient, FuncionarioLog funcionarioLog)
    {
        throw new NotImplementedException();
    }

    [HttpPut("{id}")]
public IActionResult Atualizar(int id, Funcionario funcionario)
{
    var funcionarioBanco = _context.Funcionarios.Find(id);

    if (funcionarioBanco == null)
        return NotFound();

    // Atualize as propriedades específicas que você deseja
    funcionarioBanco.Nome = funcionario.Nome;
    funcionarioBanco.Endereco = funcionario.Endereco;
    
    // TODO: Lidar com outras propriedades incompletas, se necessário

    // Atualize o funcionário no Banco SQL
    _context.SaveChanges();

    var tableClient = GetTableClient();
    var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Atualizacao, funcionarioBanco.Departamento, Guid.NewGuid().ToString());

    // Salve as informações atualizadas no Azure Table
    UpsertEntity(tableClient, funcionarioLog);

    return Ok();
}


    [HttpDelete("{id}")]
public IActionResult Deletar(int id)
{
    var funcionarioBanco = _context.Funcionarios.Find(id);

    if (funcionarioBanco == null)
        return NotFound();

    // Remova o funcionário do Banco SQL
    _context.Funcionarios.Remove(funcionarioBanco);
    _context.SaveChanges();

    var tableClient = GetTableClient();
    var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Remocao, funcionarioBanco.Departamento, Guid.NewGuid().ToString());

    // Salve as informações de funcionarioLog no Azure Table
    UpsertEntity(tableClient, funcionarioLog);

    return NoContent();
}


}
