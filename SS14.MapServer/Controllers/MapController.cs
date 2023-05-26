using System.Diagnostics.CodeAnalysis;
using System.Web;
using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SS14.MapServer.Helpers;
using SS14.MapServer.Models;
using SS14.MapServer.Models.Entities;
using SS14.MapServer.Services;
using SS14.MapServer.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SS14.MapServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapController : ControllerBase
    {
        private readonly Context _context;
        private readonly FileUploadService _fileUploadService;
        private readonly IJobSchedulingService _schedulingService;

        public MapController(Context context, FileUploadService fileUploadService, IJobSchedulingService schedulingService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _schedulingService = schedulingService;
        }

        // GET: api/Map
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Map>>> GetMaps()
        {
            if (_context.Maps == null)
            {
                return NotFound();
            }

            return await _context.Maps.Include(map => map.Grids).ToListAsync();
        }
        
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<Map>> GetMap(string id)
        {
            if (_context.Maps == null)
            {
                return NotFound();
            }
            var map = await FindMapWithGrids(id);

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
            
            await _fileUploadService.UploadGridImages(map, images);
            
            if (!MapExists(id))
                return await CreateMap(map);

            _context.Entry(map).State = EntityState.Modified;

            foreach (var grid in map.Grids)
            {
                _context.Entry(grid).State = EntityState.Modified;
            }
            
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
                await _fileUploadService.UploadGridImages(result.Value, images);
                
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

        [HttpPost("sync")]
        [Consumes("application/json")]
        public async Task<IActionResult> SyncMaps(List<string> mapFileNames)
        {
            var data = new JobDataMap
            {
                {Jobs.SyncMaps.MapListKey, mapFileNames}
            };
            
            await _schedulingService.RunJob<Jobs.SyncMaps>(nameof(Jobs.SyncMaps), "Sync", data);
            return Ok();
        }

        private async Task<Map?> FindMapWithGrids(string id)
        {
            return await _context.Maps!
                .Include(map => map.Grids)
                .Where(map => map.Id.Equals(id))
                .SingleOrDefaultAsync();
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
