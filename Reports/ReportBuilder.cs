using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using MyHippocrates.Data;
using MyHippocrates.Models;

namespace MyHippocrates.Reports
{
    /// <summary>
    /// Строит FlowDocument-отчёты для трёх типов аналитики.
    /// Все стили задаются программно — никаких внешних словарей не нужно.
    /// </summary>
    public static class ReportBuilder
    {
        // ── Цветовая палитра ─────────────────────────────────────
        private static readonly SolidColorBrush HeaderBg = new(Color.FromRgb(27, 94, 32));
        private static readonly SolidColorBrush SubHeaderBg = new(Color.FromRgb(46, 125, 50));
        private static readonly SolidColorBrush TableHead = new(Color.FromRgb(232, 245, 233));
        private static readonly SolidColorBrush AltRow = new(Color.FromRgb(247, 251, 247));
        private static readonly SolidColorBrush WarnBg = new(Color.FromRgb(255, 243, 224));
        private static readonly SolidColorBrush DangerBg = new(Color.FromRgb(255, 205, 210));
        private static readonly SolidColorBrush TextDark = new(Color.FromRgb(28, 43, 28));
        private static readonly SolidColorBrush TextGray = new(Color.FromRgb(96, 125, 139));
        private static readonly SolidColorBrush White = Brushes.White;
        private static readonly SolidColorBrush BorderBrush = new(Color.FromRgb(200, 222, 200));

        // ══════════════════════════════════════════════════════════
        // ОТЧЁТ 1 — Сводка продаж за период
        // ══════════════════════════════════════════════════════════
        public static FlowDocument BuildSalesReport(
            AppDbContext ctx,
            DateTime dateFrom, DateTime dateTo,
            string pharmacyFilter,   // "Все аптеки" или адрес
            string categoryFilter)   // "Все товары" | "По рецепту" | "Без рецепта"
        {
            var doc = CreateDocument();

            // Шапка
            AddHeader(doc, "Отчёт по продажам за период",
                $"Период: {dateFrom:dd.MM.yyyy} — {dateTo:dd.MM.yyyy}   |   " +
                $"Аптека: {pharmacyFilter}   |   Категория: {categoryFilter}");

            // Загрузка данных
            var dfUtc = DateTime.SpecifyKind(dateFrom.Date, DateTimeKind.Utc);
            var dtUtc = DateTime.SpecifyKind(dateTo.Date.AddDays(1), DateTimeKind.Utc);

            var receiptsQ = ctx.Receipts
                .Include(r => r.Pharmacy).Include(r => r.Employee)
                .Include(r => r.OrderItems).ThenInclude(oi => oi.Product)
                .Where(r => r.Date >= dfUtc && r.Date < dtUtc);

            if (pharmacyFilter != "Все аптеки")
                receiptsQ = receiptsQ.Where(r => r.Pharmacy!.Address == pharmacyFilter);

            var receipts = receiptsQ.OrderBy(r => r.Date).ThenBy(r => r.ReceiptNumber).ToList();

            if (categoryFilter != "Все товары")
            {
                bool rx = categoryFilter == "По рецепту";
                receipts = receipts.Where(r =>
                    r.OrderItems.Any(oi => oi.Product?.PrescriptionRequired == rx)).ToList();
            }

            // ── KPI-блок ─────────────────────────────────────────
            var totalRev = receipts.Sum(r => r.TotalAmount);
            var receiptCnt = receipts.Count;
            var avgReceipt = receiptCnt > 0 ? receipts.Average(r => r.TotalAmount) : 0;
            var maxReceipt = receiptCnt > 0 ? receipts.Max(r => r.TotalAmount) : 0;
            var minReceipt = receiptCnt > 0 ? receipts.Min(r => r.TotalAmount) : 0;
            var totalItems = receipts.SelectMany(r => r.OrderItems).Sum(oi => oi.Quantity);

            AddSectionTitle(doc, "Ключевые показатели");
            var kpiTable = CreateTable(doc, new[] { "*", "*", "*", "*", "*", "*" });
            AddTableRow(kpiTable, true,
                "Общая выручка", "Кол-во чеков", "Средний чек",
                "Мин. чек", "Макс. чек", "Единиц продано");
            AddTableRow(kpiTable, false,
                $"{totalRev:F2} MDL", $"{receiptCnt}",
                $"{avgReceipt:F2} MDL", $"{minReceipt:F2} MDL",
                $"{maxReceipt:F2} MDL", $"{totalItems}");

            // ── Выручка по аптекам ────────────────────────────────
            AddSectionTitle(doc, "Выручка по аптекам");
            var byPharmacy = receipts
                .GroupBy(r => r.Pharmacy?.Address ?? "—")
                .Select(g => new {
                    Address = g.Key,
                    Revenue = g.Sum(r => r.TotalAmount),
                    Count = g.Count(),
                    Avg = g.Average(r => r.TotalAmount)
                })
                .OrderByDescending(x => x.Revenue).ToList();

            var phTable = CreateTable(doc, new[] { "3*", "*", "*", "*" });
            AddTableRow(phTable, true, "Аптека", "Выручка (MDL)", "Чеков", "Средний чек");
            bool alt = false;
            foreach (var ph in byPharmacy)
            {
                AddTableRow(phTable, false,
                    ph.Address, $"{ph.Revenue:F2}", $"{ph.Count}", $"{ph.Avg:F2}",
                    alt ? AltRow : White);
                alt = !alt;
            }
            AddTotalRow(phTable, "ИТОГО", $"{totalRev:F2}", $"{receiptCnt}", "");

            // ── Список чеков ─────────────────────────────────────
            AddSectionTitle(doc, "Детализация чеков");
            var rTable = CreateTable(doc, new[] { "*", "2*", "2*", "*", "*", "*" });
            AddTableRow(rTable, true,
                "№ чека", "Аптека", "Сотрудник",
                "Сумма", "Дата", "Время");
            alt = false;
            foreach (var r in receipts)
            {
                AddTableRow(rTable, false,
                    $"{r.ReceiptNumber}",
                    r.Pharmacy?.Address ?? "—",
                    r.Employee?.FullName ?? "—",
                    $"{r.TotalAmount:F2}",
                    $"{r.Date:dd.MM.yyyy}",
                    $"{r.Time:hh\\:mm}",
                    alt ? AltRow : White);
                alt = !alt;
            }

            AddFooter(doc);
            return doc;
        }

        // ══════════════════════════════════════════════════════════
        // ОТЧЁТ 2 — Анализ товаров
        // ══════════════════════════════════════════════════════════
        public static FlowDocument BuildProductsReport(
            AppDbContext ctx,
            DateTime dateFrom, DateTime dateTo,
            string categoryFilter,
            int topN)
        {
            var doc = CreateDocument();

            AddHeader(doc, "Анализ продаж по товарам",
                $"Период: {dateFrom:dd.MM.yyyy} — {dateTo:dd.MM.yyyy}   |   " +
                $"Категория: {categoryFilter}   |   Топ {topN}");

            var dfUtc = DateTime.SpecifyKind(dateFrom.Date, DateTimeKind.Utc);
            var dtUtc = DateTime.SpecifyKind(dateTo.Date.AddDays(1), DateTimeKind.Utc);

            var itemsQ = ctx.OrderItems
                .Include(oi => oi.Product).ThenInclude(p => p!.Manufacturer)
                .Include(oi => oi.Receipt)
                .Where(oi => oi.Receipt!.Date >= dfUtc && oi.Receipt.Date < dtUtc);

            if (categoryFilter == "По рецепту")
                itemsQ = itemsQ.Where(oi => oi.Product!.PrescriptionRequired);
            else if (categoryFilter == "Без рецепта")
                itemsQ = itemsQ.Where(oi => !oi.Product!.PrescriptionRequired);

            var items = itemsQ.ToList();

            // ── Сводка по категориям ──────────────────────────────
            AddSectionTitle(doc, "Сводка по категориям");
            var cats = items
                .GroupBy(oi => oi.Product?.PrescriptionRequired == true ? "По рецепту" : "Без рецепта")
                .Select(g => new {
                    Cat = g.Key,
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    Qty = g.Sum(oi => oi.Quantity),
                    Disc = g.Any() ? g.Average(oi => oi.Discount) : 0
                }).ToList();

            var catTable = CreateTable(doc, new[] { "2*", "*", "*", "*" });
            AddTableRow(catTable, true, "Категория", "Выручка (MDL)", "Кол-во ед.", "Ср. скидка %");
            foreach (var c in cats)
                AddTableRow(catTable, false,
                    c.Cat, $"{c.Revenue:F2}", $"{c.Qty}", $"{c.Disc:F2}");

            // ── Топ N товаров ─────────────────────────────────────
            AddSectionTitle(doc, $"Топ {topN} товаров по выручке");
            var topProds = items
                .GroupBy(oi => new {
                    oi.ProductId,
                    Name = oi.Product?.Name ?? "?",
                    Manuf = oi.Product?.Manufacturer?.Name ?? "—",
                    Rx = oi.Product?.PrescriptionRequired ?? false,
                    Price = oi.UnitPrice
                })
                .Select(g => new {
                    g.Key.Name,
                    g.Key.Manuf,
                    g.Key.Rx,
                    g.Key.Price,
                    TotalQty = g.Sum(oi => oi.Quantity),
                    TotalRev = g.Sum(oi => oi.TotalPrice),
                    AvgDisc = g.Any() ? g.Average(oi => oi.Discount) : 0
                })
                .OrderByDescending(x => x.TotalRev)
                .Take(topN).ToList();

            var pTable = CreateTable(doc, new[] { "3*", "2*", "*", "*", "*", "*" });
            AddTableRow(pTable, true,
                "Товар", "Производитель", "Рецепт",
                "Кол-во", "Выручка (MDL)", "Ср. скидка %");

            bool alt = false;
            int rank = 1;
            foreach (var p in topProds)
            {
                AddTableRow(pTable, false,
                    $"{rank++}. {p.Name}", p.Manuf,
                    p.Rx ? "Да" : "Нет",
                    $"{p.TotalQty}",
                    $"{p.TotalRev:F2}",
                    $"{p.AvgDisc:F2}",
                    alt ? AltRow : White);
                alt = !alt;
            }

            // ── Итог ─────────────────────────────────────────────
            var totalRev = items.Sum(oi => oi.TotalPrice);
            var totalQty = items.Sum(oi => oi.Quantity);
            AddTotalRow(pTable, "ИТОГО", "", "", $"{totalQty}", $"{totalRev:F2}", "");

            // ── Производители ─────────────────────────────────────
            AddSectionTitle(doc, "Выручка по производителям");
            var byManuf = items
                .GroupBy(oi => oi.Product?.Manufacturer?.Name ?? "—")
                .Select(g => new {
                    Manuf = g.Key,
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    Qty = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.Revenue).ToList();

            var mTable = CreateTable(doc, new[] { "3*", "*", "*" });
            AddTableRow(mTable, true, "Производитель", "Выручка (MDL)", "Кол-во ед.");
            alt = false;
            foreach (var m in byManuf)
            {
                AddTableRow(mTable, false,
                    m.Manuf, $"{m.Revenue:F2}", $"{m.Qty}",
                    alt ? AltRow : White);
                alt = !alt;
            }

            AddFooter(doc);
            return doc;
        }

        // ══════════════════════════════════════════════════════════
        // ОТЧЁТ 3 — Остатки склада
        // ══════════════════════════════════════════════════════════
        public static FlowDocument BuildStockReport(
            AppDbContext ctx,
            string pharmacyFilter,
            int lowStockThreshold)
        {
            var doc = CreateDocument();

            AddHeader(doc, "Отчёт об остатках на складе",
                $"Аптека: {pharmacyFilter}   |   " +
                $"Порог низкого остатка: {lowStockThreshold} ед.   |   " +
                $"Дата: {DateTime.Today:dd.MM.yyyy}");

            var stockQ = ctx.StockBalances
                .Include(s => s.Pharmacy)
                .Include(s => s.Product).ThenInclude(p => p!.Manufacturer);

            if (pharmacyFilter != "Все аптеки")
                stockQ = stockQ.Where(s => s.Pharmacy!.Address == pharmacyFilter);

            var stock = stockQ.OrderBy(s => s.Pharmacy!.Address)
                              .ThenBy(s => s.RemainingQty)
                              .ToList();

            // ── Сводка ───────────────────────────────────────────
            var zeroCount = stock.Count(s => s.RemainingQty == 0);
            var lowCount = stock.Count(s => s.RemainingQty > 0 && s.RemainingQty <= lowStockThreshold);
            var okCount = stock.Count(s => s.RemainingQty > lowStockThreshold);
            var totalUnits = stock.Sum(s => s.RemainingQty);

            AddSectionTitle(doc, "Сводка по складу");
            var summTable = CreateTable(doc, new[] { "*", "*", "*", "*" });
            AddTableRow(summTable, true,
                "Позиций всего", "Нет в наличии", $"Низкий остаток (≤{lowStockThreshold})", "В норме");
            AddTableRow(summTable, false,
                $"{stock.Count}", $"{zeroCount}", $"{lowCount}", $"{okCount}");

            // ── Критичные позиции (остаток = 0) ──────────────────
            var zeroed = stock.Where(s => s.RemainingQty == 0).ToList();
            if (zeroed.Any())
            {
                AddSectionTitle(doc, "⚠ Нет в наличии (требуется срочный заказ)");
                var zTable = CreateTable(doc, new[] { "3*", "2*", "2*", "*" });
                AddTableRow(zTable, true, "Товар", "Производитель", "Аптека", "Остаток");
                foreach (var s in zeroed)
                    AddTableRow(zTable, false,
                        s.Product?.Name ?? "?",
                        s.Product?.Manufacturer?.Name ?? "—",
                        s.Pharmacy?.Address ?? "?",
                        "0",
                        DangerBg);
            }

            // ── Низкий остаток ────────────────────────────────────
            var lowItems = stock
                .Where(s => s.RemainingQty > 0 && s.RemainingQty <= lowStockThreshold)
                .ToList();
            if (lowItems.Any())
            {
                AddSectionTitle(doc, $"⚡ Низкий остаток (1 — {lowStockThreshold} ед.)");
                var lTable = CreateTable(doc, new[] { "3*", "2*", "2*", "*" });
                AddTableRow(lTable, true, "Товар", "Производитель", "Аптека", "Остаток");
                foreach (var s in lowItems)
                    AddTableRow(lTable, false,
                        s.Product?.Name ?? "?",
                        s.Product?.Manufacturer?.Name ?? "—",
                        s.Pharmacy?.Address ?? "?",
                        $"{s.RemainingQty}",
                        WarnBg);
            }

            // ── Полный список ─────────────────────────────────────
            AddSectionTitle(doc, "Полный список остатков");
            var byPh = stock.GroupBy(s => s.Pharmacy?.Address ?? "?").ToList();

            foreach (var group in byPh)
            {
                // Подзаголовок аптеки
                var pharmPara = new Paragraph(new Run(group.Key))
                {
                    Background = SubHeaderBg,
                    Foreground = White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 6, 0, 0)
                };
                doc.Blocks.Add(pharmPara);

                var sTable = CreateTable(doc, new[] { "3*", "2*", "*", "*" });
                AddTableRow(sTable, true, "Товар", "Производитель", "Ед.", "Остаток");
                bool alt = false;
                foreach (var s in group.OrderBy(x => x.Product?.Name))
                {
                    var rowBg = s.RemainingQty == 0 ? DangerBg
                              : s.RemainingQty <= lowStockThreshold ? WarnBg
                              : alt ? AltRow : White;
                    AddTableRow(sTable, false,
                        s.Product?.Name ?? "?",
                        s.Product?.Manufacturer?.Name ?? "—",
                        s.Product?.Unit ?? "шт",
                        $"{s.RemainingQty}",
                        rowBg);
                    alt = !alt;
                }
                var totalRow = group.Sum(s => s.RemainingQty);
                AddTotalRow(sTable, "ИТОГО по аптеке", "", "", $"{totalRow}");
            }

            // Общий итог
            var grandPara = new Paragraph(
                new Run($"ИТОГО по всем аптекам: {totalUnits} единиц"))
            {
                Background = HeaderBg,
                Foreground = White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 8, 0, 0),
                TextAlignment = TextAlignment.Right
            };
            doc.Blocks.Add(grandPara);

            AddFooter(doc);
            return doc;
        }

        // ══════════════════════════════════════════════════════════
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ══════════════════════════════════════════════════════════

        private static FlowDocument CreateDocument()
        {
            return new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                Foreground = TextDark,
                PagePadding = new Thickness(40, 30, 40, 30),
                ColumnWidth = double.PositiveInfinity,
                PageWidth = 1100
            };
        }

        private static void AddHeader(FlowDocument doc, string title, string subtitle)
        {
            // Верхняя полоса
            var headerBorder = new BlockUIContainer(new System.Windows.Controls.Border
            {
                Background = HeaderBg,
                Padding = new Thickness(16, 12, 16, 12),
                Child = new System.Windows.Controls.StackPanel
                {
                    Children =
                    {
                        new System.Windows.Controls.TextBlock
                        {
                            Text       = "HIPPOCRATES — Аптечная сеть",
                            Foreground = new SolidColorBrush(Color.FromRgb(165, 214, 167)),
                            FontSize   = 10,
                            FontFamily = new FontFamily("Segoe UI")
                        },
                        new System.Windows.Controls.TextBlock
                        {
                            Text       = title,
                            Foreground = Brushes.White,
                            FontSize   = 20,
                            FontWeight = FontWeights.Bold,
                            FontFamily = new FontFamily("Segoe UI"),
                            Margin     = new Thickness(0, 4, 0, 0)
                        },
                        new System.Windows.Controls.TextBlock
                        {
                            Text       = subtitle,
                            Foreground = new SolidColorBrush(Color.FromRgb(165, 214, 167)),
                            FontSize   = 10,
                            FontFamily = new FontFamily("Segoe UI"),
                            Margin     = new Thickness(0, 6, 0, 0),
                            TextWrapping = System.Windows.TextWrapping.Wrap
                        },
                        new System.Windows.Controls.TextBlock
                        {
                            Text       = $"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}",
                            Foreground = new SolidColorBrush(Color.FromRgb(165, 214, 167)),
                            FontSize   = 9,
                            FontFamily = new FontFamily("Segoe UI"),
                            Margin     = new Thickness(0, 4, 0, 0)
                        }
                    }
                }
            });
            doc.Blocks.Add(headerBorder);
            doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 8, 0, 0) });
        }

        private static void AddSectionTitle(FlowDocument doc, string text)
        {
            doc.Blocks.Add(new Paragraph(new Run(text))
            {
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32)),
                Margin = new Thickness(0, 14, 0, 4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 0, 0, 3)
            });
        }

        private static Table CreateTable(FlowDocument doc, string[] columnWidths)
        {
            var table = new Table
            {
                CellSpacing = 0,
                BorderBrush = BorderBrush,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 4)
            };

            foreach (var w in columnWidths)
            {
                var col = new TableColumn();
                if (w == "*")
                    col.Width = new GridLength(1, GridUnitType.Star);
                else if (w.EndsWith("*") && double.TryParse(w[..^1], out double d))
                    col.Width = new GridLength(d, GridUnitType.Star);
                else if (double.TryParse(w, out double px))
                    col.Width = new GridLength(px);
                table.Columns.Add(col);
            }

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);
            doc.Blocks.Add(table);
            return table;
        }

        private static void AddTableRow(Table table, bool isHeader, params string[] cells)
            => AddTableRow(table, isHeader, cells, null);

        private static void AddTableRow(Table table, bool isHeader,
            string[] cells, SolidColorBrush? bg)
        {
            var row = new TableRow
            {
                Background = isHeader
                    ? TableHead
                    : (bg ?? White)
            };

            foreach (var cell in cells)
            {
                var para = new Paragraph(new Run(cell ?? ""))
                {
                    Margin = new Thickness(6, 3, 6, 3),
                    FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal,
                    FontSize = isHeader ? 11 : 11
                };
                if (isHeader)
                    para.Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32));

                var tc = new TableCell(para)
                {
                    BorderBrush = BorderBrush,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                };
                row.Cells.Add(tc);
            }

            table.RowGroups[0].Rows.Add(row);
        }

        private static void AddTableRow(Table table, bool isHeader,
            string c1, string c2, string c3 = "", string c4 = "",
            string c5 = "", string c6 = "",
            SolidColorBrush? bg = null)
        {
            var cells = new List<string> { c1, c2 };
            if (table.Columns.Count > 2) cells.Add(c3);
            if (table.Columns.Count > 3) cells.Add(c4);
            if (table.Columns.Count > 4) cells.Add(c5);
            if (table.Columns.Count > 5) cells.Add(c6);
            AddTableRow(table, isHeader, cells.ToArray(), bg);
        }

        private static void AddTotalRow(Table table, params string[] cells)
        {
            var row = new TableRow
            {
                Background = new SolidColorBrush(Color.FromRgb(200, 230, 201))
            };

            for (int i = 0; i < table.Columns.Count; i++)
            {
                var text = i < cells.Length ? cells[i] : "";
                var para = new Paragraph(new Run(text))
                {
                    Margin = new Thickness(6, 3, 6, 3),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11
                };
                row.Cells.Add(new TableCell(para)
                {
                    BorderBrush = BorderBrush,
                    BorderThickness = new Thickness(0, 1, 1, 1)
                });
            }

            table.RowGroups[0].Rows.Add(row);
        }

        private static void AddFooter(FlowDocument doc)
        {
            doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 20, 0, 0) });
            doc.Blocks.Add(new Paragraph(
                new Run($"© {DateTime.Now.Year} Hippocrates — Система управления аптечной сетью   |   " +
                        $"Документ сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}"))
            {
                Foreground = TextGray,
                FontSize = 9,
                TextAlignment = TextAlignment.Center,
                BorderBrush = BorderBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(0, 6, 0, 0)
            });
        }
    }
}