using Microsoft.AspNetCore.Mvc;
using ProjectBigData.Services;
namespace ProjectBigData.Controllers
{
    public class DataImportController:Controller
    {
        private readonly DataImportService _dataImportService;


        public DataImportController(IWebHostEnvironment environment)
        {
            // تحديد مسار الملف في مجلد Data
            string filePath = Path.Combine(environment.ContentRootPath, "Data", "word.txt");
            string connectionString = "Server=DESKTOP-9AEHMMU\\UDEMY;Database=BigData;Trusted_Connection=True;TrustServerCertificate=True;";
            _dataImportService = new DataImportService(connectionString, filePath);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Import()
        {
            var (insertedCount, errors) = _dataImportService.ImportData();

            string message;
            if (insertedCount > 0)
            {
                message = $"تم تخزين البيانات بنجاح في جدول words! عدد السجلات المُدخلة: {insertedCount}";
            }
            else
            {
                message = "لم يتم إدخال أي سجلات إلى جدول world. تحقق من الملف أو تنسيق البيانات.";
            }

            if (errors.Count > 0)
            {
                message += "<br>" + string.Join("<br>", errors);
            }

            ViewBag.Message = message;
            return View("Index");
        }
     
}
}
