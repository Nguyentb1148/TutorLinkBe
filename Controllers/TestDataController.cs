using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using TutorLinkBe.Models;
using TutorLinkBe.Services;

namespace TutorLinkBe.Controllers;

// Testing MongoDB connection by using TestItem documents
[ApiController]
[Route("api/test-data")]
public class TestDataController : ControllerBase
{
    private readonly MongoDbService _mongoDbService;

    public TestDataController(MongoDbService mongoDbService)
    {
        _mongoDbService = mongoDbService;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<TestItem>> Create([FromBody] TestItem item)
    {
        item.Id = string.IsNullOrWhiteSpace(item.Id) ? ObjectId.GenerateNewId().ToString() : item.Id;
        await _mongoDbService.TestItems.InsertOneAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TestItem>>> GetAll()
    {
        var items = await _mongoDbService.TestItems.Find(FilterDefinition<TestItem>.Empty).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestItem>> GetById(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return NotFound();
        }

        var item = await _mongoDbService.TestItems.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] TestItem update)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return NotFound();
        }

        var updateDef = Builders<TestItem>.Update
            .Set(x => x.Name, update.Name)
            .Set(x => x.Description, update.Description);

        var result = await _mongoDbService.TestItems.UpdateOneAsync(x => x.Id == id, updateDef);
        if (result.MatchedCount == 0)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return NotFound();
        }

        var result = await _mongoDbService.TestItems.DeleteOneAsync(x => x.Id == id);
        if (result.DeletedCount == 0)
        {
            return NotFound();
        }

        return NoContent();
    }
}