using System;
using System.Windows;
using MyHippocrates.Data;
using MyHippocrates.Models;
using MyHippocrates.ViewModels;

namespace MyHippocrates.Views
{
    public partial class EditDialog : Window
    {
        private readonly AppDbContext _ctx;
        private readonly bool _isNew;

        // Для OrderItem и StockBalance нам нужно помнить старый ключ (составной PK)
        private int _oldReceiptId, _oldProductId;
        private int _oldPharmacyId, _oldStockProductId;

        public EditDialog(object dataContext, AppDbContext ctx, bool isNew)
        {
            InitializeComponent();
            DataContext = dataContext;
            _ctx = ctx;
            _isNew = isNew;

            // Запоминаем старые ключи для составных PK
            if (dataContext is OrderItemEditorViewModel ovm && !isNew)
            {
                _oldReceiptId = ovm.Entity.ReceiptId;
                _oldProductId = ovm.Entity.ProductId;
            }
            if (dataContext is StockBalanceEditorViewModel svm && !isNew)
            {
                _oldPharmacyId = svm.Entity.PharmacyId;
                _oldStockProductId = svm.Entity.ProductId;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Извлекаем реальную сущность из обёртки
            var entity = DataContext switch
            {
                ProductEditorViewModel vm => (object)vm.Entity,
                ReceiptEditorViewModel vm => vm.Entity,
                OrderItemEditorViewModel vm => vm.Entity,
                StockBalanceEditorViewModel vm => vm.Entity,
                _ => DataContext
            };

            // Валидация
            var error = Validate(entity);
            if (error != null)
            {
                MessageBox.Show(error, "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ExecuteProcedure(entity);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += "\n\n" + ex.InnerException.Message;
                if (ex.InnerException?.InnerException != null)
                    msg += "\n" + ex.InnerException.InnerException.Message;
                MessageBox.Show(msg, "Ошибка сохранения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Вызывает нужную процедуру (Add или Update) в зависимости от типа сущности.
        /// </summary>
        private void ExecuteProcedure(object entity)
        {
            switch (entity)
            {
                case Manufacturer m:
                    if (_isNew) m.Id = DbProcedures.AddManufacturer(_ctx, m);
                    else DbProcedures.UpdateManufacturer(_ctx, m);
                    break;

                case Product p:
                    if (_isNew) p.Id = DbProcedures.AddProduct(_ctx, p);
                    else DbProcedures.UpdateProduct(_ctx, p);
                    break;

                case Pharmacy ph:
                    if (_isNew) ph.Id = DbProcedures.AddPharmacy(_ctx, ph);
                    else DbProcedures.UpdatePharmacy(_ctx, ph);
                    break;

                case Employee emp:
                    if (_isNew) emp.Id = DbProcedures.AddEmployee(_ctx, emp);
                    else DbProcedures.UpdateEmployee(_ctx, emp);
                    break;

                case Receipt r:
                    if (_isNew) r.Id = DbProcedures.AddReceipt(_ctx, r);
                    else DbProcedures.UpdateReceipt(_ctx, r);
                    break;

                case OrderItem o:
                    if (_isNew) DbProcedures.AddOrderItem(_ctx, o);
                    else DbProcedures.UpdateOrderItem(_ctx, _oldReceiptId, _oldProductId, o);
                    break;

                case StockBalance s:
                    if (_isNew) DbProcedures.AddStockBalance(_ctx, s);
                    else DbProcedures.UpdateStockBalance(_ctx, _oldPharmacyId, _oldStockProductId, s);
                    break;
            }
        }

        // ─── Валидация (без изменений) ────────────────────────────

        private static string? Validate(object entity) => entity switch
        {
            Manufacturer m => ValidateManufacturer(m),
            Pharmacy p => ValidatePharmacy(p),
            Employee e => ValidateEmployee(e),
            Product p => ValidateProduct(p),
            Receipt r => ValidateReceipt(r),
            OrderItem o => ValidateOrderItem(o),
            StockBalance s => ValidateStockBalance(s),
            _ => null
        };

        private static string? ValidateManufacturer(Manufacturer m)
        {
            if (string.IsNullOrWhiteSpace(m.Name)) return "Введите название производителя.";
            if (string.IsNullOrWhiteSpace(m.Country)) return "Введите страну.";
            if (string.IsNullOrWhiteSpace(m.Address)) return "Введите адрес.";
            if (string.IsNullOrWhiteSpace(m.Phone)) return "Введите телефон.";
            if (!m.Phone.StartsWith("+")) return "Телефон должен начинаться с '+'.";
            if (string.IsNullOrWhiteSpace(m.Email)) return "Введите Email.";
            if (!m.Email.Contains("@")) return "Email должен содержать символ '@'.";
            return null;
        }

        private static string? ValidatePharmacy(Pharmacy p)
        {
            if (string.IsNullOrWhiteSpace(p.Address)) return "Введите адрес аптеки.";
            if (string.IsNullOrWhiteSpace(p.Phone)) return "Введите телефон.";
            if (!p.Phone.StartsWith("+")) return "Телефон должен начинаться с '+'.";
            if (string.IsNullOrWhiteSpace(p.WorkingHours)) return "Введите часы работы.";
            return null;
        }

        private static string? ValidateEmployee(Employee e)
        {
            if (string.IsNullOrWhiteSpace(e.FullName)) return "Введите ФИО сотрудника.";
            if (string.IsNullOrWhiteSpace(e.Idnp)) return "Введите IDNP.";
            if (e.Idnp.Length != 13) return "IDNP должен содержать ровно 13 символов.";
            if (!IsAllDigits(e.Idnp)) return "IDNP должен содержать только цифры.";
            if (string.IsNullOrWhiteSpace(e.Phone)) return "Введите телефон.";
            if (!e.Phone.StartsWith("+")) return "Телефон должен начинаться с '+'.";
            if (string.IsNullOrWhiteSpace(e.Position)) return "Введите должность.";
            if (e.Salary <= 0) return "Зарплата должна быть больше нуля.";
            return null;
        }

        private static string? ValidateProduct(Product p)
        {
            if (string.IsNullOrWhiteSpace(p.Name)) return "Введите название товара.";
            if (p.ManufacturerId == 0) return "Выберите производителя.";
            if (p.PurchasePrice <= 0) return "Закупочная цена должна быть больше нуля.";
            if (p.SalePrice <= 0) return "Розничная цена должна быть больше нуля.";
            if (p.SalePrice < p.PurchasePrice) return "Розничная цена не может быть меньше закупочной.";
            if (p.ProductionDate > DateTime.UtcNow) return "Дата производства не может быть в будущем";
            if (p.ExpirationDate <= p.ProductionDate) return "Срок годности должен быть позже даты производства.";
            if (string.IsNullOrWhiteSpace(p.Unit)) return "Введите единицу измерения.";
            return null;
        }

        private static string? ValidateReceipt(Receipt r)
        {
            if (r.PharmacyId == 0) return "Выберите аптеку.";
            if (r.EmployeeId == 0) return "Выберите сотрудника.";
            return null;
        }

        private static string? ValidateOrderItem(OrderItem o)
        {
            if (o.ReceiptId == 0) return "Выберите чек.";
            if (o.ProductId == 0) return "Выберите товар.";
            if (o.Quantity <= 0) return "Количество должно быть больше нуля.";
            if (o.Discount < 0 || o.Discount > 100) return "Скидка должна быть от 0 до 100%.";
            return null;
        }

        private static string? ValidateStockBalance(StockBalance s)
        {
            if (s.PharmacyId == 0) return "Выберите аптеку.";
            if (s.ProductId == 0) return "Выберите товар.";
            if (s.RemainingQty < 0) return "Остаток не может быть отрицательным.";
            return null;
        }

        private static bool IsAllDigits(string s)
        {
            foreach (var c in s) if (!char.IsDigit(c)) return false;
            return true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}