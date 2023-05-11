using System.Diagnostics.CodeAnalysis;
using System.Web;
using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS14.MapServer.Models;
using SS14.MapServer.Models.Entities;
using SS14.MapServer.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SS14.MapServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapController : ControllerBase
    {
        private readonly Context _context;
        private readonly FileUploadService _fileUploadService;

        public MapController(Context context, FileUploadService fileUploadService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
        }

        // GET: api/Map
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Map>>> GetMaps()
        {
            if (_context.Maps == null)
            {
                return NotFound();
            }
            return await _context.Maps.ToListAsync();
        }

        // GET: api/Map/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Map>> GetMap(string id)
        {
            if (_context.Maps == null)
            {
                return NotFound();
            }
            var map = await _context.Maps.FindAsync(id);

            if (map == null)
            {
                return NotFound();
            }

            return map;
        }

        // PUT: api/Map/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        public async Task<ActionResult<Map>> PutMap(string id, [FromForm, ModelBinder(BinderType = typeof(JsonModelBinder)), SwaggerRequestBody] Map map, IList<IFormFile> images)
        {
            if (id != map.Id)
                return BadRequest("The id provided in the path and the map id don't match");

            if (ValidateMapRequest(map, images, out var error))
                return error;
            
            _fileUploadService.UploadGridImages(map, images);
            
            if (!MapExists(id))
                return await CreateMap(map);

            _context.Entry(map).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent();
        }

        private bool ValidateMapRequest(Map map, IList<IFormFile> images, [NotNullWhen(true)] out BadRequestObjectResult? error)
        {
            if (map.Grids.Count != images.Count)
            {
                error = BadRequest("Amount of uploaded images doesn't match the amount of grids");
                return true;
            }

            var gridIds = map.Grids.Select(grid => grid.GridId).ToList();

            foreach (var image in images)
            {
                //TODO: Check if the extension is allowed

                var name = Path.GetFileNameWithoutExtension(image.FileName);
                if (!int.TryParse(name, out var gridId) || !gridIds.Contains(gridId))
                {
                    error = BadRequest($"At least one filename doesn't match any grid id: {HttpUtility.HtmlEncode(name)}");
                    return true;
                }
            }

            error = null;
            return false;
        }

        // POST: api/Map
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        public async Task<ActionResult<Map>> PostMap([FromForm, ModelBinder(BinderType = typeof(JsonModelBinder))] Map map, IList<IFormFile> images)
        {
            if (MapExists(map.Id))
                return Conflict();

            if (ValidateMapRequest(map, images, out var error))
                return error;
            
            var result = await CreateMap(map);

            if (result.Value != null)
                _fileUploadService.UploadGridImages(result.Value, images);
                
            return result;
        }

        // DELETE: api/Map/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMap(string id)
        {
            if (_context.Maps == null)
            {
                return NotFound();
            }
            var map = await _context.Maps.FindAsync(id);
            if (map == null)
            {
                return NotFound();
            }

            _context.Maps.Remove(map);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<ActionResult<Map>> CreateMap(Map map)
        {
            if (_context.Maps == null)
                return Problem("Entity set 'Context.Maps'  is null.");

            _context.Maps.Add(map);
            
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetMap", new { id = map.Id }, map);
        }

        private bool MapExists(string id)
        {
            return (_context.Maps?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
