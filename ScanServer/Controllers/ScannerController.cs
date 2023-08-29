using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NAPS2.Scan;
using NAPS2.Images;
using System.Drawing;
using NAPS2.Images.Gdi;
using System.IO;

namespace ScanServer.Controllers
{

    [ApiController]
    [Route("api")]
    public class ScannerController : ControllerBase
    {

        private readonly ILogger<ScannerController> _logger;

        private readonly ScanController _scanController;

        public ScannerController(ILogger<ScannerController> logger)
        {
            this._logger = logger;
            using ScanningContext scanningContext = new ScanningContext(new GdiImageContext());
            scanningContext.SetUpWin32Worker();
            _scanController = new ScanController(scanningContext);
        }

        [HttpGet("scanners")]
        public async Task<ActionResult<List<ScanDevice>>> ListDevices()
        {
            _logger.LogDebug($"{nameof(ListDevices)}");
            // TODO based on env Var change driver
            // TODO set 32 or 64 based on OS
            //var devices = await _scanController.GetDeviceList(new ScanOptions { Driver = Driver.Twain, TwainOptions = { Dsm = TwainDsm.Old, Adapter = TwainAdapter.Legacy } });
            var devices = await _scanController.GetDeviceList();
            return Ok(devices);
        }

        [HttpGet("scan/{deviceId}")]
        public async Task<ActionResult> Scan([FromRoute] string deviceId)
        {
            _logger.LogDebug($"{nameof(Scan)}");
            var devices = await _scanController.GetDeviceList();
            ScanDevice device = devices.Find(d => d.ID == deviceId);
            if (device == null)
            {
                return NotFound();
            }
            var scannedImages = _scanController.Scan(new ScanOptions { Device = device, Dpi = 200 }).ToBlockingEnumerable().ToList();
            if (scannedImages.Count == 0)
            {
                return NotFound();
            }
            return processedImageToFile(scannedImages[0]);
        }

        private FileContentResult processedImageToFile(ProcessedImage processedImage)
        {
            var stream = processedImage.Render().SaveToMemoryStream(ImageFileFormat.Jpeg);
            return File(stream.ToArray(), "image/jpeg");
        }
    }
}
