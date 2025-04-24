using System;
namespace ProductCatalog
{      
    public class Product      
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public string Category { get; set; }
        public DateTime ManufactureDate { get; set; }

        public Product() { }
        public Product(int id, string name, double price,
                       string category, DateTime manufactureDate)
        {
            Id = id;
            Name = name;
            Price = price;
            Category = category;
            ManufactureDate = manufactureDate;
        }

        public override string ToString()
        {
            return $"{Id,-5} | {Name,-25} | {Category,-15} | {Price,10:C} | " +
                   $"{ManufactureDate:dd.MM.yyyy}";
        }
    }
}






using System;
using System.Collections.Generic;
using System.IO;

namespace ProductCatalog
{
    /// <summary>
    /// Работа с БД в бинарном файле
    /// </summary>
    public class Database
    {
        private readonly string _filePath;
        public Database(string filePath)
        {
            _filePath = filePath;
        }

        public List<Product> ReadDatabase()
        {
            if (!File.Exists(_filePath)) return new List<Product>();

            var result = new List<Product>();
            using var br = new BinaryReader(File.Open(_filePath, FileMode.Open));

            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                result.Add(new Product
                {
                    Id = br.ReadInt32(),
                    Name = br.ReadString(),
                    Price = br.ReadDouble(),
                    Category = br.ReadString(),
                    ManufactureDate = new DateTime(br.ReadInt64())
                });
            }
            return result;
        }

        public void WriteDatabase(IEnumerable<Product> products)
        {
            using var bw = new BinaryWriter(File.Open(_filePath, FileMode.Create));

            var list = new List<Product>(products);   
            bw.Write(list.Count);
            foreach (var p in list)
            {
                bw.Write(p.Id);
                bw.Write(p.Name);
                bw.Write(p.Price);
                bw.Write(p.Category);
                bw.Write(p.ManufactureDate.Ticks);
            }
        }
    }
}






using Input_Validation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ProductCatalog
{
    public class CatalogService
    {
        private readonly Database _db;

        public CatalogService(Database db)
        {
            _db = db;
        }

        public void Add(Product p)
        {
            var list = _db.ReadDatabase();
            list.Add(p);
            _db.WriteDatabase(list);
        }


        public bool RemoveById(int id)
        {
            var list = _db.ReadDatabase();
            var removed = list.RemoveAll(p => p.Id == id) > 0;
            if (removed) _db.WriteDatabase(list);
            return removed;
        }

        public List<Product> GetAll() => _db.ReadDatabase();

        /// 1) Перечень товаров заданной категории (List<Product>)
        public List<Product> GetByCategory(string category)
        {
            var query =
                from p in _db.ReadDatabase()
                where p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
                orderby p.Price ascending
                select p;
            return query.ToList();
        }

        /// 2) Перечень товаров дороже заданной цены (List<Product>)
        public List<Product> GetMoreExpensiveThan(double price)
        {
            var query =
                from p in _db.ReadDatabase()
                where p.Price > price
                orderby p.Price descending
                select p;
            return query.ToList();
        }

        /// 3) Средняя цена всех товаров (одно значение)
        public double GetAveragePrice()
        {
            var prices =
                from p in _db.ReadDatabase()
                select p.Price;
            return prices.DefaultIfEmpty(0).Average();
        }

        /// 4) Кол‑во товаров, выпущенных позже указанной даты (одно значение)
        public int CountManufacturedAfter(DateTime date)
        {
            var query =
                from p in _db.ReadDatabase()
                where p.ManufactureDate > date
                select p;
            return query.Count();
        }
    }
}







using Input_Validation;
using ProductCatalog;
using System.Globalization;

class Program
{
    static readonly string filePath = "products.bin";

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var db = new Database(filePath);
        var service = new CatalogService(db);

        while (true)
        {
            Console.Clear();
            Console.WriteLine("   Каталог продукции   ");
            Console.WriteLine("1) Просмотр всех товаров");
            Console.WriteLine("2) Добавить товар в бд");
            Console.WriteLine("3) Удалить товар из бд");
            Console.WriteLine("4) Товары по категории");
            Console.WriteLine("5) Товары дороже заданной цены");
            Console.WriteLine("6) Средняя цена всех товаров");
            Console.WriteLine("7) Кол-во товаров новее даты");
            Console.WriteLine("0) Выход");
            Console.Write("Выберите пункт: ");

            switch (Console.ReadLine())
            {
                case "1":
                    var allProducts = service.GetAll();
                    if (!allProducts.Any())
                        Console.WriteLine("Ничего не найдено");
                    else
                        foreach (var p in allProducts)
                            Console.WriteLine(p.ToString());
                    break;

                case "2":
                    string name = InputValidation.InputStringWithValidation("Название: ");
                    double price = InputValidation.InputDoubleWithValidation("Цена: ");
                    string category = InputValidation.InputStringWithValidation("Категория: ");

                    while (true)
                    {
                        string s = InputValidation.InputStringWithValidation("Дата производства (дд/мм/гггг): ");
                        if (DateTime.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var date))
                        {
                            var rnd = new Random();
                            var p = new Product(rnd.Next(1000, 9999), name, price, category, date);
                            service.Add(p);
                            Console.WriteLine("Товар добавлен в бд");
                            Console.WriteLine(p.ToString());  // Вывод добавленного продукта
                            break;
                        }
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Неверный формат даты. Пример: 05/09/2023");
                        Console.ResetColor();
                    }
                    break;

                case "3":
                    int id = InputValidation.InputIntWithValidation("id для удаления: ");
                    Console.WriteLine(service.RemoveById(id) ? "Удалено" : "Товар с таким id не найден.");
                    break;

                /// Вывод товаров по категории
                case "4":
                    string cat = InputValidation.InputStringWithValidation("Категория: ");
                    var categoryProducts = service.GetByCategory(cat);
                    if (!categoryProducts.Any())
                        Console.WriteLine("Ничего не найдено");
                    else
                        foreach (var p in categoryProducts)
                            Console.WriteLine(p.ToString());
                    break;

                /// Вывод товаров дороже заданной цены
                case "5":
                    double minPrice = InputValidation.InputDoubleWithValidation("Минимальная цена: ");
                    var expensiveProducts = service.GetMoreExpensiveThan(minPrice);
                    if (!expensiveProducts.Any())
                        Console.WriteLine("Ничего не найдено");
                    else
                        foreach (var p in expensiveProducts)
                            Console.WriteLine(p.ToString());
                    break;

                case "6":
                    Console.WriteLine($"Средняя цена: {service.GetAveragePrice():C}");
                    break;

                case "7":
                    while (true)
                    {
                        string s = InputValidation.InputStringWithValidation("Введите дату (00/00/0000): ");
                        if (DateTime.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture, 
                            DateTimeStyles.None, out var manufactureDate))
                        {
                            Console.WriteLine($"Количество: {service.CountManufacturedAfter(manufactureDate)}");
                            break;
                        }
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Неверный формат даты. Пример: 05/09/2023");
                        Console.ResetColor();
                    }
                    break;

                case "0":
                    return;

                default:
                    Console.WriteLine("Неверный пункт меню");
                    break;
            }

            Console.WriteLine("\nНажмите enter");
            Console.ReadKey();
        }
    }
}










using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Input_Validation
{
    public static class InputValidation
    {
        public static double InputDoubleWithValidation(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Пустой ввод. Повторите ввод.");
                    Console.ResetColor();
                    continue;
                }
                if (input == "0" || input == "0,0")
                {
                    return 0.0;
                }
                if (double.TryParse(input, out double inputNumber))
                {
                    if (input.StartsWith("0") && !input.StartsWith("0,"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Недопустимый ввод. Число не может начинаться с нуля, если оно не равно нулю (за исключением 0 и 0,x).");
                        Console.ResetColor();
                        continue;
                    }
                    if ((input.StartsWith("-0") && !input.StartsWith("-0,")) || input == "-0")
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Недопустимый ввод. Число не может начинаться с нуля, если оно не равно нулю (за исключением -0,x).");
                        Console.ResetColor();
                        continue;
                    }
                    return inputNumber;
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Неверный формат числа. Повторите ввод.");
                Console.ResetColor();
            }
        }

        public static string InputStringWithValidation(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Пустой ввод. Повторите ввод.");
                    Console.ResetColor();
                    continue;
                }
                return input.Trim();
            }
        }

        public static char InputCharWithValidation(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();
                if (string.IsNullOrEmpty(input) || input.Length != 1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Необходимо ввести ровно один символ. Повторите ввод.");
                    Console.ResetColor();
                    continue;
                }
                return input[0];
            }
        }

        public static int InputIntWithValidation(string prompt)
        {
            while (true)
            {
                double value = InputDoubleWithValidation(prompt);
                if (value % 1 == 0) return (int)value;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Введите ЦЕЛОЕ число.");
                Console.ResetColor();
            }
        }
    }
}

