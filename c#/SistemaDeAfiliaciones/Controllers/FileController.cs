using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SistemaDeAfiliaciones.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileController : ControllerBase
    {
private const string ftpServer = "ftp://eu-central-1.sftpcloud.io/testcsv.csv";
        private const string ftpUser = "74c4c2af7364452b8d2af9b88e6c5a5c";
        private const string ftpPassword = "wwZkAP4BSPS2bcrP6qYHFeS61T1D2wPW";
        private const int bufferSize = 4096;

        // Endpoint para subir un archivo
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No se ha seleccionado ningún archivo.");
            }

            // Guardar archivo temporalmente
            var tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Subir archivo al servidor FTP
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                request.UseBinary = true;

                using (FileStream fs = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                using (Stream requestStream = request.GetRequestStream())
                {
                    byte[] buffer = new byte[bufferSize];
                    int bytesRead;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        requestStream.Write(buffer, 0, bytesRead);
                    }
                }

                System.IO.File.Delete(tempFilePath); // Eliminar el archivo temporal
                return Ok("Archivo subido exitosamente.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al subir el archivo: {ex.Message}");
            }
        }

        // Endpoint para descargar el archivo
        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile()
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                request.UseBinary = true;

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (var memoryStream = new MemoryStream())
                {
                    byte[] buffer = new byte[bufferSize];
                    int bytesRead;
                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        memoryStream.Write(buffer, 0, bytesRead);
                    }

                    return File(memoryStream.ToArray(), "application/octet-stream", "testcsv.csv");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al descargar el archivo: {ex.Message}");
            }
        }
    }
}
