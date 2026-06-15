// 同时启用 WPF 与 WinForms 会导致 Application/Color/Rectangle 等类型重名。
// 这里统一把这些名字锁定到 WPF 版本（WinForms 仅用于托盘，全部以 Forms. 限定）。
global using Application = System.Windows.Application;
global using Color = System.Windows.Media.Color;
global using Rectangle = System.Windows.Shapes.Rectangle;
global using Brushes = System.Windows.Media.Brushes;
// 用独立别名，避免与 TextBlock 的同名属性互相遮蔽（CS0176）
global using HAlign = System.Windows.HorizontalAlignment;
global using VAlign = System.Windows.VerticalAlignment;
